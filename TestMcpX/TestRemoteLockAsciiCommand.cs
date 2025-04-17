using McpXLib.Builders;
using McpXLib.Commands;
using McpXLib.Interfaces;
using Moq;

namespace TestMcpX;

[TestClass]
public sealed class TestRemoteLockAsciiCommand
{
    private Mock<IPlc> plcMock;

    public TestRemoteLockAsciiCommand()
    {
        plcMock = new Mock<IPlc>();
        plcMock.SetupProperty(x => x.Route);
        plcMock.SetupProperty(x => x.IsAscii);
        plcMock.Object.Route = new RoutePacketBuilder();
        plcMock.Object.IsAscii = true;
    }

    [TestMethod]
    public void TestToBytes()
    {
        var command = new RemoteLockCommand("pass");

        byte[] repuestPacketExpected = [
            0x30, 0x30, 0x31, 0x34,                                     // Content Length
            0x30, 0x30, 0x30, 0x30,                                     // Monitoring Timer
            0x31, 0x36, 0x33, 0x31,                                     // Command
            0x30, 0x30, 0x30, 0x30,                                     // SubCommand
            0x30, 0x30, 0x30, 0x34,                                     // Password Length
            0x70, 0x61, 0x73, 0x73,                                     // Password ASCII
        ]; 

        CollectionAssert.AreEqual(repuestPacketExpected, command.ToAsciiBytes());
    }

    [TestMethod]
    public void TestExecute()
    {
        var command = new RemoteLockCommand("pass");

        byte[] recivePackets = [
            0x44, 0x30, 0x30, 0x30,                                     // Sub Header
            0x30, 0x30, 0x46, 0x46, 0x30, 0x33, 0x46, 0x46, 0x30, 0x30, // Route
            0x30, 0x30, 0x30, 0x34,                                     // Content Length
            0x30, 0x30, 0x30, 0x30                                      // Error Code
        ];

        plcMock.Setup(x => x.Request(It.IsAny<byte[]>(), It.IsAny<IReceiveLengthParser>())).Returns(recivePackets);

        Assert.AreEqual(true, command.Execute(plcMock.Object));
    }

    [TestMethod]
    public async Task TestExecuteAsync()
    {
        var command = new RemoteLockCommand("pass");

        byte[] recivePackets = [
            0x44, 0x30, 0x30, 0x30,                                     // Sub Header
            0x30, 0x30, 0x46, 0x46, 0x30, 0x33, 0x46, 0x46, 0x30, 0x30, // Route
            0x30, 0x30, 0x30, 0x34,                                     // Content Length
            0x30, 0x30, 0x30, 0x30                                      // Error Code
        ];

        plcMock.Setup(x => x.RequestAsync(It.IsAny<byte[]>(), It.IsAny<IReceiveLengthParser>())).ReturnsAsync(recivePackets);

        Assert.AreEqual(true, await command.ExecuteAsync(plcMock.Object));
    }
}
