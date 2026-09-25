using McpXLib;
using McpXLib.Enums;

namespace TestMcpX;

[TestClass]
public sealed class TestMcpXBatchAccess
{
    private const ushort READ = 0x0401;
    private const ushort WRITE = 0x1401;

    [TestMethod]
    public void TestBatchReadBitDeviceAsWordsAdvancesBy16PerWord()
    {
        var transport = new FakePlcTransport();
        using var mcpx = new McpX(transport);

        var values = mcpx.BatchRead<ushort>(Prefix.M, "0", 1000);

        Assert.AreEqual(1000, values.Length);
        Assert.AreEqual(2, transport.Requests.Count);
        Assert.AreEqual((0u, (ushort)960), (transport.Requests[0].DeviceNumber, transport.Requests[0].Points));
        Assert.AreEqual((960u * 16, (ushort)40), (transport.Requests[1].DeviceNumber, transport.Requests[1].Points));
    }

    [TestMethod]
    public async Task TestBatchReadAsyncHexBitDeviceAsWordsAdvancesBy16PerWord()
    {
        var transport = new FakePlcTransport();
        using var mcpx = new McpX(transport);

        await mcpx.BatchReadAsync<short>(Prefix.X, "0", 1000);

        Assert.AreEqual(2, transport.Requests.Count);
        Assert.AreEqual(0x3C00u, transport.Requests[1].DeviceNumber);
    }

    [TestMethod]
    public void TestBatchReadWordDeviceAdvancesByWord()
    {
        var transport = new FakePlcTransport();
        using var mcpx = new McpX(transport);

        mcpx.BatchRead<short>(Prefix.D, "0", 1000);

        Assert.AreEqual(960u, transport.Requests[1].DeviceNumber);
    }

    [TestMethod]
    public async Task TestBatchWriteBitDeviceAsWordsAdvancesBy16PerWord()
    {
        var transport = new FakePlcTransport();
        using var mcpx = new McpX(transport);

        mcpx.BatchWrite(Prefix.M, "0", new short[1000]);
        await mcpx.BatchWriteAsync(Prefix.M, "0", new short[1000]);

        Assert.AreEqual(4, transport.Requests.Count);
        Assert.IsTrue(transport.Requests.All(r => r.Command == WRITE));
        Assert.AreEqual((960u * 16, (ushort)40), (transport.Requests[1].DeviceNumber, transport.Requests[1].Points));
        Assert.AreEqual((960u * 16, (ushort)40), (transport.Requests[3].DeviceNumber, transport.Requests[3].Points));
    }

    [TestMethod]
    public void TestBatchReadOver65535WordsDoesNotOverflow()
    {
        var transport = new FakePlcTransport();
        using var mcpx = new McpX(transport);

        var values = mcpx.BatchRead<int>(Prefix.D, "0", 40000);

        Assert.AreEqual(40000, values.Length);
        Assert.AreEqual(80000, transport.Requests.Sum(r => r.Points));

        // 各 int は (D[2i+1] << 16) | D[2i]。分割をまたいでも順序が保たれること。
        for (int i = 0; i < values.Length; i += 997)
        {
            uint lo = (uint)(2 * i) & 0xFFFF;
            uint hi = (uint)(2 * i + 1) & 0xFFFF;
            Assert.AreEqual(unchecked((int)(hi << 16 | lo)), values[i]);
        }
    }

    [TestMethod]
    public void TestBatchWriteOver65535WordsDoesNotOverflow()
    {
        var transport = new FakePlcTransport();
        using var mcpx = new McpX(transport);

        mcpx.BatchWrite(Prefix.D, "0", new short[70000]);

        Assert.AreEqual(70000, transport.Requests.Sum(r => r.Points));
    }

    [TestMethod]
    public async Task TestReadRequestsOneElementOfWords()
    {
        var transport = new FakePlcTransport();
        using var mcpx = new McpX(transport);

        mcpx.Read<short>(Prefix.D, "0");
        mcpx.Read<int>(Prefix.D, "0");
        mcpx.Read<long>(Prefix.D, "0");
        await mcpx.ReadAsync<double>(Prefix.D, "0");

        CollectionAssert.AreEqual(
            new ushort[] { 1, 2, 4, 4 },
            transport.Requests.Select(r => r.Points).ToArray()
        );
        Assert.IsTrue(transport.Requests.All(r => r.Command == READ));
    }

    [TestMethod]
    public void TestByteAndSByteUseOneWordPerElement()
    {
        var transport = new FakePlcTransport();
        transport.Words[10] = 0x12AB;
        transport.Words[11] = 0x00FF;
        using var mcpx = new McpX(transport);

        mcpx.Write<byte>(Prefix.D, "0", 0xAB);
        mcpx.Write<sbyte>(Prefix.D, "1", -1);
        mcpx.BatchWrite(Prefix.D, "2", new byte[] { 1, 2, 3 });

        Assert.AreEqual((ushort)1, transport.Requests[0].Points);
        CollectionAssert.AreEqual(new byte[] { 0xAB, 0x00 }, transport.Requests[0].Data);
        Assert.AreEqual((ushort)1, transport.Requests[1].Points);
        CollectionAssert.AreEqual(new byte[] { 0xFF, 0xFF }, transport.Requests[1].Data);
        Assert.AreEqual((ushort)3, transport.Requests[2].Points);
        CollectionAssert.AreEqual(new byte[] { 1, 0, 2, 0, 3, 0 }, transport.Requests[2].Data);

        CollectionAssert.AreEqual(new byte[] { 0xAB, 0xFF }, mcpx.BatchRead<byte>(Prefix.D, "10", 2));
        Assert.AreEqual((sbyte)-1, mcpx.ReadSByte(Prefix.D, "11"));
        Assert.AreEqual((byte)0xAB, mcpx.ReadByte(Prefix.D, "10"));
    }

    [TestMethod]
    public async Task TestReadStringFromWords()
    {
        var transport = new FakePlcTransport();
        transport.Words[0] = 0x4241; // "AB"
        transport.Words[1] = 0x0043; // "C\0"
        using var mcpx = new McpX(transport);

        Assert.AreEqual("ABC", mcpx.ReadString(Prefix.D, "0", 2));
        Assert.AreEqual("ABC", await mcpx.ReadStringAsync(Prefix.D, "0", 2));
    }
}
