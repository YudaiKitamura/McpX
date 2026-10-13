using System.Text;
using McpXLib;
using McpXLib.Enums;
using McpXLib.Exceptions;
using McpXLib.Interfaces;
using McpXLib.Utils;

namespace TestMcpX;

/// <summary>
/// 残課題（E3・E4・E5・E9、ASCII のビット点数上限）の修正のテスト。
/// </summary>
[TestClass]
public sealed class TestKnownIssueFixes
{
    [TestMethod]
    public void TestQAddressOutOfRange()
    {
        // Q/L のバイナリはアドレス3バイト（FFFFFF）まで。iQ-R は4バイト
        CollectionAssert.AreEqual(new byte[] { 0xFF, 0xFF, 0xFF, (byte)Prefix.R }, DeviceConverter.ToByteAddress(Prefix.R, "16777215"));
        Assert.ThrowsException<DeviceAddressException>(() => DeviceConverter.ToByteAddress(Prefix.R, "16777216"));
        Assert.AreEqual(6, DeviceConverter.ToByteAddress(Prefix.R, "16777216", ProcessorSeries.iQR).Length);

        // ASCII のデバイス番号は Q/L が6桁、iQ-R が8桁まで（先頭の 0 は数えない）
        Assert.AreEqual("D*000100", DeviceConverter.ToASCIIAddress(Prefix.D, "0000100"));
        Assert.ThrowsException<DeviceAddressException>(() => DeviceConverter.ToASCIIAddress(Prefix.D, "1000000"));
        Assert.AreEqual("D***01000000", DeviceConverter.ToASCIIAddress(Prefix.D, "1000000", ProcessorSeries.iQR));
        Assert.ThrowsException<DeviceAddressException>(() => DeviceConverter.ToASCIIAddress(Prefix.D, "100000000", ProcessorSeries.iQR));
    }

    [TestMethod]
    public async Task TestBoolOnWordDevice()
    {
        var transport = new FakePlcTransport();
        using var mcpx = new McpX(transport);

        // ワードデバイスへの bool は送信前に ArgumentException（PLC は C05C を返すため）
        Assert.ThrowsException<ArgumentException>(() => mcpx.Read<bool>(Prefix.D, "0"));
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => mcpx.ReadAsync<bool>(Prefix.D, "0"));
        Assert.ThrowsException<ArgumentException>(() => mcpx.Write(Prefix.D, "0", true));
        Assert.ThrowsException<ArgumentException>(() => mcpx.BatchRead<bool>(Prefix.W, "0", 16));
        Assert.ThrowsException<ArgumentException>(() => mcpx.BatchWrite(Prefix.R, "0", new[] { true }));
        Assert.ThrowsException<ArgumentException>(() => mcpx.RandomWrite(b => b.Add(Prefix.D, "0", true)));
        Assert.AreEqual(0, transport.Requests.Count);

        // ビットデバイスへの bool は送信する
        mcpx.Write(Prefix.M, "0", true);
        mcpx.Read<bool>(Prefix.M, "0");
        Assert.AreEqual(2, transport.Requests.Count);
    }

    [TestMethod]
    public async Task TestNullArguments()
    {
        var transport = new FakePlcTransport();
        using var mcpx = new McpX(transport);

        Assert.ThrowsException<ArgumentNullException>(() => mcpx.Read(null!));
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => mcpx.WriteAsync(null!));
        Assert.ThrowsException<ArgumentNullException>(() => mcpx.RandomRead(b => b.Add<short>(Prefix.D, "0", null!)));
        Assert.ThrowsException<ArgumentNullException>(() => mcpx.MonitorRegister(b => b.Add<short>(Prefix.D, "0", null!)));
        Assert.ThrowsException<ArgumentNullException>(() => mcpx.Read(b => b.Add<short>(Prefix.D, "0", 10, null!)));
        Assert.ThrowsException<ArgumentNullException>(() => mcpx.Write(b => b.Add<short>(Prefix.D, "0", (short[])null!)));
        Assert.ThrowsException<ArgumentNullException>(() => mcpx.BlockWrite(b => b.Add<short>(Prefix.D, "0", null!)));

        // アドレスの null は DeviceAddressException
        Assert.ThrowsException<DeviceAddressException>(() => mcpx.Read<short>(Prefix.D, null!));
        Assert.AreEqual(0, transport.Requests.Count);
    }

    [TestMethod]
    public void TestAsciiBitBatchLimit()
    {
        // ASCII 交信のビット単位の一括書込みは 3584点ごとに分割する（バイナリは 7168点）
        var ascii = new AsciiWriteTransport();
        using (var mcpx = new McpX(ascii, isAscii: true))
        {
            mcpx.BatchWrite(Prefix.M, "0", new bool[4000]);
        }
        Assert.AreEqual(2, ascii.Requests);

        var binary = new FakePlcTransport();
        using (var mcpx = new McpX(binary))
        {
            mcpx.BatchWrite(Prefix.M, "0", new bool[4000]);
        }
        Assert.AreEqual(1, binary.Requests.Count);
    }

    // ASCII の書込み要求に、正常終了（応答データなし）の応答を返すトランスポート
    private sealed class AsciiWriteTransport : IPlcTransport
    {
        internal int Requests { get; private set; }

        public byte[] Request(byte[] packet, IReceiveLengthParser receiveLengthParser)
        {
            Requests++;
            // サブヘッダ + 経路 + 応答データ長（4: 終了コードのみ）+ 終了コード
            return Encoding.ASCII.GetBytes("D000" + "00FF03FF00" + "0004" + "0000");
        }

        public Task<byte[]> RequestAsync(byte[] packet, IReceiveLengthParser receiveLengthParser, CancellationToken cancellationToken)
            => Task.FromResult(Request(packet, receiveLengthParser));

        public void Dispose()
        {
        }
    }
}
