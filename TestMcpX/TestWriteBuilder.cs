using McpXLib;
using McpXLib.Enums;

namespace TestMcpX;

[TestClass]
public sealed class TestWriteBuilder
{
    [TestMethod]
    public void TestPartitioningByValueShape()
    {
        var builder = new WriteBuilder();
        builder
            .Add(Prefix.D, "400", new short[] { 1, 2, 3 }) // batch
            .Add<short>(Prefix.D, "100", 1)                 // random word
            .Add(Prefix.M, "0", new[] { true, false })      // batch
            .Add<float>(Prefix.D, "200", 1.5f)              // random dword
            .Add(Prefix.M, "500", true);                    // random bit

        Assert.AreEqual(2, builder.batchEntries.Count);
        Assert.AreEqual(1, builder.random.wordDevices.Count);
        Assert.AreEqual((Prefix.D, "100", (ushort)1), builder.random.wordDevices[0]);
        Assert.AreEqual(1, builder.random.doubleWordDevices.Count);
        Assert.AreEqual(BitConverter.SingleToUInt32Bits(1.5f), builder.random.doubleWordDevices[0].value);
        Assert.AreEqual(1, builder.random.bitDevices.Count);
        Assert.AreEqual((Prefix.M, "500", true), builder.random.bitDevices[0]);
    }

    [TestMethod]
    public void TestUnsupportedRandomTypeThrows()
    {
        Assert.ThrowsException<NotSupportedException>(() => new WriteBuilder().Add<long>(Prefix.D, "0", 1L));
        Assert.ThrowsException<NotSupportedException>(() => new WriteBuilder().Add<double>(Prefix.D, "0", 1.0));
    }

    [TestMethod]
    public void TestBatchAcceptsAllTypes()
    {
        var builder = new WriteBuilder();
        builder
            .Add(Prefix.D, "0", new long[] { 1, 2 })
            .Add(Prefix.D, "8", new double[] { 1.0 });

        Assert.AreEqual(2, builder.batchEntries.Count);
        Assert.AreEqual(0, builder.random.wordDevices.Count);
        Assert.AreEqual(0, builder.random.doubleWordDevices.Count);
    }
}
