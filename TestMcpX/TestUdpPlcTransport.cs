using McpXLib.Exceptions;
using System.Net;
using System.Net.Sockets;
using Bogus;
using McpXLib.Interfaces;
using McpXLib.Transports;

namespace TestMcpX;

[TestClass]
public class TestUdpPlcTransport
{
    private Faker faker = new Faker();

    private (int Port, CancellationTokenSource Cts) StartEchoServer(AddressFamily family = AddressFamily.InterNetwork)
    {
        var udpClient = family == AddressFamily.InterNetworkV6 ? UdpTestServer.Create(new IPEndPoint(IPAddress.IPv6Loopback, 0)) : UdpTestServer.Create();
        int port = ((IPEndPoint)udpClient.Client.LocalEndPoint!).Port;
        var cts = new CancellationTokenSource();

        _ = Task.Run(async () =>
        {
            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    var result = await udpClient.ReceiveAsync(cts.Token);
                    await udpClient.SendAsync(result.Buffer, result.Buffer.Length, result.RemoteEndPoint);
                }
            }
            catch (OperationCanceledException)
            {
                // キャンセル時は終了
            }
            finally
            {
                udpClient.Close();
            }
        }, cts.Token);

        return (port, cts);
    }

    private (int Port, CancellationTokenSource Cts) StartReceiveTimeoutEchoServer()
    {
        var udpClient = UdpTestServer.Create();
        int port = ((IPEndPoint)udpClient.Client.LocalEndPoint!).Port;
        var cts = new CancellationTokenSource();

        _ = Task.Run(async () =>
        {
            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    var result = await udpClient.ReceiveAsync(cts.Token);
                    await Task.Delay(6000, cts.Token);
                    await udpClient.SendAsync(result.Buffer, result.Buffer.Length, result.RemoteEndPoint);
                }
            }
            catch (OperationCanceledException)
            {
                // キャンセル時は終了
            }
            finally
            {
                udpClient.Close();
            }
        }, cts.Token);

        return (port, cts);
    }

    private (int Port, CancellationTokenSource Cts) StartSkipFirstEchoServer()
    {
        var udpClient = UdpTestServer.Create();
        int port = ((IPEndPoint)udpClient.Client.LocalEndPoint!).Port;
        var cts = new CancellationTokenSource();

        _ = Task.Run(async () =>
        {
            try
            {
                var isFirst = true;
                while (!cts.Token.IsCancellationRequested)
                {
                    var result = await udpClient.ReceiveAsync(cts.Token);
                    if (isFirst)
                    {
                        // 1回目の要求には応答しない
                        isFirst = false;
                        continue;
                    }
                    await udpClient.SendAsync(result.Buffer, result.Buffer.Length, result.RemoteEndPoint);
                }
            }
            catch (OperationCanceledException)
            {
                // キャンセル時は終了
            }
            finally
            {
                udpClient.Close();
            }
        }, cts.Token);

        return (port, cts);
    }

    private class DummyReceiveLengthParser : IReceiveLengthParser
    {
        ushort headerLength;
        ushort contentLength;
        
        public DummyReceiveLengthParser(ushort headerLength, ushort contentLength)
        {
            this.headerLength = headerLength; 
            this.contentLength = contentLength;
        }

        public ushort GetHeaderLength()
        {
            return headerLength;
        }

        public ushort ParseContentLength(byte[] bytes)
        {
            return contentLength;
        }
    }

    [TestMethod]
    public void TestRequest()
    {   
        var (port, cts) = StartEchoServer();
        using var plc = UdpPlcTransport.Create("127.0.0.1", port, 5000);

        ushort contentLength = faker.Random.UShort(min: 100, max: 1024); // 上限値は送信バッファのサイズに合わせる必要がある　var buffer = new byte[1024]
        byte[] headerBytes = faker.Random.Bytes(7);
        byte[] lengthBytes = BitConverter.GetBytes(contentLength);
        byte[] contentBytes = faker.Random.Bytes(contentLength);

        byte[] sendData = headerBytes.Concat(lengthBytes).Concat(contentBytes).ToArray();
        CollectionAssert.AreEqual(sendData, plc.Request(sendData, new DummyReceiveLengthParser((ushort)(headerBytes.Length + lengthBytes.Length), contentLength)));

        cts.Cancel();
    }

    [TestMethod]
    public async Task TestRequestIPv6()
    {
        if (!Socket.OSSupportsIPv6)
        {
            Assert.Inconclusive("IPv6 is not supported.");
        }

        var (port, cts) = StartEchoServer(AddressFamily.InterNetworkV6);
        using var plc = UdpPlcTransport.Create("::1", port, 5000);

        byte[] sendData = { 0xD0, 0x00, 0x00, 0xFF, 0xFF, 0x03, 0x00, 0x02, 0x00, 0x00, 0x00 };
        var parser = new DummyReceiveLengthParser(9, 2);
        CollectionAssert.AreEqual(sendData, plc.Request(sendData, parser));
        CollectionAssert.AreEqual(sendData, await plc.RequestAsync(sendData, parser, CancellationToken.None));

        cts.Cancel();
    }

    [TestMethod]
    public async Task TestRequestAsync()
    {
        var (port, cts) = StartEchoServer();
        using var plc = UdpPlcTransport.Create("127.0.0.1", port, 5000);

        ushort contentLength = faker.Random.UShort(min: 100, max: 1024); // 上限値は送信バッファのサイズに合わせる必要がある　var buffer = new byte[1024]
        byte[] headerBytes = faker.Random.Bytes(7);
        byte[] lengthBytes = BitConverter.GetBytes(contentLength);
        byte[] contentBytes = faker.Random.Bytes(contentLength);

        byte[] sendData = headerBytes.Concat(lengthBytes).Concat(contentBytes).ToArray();
        CollectionAssert.AreEqual(sendData, await plc.RequestAsync(sendData, new DummyReceiveLengthParser((ushort)(headerBytes.Length + lengthBytes.Length), contentLength), CancellationToken.None));

        cts.Cancel();
    }

    [TestMethod]
    public void TestException()
    {
        var (port, cts) = StartReceiveTimeoutEchoServer();

        var ex = Assert.ThrowsException<TimeoutException>(() => {
            using var plc = UdpPlcTransport.Create("127.0.0.1", port, 5000);
            
            ushort contentLength = faker.Random.UShort(min: 100, max: 1024);
            byte[] headerBytes = faker.Random.Bytes(7);
            byte[] lengthBytes = BitConverter.GetBytes(contentLength);
            byte[] contentBytes = faker.Random.Bytes(contentLength);

            byte[] sendData = headerBytes.Concat(lengthBytes).Concat(contentBytes).ToArray();
            plc.Request(sendData, new DummyReceiveLengthParser((ushort)(headerBytes.Length + lengthBytes.Length), contentLength));
        });
        
        cts.Cancel();
        
        Assert.IsInstanceOfType<TimeoutException>(ex);
        // メッセージは OS により異なるため、エラー種別で判定する
        Assert.AreEqual(SocketError.TimedOut, ((SocketException)ex.InnerException!).SocketErrorCode);

        var ex2 = Assert.ThrowsException<TimeoutException>(() => {
            // 実在しない、テスト用IPアドレス
            using var plc = UdpPlcTransport.Create("192.0.2.1", port, 5000);
            
            ushort contentLength = faker.Random.UShort(min: 100, max: 1024);
            byte[] headerBytes = faker.Random.Bytes(7);
            byte[] lengthBytes = BitConverter.GetBytes(contentLength);
            byte[] contentBytes = faker.Random.Bytes(contentLength);

            byte[] sendData = headerBytes.Concat(lengthBytes).Concat(contentBytes).ToArray();
            plc.Request(sendData, new DummyReceiveLengthParser((ushort)(headerBytes.Length + lengthBytes.Length), contentLength));
        });
        
        Assert.IsInstanceOfType<TimeoutException>(ex2);
        // メッセージは OS により異なるため、エラー種別で判定する
        Assert.AreEqual(SocketError.TimedOut, ((SocketException)ex2.InnerException!).SocketErrorCode);
    }

    [TestMethod]
    public async Task TestExceptionAsync()
    {
        var (port, cts) = StartReceiveTimeoutEchoServer();

        var ex = await Assert.ThrowsExceptionAsync<TimeoutException>(async () => {
            using var plc = UdpPlcTransport.Create("127.0.0.1", port, 5000);
            
            ushort contentLength = faker.Random.UShort(min: 100, max: 1024);
            byte[] headerBytes = faker.Random.Bytes(7);
            byte[] lengthBytes = BitConverter.GetBytes(contentLength);
            byte[] contentBytes = faker.Random.Bytes(contentLength);

            byte[] sendData = headerBytes.Concat(lengthBytes).Concat(contentBytes).ToArray();
            await plc.RequestAsync(sendData, new DummyReceiveLengthParser((ushort)(headerBytes.Length + lengthBytes.Length), contentLength), CancellationToken.None);
        });

        Assert.IsInstanceOfType<TimeoutException>(ex);
        // メッセージは OS により異なるため、エラー種別で判定する
        Assert.AreEqual(SocketError.TimedOut, ((SocketException)ex.InnerException!).SocketErrorCode);

        var ex2 = await Assert.ThrowsExceptionAsync<TimeoutException>(async () => {
            // 実在しない、テスト用IPアドレス
            using var plc = UdpPlcTransport.Create("192.0.2.1", port, 5000);
            
            ushort contentLength = faker.Random.UShort(min: 100, max: 1024);
            byte[] headerBytes = faker.Random.Bytes(7);
            byte[] lengthBytes = BitConverter.GetBytes(contentLength);
            byte[] contentBytes = faker.Random.Bytes(contentLength);

            byte[] sendData = headerBytes.Concat(lengthBytes).Concat(contentBytes).ToArray();
            await plc.RequestAsync(sendData, new DummyReceiveLengthParser((ushort)(headerBytes.Length + lengthBytes.Length), contentLength), CancellationToken.None);
        });
        
        Assert.IsInstanceOfType<TimeoutException>(ex2);
        // メッセージは OS により異なるため、エラー種別で判定する
        Assert.AreEqual(SocketError.TimedOut, ((SocketException)ex2.InnerException!).SocketErrorCode);
        
        cts.Cancel();
    }

    [TestMethod]
    public async Task TestRequestAsyncAfterTimeout()
    {
        var (port, cts) = StartSkipFirstEchoServer();
        using var plc = UdpPlcTransport.Create("127.0.0.1", port, 500);
        var parser = new DummyReceiveLengthParser(0, 16);

        byte[] firstData = faker.Random.Bytes(16);
        await Assert.ThrowsExceptionAsync<TimeoutException>(async () => await plc.RequestAsync(firstData, parser, CancellationToken.None));

        // タイムアウトした受信が次の応答を横取りしないこと
        byte[] secondData = faker.Random.Bytes(16);
        CollectionAssert.AreEqual(secondData, await plc.RequestAsync(secondData, parser, CancellationToken.None));

        cts.Cancel();
    }

    [TestMethod]
    public async Task TestRequestLengthMismatch()
    {
        var (port, cts) = StartEchoServer();
        using var plc = UdpPlcTransport.Create("127.0.0.1", port, 5000);

        // ヘッダに書かれた応答データ長とデータグラムの長さが一致しない場合は ReceivePacketException
        byte[] data = faker.Random.Bytes(16);
        Assert.ThrowsException<ReceivePacketException>(() => plc.Request(data, new DummyReceiveLengthParser(0, 8)));
        await Assert.ThrowsExceptionAsync<ReceivePacketException>(() => plc.RequestAsync(data, new DummyReceiveLengthParser(0, 32), CancellationToken.None));

        cts.Cancel();
    }

    // 1回目の要求には「長さの不正な応答」をすぐに返し、少し遅れて「正しい長さの応答（0xEE）」を返す。2回目以降はエコーする。
    private (int Port, CancellationTokenSource Cts) StartLateReplyServer()
    {
        var udpClient = UdpTestServer.Create();
        int port = ((IPEndPoint)udpClient.Client.LocalEndPoint!).Port;
        var cts = new CancellationTokenSource();

        _ = Task.Run(async () =>
        {
            try
            {
                var isFirst = true;
                while (!cts.Token.IsCancellationRequested)
                {
                    var result = await udpClient.ReceiveAsync(cts.Token);
                    if (isFirst)
                    {
                        isFirst = false;
                        await udpClient.SendAsync(new byte[] { 0x00, 0x00 }, 2, result.RemoteEndPoint);
                        await Task.Delay(300, cts.Token);
                        await udpClient.SendAsync(new byte[] { 0xEE, 0xEE, 0xEE, 0xEE }, 4, result.RemoteEndPoint);
                        continue;
                    }
                    await udpClient.SendAsync(result.Buffer, result.Buffer.Length, result.RemoteEndPoint);
                }
            }
            catch (OperationCanceledException)
            {
                // キャンセル時は終了
            }
            finally
            {
                udpClient.Close();
            }
        }, cts.Token);

        return (port, cts);
    }

    [TestMethod]
    public async Task TestLateReplyAfterFailedRequest()
    {
        foreach (var isAsync in new[] { false, true })
        {
            var (port, cts) = StartLateReplyServer();
            using var plc = UdpPlcTransport.Create("127.0.0.1", port, 2000);
            var parser = new DummyReceiveLengthParser(0, 4);

            byte[] first = [0x01, 0x01, 0x01, 0x01];
            byte[] second = [0x02, 0x02, 0x02, 0x02];
            if (isAsync)
            {
                await Assert.ThrowsExceptionAsync<ReceivePacketException>(() => plc.RequestAsync(first, parser, CancellationToken.None));
            }
            else
            {
                Assert.ThrowsException<ReceivePacketException>(() => plc.Request(first, parser));
            }

            // 失敗した要求の応答が遅れて届いても、次の要求は自分の応答を受け取る（ソケットを作り直しているため）
            await Task.Delay(500);
            var response = isAsync ? await plc.RequestAsync(second, parser, CancellationToken.None) : plc.Request(second, parser);
            CollectionAssert.AreEqual(second, response, isAsync ? "async" : "sync");

            cts.Cancel();
        }
    }
}
