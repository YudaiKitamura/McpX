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
}
