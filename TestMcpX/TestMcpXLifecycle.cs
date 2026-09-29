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

    [TestMethod]
    public void TestDisposeReleasesTransportWhenLockFails()
    {
        var transport = new FakePlcTransport();
        transport.EndCodes[REMOTE_LOCK] = 0xC201;
        var mcpx = new McpX(transport, password: "1234");

        // リモートロックが失敗しても例外にならず、接続は解放されること
        mcpx.Dispose();

        Assert.IsTrue(transport.Disposed);
    }

    [TestMethod]
    public void TestDisposeTwiceIsNoOp()
    {
        var transport = new FakePlcTransport();
        var mcpx = new McpX(transport, password: "1234");

        mcpx.Dispose();
        mcpx.Dispose();

        // リモートロックは1回だけ送ること
        Assert.AreEqual(1, transport.Requests.Count(r => r.Command == REMOTE_LOCK));
    }

    private const ushort MONITOR_REGIST = 0x0801;
    private const ushort MONITOR = 0x0802;

    [TestMethod]
    public async Task TestStaleMonitorSessionThrows()
    {
        var transport = new FakePlcTransport();
        using var mcpx = new McpX(transport);

        short a = -1, b = -1;
        var s1 = mcpx.MonitorRegist(x => x.Add<short>(Prefix.D, "100", v => a = v));
        s1.Read();
        s1.Read();  // 登録が変わらなければ繰り返し読めること
        Assert.AreEqual((short)0, a);

        var s2 = await mcpx.MonitorRegistAsync(x => x.Add<short>(Prefix.D, "200", v => b = v));

        // 登録が置き換わった後の古いセッションは例外になり、通信もしないこと
        int monitors = transport.Requests.Count(r => r.Command == MONITOR);
        Assert.ThrowsException<InvalidOperationException>(() => s1.Read());
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => s1.ReadAsync());
        Assert.AreEqual(monitors, transport.Requests.Count(r => r.Command == MONITOR));

        // 新しいセッションは読めること
        await s2.ReadAsync();
        Assert.AreEqual((short)0, b);
    }

    [TestMethod]
    public void TestLegacyMonitorRegistInvalidatesSession()
    {
        var transport = new FakePlcTransport();
        using var mcpx = new McpX(transport);

        var session = mcpx.MonitorRegist(x => x.Add<short>(Prefix.D, "100", _ => { }));
        mcpx.MonitorRegist([(Prefix.D, "0"), (Prefix.D, "1")], []);

        Assert.ThrowsException<InvalidOperationException>(() => session.Read());
    }

    [TestMethod]
    public void TestFailedMonitorRegistInvalidatesSession()
    {
        var transport = new FakePlcTransport();
        using var mcpx = new McpX(transport);

        var session = mcpx.MonitorRegist(x => x.Add<short>(Prefix.D, "100", _ => { }));

        // 送信後に失敗した登録でも、PLC の登録が置き換わった可能性があるため古いセッションは無効にする
        transport.EndCodes[MONITOR_REGIST] = 0xC059;
        Assert.ThrowsException<McProtocolException>(() => mcpx.MonitorRegist(x => x.Add<short>(Prefix.D, "200", _ => { })));

        Assert.ThrowsException<InvalidOperationException>(() => session.Read());
    }

    [TestMethod]
    public void TestInvalidMonitorRegistKeepsSession()
    {
        var transport = new FakePlcTransport();
        using var mcpx = new McpX(transport);

        var session = mcpx.MonitorRegist(x => x.Add<short>(Prefix.D, "100", _ => { }));

        // 点数0など送信前の検証エラーでは PLC の登録は変わらないため、既存のセッションは使えること
        Assert.ThrowsException<ArgumentException>(() => mcpx.MonitorRegist(_ => { }));

        session.Read();
    }

    [TestMethod]
    public void TestReconnectInvalidatesSession()
    {
        using var mcpx = new McpX(() => new FakePlcTransport());

        var session = mcpx.MonitorRegist(x => x.Add<short>(Prefix.D, "100", _ => { }));

        // 接続し直すと PLC のモニタ登録は引き継がれないため、既存のセッションは無効になること
        mcpx.Reconnect();

        Assert.ThrowsException<InvalidOperationException>(() => session.Read());
    }
}
