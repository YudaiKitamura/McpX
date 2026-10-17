using System.Text;
using McpXLib;
using McpXLib.Builders;
using McpXLib.Enums;

namespace TestMcpX;

/// <summary>
/// アクセス経路（McpXRoute）のテスト。
/// </summary>
[TestClass]
public sealed class TestRoute
{
    [TestMethod]
    public void TestDefaultRoute()
    {
        var route = McpXRoute.Default;
        Assert.AreEqual((byte)0x00, route.NetworkNo);
        Assert.AreEqual((byte)0xFF, route.PcNo);
        Assert.AreEqual((ushort)0x03FF, route.ModuleIoNo);
        Assert.AreEqual((byte)0x00, route.ModuleStationNo);

        var builder = new RoutePacketBuilder(route);
        CollectionAssert.AreEqual(new byte[] { 0x00, 0xFF, 0xFF, 0x03, 0x00 }, builder.ToBinaryBytes());
        Assert.AreEqual("00FF03FF00", Encoding.ASCII.GetString(builder.ToAsciiBytes()));
    }

    [TestMethod]
    public void TestOtherStationRoute()
    {
        var builder = new RoutePacketBuilder(new McpXRoute(networkNo: 0x01, pcNo: 0x02, moduleIoNo: 0x03E1, moduleStationNo: 0x05));

        // I/O番号は、バイナリは下位→上位、ASCII は上位→下位
        CollectionAssert.AreEqual(new byte[] { 0x01, 0x02, 0xE1, 0x03, 0x05 }, builder.ToBinaryBytes());
        Assert.AreEqual("010203E105", Encoding.ASCII.GetString(builder.ToAsciiBytes()));
    }

    [TestMethod]
    public void TestForCpu()
    {
        Assert.AreEqual((ushort)0x03E0, McpXRoute.ForCpu(1).ModuleIoNo);
        Assert.AreEqual((ushort)0x03E3, McpXRoute.ForCpu(4).ModuleIoNo);

        var route = McpXRoute.ForCpu(2, networkNo: 0x01, pcNo: 0x03);
        Assert.AreEqual((byte)0x01, route.NetworkNo);
        Assert.AreEqual((byte)0x03, route.PcNo);
        Assert.AreEqual((ushort)0x03E1, route.ModuleIoNo);

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => McpXRoute.ForCpu(0));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => McpXRoute.ForCpu(5));
    }

    [TestMethod]
    public void TestRequestUsesRoute()
    {
        var transport = new FakePlcTransport();
        var route = new McpXRoute(networkNo: 0x01, pcNo: 0x02, moduleIoNo: 0x03E1);
        using var mcpx = new McpX(transport, route: route);

        mcpx.Read<short>(Prefix.D, "0");

        // 3E フレーム（バイナリ）：サブヘッダ（2バイト）の後に経路（5バイト）
        Assert.AreSame(route, mcpx.Route);
        CollectionAssert.AreEqual(new byte[] { 0x01, 0x02, 0xE1, 0x03, 0x00 }, transport.LastPacket!.Skip(2).Take(5).ToArray());
    }

    [TestMethod]
    public void TestOptionsRouteNull()
    {
        Assert.ThrowsException<ArgumentNullException>(() => McpX.Connect(new McpXOptions { Host = "127.0.0.1", Port = 5000, Route = null! }));
    }
}
