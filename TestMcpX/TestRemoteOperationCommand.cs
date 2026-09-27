using System.Text;
using McpXLib;
using McpXLib.Builders;
using McpXLib.Exceptions;
using McpXLib.Commands;
using McpXLib.Enums;
using McpXLib.Interfaces;
using Moq;

namespace TestMcpX;

/// <summary>
/// リモート操作（1001 / 1002 / 1003 / 1005 / 1006）の電文テスト。
/// </summary>
/// <remarks>
/// 期待値は MELSECコミュニケーションプロトコルリファレンスマニュアル（SH-080003）11.2 の交信例（紙面169〜174ページ）。
/// <c>ToBinaryBytes</c> / <c>ToAsciiBytes</c> の先頭（要求データ長・監視タイマ）は除いて比較する。
/// </remarks>
[TestClass]
public sealed class TestRemoteOperationCommand
{
    private static byte[] Binary(IPacketBuilder command) => command.ToBinaryBytes().Skip(4).ToArray();

    private static string Ascii(IPacketBuilder command) => Encoding.ASCII.GetString(command.ToAsciiBytes().Skip(8).ToArray());

    [TestMethod]
    public void TestRun()
    {
        // モード: 強制実行しない、クリアモード: すべてクリア
        var command = RemoteOperationCommand.Run(force: false, RemoteRunClearMode.All);

        CollectionAssert.AreEqual(new byte[] { 0x01, 0x10, 0x00, 0x00, 0x01, 0x00, 0x02, 0x00 }, Binary(command));
        Assert.AreEqual("1001" + "0000" + "0001" + "02" + "00", Ascii(command));

        // 強制実行する（0003H）・クリアしない
        CollectionAssert.AreEqual(new byte[] { 0x01, 0x10, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00 }, Binary(RemoteOperationCommand.Run(force: true, RemoteRunClearMode.None)));
        Assert.AreEqual("1001" + "0000" + "0001" + "01" + "00", Ascii(RemoteOperationCommand.Run(force: false, RemoteRunClearMode.OutsideLatch)));
    }

    [TestMethod]
    public void TestStop()
    {
        var command = RemoteOperationCommand.Stop();

        CollectionAssert.AreEqual(new byte[] { 0x02, 0x10, 0x00, 0x00, 0x01, 0x00 }, Binary(command));
        Assert.AreEqual("1002" + "0000" + "0001", Ascii(command));
    }

    [TestMethod]
    public void TestPause()
    {
        // モード: 強制実行しない
        var command = RemoteOperationCommand.Pause(force: false);

        CollectionAssert.AreEqual(new byte[] { 0x03, 0x10, 0x00, 0x00, 0x01, 0x00 }, Binary(command));
        Assert.AreEqual("1003" + "0000" + "0001", Ascii(command));
        Assert.AreEqual("1003" + "0000" + "0003", Ascii(RemoteOperationCommand.Pause(force: true)));
    }

    [TestMethod]
    public void TestLatchClear()
    {
        var command = RemoteOperationCommand.LatchClear();

        CollectionAssert.AreEqual(new byte[] { 0x05, 0x10, 0x00, 0x00, 0x01, 0x00 }, Binary(command));
        Assert.AreEqual("1005" + "0000" + "0001", Ascii(command));
    }

    [TestMethod]
    public void TestReset()
    {
        var command = RemoteOperationCommand.Reset();

        CollectionAssert.AreEqual(new byte[] { 0x06, 0x10, 0x00, 0x00, 0x01, 0x00 }, Binary(command));
        Assert.AreEqual("1006" + "0000" + "0001", Ascii(command));
    }

    [TestMethod]
    public async Task TestExecute()
    {
        var plc = new Mock<IPlc>();
        plc.SetupProperty(x => x.Route);
        plc.Object.Route = new RoutePacketBuilder();
        byte[] response = [0xD0, 0x00, 0x00, 0xFF, 0xFF, 0x03, 0x00, 0x02, 0x00, 0x00, 0x00];
        plc.Setup(x => x.Request(It.IsAny<byte[]>(), It.IsAny<IReceiveLengthParser>())).Returns(response);
        plc.Setup(x => x.RequestAsync(It.IsAny<byte[]>(), It.IsAny<IReceiveLengthParser>())).ReturnsAsync(response);

        Assert.IsTrue(RemoteOperationCommand.Stop().Execute(plc.Object));
        Assert.IsTrue(await RemoteOperationCommand.Run(false, RemoteRunClearMode.None).ExecuteAsync(plc.Object));
    }

    [TestMethod]
    public async Task TestResetIgnoresDisconnect()
    {
        // リセットによる切断・タイムアウトは例外にしない
        foreach (Exception error in new Exception[] { new IOException("Connection closed unexpectedly"), new TimeoutException("Recive Timeout"), new System.Net.Sockets.SocketException((int)System.Net.Sockets.SocketError.TimedOut) })
        {
            using var mcpx = new McpX(new StubTransport(() => throw error));
            mcpx.RemoteReset();
            await mcpx.RemoteResetAsync();
        }
    }

    [TestMethod]
    public async Task TestResetThrowsErrorCode()
    {
        // PLC がエラーコードを返した場合（RESET が許可されていないなど）は例外
        byte[] error = [0xD0, 0x00, 0x00, 0xFF, 0xFF, 0x03, 0x00, 0x0B, 0x00, 0x5A, 0x40, 0x00, 0xFF, 0xFF, 0x03, 0x00, 0x06, 0x10, 0x00, 0x00];
        using var mcpx = new McpX(new StubTransport(() => error));

        Assert.AreEqual((ushort)0x405A, Assert.ThrowsException<McProtocolException>(() => mcpx.RemoteReset()).ErrorCode);
        await Assert.ThrowsExceptionAsync<McProtocolException>(() => mcpx.RemoteResetAsync());

        // RESET 以外の操作では、切断は例外のまま
        using var stop = new McpX(new StubTransport(() => throw new IOException("Connection closed unexpectedly")));
        Assert.ThrowsException<IOException>(() => stop.RemoteStop());
    }

    [TestMethod]
    public async Task TestResetReconnect()
    {
        // SM400 のビット読み出し応答（ON）
        byte[] sm400 = [0xD0, 0x00, 0x00, 0xFF, 0xFF, 0x03, 0x00, 0x03, 0x00, 0x00, 0x00, 0x10];

        foreach (var async in new[] { false, true })
        {
            // 1本目: リセットで切断、2本目: まだ応答なし、3本目以降: 正常
            var transports = new List<StubTransport>();
            Func<byte[]>[] behaviors = [
                () => throw new IOException("Connection closed unexpectedly"),
                () => throw new IOException("Connection closed unexpectedly"),
                () => sm400,
            ];
            using var mcpx = new McpX(() =>
            {
                var transport = new StubTransport(behaviors[Math.Min(transports.Count, behaviors.Length - 1)]);
                transports.Add(transport);
                return transport;
            });

            if (async)
            {
                await mcpx.RemoteResetAsync();
            }
            else
            {
                mcpx.RemoteReset();
            }

            // 同じインスタンスで続けて読み出せること
            Assert.AreEqual(3, transports.Count);
            Assert.IsTrue(transports[0].IsDisposed && transports[1].IsDisposed && !transports[2].IsDisposed);
            Assert.IsTrue(mcpx.Read<bool>(Prefix.SM, "400"));
        }
    }

    [TestMethod]
    public void TestResetReconnectTimeout()
    {
        int created = 0;
        using var mcpx = new McpX(() =>
        {
            // 初回の接続のみ成功し、リセット後は接続できない
            if (created++ > 0)
            {
                throw new System.Net.Sockets.SocketException((int)System.Net.Sockets.SocketError.ConnectionRefused);
            }
            return new StubTransport(() => throw new IOException("Connection closed unexpectedly"));
        });

        var ex = Assert.ThrowsException<TimeoutException>(() => mcpx.RemoteReset(reconnectTimeoutMilliseconds: 1500));
        Assert.IsInstanceOfType<System.Net.Sockets.SocketException>(ex.InnerException);

        // 0 を指定した場合は接続し直さない
        mcpx.RemoteReset(reconnectTimeoutMilliseconds: 0);
    }

    private sealed class StubTransport(Func<byte[]> respond) : IPlcTransport
    {
        internal bool IsDisposed { get; private set; }

        public byte[] Request(byte[] packet, IReceiveLengthParser receiveLengthParser) => respond();

        public Task<byte[]> RequestAsync(byte[] packet, IReceiveLengthParser receiveLengthParser) => Task.FromResult(respond());

        [Obsolete]
        public byte[] Request(byte[] packet) => throw new NotSupportedException();

        [Obsolete]
        public Task<byte[]> RequestAsync(byte[] packet) => throw new NotSupportedException();

        public void Dispose()
        {
            IsDisposed = true;
        }
    }
}
