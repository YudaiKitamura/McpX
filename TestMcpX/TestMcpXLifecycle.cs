using McpXLib;
using McpXLib.Enums;
using McpXLib.Exceptions;
using McpXLib.Interfaces;

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

    private const ushort MONITOR_REGISTER = 0x0801;
    private const ushort MONITOR = 0x0802;

    [TestMethod]
    public async Task TestStaleMonitorSessionThrows()
    {
        var transport = new FakePlcTransport();
        using var mcpx = new McpX(transport);

        short a = -1, b = -1;
        var s1 = mcpx.MonitorRegister(x => x.Add<short>(Prefix.D, "100", v => a = v));
        s1.Read();
        s1.Read();  // 登録が変わらなければ繰り返し読めること
        Assert.AreEqual((short)0, a);

        var s2 = await mcpx.MonitorRegisterAsync(x => x.Add<short>(Prefix.D, "200", v => b = v));

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
    public void TestFailedMonitorRegisterInvalidatesSession()
    {
        var transport = new FakePlcTransport();
        using var mcpx = new McpX(transport);

        var session = mcpx.MonitorRegister(x => x.Add<short>(Prefix.D, "100", _ => { }));

        // 送信後に失敗した登録でも、PLC の登録が置き換わった可能性があるため古いセッションは無効にする
        transport.EndCodes[MONITOR_REGISTER] = 0xC059;
        Assert.ThrowsException<McProtocolException>(() => mcpx.MonitorRegister(x => x.Add<short>(Prefix.D, "200", _ => { })));

        Assert.ThrowsException<InvalidOperationException>(() => session.Read());
    }

    [TestMethod]
    public void TestInvalidMonitorRegisterKeepsSession()
    {
        var transport = new FakePlcTransport();
        using var mcpx = new McpX(transport);

        var session = mcpx.MonitorRegister(x => x.Add<short>(Prefix.D, "100", _ => { }));

        // 点数0など送信前の検証エラーでは PLC の登録は変わらないため、既存のセッションは使えること
        Assert.ThrowsException<ArgumentException>(() => mcpx.MonitorRegister(_ => { }));

        session.Read();
    }

    [TestMethod]
    public void TestReconnectInvalidatesSession()
    {
        using var mcpx = new McpX(() => new FakePlcTransport());

        var session = mcpx.MonitorRegister(x => x.Add<short>(Prefix.D, "100", _ => { }));

        // 接続し直すと PLC のモニタ登録は引き継がれないため、既存のセッションは無効になること
        mcpx.Reconnect();

        Assert.ThrowsException<InvalidOperationException>(() => session.Read());
    }

    [TestMethod]
    public void TestReconnectAfterDisposeDoesNotLeakTransport()
    {
        var created = new List<FakePlcTransport>();
        var mcpx = new McpX(() =>
        {
            var transport = new FakePlcTransport();
            created.Add(transport);
            return transport;
        });

        mcpx.Dispose();

        // RemoteReset の再接続中に Dispose された場合を想定：再接続は失敗し、新しい接続も閉じられること
        Assert.ThrowsException<ObjectDisposedException>(() => mcpx.Reconnect());
        Assert.AreEqual(2, created.Count);
        Assert.IsTrue(created.All(t => t.Disposed));
    }

    [TestMethod]
    public async Task TestRemoteResetAsyncInvalidatesMonitorSession()
    {
        using var mcpx = new McpX(() => new FakePlcTransport());
        var session = await mcpx.MonitorRegisterAsync(x => x.Add<short>(Prefix.D, "100", _ => { }));

        await mcpx.RemoteResetAsync(TimeSpan.FromSeconds(5));

        // 再接続した接続には PLC のモニタ登録が引き継がれないため、古いセッションは無効
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => session.ReadAsync());
    }

    [TestMethod]
    public async Task TestRemoteResetAsyncWaitsForMonitorRead()
    {
        // 1本目の接続は、モニタ（0802）の応答を release まで保留する
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = new HoldMonitorTransport(release.Task);
        int created = 0;
        using var mcpx = new McpX(() => created++ == 0 ? first : new FakePlcTransport());
        var session = await mcpx.MonitorRegisterAsync(x => x.Add<short>(Prefix.D, "100", _ => { }));

        // モニタ読み出しの途中で RemoteResetAsync を実行する
        var read = session.ReadAsync();
        var reset = mcpx.RemoteResetAsync(TimeSpan.FromSeconds(10));

        // 再接続（約1秒後）はモニタ読み出しが終わるのを待つ
        await Task.Delay(1500);
        Assert.IsFalse(reset.IsCompleted);

        // 読み出しが終われば、RemoteResetAsync も完了し、古いセッションは無効になる
        release.SetResult(true);
        await read;
        await reset.WaitAsync(TimeSpan.FromSeconds(5));
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => session.ReadAsync());
    }

    // モニタ（0802）の応答だけを、指定したタスクの完了まで保留するトランスポート
    private sealed class HoldMonitorTransport(Task hold) : IPlcTransport
    {
        private readonly FakePlcTransport inner = new();

        public byte[] Request(byte[] packet, IReceiveLengthParser receiveLengthParser) => inner.Request(packet, receiveLengthParser);

        public async Task<byte[]> RequestAsync(byte[] packet, IReceiveLengthParser receiveLengthParser, CancellationToken cancellationToken)
        {
            if (BitConverter.ToUInt16(packet, 11) == 0x0802)
            {
                await hold;
            }
            return inner.Request(packet, receiveLengthParser);
        }

        public void Dispose()
        {
        }
    }
}
