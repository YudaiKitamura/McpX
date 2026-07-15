using McpXLib;
using McpXLib.Enums;

namespace TestMcpX;

[TestClass]
public sealed class TestRandomWriteBuilder
{
    [TestMethod]
    public void TestPartitioningAndReinterpretation()
    {
        var builder = new RandomWriteBuilder();
        builder
            .Add<short> (Prefix.D, "100", -1)          // word
            .Add<ushort>(Prefix.D, "101", 0x1234)      // word
            .Add<int>   (Prefix.D, "200", 0x0A0B0C0D)  // dword
            .Add<uint>  (Prefix.D, "202", 0xDEADBEEF)  // dword
            .Add<float> (Prefix.D, "204", 1.5f)        // dword
            .Add<bool>  (Prefix.M, "0",   true)        // bit
            .Add<bool>  (Prefix.Y, "2F",  false);      // bit

        // ---- word ----
        Assert.AreEqual(2, builder.wordDevices.Count);
        Assert.AreEqual((Prefix.D, "100", (ushort)0xFFFF), builder.wordDevices[0]);
        Assert.AreEqual((Prefix.D, "101", (ushort)0x1234), builder.wordDevices[1]);

        // ---- dword ----
        Assert.AreEqual(3, builder.doubleWordDevices.Count);
        Assert.AreEqual((Prefix.D, "200", (uint)0x0A0B0C0D), builder.doubleWordDevices[0]);
        Assert.AreEqual((Prefix.D, "202", (uint)0xDEADBEEF), builder.doubleWordDevices[1]);
        Assert.AreEqual((Prefix.D, "204", BitConverter.SingleToUInt32Bits(1.5f)), builder.doubleWordDevices[2]);

        // ---- bit ----
        Assert.AreEqual(2, builder.bitDevices.Count);
        Assert.AreEqual((Prefix.M, "0", true), builder.bitDevices[0]);
        Assert.AreEqual((Prefix.Y, "2F", false), builder.bitDevices[1]);
    }

    [TestMethod]
    public void TestUnsupportedTypeThrows()
    {
        Assert.ThrowsException<NotSupportedException>(() => new RandomWriteBuilder().Add<long>(Prefix.D, "0", 0L));
        Assert.ThrowsException<NotSupportedException>(() => new RandomWriteBuilder().Add<double>(Prefix.D, "0", 0d));
        Assert.ThrowsException<NotSupportedException>(() => new RandomWriteBuilder().Add<byte>(Prefix.D, "0", (byte)0));
    }
}
