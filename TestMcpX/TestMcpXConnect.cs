using System.Net;
using System.Net.Sockets;
using McpXLib;
using McpXLib.Enums;

namespace TestMcpX;

/// <summary>
/// 接続設定（McpXOptions）、接続（Connect / ConnectAsync）、破棄（Dispose / DisposeAsync）のテスト。
/// </summary>
[TestClass]
public sealed class TestMcpXConnect
{
    private const ushort REMOTE_LOCK = 0x1631;

    [TestMethod]
    public void TestOptionsValidation()
    {
        // 接続前に例外（接続タイムアウトを待たない）
        Assert.ThrowsException<ArgumentNullException>(() => McpX.Connect(null!));
        Assert.ThrowsException<ArgumentException>(() => McpX.Connect(new McpXOptions { Host = "", Port = 5000 }));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => McpX.Connect(new McpXOptions { Host = "127.0.0.1", Port = 0 }));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => McpX.Connect(new McpXOptions { Host = "127.0.0.1", Port = 65536 }));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => McpX.Connect(new McpXOptions { Host = "127.0.0.1", Port = 5000, Timeout = TimeSpan.Zero }));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => McpX.Connect(new McpXOptions { Host = "127.0.0.1", Port = 5000, Timeout = TimeSpan.FromSeconds(-1) }));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => McpX.Connect(new McpXOptions { Host = "127.0.0.1", Port = 5000, RequestFrame = (RequestFrame)99 }));
    }

    [TestMethod]
    public async Task TestConnectAsync()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            await using var mcpx = await McpX.ConnectAsync(new McpXOptions
            {
                Host = "127.0.0.1",
                Port = port,
                IsAscii = true,
                RequestFrame = RequestFrame.E4,
                ProcessorSeries = ProcessorSeries.iQR,
                Timeout = Timeout.InfiniteTimeSpan,
                UseMultiBlockAccess = true,
            });

            Assert.IsTrue(mcpx.IsAscii);
            Assert.AreEqual(RequestFrame.E4, mcpx.RequestFrame);
            Assert.AreEqual(ProcessorSeries.iQR, mcpx.ProcessorSeries);
            Assert.IsTrue(mcpx.UseMultiBlockAccess);

            // ホストとポートだけを指定する形（既定の設定）
            using var simple = await McpX.ConnectAsync("127.0.0.1", port);
            Assert.IsFalse(simple.IsAscii);
            Assert.AreEqual(RequestFrame.E3, simple.RequestFrame);
            Assert.AreEqual(ProcessorSeries.Q, simple.ProcessorSeries);
        }
        finally
        {
            listener.Stop();
        }
    }

    [TestMethod]
    public async Task TestConnectAsyncCancel()
    {
        using var cancel = new CancellationTokenSource(200);

        // 実在しない、テスト用IPアドレス（接続が完了しない）
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(
            () => McpX.ConnectAsync(new McpXOptions { Host = "192.0.2.1", Port = 5000 }, cancel.Token));
    }

    [TestMethod]
    public async Task TestUseAfterDispose()
    {
        var transport = new FakePlcTransport();
        var mcpx = new McpX(transport);
        mcpx.Dispose();

        Assert.ThrowsException<ObjectDisposedException>(() => mcpx.Read<short>(Prefix.D, "0"));
        await Assert.ThrowsExceptionAsync<ObjectDisposedException>(() => mcpx.ReadAsync<short>(Prefix.D, "0"));

        // 2回目以降の破棄は何もしない
        mcpx.Dispose();
        await mcpx.DisposeAsync();
    }

    [TestMethod]
    public async Task TestDisposeAsyncLocks()
    {
        var transport = new FakePlcTransport();
        var mcpx = new McpX(transport, password: "1234");

        await mcpx.DisposeAsync();

        // リモートパスワードを指定した場合は、閉じる前にリモートロックを送ること
        Assert.AreEqual(1, transport.Requests.Count(r => r.Command == REMOTE_LOCK));
        Assert.IsTrue(transport.Disposed);

        await mcpx.DisposeAsync();
        Assert.AreEqual(1, transport.Requests.Count(r => r.Command == REMOTE_LOCK));
    }
}
