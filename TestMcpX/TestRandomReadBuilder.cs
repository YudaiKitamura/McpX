using McpXLib;
using McpXLib.Enums;

namespace TestMcpX;

[TestClass]
public sealed class TestRandomReadBuilder
{
    [TestMethod]
    public void TestPartitioningOrder()
    {
        var builder = new RandomReadBuilder();
        builder
            .Add<short>(Prefix.D, "100", _ => { })   // word  -> wordEntries[0]
            .Add<bool> (Prefix.M, "0",   _ => { })   // bit   -> wordEntries[1]
            .Add<int>  (Prefix.D, "200", _ => { })   // dword -> doubleWordEntries[0]
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
        ushort gotUShort = 0;
        bool gotBitOn = false;
        bool gotBitOff = true;
        int gotInt = 0;
        float gotFloat = 0f;

        var builder = new RandomReadBuilder();
        builder
            .Add<short> (Prefix.D, "100", x => gotShort = x)   // wordEntries[0]
            .Add<ushort>(Prefix.D, "101", x => gotUShort = x)  // wordEntries[1]
            .Add<bool>  (Prefix.M, "0",   x => gotBitOn = x)   // wordEntries[2]
            .Add<bool>  (Prefix.M, "1",   x => gotBitOff = x)  // wordEntries[3]
            .Add<int>   (Prefix.D, "200", x => gotInt = x)     // doubleWordEntries[0]
            .Add<float> (Prefix.D, "204", x => gotFloat = x);  // doubleWordEntries[1]

        // ---- word entries (raw is the 16-bit word) ----
        builder.wordEntries[0].apply(0xFFFF);            // short: -1
        builder.wordEntries[1].apply(0x1234);            // ushort: 0x1234
        builder.wordEntries[2].apply(0x0001);            // bit0 = 1 -> ON
        builder.wordEntries[3].apply(0x0002);            // bit0 = 0 -> OFF (only bit0 matters)

        Assert.AreEqual((short)-1, gotShort);
        Assert.AreEqual((ushort)0x1234, gotUShort);
        Assert.IsTrue(gotBitOn);
        Assert.IsFalse(gotBitOff);

        // ---- dword entries (raw is the 32-bit value) ----
        builder.doubleWordEntries[0].apply(unchecked((uint)-1));               // int: -1
        builder.doubleWordEntries[1].apply(BitConverter.SingleToUInt32Bits(2.5f)); // float: 2.5

        Assert.AreEqual(-1, gotInt);
        Assert.AreEqual(2.5f, gotFloat);
    }

    [TestMethod]
    public void TestUnsupportedTypeThrows()
    {
        Assert.ThrowsException<NotSupportedException>(() => new RandomReadBuilder().Add<long>(Prefix.D, "0", _ => { }));
        Assert.ThrowsException<NotSupportedException>(() => new RandomReadBuilder().Add<double>(Prefix.D, "0", _ => { }));
    }
}
