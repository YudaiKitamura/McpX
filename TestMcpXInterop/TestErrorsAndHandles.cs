using System.Net.Sockets;
using McpXLib.Enums;
using McpXNative;
using TestMcpX;

namespace TestMcpXInterop;

[TestClass]
public sealed unsafe class TestErrorsAndHandles
{
    private static int ReadOne(ulong client, NativeError* err)
    {
        short value = 0;
        return Native.Read(client, Prefix.D, "0", McpxType.I16, &value, 2, err);
    }

    [TestMethod]
    public void TestCloseAndInvalidHandle()
    {
        var transport = new FakePlcTransport();
        var client = Native.Register(transport);
        var err = Native.NewError();

        Assert.AreEqual((int)McpxStatus.Ok, Native.Close(0, &err));
        Assert.AreEqual((int)McpxStatus.Ok, Native.Close(client, &err));
        Assert.IsTrue(transport.Disposed);

        // close 済みのハンドルは無効（二重 close・close 後の使用）
        Assert.AreEqual((int)McpxStatus.InvalidHandle, Native.Close(client, &err));
        Assert.AreEqual((int)McpxStatus.InvalidHandle, ReadOne(client, &err));
        Assert.AreEqual((int)McpxStatus.InvalidHandle, ReadOne(123456789, &err));
    }

    [TestMethod]
    public void TestProtocolErrorCarriesEndCode()
    {
        var transport = new FakePlcTransport();
        transport.EndCodes[0x0401] = 0xC051;
        var client = Native.Register(transport);
        var err = Native.NewError();

        Assert.AreEqual((int)McpxStatus.Protocol, ReadOne(client, &err));
        Assert.AreEqual((ushort)0xC051, err.EndCode);
        StringAssert.StartsWith(Native.Message(err), "McProtocolException");
    }

    [TestMethod]
    public void TestExceptionMapping()
    {
        (Exception exception, McpxStatus status, int socketError)[] cases =
        [
            (new TimeoutException("t", new SocketException((int)SocketError.TimedOut)), McpxStatus.Timeout, (int)SocketError.TimedOut),
            (new SocketException((int)SocketError.ConnectionRefused), McpxStatus.ConnectionRefused, (int)SocketError.ConnectionRefused),
            (new SocketException((int)SocketError.HostUnreachable), McpxStatus.Network, (int)SocketError.HostUnreachable),
            (new IOException("closed"), McpxStatus.Disconnected, 0),
            (new ObjectDisposedException("x"), McpxStatus.Disconnected, 0),
            (new InvalidOperationException("x"), McpxStatus.InvalidOperation, 0),
            (new ArgumentException("x"), McpxStatus.InvalidArgument, 0),
            (new NotSupportedException("x"), McpxStatus.Unsupported, 0),
            (new McpXLib.Exceptions.RecivePacketException("x"), McpxStatus.ReceivePacket, 0),
            (new FormatException("x"), McpxStatus.Internal, 0),
        ];

        foreach (var (exception, expected, socketError) in cases)
        {
            var client = Native.Register(new ThrowingTransport(() => exception));
            var err = Native.NewError();

            Assert.AreEqual((int)expected, ReadOne(client, &err), exception.GetType().Name);
            Assert.AreEqual((int)expected, err.Status);
            Assert.AreEqual(socketError, err.SocketError, exception.GetType().Name);
            StringAssert.StartsWith(Native.Message(err), exception.GetType().Name);
        }
    }

    [TestMethod]
    public void TestErrorStructIsOptional()
    {
        var client = Native.Register(new ThrowingTransport(() => new IOException("closed")));

        // err が NULL でも例外は漏れず、状態コードだけが返ること
        Assert.AreEqual((int)McpxStatus.Disconnected, ReadOne(client, null));

        // struct_size がヘッダ分だけなら、メッセージには書き込まないこと
        var err = Native.NewError();
        err.StructSize = 16;
        err.Message[0] = (byte)'x';
        Assert.AreEqual((int)McpxStatus.Disconnected, ReadOne(client, &err));
        Assert.AreEqual((int)McpxStatus.Disconnected, err.Status);
        Assert.AreEqual((byte)'x', err.Message[0]);

        // struct_size が 0 なら何も書き込まないこと
        var empty = default(NativeError);
        Assert.AreEqual((int)McpxStatus.Disconnected, ReadOne(client, &empty));
        Assert.AreEqual(0, empty.Status);
    }
}
