using McpXLib;
using McpXLib.Enums;

namespace TestMcpX;

[TestClass]
public sealed class TestMonitoringTimer
{
    // 要求パケットの監視タイマ（50 00 | NW PC IO(2) ST | 長さ(2) | 監視タイマ(2) ...）
    private static ushort SentMonitoringTimer(ushort timeoutMilliseconds)
    {
        var transport = new FakePlcTransport();
        using var mcpx = new McpX(transport, timeoutMilliseconds: timeoutMilliseconds);

        mcpx.Read<short>(Prefix.D, "0");

        return transport.LastPacket is { } packet ? BitConverter.ToUInt16(packet, 9) : throw new AssertFailedException("no request");
    }

    [TestMethod]
    [DataRow((ushort)5000, (ushort)19)] // 4750ms
    [DataRow((ushort)1000, (ushort)3)]  // 750ms
    [DataRow((ushort)749, (ushort)1)]   // 250ms
    [DataRow((ushort)500, (ushort)1)]   // 250ms
    [DataRow((ushort)499, (ushort)0)]   // PLC 側は無限待ち
    [DataRow((ushort)100, (ushort)0)]   // 以前は ArgumentOutOfRangeException
    [DataRow((ushort)0, (ushort)0)]
    public void TestMonitoringTimerIsShorterThanTimeout(ushort timeoutMilliseconds, ushort expectedUnits)
    {
        Assert.AreEqual(expectedUnits, SentMonitoringTimer(timeoutMilliseconds));
    }
}
