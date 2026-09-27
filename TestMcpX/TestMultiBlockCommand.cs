using System.Text;
using McpXLib.Builders;
using McpXLib.Commands;
using McpXLib.Enums;
using McpXLib.Interfaces;
using Moq;

namespace TestMcpX;

/// <summary>
/// 複数ブロック一括読出し・書込み（0406 / 1406）の電文テスト。
/// </summary>
/// <remarks>
/// 期待値は MELSECコミュニケーションプロトコルリファレンスマニュアル（SH-080003）8.4 の交信例
/// （ワード: D0〜D3, W100〜W107、ビット: M0〜M31, M128〜M159, B100〜B12F）。
/// <c>ToBinaryBytes</c> / <c>ToAsciiBytes</c> の先頭（要求データ長・監視タイマ）は除いて比較する。
/// </remarks>
[TestClass]
public sealed class TestMultiBlockCommand
{
    private static DeviceBlock[] ManualBlocks(bool withData = false) =>
    [
        new(Prefix.D, "0", 4, withData ? [0x0008, 0x2030, 0x1545, 0x2800] : null),
        new(Prefix.M, "0", 2, withData ? [0x2030, 0x4849] : null),              // ビットブロックは指定順が前でも後ろに並ぶ
        new(Prefix.W, "100", 8, withData ? Enumerable.Range(0, 8).Select(i => (ushort)(0x0970 + i)).ToArray() : null),
        new(Prefix.M, "128", 2, withData ? [0xC3DE, 0x2800] : null),
        new(Prefix.B, "100", 3, withData ? [0xB9AF, 0xB9AF, 0xB9AF] : null),
    ];

    private static byte[] Binary(IPacketBuilder command) => command.ToBinaryBytes().Skip(4).ToArray();

    private static string Ascii(IPacketBuilder command) => Encoding.ASCII.GetString(command.ToAsciiBytes().Skip(8).ToArray());

    private static Mock<IPlc> CreatePlc(bool isAscii)
    {
        var plc = new Mock<IPlc>();
        plc.SetupProperty(x => x.Route);
        plc.SetupProperty(x => x.IsAscii);
        plc.Object.Route = new RoutePacketBuilder();
        plc.Object.IsAscii = isAscii;
        return plc;
    }

    // ---------------------------------------------------------------
    // 複数ブロック一括読出し（0406）: 紙面110〜113ページ
    // ---------------------------------------------------------------

    [TestMethod]
    public void TestReadToBytes()
    {
        var command = new MultiBlockReadCommand(ManualBlocks());

        byte[] expected = [
            0x06, 0x04, 0x00, 0x00,                 // Command + SubCommand
            0x02, 0x03,                             // Word Blocks, Bit Blocks
            0x00, 0x00, 0x00, 0xA8, 0x04, 0x00,     // D0 4点
            0x00, 0x01, 0x00, 0xB4, 0x08, 0x00,     // W100 8点
            0x00, 0x00, 0x00, 0x90, 0x02, 0x00,     // M0 2点
            0x80, 0x00, 0x00, 0x90, 0x02, 0x00,     // M128 2点
            0x00, 0x01, 0x00, 0xA0, 0x03, 0x00,     // B100 3点
        ];

        CollectionAssert.AreEqual(expected, Binary(command));
        Assert.AreEqual(
            "04060000" + "02" + "03" +
            "D*000000" + "0004" + "W*000100" + "0008" +
            "M*000000" + "0002" + "M*000128" + "0002" + "B*000100" + "0003",
            Ascii(command));
    }

    [TestMethod]
    public void TestReadToBytesForiQR()
    {
        var command = new MultiBlockReadCommand([new(Prefix.D, "0", 4), new(Prefix.M, "0", 2)], series: ProcessorSeries.iQR);

        byte[] expected = [
            0x06, 0x04, 0x02, 0x00,                             // Command + SubCommand (iQ-R = 0002)
            0x01, 0x01,
            0x00, 0x00, 0x00, 0x00, 0xA8, 0x00, 0x04, 0x00,     // D0 (4バイト + 2バイト)
            0x00, 0x00, 0x00, 0x00, 0x90, 0x00, 0x02, 0x00,     // M0
        ];

        CollectionAssert.AreEqual(expected, Binary(command));
        Assert.AreEqual("04060002" + "01" + "01" + "D***00000000" + "0004" + "M***00000000" + "0002", Ascii(command));
    }

    [TestMethod]
    public async Task TestReadResponse()
    {
        // 応答はワードブロック（D0〜D3, W100〜W107）→ビットブロック（M0, M128, B100）の順。結果は指定順に並べ直す。
        ushort[] response = [
            0x0008, 0x2030, 0x1545, 0x2800,
            0x0970, 0x0971, 0x0972, 0x0973, 0x0974, 0x0975, 0x0976, 0x0977,
            0x2030, 0x4849,
            0xC3DE, 0x2800,
            0xB9AF, 0xB9AF, 0xB9AF,
        ];

        var binaryPlc = CreatePlc(false);
        binaryPlc.Setup(x => x.RequestAsync(It.IsAny<byte[]>(), It.IsAny<IReceiveLengthParser>())).ReturnsAsync(
            [0xD0, 0x00, 0x00, 0xFF, 0xFF, 0x03, 0x00, .. BitConverter.GetBytes((ushort)(response.Length * 2 + 2)), 0x00, 0x00, .. response.SelectMany(BitConverter.GetBytes)]);
        AssertBlocks(await new MultiBlockReadCommand(ManualBlocks()).ExecuteAsync(binaryPlc.Object));

        var asciiContent = string.Concat(response.Select(w => w.ToString("X4")));
        var asciiPlc = CreatePlc(true);
        asciiPlc.Setup(x => x.Request(It.IsAny<byte[]>(), It.IsAny<IReceiveLengthParser>())).Returns(
            Encoding.ASCII.GetBytes("D00000FF03FF00" + (asciiContent.Length + 4).ToString("X4") + "0000" + asciiContent));
        AssertBlocks(new MultiBlockReadCommand(ManualBlocks()).Execute(asciiPlc.Object));

        static void AssertBlocks(ushort[][] blocks)
        {
            Assert.AreEqual(5, blocks.Length);
            CollectionAssert.AreEqual(new ushort[] { 0x0008, 0x2030, 0x1545, 0x2800 }, blocks[0]);   // D0
            CollectionAssert.AreEqual(new ushort[] { 0x2030, 0x4849 }, blocks[1]);                   // M0
            Assert.AreEqual((ushort)0x0977, blocks[2][7]);                                             // W107
            CollectionAssert.AreEqual(new ushort[] { 0xC3DE, 0x2800 }, blocks[3]);                   // M128
            CollectionAssert.AreEqual(new ushort[] { 0xB9AF, 0xB9AF, 0xB9AF }, blocks[4]);           // B100
        }
    }

    [TestMethod]
    public void TestReadLimits()
    {
        // ブロック数: Q/L 120、iQ-R 60
        _ = new MultiBlockReadCommand(Enumerable.Range(0, 120).Select(i => new DeviceBlock(Prefix.D, i.ToString(), 1)).ToArray());
        Assert.ThrowsException<ArgumentException>(() => new MultiBlockReadCommand(Enumerable.Range(0, 121).Select(i => new DeviceBlock(Prefix.D, i.ToString(), 1)).ToArray()));
        Assert.ThrowsException<ArgumentException>(() => new MultiBlockReadCommand(Enumerable.Range(0, 61).Select(i => new DeviceBlock(Prefix.D, i.ToString(), 1)).ToArray(), series: ProcessorSeries.iQR));
        Assert.ThrowsException<ArgumentException>(() => new MultiBlockReadCommand([]));

        // 点数の合計: 960
        _ = new MultiBlockReadCommand([new(Prefix.D, "0", 900), new(Prefix.M, "0", 60)]);
        Assert.ThrowsException<ArgumentException>(() => new MultiBlockReadCommand([new(Prefix.D, "0", 900), new(Prefix.M, "0", 61)]));
    }

    // ---------------------------------------------------------------
    // 複数ブロック一括書込み（1406）: 紙面114〜117ページ
    // ---------------------------------------------------------------

    [TestMethod]
    public void TestWriteToBytes()
    {
        var command = new MultiBlockWriteCommand([
            new(Prefix.M, "0", 1, [0x2030]),
            new(Prefix.D, "0", 2, [0x0008, 0x2800]),
            new(Prefix.W, "100", 1, [0x0970]),
        ]);

        byte[] expected = [
            0x06, 0x14, 0x00, 0x00,                                     // Command + SubCommand
            0x02, 0x01,                                                 // Word Blocks, Bit Blocks
            0x00, 0x00, 0x00, 0xA8, 0x02, 0x00, 0x08, 0x00, 0x00, 0x28, // D0 2点 + データ
            0x00, 0x01, 0x00, 0xB4, 0x01, 0x00, 0x70, 0x09,             // W100 1点 + データ
            0x00, 0x00, 0x00, 0x90, 0x01, 0x00, 0x30, 0x20,             // M0 1点（16ビット） + データ
        ];

        CollectionAssert.AreEqual(expected, Binary(command));
        Assert.AreEqual(
            "14060000" + "02" + "01" +
            "D*000000" + "0002" + "0008" + "2800" +
            "W*000100" + "0001" + "0970" +
            "M*000000" + "0001" + "2030",
            Ascii(command));
    }

    [TestMethod]
    public void TestWriteLimits()
    {
        // (ブロック数×4)+点数 ≦ 960（Q/L）
        _ = new MultiBlockWriteCommand([new(Prefix.D, "0", 956, new ushort[956])]);                                        // 4 + 956 = 960
        _ = new MultiBlockWriteCommand([new(Prefix.D, "0", 948, new ushort[948]), new(Prefix.D, "1000", 4, new ushort[4])]); // 8 + 952 = 960
        Assert.ThrowsException<ArgumentException>(() => new MultiBlockWriteCommand([new(Prefix.D, "0", 948, new ushort[948]), new(Prefix.D, "1000", 5, new ushort[5])]));

        // (ブロック数×9)+点数 ≦ 960（iQ-R）
        _ = new MultiBlockWriteCommand([new(Prefix.D, "0", 951, new ushort[951])], series: ProcessorSeries.iQR);           // 9 + 951 = 960
        Assert.ThrowsException<ArgumentException>(() => new MultiBlockWriteCommand([new(Prefix.D, "0", 952, new ushort[952])], series: ProcessorSeries.iQR));

        // データ数が点数と一致しない
        Assert.ThrowsException<ArgumentException>(() => new MultiBlockWriteCommand([new(Prefix.D, "0", 2, [0x0001])]));
    }

    [TestMethod]
    public void TestWriteExecute()
    {
        var plc = CreatePlc(false);
        plc.Setup(x => x.Request(It.IsAny<byte[]>(), It.IsAny<IReceiveLengthParser>())).Returns([0xD0, 0x00, 0x00, 0xFF, 0xFF, 0x03, 0x00, 0x02, 0x00, 0x00, 0x00]);

        Assert.IsTrue(new MultiBlockWriteCommand([new(Prefix.D, "0", 1, [0x0001])]).Execute(plc.Object));
    }
}
