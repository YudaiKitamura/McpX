using McpXLib.Commands;
using McpXLib.Enums;
using McpXLib.Helpers;
using McpXLib.Interfaces;
using Moq;
using Bogus;
using McpXLib.Builders;

namespace TestMcpX;

[TestClass]
public sealed class TestMonitorAsciiCommand
{
    private readonly Faker faker;
    private Mock<IPlc> plcMock;

    public TestMonitorAsciiCommand()
    {
        faker = new Faker();
        plcMock = new Mock<IPlc>();
        plcMock.SetupProperty(x => x.Route);
        plcMock.SetupProperty(x => x.IsAscii);
        plcMock.Object.Route = new RoutePacketBuilder();
        plcMock.Object.IsAscii = true;
    }

    [TestMethod]
    public void TestToBytes()
    {
        var command = new MonitorCommand<short, int>([(Prefix.D, "0"),(Prefix.D, "1")],[(Prefix.D, "2"),(Prefix.D, "4")]);

        byte[] repuestPacketExpected = [
            0x30, 0x30, 0x30, 0x43,                                     // Content Length
            0x30, 0x30, 0x30, 0x30,                                     // Monitoring Timer
            0x30, 0x38, 0x30, 0x32,                                     // Command
            0x30, 0x30, 0x30, 0x30,                                     // SubCommand
        ];

        CollectionAssert.AreEqual(repuestPacketExpected, command.ToAsciiBytes());
    }

    [TestMethod]
    public void TestExecute()
    {
        var command = new MonitorCommand<short, int>([(Prefix.D, "0"),(Prefix.D, "1")],[(Prefix.D, "2"),(Prefix.D, "4")]);

        byte[] recivePackets = [
            0x44, 0x30, 0x30, 0x30,                                     // Sub Header
            0x30, 0x30, 0x46, 0x46, 0x30, 0x33, 0x46, 0x46, 0x30, 0x30, // Route
            0x30, 0x30, 0x31, 0x43,                                     // Content Length
            0x30, 0x30, 0x30, 0x30,                                     // Error Code
            0x30, 0x30, 0x30, 0x31,                                     // Value1
            0x30, 0x30, 0x30, 0x32,                                     // Value2
            0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x33,             // Value3
            0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x34,             // Value4
        ];

        plcMock.Setup(x => x.Request(It.IsAny<byte[]>())).Returns(recivePackets);

        short[] wordDeviceExpected = [
            1,
            2,
        ];

        CollectionAssert.AreEqual(wordDeviceExpected, command.Execute(plcMock.Object).wordValues);

        int[] doubleDeviceExpected = [
            3,
            4,
        ];

        CollectionAssert.AreEqual(doubleDeviceExpected, command.Execute(plcMock.Object).doubleValues);
    }

    [TestMethod]
    public async Task TestExecuteAsync()
    {
        var command = new MonitorCommand<short, int>([(Prefix.D, "0"),(Prefix.D, "1")],[(Prefix.D, "2"),(Prefix.D, "4")]);

        byte[] recivePackets = [
            0x44, 0x30, 0x30, 0x30,                                     // Sub Header
            0x30, 0x30, 0x46, 0x46, 0x30, 0x33, 0x46, 0x46, 0x30, 0x30, // Route
            0x30, 0x30, 0x31, 0x43,                                     // Content Length
            0x30, 0x30, 0x30, 0x30,                                     // Error Code
            0x30, 0x30, 0x30, 0x31,                                     // Value1
            0x30, 0x30, 0x30, 0x32,                                     // Value2
            0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x33,             // Value3
            0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x34,             // Value4
        ];

        plcMock.Setup(x => x.RequestAsync(It.IsAny<byte[]>())).ReturnsAsync(recivePackets);

        short[] wordDeviceExpected = [
            1,
            2,
        ];

        CollectionAssert.AreEqual(wordDeviceExpected, (await command.ExecuteAsync(plcMock.Object)).wordValues);

        int[] doubleDeviceExpected = [
            3,
            4,
        ];

        CollectionAssert.AreEqual(doubleDeviceExpected, (await command.ExecuteAsync(plcMock.Object)).doubleValues);
    }
}
