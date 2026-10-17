using System.Net;
using System.Net.Sockets;
using Bogus;
using McpXLib.Interfaces;
using McpXLib.Transports;

namespace TestMcpX;

[TestClass]
public class TestTcpPlcTransport
{
    private Faker faker = new Faker();

    private (int Port, CancellationTokenSource Cts) StartEchoServer()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var cts = new CancellationTokenSource();

        _ = Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    var client = await listener.AcceptTcpClientAsync(cts.Token);
                    
                    _ = Task.Run(async () =>
                    {
                        using var stream = client.GetStream();
                        var buffer = new byte[1024];
                        var length = await stream.ReadAsync(buffer, 0, buffer.Length);

                        if (length > 0)
                        {
                            await stream.WriteAsync(buffer, 0, length);
                            await stream.FlushAsync();
                        }

                        client.Close();
                    }, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
            listener.Stop();
        }, cts.Token);

        return (port, cts);
    }

    private (int Port, CancellationTokenSource Cts) StartReceiveTimeoutEchoServer()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var cts = new CancellationTokenSource();

        _ = Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(6000, cts.Token);
                    var client = await listener.AcceptTcpClientAsync(cts.Token);
                    
                    _ = Task.Run(async () =>
                    {
                        using var stream = client.GetStream();
                        var buffer = new byte[1024];
                        var length = await stream.ReadAsync(buffer, 0, buffer.Length);

                        if (length > 0)
                        {
                            await stream.WriteAsync(buffer, 0, length);
                            await stream.FlushAsync();
                        }

                        client.Close();
                    }, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
            listener.Stop();
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
        using var plc = TcpPlcTransport.Connect("127.0.0.1", port, 5000);

        ushort contentLength = faker.Random.UShort(min: 100, max: 1024); // 上限値は送信バッファのサイズに合わせる必要がある　var buffer = new byte[1024]
        byte[] headerBytes = faker.Random.Bytes(7);
        byte[] lengthBytes = BitConverter.GetBytes(contentLength);
        byte[] contentBytes = faker.Random.Bytes(contentLength);

        byte[] sendData = headerBytes.Concat(lengthBytes).Concat(contentBytes).ToArray();
        CollectionAssert.AreEqual(sendData, plc.Request(sendData, new DummyReceiveLengthParser((ushort)(headerBytes.Length + lengthBytes.Length), contentLength)));

        cts.Cancel();
    }

    [TestMethod]
    public async Task TestRequestAsync()
    {
        var (port, cts) = StartEchoServer();
        using var plc = TcpPlcTransport.Connect("127.0.0.1", port, 5000);

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
            using var plc = TcpPlcTransport.Connect("127.0.0.1", port, 5000);
            
            ushort contentLength = faker.Random.UShort(min: 100, max: 1024);
            byte[] headerBytes = faker.Random.Bytes(7);
            byte[] lengthBytes = BitConverter.GetBytes(contentLength);
            byte[] contentBytes = faker.Random.Bytes(contentLength);

            byte[] sendData = headerBytes.Concat(lengthBytes).Concat(contentBytes).ToArray();
            plc.Request(sendData, new DummyReceiveLengthParser((ushort)(headerBytes.Length + lengthBytes.Length), contentLength));
        });
        
        cts.Cancel();
        
        // メッセージは OS により異なる（Linux: Connection timed out / macOS: Operation timed out）ため、エラー種別で判定する
        Assert.AreEqual(SocketError.TimedOut, ((SocketException)ex.InnerException!).SocketErrorCode);

        var ex2 = Assert.ThrowsException<TimeoutException>(() => {
            // 実在しない、テスト用IPアドレス
            using var plc = TcpPlcTransport.Connect("192.0.2.1", port, 5000);
            
            ushort contentLength = faker.Random.UShort(min: 100, max: 1024);
            byte[] headerBytes = faker.Random.Bytes(7);
            byte[] lengthBytes = BitConverter.GetBytes(contentLength);
            byte[] contentBytes = faker.Random.Bytes(contentLength);

            byte[] sendData = headerBytes.Concat(lengthBytes).Concat(contentBytes).ToArray();
            plc.Request(sendData, new DummyReceiveLengthParser((ushort)(headerBytes.Length + lengthBytes.Length), contentLength));
        });
        
        Assert.IsInstanceOfType<TimeoutException>(ex2);
        Assert.AreEqual("Connection Timeout", ex2.Message);
    }

    [TestMethod]
    public async Task TestExceptionAsync()
    {
        var (port, cts) = StartReceiveTimeoutEchoServer();

        var ex = await Assert.ThrowsExceptionAsync<TimeoutException>(async () => {
            using var plc = TcpPlcTransport.Connect("127.0.0.1", port, 5000);
            
            ushort contentLength = faker.Random.UShort(min: 100, max: 1024);
            byte[] headerBytes = faker.Random.Bytes(7);
            byte[] lengthBytes = BitConverter.GetBytes(contentLength);
            byte[] contentBytes = faker.Random.Bytes(contentLength);

            byte[] sendData = headerBytes.Concat(lengthBytes).Concat(contentBytes).ToArray();
            await plc.RequestAsync(sendData, new DummyReceiveLengthParser((ushort)(headerBytes.Length + lengthBytes.Length), contentLength), CancellationToken.None);
        });

        // メッセージは OS により異なる（Linux: Connection timed out / macOS: Operation timed out）ため、エラー種別で判定する
        Assert.AreEqual(SocketError.TimedOut, ((SocketException)ex.InnerException!).SocketErrorCode);

        var ex2 = await Assert.ThrowsExceptionAsync<TimeoutException>(async () => {
            // 実在しない、テスト用IPアドレス
            using var plc = TcpPlcTransport.Connect("192.0.2.1", port, 5000);
            
            ushort contentLength = faker.Random.UShort(min: 100, max: 1024);
            byte[] headerBytes = faker.Random.Bytes(7);
            byte[] lengthBytes = BitConverter.GetBytes(contentLength);
            byte[] contentBytes = faker.Random.Bytes(contentLength);

            byte[] sendData = headerBytes.Concat(lengthBytes).Concat(contentBytes).ToArray();
            await plc.RequestAsync(sendData, new DummyReceiveLengthParser((ushort)(headerBytes.Length + lengthBytes.Length), contentLength), CancellationToken.None);
        });
        
        Assert.IsInstanceOfType<TimeoutException>(ex2);
        Assert.AreEqual("Connection Timeout", ex2.Message);
        
        cts.Cancel();
    }

    [TestMethod]
    public async Task TestRequestAsyncCancel()
    {
        var (port, cts) = StartReceiveTimeoutEchoServer();
        using var plc = TcpPlcTransport.Connect("127.0.0.1", port, 5000);
        var parser = new DummyReceiveLengthParser(9, 2);

        using var cancel = new CancellationTokenSource(200);
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => plc.RequestAsync(faker.Random.Bytes(11), parser, cancel.Token));

        // キャンセルした要求の応答が後から届く可能性があるため、接続は閉じられている
        await Assert.ThrowsExceptionAsync<IOException>(() => plc.RequestAsync(faker.Random.Bytes(11), parser, CancellationToken.None));

        cts.Cancel();
    }

    [TestMethod]
    public async Task TestConnectAsyncCancel()
    {
        using var cancel = new CancellationTokenSource(200);

        // 実在しない、テスト用IPアドレス（接続が完了しない）
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => TcpPlcTransport.ConnectAsync("192.0.2.1", 5000, 5000, cancel.Token));
    }
}
