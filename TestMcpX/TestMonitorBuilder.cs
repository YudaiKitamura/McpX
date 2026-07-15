using McpXLib;
using McpXLib.Enums;

namespace TestMcpX;

[TestClass]
public sealed class TestMonitorBuilder
{
    [TestMethod]
    public void TestPartitioningOrder()
    {
        var builder = new MonitorBuilder();
        builder
            .Add<short> (Prefix.D, "100", _ => { })  // word  -> wordEntries[0]
            .Add<bool>  (Prefix.M, "0",   _ => { })  // bit   -> wordEntries[1]
            .Add<int>   (Prefix.D, "200", _ => { })  // dword -> doubleWordEntries[0]
            .Add<ushort>(Prefix.D, "101", _ => { }); // word  -> wordEntries[2]

        Assert.AreEqual(3, builder.wordEntries.Count);
        Assert.AreEqual((Prefix.D, "100"), (builder.wordEntries[0].prefix, builder.wordEntries[0].address));
        Assert.AreEqual((Prefix.M, "0"), (builder.wordEntries[1].prefix, builder.wordEntries[1].address));
        Assert.AreEqual((Prefix.D, "101"), (builder.wordEntries[2].prefix, builder.wordEntries[2].address));

        Assert.AreEqual(1, builder.doubleWordEntries.Count);
        Assert.AreEqual((Prefix.D, "200"), (builder.doubleWordEntries[0].prefix, builder.doubleWordEntries[0].address));
    }

    [TestMethod]
    public void TestApplyReinterpretsRawValues()
    {
        short gotShort = 0;
        bool gotBitOn = false;
        int gotInt = 0;
        float gotFloat = 0f;

        var builder = new MonitorBuilder();
        builder
            .Add<short>(Prefix.D, "100", x => gotShort = x)  // wordEntries[0]
            .Add<bool> (Prefix.M, "0",   x => gotBitOn = x)  // wordEntries[1]
            .Add<int>  (Prefix.D, "200", x => gotInt = x)    // doubleWordEntries[0]
            .Add<float>(Prefix.D, "204", x => gotFloat = x); // doubleWordEntries[1]

        builder.wordEntries[0].apply(0xFFFF);   // short: -1
        builder.wordEntries[1].apply(0x0001);   // bit0 = 1 -> ON
        builder.doubleWordEntries[0].apply(unchecked((uint)-1));                   // int: -1
        builder.doubleWordEntries[1].apply(BitConverter.SingleToUInt32Bits(2.5f)); // float: 2.5

        Assert.AreEqual((short)-1, gotShort);
        Assert.IsTrue(gotBitOn);
        Assert.AreEqual(-1, gotInt);
        Assert.AreEqual(2.5f, gotFloat);
    }

    [TestMethod]
    public void TestUnsupportedTypeThrows()
    {
        Assert.ThrowsException<NotSupportedException>(() => new MonitorBuilder().Add<long>(Prefix.D, "0", _ => { }));
        Assert.ThrowsException<NotSupportedException>(() => new MonitorBuilder().Add<double>(Prefix.D, "0", _ => { }));
    }
}
