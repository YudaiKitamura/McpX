using System.Net;
using System.Net.Sockets;
using McpXNative;

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
}
