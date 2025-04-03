using System.Net;
using System.Net.Sockets;
using Bogus;
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

    private class TestPlc : UdpPlcTransport
    {
        public TestPlc(string ip, int port) : base(ip, port) { }
    }

    [TestMethod]
    public void TestRequest()
    {   
        var (port, cts) = StartEchoServer();
        using var plc = new TestPlc("127.0.0.1", port);

        byte[] sendData = faker.Random.Bytes(10);
        CollectionAssert.AreEqual(sendData, plc.Request(sendData));

        cts.Cancel();
    }

    [TestMethod]
    public async Task TestRequestAsync()
    {
        var (port, cts) = StartEchoServer();
        using var plc = new TestPlc("127.0.0.1", port);

        byte[] sendData = faker.Random.Bytes(10);
        CollectionAssert.AreEqual(sendData, await plc.RequestAsync(sendData));

        cts.Cancel();
    }
}
