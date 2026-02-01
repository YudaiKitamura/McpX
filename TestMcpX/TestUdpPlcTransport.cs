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

    private (int Port, CancellationTokenSource Cts) StartEchoServer()
    {
        var udpClient = new UdpClient(0);
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

    private (int Port, CancellationTokenSource Cts) StartReciveTimeoutEchoServer()
    {
        var udpClient = new UdpClient(0);
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

    private class TestPlc : UdpPlcTransport
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

        var ex = Assert.ThrowsException<SocketException>(() => {
            using var plc = new TestPlc("127.0.0.1", port);
            
            ushort contentLength = faker.Random.UShort(min: 100, max: 1024);
            byte[] headerBytes = faker.Random.Bytes(7);
            byte[] lengthBytes = BitConverter.GetBytes(contentLength);
            byte[] contentBytes = faker.Random.Bytes(contentLength);

            byte[] sendData = headerBytes.Concat(lengthBytes).Concat(contentBytes).ToArray();
            plc.Request(sendData, new DummyReceiveLengthParser((ushort)(headerBytes.Length + lengthBytes.Length), contentLength));
        });
        
        cts.Cancel();
        
        Assert.IsInstanceOfType<SocketException>(ex);
        Assert.AreEqual("Connection timed out", ex.Message);

        var ex2 = Assert.ThrowsException<SocketException>(() => {
            // 実在しない、テスト用IPアドレス
            using var plc = new TestPlc("192.0.2.1", port);
            
            ushort contentLength = faker.Random.UShort(min: 100, max: 1024);
            byte[] headerBytes = faker.Random.Bytes(7);
            byte[] lengthBytes = BitConverter.GetBytes(contentLength);
            byte[] contentBytes = faker.Random.Bytes(contentLength);

            byte[] sendData = headerBytes.Concat(lengthBytes).Concat(contentBytes).ToArray();
            plc.Request(sendData, new DummyReceiveLengthParser((ushort)(headerBytes.Length + lengthBytes.Length), contentLength));
        });
        
        Assert.IsInstanceOfType<SocketException>(ex2);
        Assert.AreEqual("Connection timed out", ex2.Message);
    }

    [TestMethod]
    public async Task TestExceptionAsync()
    {
        var (port, cts) = StartReciveTimeoutEchoServer();

        var ex = await Assert.ThrowsExceptionAsync<SocketException>(async () => {
            using var plc = new TestPlc("127.0.0.1", port);
            
            ushort contentLength = faker.Random.UShort(min: 100, max: 1024);
            byte[] headerBytes = faker.Random.Bytes(7);
            byte[] lengthBytes = BitConverter.GetBytes(contentLength);
            byte[] contentBytes = faker.Random.Bytes(contentLength);

            byte[] sendData = headerBytes.Concat(lengthBytes).Concat(contentBytes).ToArray();
            await plc.RequestAsync(sendData, new DummyReceiveLengthParser((ushort)(headerBytes.Length + lengthBytes.Length), contentLength));
        });

        Assert.IsInstanceOfType<SocketException>(ex);
        Assert.AreEqual("Connection timed out", ex.Message);

        var ex2 = await Assert.ThrowsExceptionAsync<SocketException>(async () => {
            // 実在しない、テスト用IPアドレス
            using var plc = new TestPlc("192.0.2.1", port);;
            
            ushort contentLength = faker.Random.UShort(min: 100, max: 1024);
            byte[] headerBytes = faker.Random.Bytes(7);
            byte[] lengthBytes = BitConverter.GetBytes(contentLength);
            byte[] contentBytes = faker.Random.Bytes(contentLength);

            byte[] sendData = headerBytes.Concat(lengthBytes).Concat(contentBytes).ToArray();
            await plc.RequestAsync(sendData, new DummyReceiveLengthParser((ushort)(headerBytes.Length + lengthBytes.Length), contentLength));
        });
        
        Assert.IsInstanceOfType<SocketException>(ex2);
        Assert.AreEqual("Connection timed out", ex2.Message);
        
        cts.Cancel();
    }
}
