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
}
