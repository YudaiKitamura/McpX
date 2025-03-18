using System.Net;
using System.Net.Sockets;
using McpXLib.Abstructs;
using Bogus;

namespace TestMcpX;

[TestClass]
public class TestBasePlc
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

    private class TestPlc : BasePlc
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
