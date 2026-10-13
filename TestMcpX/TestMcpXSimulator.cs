using System.Net;
using System.Net.Sockets;
using McpXLib;
using McpXLib.Enums;

namespace TestMcpX;

[TestClass]
public sealed class TestMcpXSimulator
{
    [TestMethod]
    public void TestGetPort()
    {
        Assert.AreEqual(5511, McpXSimulator.GetPort());
        Assert.AreEqual(5511, McpXSimulator.GetPort(1, 1));
        Assert.AreEqual(5512, McpXSimulator.GetPort(1, 2));
        Assert.AreEqual(5514, McpXSimulator.GetPort(1, 4));
        Assert.AreEqual(5521, McpXSimulator.GetPort(systemNo: 2));
    }

    [TestMethod]
    public void TestGetPortOutOfRange()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => McpXSimulator.GetPort(0, 1));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => McpXSimulator.GetPort(1, 0));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => McpXSimulator.GetPort(1, 5));

        // ポート番号が 65535 を超えるシステムNo.（int の桁あふれも含む）
        Assert.AreEqual(65533, McpXSimulator.GetPort(6003, 3));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => McpXSimulator.GetPort(6004, 1));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => McpXSimulator.GetPort(int.MaxValue, 1));
    }

    [TestMethod]
    public void TestConnect()
    {
        // システム9・号機4 = 5594 で待ち受け、接続できること
        var listener = new TcpListener(IPAddress.Loopback, McpXSimulator.GetPort(9, 4));
        listener.Start();
        try
        {
            using var simulator = McpXSimulator.Connect(systemNo: 9, cpuNo: 4);

            Assert.AreEqual(ProcessorSeries.iQR, simulator.ProcessorSeries);
            Assert.AreEqual(RequestFrame.E3, simulator.RequestFrame);
            Assert.IsFalse(simulator.IsAscii);

            Assert.IsFalse(simulator.UseMultiBlockAccess);

            var options = McpXSimulator.CreateOptions(9, 4);
            options.RequestFrame = RequestFrame.E4;
            options.ProcessorSeries = ProcessorSeries.Q;
            options.UseMultiBlockAccess = true;
            using var e4 = McpX.Connect(options);
            Assert.AreEqual(RequestFrame.E4, e4.RequestFrame);
            Assert.AreEqual(ProcessorSeries.Q, e4.ProcessorSeries);
            Assert.IsTrue(e4.UseMultiBlockAccess);
        }
        finally
        {
            listener.Stop();
        }
    }

    [TestMethod]
    public void TestConnectOutOfRange()
    {
        // 範囲外は接続前に例外（接続タイムアウトを待たない）
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => McpXSimulator.Connect(cpuNo: 5));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => McpXSimulator.CreateOptions(systemNo: 0));
    }
}
