using System.Net;
using System.Net.Sockets;
using McpXNative;
using TestMcpX;

namespace TestMcpXInterop;

[TestClass]
public sealed unsafe class TestConnect
{
    private static int Connect(NativeConnectOptions options, out ulong client, out NativeError err)
    {
        err = Native.NewError();
        ulong handle = 12345;
        int status;
        fixed (NativeError* e = &err)
        {
            status = Native.Connect(&options, &handle, e);
        }
        client = handle;
        return status;
    }

    private static NativeConnectOptions Options(byte* host, int port)
    {
        var options = default(NativeConnectOptions);
        Native.ConnectOptionsInit(&options);
        options.Host = (IntPtr)host;
        options.Port = port;
        return options;
    }

    [TestMethod]
    public void TestInvalidOptions()
    {
        fixed (byte* host = Native.Utf8Z("127.0.0.1"))
        {
            var valid = Options(host, 5000);

            var smallStruct = valid;
            smallStruct.StructSize = 8;
            Assert.AreEqual((int)McpxStatus.InvalidArgument, Connect(smallStruct, out var client, out var err));
            Assert.AreEqual(0UL, client);
            StringAssert.Contains(Native.Message(err), "struct_size");

            var noHost = valid;
            noHost.Host = IntPtr.Zero;
            Assert.AreEqual((int)McpxStatus.InvalidArgument, Connect(noHost, out _, out _));

            var badPort = valid;
            badPort.Port = 0;
            Assert.AreEqual((int)McpxStatus.InvalidArgument, Connect(badPort, out _, out _));

            var badTimeout = valid;
            badTimeout.TimeoutMs = 70000;
            Assert.AreEqual((int)McpxStatus.InvalidArgument, Connect(badTimeout, out _, out _));

            var badFrame = valid;
            badFrame.Frame = 2;
            Assert.AreEqual((int)McpxStatus.InvalidArgument, Connect(badFrame, out _, out _));

            var badSeries = valid;
            badSeries.Series = 2;
            Assert.AreEqual((int)McpxStatus.InvalidArgument, Connect(badSeries, out _, out _));
        }

        var nullErr = Native.NewError();
        ulong output = 0;
        Assert.AreEqual((int)McpxStatus.InvalidArgument, Native.Connect(null, &output, &nullErr));
    }

    [TestMethod]
    public void TestLargerStructSizeIsAccepted()
    {
        // 将来のバージョンで末尾にフィールドが増えた構造体（struct_size が大きい）も受け付けること
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;

        try
        {
            fixed (byte* host = Native.Utf8Z("127.0.0.1"))
            {
                var options = Options(host, port);
                options.StructSize += 16;

                Assert.AreEqual((int)McpxStatus.Ok, Connect(options, out var client, out var err), Native.Message(err));
                Assert.AreNotEqual(0UL, client);

                var closeErr = Native.NewError();
                Assert.AreEqual((int)McpxStatus.Ok, Native.Close(client, &closeErr));
            }
        }
        finally
        {
            listener.Stop();
        }
    }

    [TestMethod]
    public void TestConnectionRefused()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        fixed (byte* host = Native.Utf8Z("127.0.0.1"))
        {
            Assert.AreEqual((int)McpxStatus.ConnectionRefused, Connect(Options(host, port), out var client, out var err));
            Assert.AreEqual(0UL, client);
            Assert.AreEqual((int)SocketError.ConnectionRefused, err.SocketError);
        }
    }

    [TestMethod]
    public void TestConnectSimulator()
    {
        // システム9・号機4 = 5594 で待ち受け、既定のホスト（127.0.0.1）で接続できること
        var listener = new TcpListener(IPAddress.Loopback, McpXLib.McpXSimulator.GetPort(9, 4));
        listener.Start();
        try
        {
            var options = default(NativeSimulatorOptions);
            Native.SimulatorOptionsInit(&options);
            options.SystemNo = 9;
            options.CpuNo = 4;

            var err = Native.NewError();
            ulong client = 0;
            Assert.AreEqual((int)McpxStatus.Ok, Native.ConnectSimulator(&options, &client, &err), Native.Message(err));
            Assert.AreEqual(McpXLib.Enums.ProcessorSeries.iQR, HandleTable.Get(client)!.Client.ProcessorSeries);
            Assert.AreEqual((int)McpxStatus.Ok, Native.Close(client, &err));

            // 範囲外の号機No.は接続せずに InvalidArgument
            options.CpuNo = 5;
            client = 12345;
            Assert.AreEqual((int)McpxStatus.InvalidArgument, Native.ConnectSimulator(&options, &client, &err));
            Assert.AreEqual(0UL, client);
        }
        finally
        {
            listener.Stop();
        }
    }

    [TestMethod]
    public void TestSetMultiBlock()
    {
        ulong client = Native.Register(new FakePlcTransport());
        var err = Native.NewError();
        try
        {
            Assert.IsFalse(HandleTable.Get(client)!.Client.UseMultiBlockAccess);
            Assert.AreEqual((int)McpxStatus.Ok, Native.SetMultiBlock(client, 1, &err));
            Assert.IsTrue(HandleTable.Get(client)!.Client.UseMultiBlockAccess);
            Assert.AreEqual((int)McpxStatus.Ok, Native.SetMultiBlock(client, 0, &err));
            Assert.IsFalse(HandleTable.Get(client)!.Client.UseMultiBlockAccess);

            Assert.AreEqual((int)McpxStatus.InvalidHandle, Native.SetMultiBlock(0xDEAD, 1, &err));
        }
        finally
        {
            Native.Close(client, &err);
        }
    }
}
