using McpXLib;
using McpXLib.Builders;
using McpXLib.Commands;
using McpXLib.Enums;
using McpXLib.Exceptions;
using McpXLib.Interfaces;
using Moq;

namespace TestMcpX;

/// <summary>
/// 壊れた応答が ReceivePacketException になること（ドキュメントにない例外を漏らさないこと）のテスト。
/// </summary>
[TestClass]
public sealed class TestMalformedResponse
{
    private static Mock<IPlc> CreatePlc(bool isAscii, byte[] response)
    {
        var plc = new Mock<IPlc>();
        plc.SetupGet(x => x.Route).Returns(new RoutePacketBuilder());
        plc.SetupGet(x => x.IsAscii).Returns(isAscii);
        plc.SetupGet(x => x.RequestFrame).Returns(RequestFrame.E3);
        plc.Setup(x => x.Request(It.IsAny<byte[]>(), It.IsAny<IReceiveLengthParser>())).Returns(response);
        plc.Setup(x => x.RequestAsync(It.IsAny<byte[]>(), It.IsAny<IReceiveLengthParser>(), It.IsAny<CancellationToken>())).ReturnsAsync(response);
        return plc;
    }

    [TestMethod]
    public async Task TestAsciiNonHexContent()
    {
        byte[] response = [
            0x44, 0x30, 0x30, 0x30,                                     // Sub Header
            0x30, 0x30, 0x46, 0x46, 0x30, 0x33, 0x46, 0x46, 0x30, 0x30, // Route
            0x30, 0x30, 0x30, 0x38,                                     // Content Length
            0x30, 0x30, 0x30, 0x30,                                     // Error Code
            0x5A, 0x5A, 0x5A, 0x5A,                                     // Value1（"ZZZZ"：16進以外）
        ];
        var plc = CreatePlc(isAscii: true, response);
        var command = new WordBatchReadCommand<short>(Prefix.D, "0", 1);

        var ex = Assert.ThrowsException<ReceivePacketException>(() => command.Execute(plc.Object));
        Assert.IsNotNull(ex.InnerException);
        await Assert.ThrowsExceptionAsync<ReceivePacketException>(() => command.ExecuteAsync(plc.Object, CancellationToken.None));
    }

    [TestMethod]
    public void TestBinaryShortContent()
    {
        byte[] response = [
            0xD0, 0x00,                         // Sub Header
            0x00, 0xFF, 0xFF, 0x03, 0x00,       // Route
            0x04, 0x00,                         // Content Length（終了コード2 + データ2）
            0x00, 0x00,                         // Error Code
            0x01, 0x00,                         // 1ワード分のデータしかない
        ];
        var plc = CreatePlc(isAscii: false, response);

        // int（2ワード）を1点読む要求に、1ワードの応答
        var command = new WordBatchReadCommand<int>(Prefix.D, "0", 1);

        Assert.ThrowsException<ReceivePacketException>(() => command.Execute(plc.Object));
    }

    [TestMethod]
    public void TestErrorCodeIsNotWrapped()
    {
        byte[] response = [
            0xD0, 0x00,                         // Sub Header
            0x00, 0xFF, 0xFF, 0x03, 0x00,       // Route
            0x02, 0x00,                         // Content Length
            0x59, 0xC0,                         // Error Code（C059）
        ];
        var plc = CreatePlc(isAscii: false, response);
        var command = new WordBatchReadCommand<short>(Prefix.D, "0", 1);

        // PLC のエラーコードは McProtocolException のまま（ReceivePacketException に包まない）
        var ex = Assert.ThrowsException<McProtocolException>(() => command.Execute(plc.Object));
        Assert.AreEqual((ushort)0xC059, ex.ErrorCode);
        Assert.IsInstanceOfType<McpXException>(ex);
    }

    // 3E フレーム（バイナリ）の正常応答（終了コード 0000）に、応答データ data を付ける
    private static byte[] BinaryResponse(params byte[] data)
    {
        var header = new byte[] { 0xD0, 0x00, 0x00, 0xFF, 0xFF, 0x03, 0x00 };
        var length = BitConverter.GetBytes((ushort)(data.Length + 2));
        return [.. header, .. length, 0x00, 0x00, .. data];
    }

    [TestMethod]
    public async Task TestShortWordBatchResponse()
    {
        // 応答データなし（ヘッダの長さとは一致する正しい形式）で、1点の読み込み
        var empty = CreatePlc(isAscii: false, BinaryResponse());
        Assert.ThrowsException<ReceivePacketException>(() => new WordBatchReadCommand<short>(Prefix.D, "0", 1).Execute(empty.Object));
        await Assert.ThrowsExceptionAsync<ReceivePacketException>(() => new WordBatchReadCommand<short>(Prefix.D, "0", 1).ExecuteAsync(empty.Object, CancellationToken.None));

        // 10点の読み込みに、2点分の応答データ
        var shortData = CreatePlc(isAscii: false, BinaryResponse(0x01, 0x00, 0x02, 0x00));
        Assert.ThrowsException<ReceivePacketException>(() => new WordBatchReadCommand<short>(Prefix.D, "0", 10).Execute(shortData.Object));
    }

    [TestMethod]
    public void TestShortBitBatchResponse()
    {
        // 4点（2バイト）の読み込みに、2点（1バイト）の応答データ
        var plc = CreatePlc(isAscii: false, BinaryResponse(0x11));
        Assert.ThrowsException<ReceivePacketException>(() => new BitBatchReadCommand(Prefix.M, "0", 4).Execute(plc.Object));

        // 3点（奇数）の読み込みは、2バイト（4点分）の応答で正常
        var odd = CreatePlc(isAscii: false, BinaryResponse(0x10, 0x10));
        CollectionAssert.AreEqual(new[] { true, false, true }, new BitBatchReadCommand(Prefix.M, "0", 3).Execute(odd.Object));
    }

    [TestMethod]
    public void TestShortRandomAndMonitorResponse()
    {
        (Prefix, string)[] words = [(Prefix.D, "0"), (Prefix.D, "1")];
        (Prefix, string)[] doubleWords = [(Prefix.D, "10")];

        // ワード2点 + ダブルワード1点（8バイト）の読み込みに、ワード1点分（2バイト）の応答データ
        var plc = CreatePlc(isAscii: false, BinaryResponse(0x01, 0x00));
        Assert.ThrowsException<ReceivePacketException>(() => new WordRandomReadCommand<ushort, uint>(words, doubleWords).Execute(plc.Object));
        Assert.ThrowsException<ReceivePacketException>(() => new MonitorCommand<ushort, uint>(words, doubleWords).Execute(plc.Object));
    }

    [TestMethod]
    public async Task TestShortResponseThroughMcpX()
    {
        // 終了コードだけの応答（応答データなし）
        using var mcpx = new McpX(new FixedResponseTransport(BinaryResponse()));

        // IndexOutOfRangeException や、0 で埋めた配列ではなく ReceivePacketException
        Assert.ThrowsException<ReceivePacketException>(() => mcpx.Read<short>(Prefix.D, "0"));
        Assert.ThrowsException<ReceivePacketException>(() => mcpx.BatchRead<short>(Prefix.D, "0", 10));
        await Assert.ThrowsExceptionAsync<ReceivePacketException>(() => mcpx.RandomReadAsync(b => b.Add<short>(Prefix.D, "0", _ => { })));
    }

    // 常に同じ応答を返すトランスポート
    private sealed class FixedResponseTransport(byte[] response) : IPlcTransport
    {
        public byte[] Request(byte[] packet, IReceiveLengthParser receiveLengthParser) => response;

        public Task<byte[]> RequestAsync(byte[] packet, IReceiveLengthParser receiveLengthParser, CancellationToken cancellationToken)
            => Task.FromResult(response);

        public void Dispose()
        {
        }
    }
}
