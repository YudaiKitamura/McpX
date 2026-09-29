using McpXLib;
using McpXNative;

namespace TestMcpXInterop;

[TestClass]
public sealed unsafe class TestLibrary
{
    [TestMethod]
    public void TestAbiVersion()
    {
        Assert.AreEqual((1u << 16) | 0u, Native.AbiVersion());
    }

    [TestMethod]
    public void TestVersionMatchesMcpX()
    {
        Assert.AreEqual(typeof(McpX).Assembly.GetName().Version!.ToString(3), Native.Version());
    }

    [TestMethod]
    public void TestStructSizesMatchHeader()
    {
        // include/mcpx.h の定義（64bit）と一致すること
        Assert.AreEqual(528u, Native.StructSize(1));
        Assert.AreEqual(40u, Native.StructSize(2));
        Assert.AreEqual(32u, Native.StructSize(3));
        Assert.AreEqual(0u, Native.StructSize(99));
    }

    [TestMethod]
    public void TestConnectOptionsInit()
    {
        var options = new NativeConnectOptions { Port = 123, IsUdp = 1 };
        Native.ConnectOptionsInit(&options);

        Assert.AreEqual((uint)sizeof(NativeConnectOptions), options.StructSize);
        Assert.AreEqual(5000u, options.TimeoutMs);
        Assert.AreEqual(0, options.Port);
        Assert.AreEqual((byte)0, options.IsUdp);
        Assert.AreEqual((byte)0, options.Series);
    }

    [TestMethod]
    public void TestSimulatorOptionsInit()
    {
        var options = default(NativeSimulatorOptions);
        Native.SimulatorOptionsInit(&options);

        Assert.AreEqual((uint)sizeof(NativeSimulatorOptions), options.StructSize);
        Assert.AreEqual(1, options.SystemNo);
        Assert.AreEqual(1, options.CpuNo);
        Assert.AreEqual(5000u, options.TimeoutMs);
        Assert.AreEqual((byte)1, options.Series);
    }

    [TestMethod]
    public void TestUtf8TruncationKeepsCharacterBoundary()
    {
        // 3バイト文字を容量 10（NUL 込み）に書くと、3文字（9バイト）で止まること
        var buffer = new byte[10];
        fixed (byte* p = buffer)
        {
            Utf8.WriteTruncated("あいうえお", p, buffer.Length);
        }

        Assert.AreEqual("あいう", System.Text.Encoding.UTF8.GetString(buffer, 0, Array.IndexOf(buffer, (byte)0)));
    }
}
