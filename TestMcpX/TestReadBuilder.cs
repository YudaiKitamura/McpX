using McpXLib;
using McpXLib.Enums;

namespace TestMcpX;

[TestClass]
public sealed class TestReadBuilder
{
    [TestMethod]
    public void TestPartitioningByLength()
    {
        var builder = new ReadBuilder();
        builder
            .Add<short>(Prefix.D, "400", 100, _ => { }) // batch
            .Add<short>(Prefix.D, "100", _ => { })      // random word
            .Add<bool> (Prefix.M, "0", 64, _ => { })    // batch
            .Add<bool> (Prefix.M, "500", _ => { })      // random word (bit0)
            .Add<int>  (Prefix.D, "200", _ => { });     // random dword

        Assert.AreEqual(2, builder.batchEntries.Count);
        Assert.AreEqual(2, builder.random.wordEntries.Count);
        Assert.AreEqual((Prefix.D, "100"), (builder.random.wordEntries[0].prefix, builder.random.wordEntries[0].address));
        Assert.AreEqual((Prefix.M, "500"), (builder.random.wordEntries[1].prefix, builder.random.wordEntries[1].address));
        Assert.AreEqual(1, builder.random.doubleWordEntries.Count);
        Assert.AreEqual((Prefix.D, "200"), (builder.random.doubleWordEntries[0].prefix, builder.random.doubleWordEntries[0].address));
    }

    [TestMethod]
    public void TestUnsupportedRandomTypeThrows()
    {
        Assert.ThrowsException<NotSupportedException>(() => new ReadBuilder().Add<long>(Prefix.D, "0", _ => { }));
        Assert.ThrowsException<NotSupportedException>(() => new ReadBuilder().Add<double>(Prefix.D, "0", _ => { }));
    }

    [TestMethod]
    public void TestBatchAcceptsAllTypes()
    {
        var builder = new ReadBuilder();
        builder
            .Add<long>  (Prefix.D, "0", 2, _ => { })
            .Add<double>(Prefix.D, "8", 2, _ => { });

        Assert.AreEqual(2, builder.batchEntries.Count);
        Assert.AreEqual(0, builder.random.wordEntries.Count);
        Assert.AreEqual(0, builder.random.doubleWordEntries.Count);
    }
}
