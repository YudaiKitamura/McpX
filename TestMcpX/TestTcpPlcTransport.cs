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

    private (int Port, CancellationTokenSource Cts) StartReciveTimeoutEchoServer()
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

    private class TestPlc : TcpPlcTransport
    {
        public TestPlc(string ip, int port) : base(ip, port, 5000) { }
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
        using var plc = new TestPlc("127.0.0.1", port);

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
        using var plc = new TestPlc("127.0.0.1", port);

        ushort contentLength = faker.Random.UShort(min: 100, max: 1024); // 上限値は送信バッファのサイズに合わせる必要がある　var buffer = new byte[1024]
        byte[] headerBytes = faker.Random.Bytes(7);
        byte[] lengthBytes = BitConverter.GetBytes(contentLength);
        byte[] contentBytes = faker.Random.Bytes(contentLength);

        byte[] sendData = headerBytes.Concat(lengthBytes).Concat(contentBytes).ToArray();
        CollectionAssert.AreEqual(sendData, await plc.RequestAsync(sendData, new DummyReceiveLengthParser((ushort)(headerBytes.Length + lengthBytes.Length), contentLength)));

        cts.Cancel();
    }

    [TestMethod]
    public void TestException()
    {
        var (port, cts) = StartReciveTimeoutEchoServer();

        var ex = Assert.ThrowsException<IOException>(() => {
            using var plc = new TestPlc("127.0.0.1", port);
            
            ushort contentLength = faker.Random.UShort(min: 100, max: 1024);
            byte[] headerBytes = faker.Random.Bytes(7);
            byte[] lengthBytes = BitConverter.GetBytes(contentLength);
            byte[] contentBytes = faker.Random.Bytes(contentLength);

            byte[] sendData = headerBytes.Concat(lengthBytes).Concat(contentBytes).ToArray();
            plc.Request(sendData, new DummyReceiveLengthParser((ushort)(headerBytes.Length + lengthBytes.Length), contentLength));
        });
        
        cts.Cancel();
        
        Assert.IsInstanceOfType<IOException>(ex);
        Assert.AreEqual("Unable to read data from the transport connection: Connection timed out.", ex.Message);

        var ex2 = Assert.ThrowsException<TimeoutException>(() => {
            // 実在しない、テスト用IPアドレス
            using var plc = new TestPlc("192.0.2.1", port);
            
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
        var (port, cts) = StartReciveTimeoutEchoServer();

        var ex = await Assert.ThrowsExceptionAsync<IOException>(async () => {
            using var plc = new TestPlc("127.0.0.1", port);
            
            ushort contentLength = faker.Random.UShort(min: 100, max: 1024);
            byte[] headerBytes = faker.Random.Bytes(7);
            byte[] lengthBytes = BitConverter.GetBytes(contentLength);
            byte[] contentBytes = faker.Random.Bytes(contentLength);

            byte[] sendData = headerBytes.Concat(lengthBytes).Concat(contentBytes).ToArray();
            await plc.RequestAsync(sendData, new DummyReceiveLengthParser((ushort)(headerBytes.Length + lengthBytes.Length), contentLength));
        });

        Assert.IsInstanceOfType<IOException>(ex);
        Assert.AreEqual("Unable to read data from the transport connection: Connection timed out.", ex.Message);

        var ex2 = await Assert.ThrowsExceptionAsync<TimeoutException>(async () => {
            // 実在しない、テスト用IPアドレス
            using var plc = new TestPlc("192.0.2.1", port);;
            
            ushort contentLength = faker.Random.UShort(min: 100, max: 1024);
            byte[] headerBytes = faker.Random.Bytes(7);
            byte[] lengthBytes = BitConverter.GetBytes(contentLength);
            byte[] contentBytes = faker.Random.Bytes(contentLength);

            byte[] sendData = headerBytes.Concat(lengthBytes).Concat(contentBytes).ToArray();
            await plc.RequestAsync(sendData, new DummyReceiveLengthParser((ushort)(headerBytes.Length + lengthBytes.Length), contentLength));
        });
        
        Assert.IsInstanceOfType<TimeoutException>(ex2);
        Assert.AreEqual("Connection Timeout", ex2.Message);
        
        cts.Cancel();
    }
}
