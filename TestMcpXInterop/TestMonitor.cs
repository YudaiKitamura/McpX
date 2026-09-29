using McpXLib.Enums;
using McpXNative;
using TestMcpX;

namespace TestMcpXInterop;

[TestClass]
public sealed unsafe class TestMonitor
{
    private static int Register(ulong client, ItemList list, out ulong session, out NativeError err)
    {
        var items = list.ToArray();
        err = Native.NewError();
        ulong handle = 0;
        int status;
        fixed (NativeItem* p = items)
        fixed (NativeError* e = &err)
        {
            status = Native.MonitorRegister(client, p, (nuint)items.Length, &handle, e);
        }
        session = handle;
        return status;
    }

    private static int Read(ulong session, ItemList list, out NativeError err)
    {
        var items = list.ToArray();
        err = Native.NewError();
        fixed (NativeItem* p = items)
        fixed (NativeError* e = &err)
        {
            return Native.MonitorRead(session, p, (nuint)items.Length, e);
        }
    }

    private static ItemList Items() => new ItemList()
        .Add(Prefix.D, "10", McpxType.I16, McpxItemKind.Single, 1)
        .Add(Prefix.D, "20", McpxType.I32, McpxItemKind.Single, 1)
        .Add(Prefix.M, "0", McpxType.Bool, McpxItemKind.Single, 1);

    [TestMethod]
    public void TestRegisterAndReadRepeatedly()
    {
        var transport = new FakePlcTransport();
        transport.Words[10] = 123;
        transport.Words[20] = 0x5678;
        transport.Words[21] = 0x1234;
        transport.Words[0] = 1;
        var client = Native.Register(transport);

        using var list = Items();
        Assert.AreEqual((int)McpxStatus.Ok, Register(client, list, out var session, out var err), Native.Message(err));
        Assert.AreNotEqual(0UL, session);

        Assert.AreEqual((int)McpxStatus.Ok, Read(session, list, out err), Native.Message(err));
        Assert.AreEqual((short)123, list.Values<short>(0)[0]);
        Assert.AreEqual(0x12345678, list.Values<int>(1)[0]);
        Assert.AreEqual((byte)1, list.Values<byte>(2)[0]);

        // 登録し直さずに、最新の値を繰り返し読めること
        transport.Words[10] = 456;
        transport.Words[0] = 0;
        Assert.AreEqual((int)McpxStatus.Ok, Read(session, list, out err), Native.Message(err));
        Assert.AreEqual((short)456, list.Values<short>(0)[0]);
        Assert.AreEqual((byte)0, list.Values<byte>(2)[0]);

        // モニタ（0802）は登録（0801）の後に送られ、再登録はしない
        Assert.AreEqual(1, transport.Requests.Count(r => r.Command == 0x0801));
        Assert.AreEqual(2, transport.Requests.Count(r => r.Command == 0x0802));
    }

    [TestMethod]
    public void TestStaleSessionIsInvalidOperation()
    {
        var client = Native.Register(new FakePlcTransport());
        using var list = Items();

        Assert.AreEqual((int)McpxStatus.Ok, Register(client, list, out var first, out _));
        Assert.AreEqual((int)McpxStatus.Ok, Register(client, list, out var second, out _));

        // 別の登録で置き換わった古いセッションは使えないこと
        Assert.AreEqual((int)McpxStatus.InvalidOperation, Read(first, list, out var err));
        StringAssert.StartsWith(Native.Message(err), "InvalidOperationException");
        Assert.AreEqual((int)McpxStatus.Ok, Read(second, list, out _));
    }

    [TestMethod]
    public void TestFreeAndCloseInvalidateSession()
    {
        var client = Native.Register(new FakePlcTransport());
        using var list = Items();
        var err = Native.NewError();

        Assert.AreEqual((int)McpxStatus.Ok, Register(client, list, out var freed, out _));
        Assert.AreEqual((int)McpxStatus.Ok, Native.SessionFree(0, &err));
        Assert.AreEqual((int)McpxStatus.Ok, Native.SessionFree(freed, &err));
        Assert.AreEqual((int)McpxStatus.InvalidHandle, Native.SessionFree(freed, &err));
        Assert.AreEqual((int)McpxStatus.InvalidHandle, Read(freed, list, out _));

        // クライアントを close すると、所有するセッションも解放されること
        Assert.AreEqual((int)McpxStatus.Ok, Register(client, list, out var owned, out _));
        Assert.AreEqual((int)McpxStatus.Ok, Native.Close(client, &err));
        Assert.AreEqual((int)McpxStatus.InvalidHandle, Read(owned, list, out _));
        Assert.AreEqual((int)McpxStatus.InvalidHandle, Native.SessionFree(owned, &err));
    }

    [TestMethod]
    public void TestInvalidMonitorArguments()
    {
        var client = Native.Register(new FakePlcTransport());
        using var list = Items();
        Assert.AreEqual((int)McpxStatus.Ok, Register(client, list, out var session, out _));

        // 登録時と数・型が違う
        using (var fewer = new ItemList().Add(Prefix.D, "10", McpxType.I16, McpxItemKind.Single, 1))
        {
            Assert.AreEqual((int)McpxStatus.InvalidArgument, Read(session, fewer, out _));
        }
        using (var wrongType = new ItemList()
            .Add(Prefix.D, "10", McpxType.U16, McpxItemKind.Single, 1)
            .Add(Prefix.D, "20", McpxType.I32, McpxItemKind.Single, 1)
            .Add(Prefix.M, "0", McpxType.Bool, McpxItemKind.Single, 1))
        {
            Assert.AreEqual((int)McpxStatus.InvalidArgument, Read(session, wrongType, out var err));
            StringAssert.Contains(Native.Message(err), "items[0]");
        }

        // モニタは SINGLE のみ、型は bool / i16 / u16 / i32 / u32 / f32 のみ
        using (var range = new ItemList().Add(Prefix.D, "0", McpxType.I16, McpxItemKind.Range, 2))
        {
            Assert.AreEqual((int)McpxStatus.InvalidArgument, Register(client, range, out _, out _));
        }
        using (var longType = new ItemList().Add(Prefix.D, "0", McpxType.I64, McpxItemKind.Single, 1))
        {
            Assert.AreEqual((int)McpxStatus.Unsupported, Register(client, longType, out var none, out _));
            Assert.AreEqual(0UL, none);
        }
    }
}
