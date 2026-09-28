using McpXLib;
using McpXLib.Enums;
using McpXLib.Exceptions;

namespace TestMcpX;

/// <summary>
/// McpX の生成・破棄（リモートパスワード）とモニタセッションのテスト。
/// </summary>
[TestClass]
public sealed class TestMcpXLifecycle
{
    private const ushort REMOTE_UNLOCK = 0x1630;
    private const ushort REMOTE_LOCK = 0x1631;

    [TestMethod]
    public void TestConstructorReleasesTransportWhenUnlockFails()
    {
        var transport = new FakePlcTransport();
        transport.EndCodes[REMOTE_UNLOCK] = 0xC201; // パスワード不一致など

        Assert.ThrowsException<McProtocolException>(() => new McpX(transport, password: "1234"));

        // インスタンスが返らず Dispose できないため、コンストラクタ内で接続が閉じられていること
        Assert.IsTrue(transport.Disposed);
        // リモートロックは送らないこと
        Assert.IsFalse(transport.Requests.Any(r => r.Command == REMOTE_LOCK));
    }

    [TestMethod]
    public void TestConstructorUnlocksWithPassword()
    {
        var transport = new FakePlcTransport();

        using var mcpx = new McpX(transport, password: "1234");

        Assert.AreEqual(REMOTE_UNLOCK, transport.Requests.Single().Command);
        Assert.IsFalse(transport.Disposed);
    }
}
