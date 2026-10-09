using McpXLib.Enums;
using McpXNative;
using TestMcpX;

namespace TestMcpXInterop;

[TestClass]
public sealed unsafe class TestItems
{
    private static int Run(delegate*<ulong, NativeItem*, nuint, NativeError*, int> function, ulong client, ItemList list, out NativeError err)
    {
        var items = list.ToArray();
        err = Native.NewError();
        fixed (NativeItem* p = items)
        fixed (NativeError* e = &err)
        {
            return function(client, p, (nuint)items.Length, e);
        }
    }

    [TestMethod]
    public void TestReadItemsMixesRangeAndSingle()
    {
        var transport = new FakePlcTransport();
        transport.Bits.UnionWith([1u, 3u]);
        var client = Native.Register(transport);

        using var list = new ItemList()
            .Add(Prefix.D, "100", McpxType.I16, McpxItemKind.Range, 3)
            .Add(Prefix.D, "200", McpxType.I32, McpxItemKind.Single, 1)
            .Add(Prefix.M, "0", McpxType.Bool, McpxItemKind.Range, 4)
            .Add(Prefix.M, "5", McpxType.Bool, McpxItemKind.Single, 0);

        Assert.AreEqual((int)McpxStatus.Ok, Run(&Native.ReadItems, client, list, out var err), Native.Message(err));

        CollectionAssert.AreEqual(new short[] { 100, 101, 102 }, list.Values<short>(0));
        Assert.AreEqual((201 << 16) | 200, list.Values<int>(1)[0]);
        CollectionAssert.AreEqual(new byte[] { 0, 1, 0, 1 }, list.Values<byte>(2));
        // SINGLE のビットはワードで読み bit0 を使う（ダミー PLC の M5 のワード値は 5 → bit0 = 1）
        Assert.AreEqual((byte)1, list.Values<byte>(3)[0]);
    }

    [TestMethod]
    public void TestWriteItemsMixesRangeAndSingle()
    {
        var transport = new FakePlcTransport();
        var client = Native.Register(transport);

        using var list = new ItemList()
            .Add(Prefix.D, "10", McpxType.I16, McpxItemKind.Range, 2, [0x01, 0x00, 0x02, 0x00])
            .Add(Prefix.D, "20", McpxType.F32, McpxItemKind.Single, 1, BitConverter.GetBytes(1.5f));

        Assert.AreEqual((int)McpxStatus.Ok, Run(&Native.WriteItems, client, list, out var err), Native.Message(err));

        // 連続書き込み（1401）とランダム書き込み（1402）が送られること
        Assert.AreEqual(0x1401, transport.Requests[0].Command);
        Assert.AreEqual(10u, transport.Requests[0].DeviceNumber);
        CollectionAssert.AreEqual(new byte[] { 0x01, 0x00, 0x02, 0x00 }, transport.Requests[0].Data);
        Assert.AreEqual(0x1402, transport.Requests[1].Command);
    }

    [TestMethod]
    public void TestBlockReadAndWrite()
    {
        var transport = new FakePlcTransport();
        var client = Native.Register(transport);

        using var read = new ItemList()
            .Add(Prefix.D, "0", McpxType.I16, McpxItemKind.Range, 2)
            .Add(Prefix.D, "50", McpxType.U16, McpxItemKind.Range, 3);

        Assert.AreEqual((int)McpxStatus.Ok, Run(&Native.BlockRead, client, read, out var err), Native.Message(err));
        CollectionAssert.AreEqual(new short[] { 0, 1 }, read.Values<short>(0));
        CollectionAssert.AreEqual(new ushort[] { 50, 51, 52 }, read.Values<ushort>(1));
        Assert.AreEqual(1, transport.Requests.Count(r => r.Command == 0x0406));

        using var write = new ItemList()
            .Add(Prefix.D, "0", McpxType.I16, McpxItemKind.Range, 1, [0x05, 0x00]);
        Assert.AreEqual((int)McpxStatus.Ok, Run(&Native.BlockWrite, client, write, out err), Native.Message(err));
        Assert.AreEqual(1, transport.Requests.Count(r => r.Command == 0x1406));
    }

    [TestMethod]
    public void TestInvalidItems()
    {
        var client = Native.Register(new FakePlcTransport());
        var err = Native.NewError();

        // NULL・0件
        Assert.AreEqual((int)McpxStatus.InvalidArgument, Native.ReadItems(client, null, 1, &err));
        using (var empty = new ItemList())
        {
            Assert.AreEqual((int)McpxStatus.InvalidArgument, Run(&Native.ReadItems, client, empty, out _));
        }

        // 複数ブロックは RANGE のみ
        using (var single = new ItemList().Add(Prefix.D, "0", McpxType.I16, McpxItemKind.Single, 1))
        {
            Assert.AreEqual((int)McpxStatus.InvalidArgument, Run(&Native.BlockRead, client, single, out err));
            StringAssert.Contains(Native.Message(err), "items[0]");
        }

        // SINGLE の count は 0 または 1、kind は既知の値のみ
        using (var badCount = new ItemList().Add(Prefix.D, "0", McpxType.I16, McpxItemKind.Single, 2, size: 4))
        {
            Assert.AreEqual((int)McpxStatus.InvalidArgument, Run(&Native.ReadItems, client, badCount, out _));
        }
        using (var badKind = new ItemList().Add(Prefix.D, "0", McpxType.I16, (McpxItemKind)5, 1))
        {
            Assert.AreEqual((int)McpxStatus.InvalidArgument, Run(&Native.ReadItems, client, badKind, out _));
        }

        // バッファが小さい（2番目の項目）
        using (var small = new ItemList()
            .Add(Prefix.D, "0", McpxType.I16, McpxItemKind.Range, 1)
            .Add(Prefix.D, "10", McpxType.I16, McpxItemKind.Range, 5, size: 4))
        {
            Assert.AreEqual((int)McpxStatus.BufferTooSmall, Run(&Native.ReadItems, client, small, out err));
            StringAssert.Contains(Native.Message(err), "items[1]");
        }

        // ランダムアクセス（SINGLE）に使えない型は UNSUPPORTED
        using (var unsupported = new ItemList().Add(Prefix.D, "0", McpxType.I64, McpxItemKind.Single, 1))
        {
            Assert.AreEqual((int)McpxStatus.Unsupported, Run(&Native.ReadItems, client, unsupported, out err));
        }
    }
}
