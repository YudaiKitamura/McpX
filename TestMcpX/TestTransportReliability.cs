using System.Net;
using System.Net.Sockets;
using System.Text;
using McpXLib.Interfaces;
using McpXLib.Transports;

namespace TestMcpX;

/// <summary>
/// トランスポートの排他制御・タイムアウト・異常時の接続破棄のテスト。
/// </summary>
/// <remarks>
/// 要求・応答とも「長さ(2バイトLE) + 本体」の簡易プロトコルで、ループバックのサーバーと通信する。
/// </remarks>
[TestClass]
public sealed class TestTransportReliability
{
    private sealed class LengthPrefixParser : IReceiveLengthParser
    {
        public ushort GetHeaderLength() => 2;

        public ushort ParseContentLength(byte[] bytes) => BitConverter.ToUInt16(bytes, 0);
    }

    private static readonly LengthPrefixParser parser = new();

    private static byte[] Packet(string body)
    {
        var payload = Encoding.ASCII.GetBytes(body);
        return BitConverter.GetBytes((ushort)payload.Length).Concat(payload).ToArray();
    }

    private static async Task<byte[]> ReadExactlyAsync(NetworkStream stream, int length)
    {
        var buffer = new byte[length];
        int read = 0;
        while (read < length)
        {
            int n = await stream.ReadAsync(buffer, read, length - read);
            if (n == 0)
            {
                throw new IOException("closed");
            }
            read += n;
        }
        return buffer;
    }

    /// <summary>
    /// 接続ごとに要求を1件ずつ読み、handler に応答させる TCP サーバー。
    /// </summary>
    private static (int Port, CancellationTokenSource Cts) StartTcpServer(Func<NetworkStream, byte[], Task> handler)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var cts = new CancellationTokenSource();

        _ = Task.Run(async () =>
        {
            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    var client = await listener.AcceptTcpClientAsync(cts.Token);
                    _ = Task.Run(async () =>
                    {
                        using (client)
                        using (var stream = client.GetStream())
                        {
                            try
                            {
                                while (true)
                                {
                                    var header = await ReadExactlyAsync(stream, 2);
                                    var body = await ReadExactlyAsync(stream, BitConverter.ToUInt16(header, 0));
                                    await handler(stream, header.Concat(body).ToArray());
                                }
                            }
                            catch (Exception)
                            {
                                // 切断・キャンセル時は終了
                            }
                        }
                    });
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                listener.Stop();
            }
        });

        return (port, cts);
    }

    // 応答を2回に分け、間を空けて返すエコー（排他がないと並行要求の送受信が交錯しやすくする）。
    private static async Task SlowEcho(NetworkStream stream, byte[] request)
    {
        await stream.WriteAsync(request, 0, 1);
        await Task.Delay(5);
        await stream.WriteAsync(request, 1, request.Length - 1);
    }

    [TestMethod]
    public async Task TestTcpConcurrentRequestsAreSerialized()
    {
        var (port, cts) = StartTcpServer(SlowEcho);
        using var _ = cts;
        using var transport = new TcpPlcTransport("127.0.0.1", port, 5000);

        var tasks = Enumerable.Range(0, 20).Select(i =>
        {
            var packet = Packet($"request-{i:D2}");
            // 同期と非同期を混在させる
            Task<byte[]> task = i % 2 == 0
                ? transport.RequestAsync(packet, parser)
                : Task.Run(() => transport.Request(packet, parser));
            return task.ContinueWith(t => (packet, response: t.Result));
        }).ToArray();

        foreach (var (packet, response) in await Task.WhenAll(tasks))
        {
            CollectionAssert.AreEqual(packet, response);
        }
    }

    [TestMethod]
    public async Task TestUdpConcurrentRequestsAreSerialized()
    {
        // 受信した順とは異なる順で応答を返す UDP エコーサーバー
        var server = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        int port = ((IPEndPoint)server.Client.LocalEndPoint!).Port;
        using var cts = new CancellationTokenSource();
        var random = new Random();
        _ = Task.Run(async () =>
        {
            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    var result = await server.ReceiveAsync(cts.Token);
                    int delay = random.Next(0, 20);
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(delay);
                        await server.SendAsync(result.Buffer, result.Buffer.Length, result.RemoteEndPoint);
                    });
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                server.Close();
            }
        });

        using var transport = new UdpPlcTransport("127.0.0.1", port, 5000);

        var tasks = Enumerable.Range(0, 20).Select(i =>
        {
            var packet = Packet($"request-{i:D2}");
            Task<byte[]> task = i % 2 == 0
                ? transport.RequestAsync(packet, parser)
                : Task.Run(() => transport.Request(packet, parser));
            return task.ContinueWith(t => (packet, response: t.Result));
        }).ToArray();

        foreach (var (packet, response) in await Task.WhenAll(tasks))
        {
            CollectionAssert.AreEqual(packet, response);
        }
    }

    private static void AssertTimedOut(IOException ex)
    {
        Assert.IsInstanceOfType<SocketException>(ex.InnerException);
        Assert.AreEqual(SocketError.TimedOut, ((SocketException)ex.InnerException!).SocketErrorCode);
    }

    // 応答を返さないサーバー
    private static Task NoResponse(NetworkStream stream, byte[] request) => Task.Delay(Timeout.Infinite);

    // 応答を1バイトずつ 200ms 間隔で返すサーバー（1回の待ちは期限内だが、合計は期限を超える）
    private static async Task TrickleEcho(NetworkStream stream, byte[] request)
    {
        foreach (var b in request)
        {
            await stream.WriteAsync(new[] { b }, 0, 1);
            await Task.Delay(200);
        }
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task TestTcpAsyncRequestTimesOutWhenNoResponse()
    {
        var (port, cts) = StartTcpServer(NoResponse);
        using var _ = cts;
        using var transport = new TcpPlcTransport("127.0.0.1", port, 500);

        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        var ex = await Assert.ThrowsExceptionAsync<IOException>(() => transport.RequestAsync(Packet("no-response"), parser));
        elapsed.Stop();

        AssertTimedOut(ex);
        Assert.IsTrue(elapsed.ElapsedMilliseconds is >= 400 and < 3000, $"elapsed: {elapsed.ElapsedMilliseconds}ms");
    }

    [TestMethod]
    [Timeout(10000)]
    public void TestTcpRequestDeadlineCoversWholeResponse()
    {
        var (port, cts) = StartTcpServer(TrickleEcho);
        using var _ = cts;
        using var transport = new TcpPlcTransport("127.0.0.1", port, 500);

        // 応答は 2 + 10 バイト = 約 2.4 秒かかるが、期限 500ms で打ち切られること
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        var ex = Assert.ThrowsException<IOException>(() => transport.Request(Packet("0123456789"), parser));
        elapsed.Stop();

        AssertTimedOut(ex);
        Assert.IsTrue(elapsed.ElapsedMilliseconds < 1500, $"elapsed: {elapsed.ElapsedMilliseconds}ms");
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task TestTcpAsyncRequestDeadlineCoversWholeResponse()
    {
        var (port, cts) = StartTcpServer(TrickleEcho);
        using var _ = cts;
        using var transport = new TcpPlcTransport("127.0.0.1", port, 500);

        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        var ex = await Assert.ThrowsExceptionAsync<IOException>(() => transport.RequestAsync(Packet("0123456789"), parser));
        elapsed.Stop();

        AssertTimedOut(ex);
        Assert.IsTrue(elapsed.ElapsedMilliseconds < 1500, $"elapsed: {elapsed.ElapsedMilliseconds}ms");
    }

    // 最初の要求だけ応答を 800ms 遅らせ、以降はすぐに返すエコー
    private static Func<NetworkStream, byte[], Task> FirstResponseDelayedEcho(Action onRequest)
    {
        int count = 0;
        return async (stream, request) =>
        {
            onRequest();
            if (Interlocked.Increment(ref count) == 1)
            {
                await Task.Delay(800);
            }
            await stream.WriteAsync(request, 0, request.Length);
        };
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task TestTcpConnectionIsClosedAfterAsyncTimeout()
    {
        int requests = 0;
        var (port, cts) = StartTcpServer(FirstResponseDelayedEcho(() => Interlocked.Increment(ref requests)));
        using var _ = cts;
        using var transport = new TcpPlcTransport("127.0.0.1", port, 500);

        await Assert.ThrowsExceptionAsync<IOException>(() => transport.RequestAsync(Packet("first"), parser));

        // 遅れて届いた1回目の応答を2回目の応答として返さず、例外になること
        await Task.Delay(500);
        var ex = await Assert.ThrowsExceptionAsync<IOException>(() => transport.RequestAsync(Packet("second"), parser));
        StringAssert.Contains(ex.Message, "closed");
        Assert.ThrowsException<IOException>(() => transport.Request(Packet("third"), parser));

        // 閉じた後の要求は送信されないこと
        Assert.AreEqual(1, requests);
    }

    [TestMethod]
    [Timeout(10000)]
    public void TestTcpConnectionIsClosedAfterSyncTimeout()
    {
        var (port, cts) = StartTcpServer(FirstResponseDelayedEcho(() => { }));
        using var _ = cts;
        using var transport = new TcpPlcTransport("127.0.0.1", port, 500);

        Assert.ThrowsException<IOException>(() => transport.Request(Packet("first"), parser));

        Thread.Sleep(500);
        var ex = Assert.ThrowsException<IOException>(() => transport.Request(Packet("second"), parser));
        StringAssert.Contains(ex.Message, "closed");
    }

    [TestMethod]
    [Timeout(10000)]
    public void TestUdpSyncTimeoutDoesNotReturnLateResponse()
    {
        // 最初の要求だけ応答を 800ms 遅らせる UDP エコーサーバー
        var server = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        int port = ((IPEndPoint)server.Client.LocalEndPoint!).Port;
        using var cts = new CancellationTokenSource();
        _ = Task.Run(async () =>
        {
            int count = 0;
            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    var result = await server.ReceiveAsync(cts.Token);
                    int delay = ++count == 1 ? 800 : 0;
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(delay);
                        await server.SendAsync(result.Buffer, result.Buffer.Length, result.RemoteEndPoint);
                    });
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                server.Close();
            }
        });

        using var transport = new UdpPlcTransport("127.0.0.1", port, 500);

        var ex = Assert.ThrowsException<SocketException>(() => transport.Request(Packet("first"), parser));
        Assert.AreEqual(SocketError.TimedOut, ex.SocketErrorCode);

        // 1回目の遅延応答が届いた後でも、2回目は自分の応答を受け取ること（UDP はソケットを作り直して継続利用できる）
        Thread.Sleep(500);
        var second = Packet("second");
        CollectionAssert.AreEqual(second, transport.Request(second, parser));
    }
}
