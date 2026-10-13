using McpXLib.Builders;
using McpXLib.Commands;
using McpXLib.Interfaces;
using Moq;

namespace TestMcpX;

[TestClass]
public sealed class TestRemoteUnlockAsciiCommand
{
    private Mock<IPlc> plcMock;

    public TestRemoteUnlockAsciiCommand()
    {
        plcMock = new Mock<IPlc>();
        plcMock.SetupGet(x => x.Route).Returns(new RoutePacketBuilder());
        plcMock.SetupGet(x => x.IsAscii).Returns(true);
    }

    [TestMethod]
    public void TestToBytes()
    {
        var command = new RemoteUnlockCommand("pass");

        byte[] requestPacketExpected = [
            0x30, 0x30, 0x31, 0x34,                                     // Content Length
            0x30, 0x30, 0x30, 0x30,                                     // Monitoring Timer
            0x31, 0x36, 0x33, 0x30,                                     // Command
            0x30, 0x30, 0x30, 0x30,                                     // SubCommand
            0x30, 0x30, 0x30, 0x34,                                     // Password Length
            0x70, 0x61, 0x73, 0x73,                                     // Password ASCII
        ]; 

        CollectionAssert.AreEqual(requestPacketExpected, command.ToAsciiBytes());
    }

    [TestMethod]
    public void TestExecute()
    {
        var command = new RemoteUnlockCommand("pass");

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
        var command = new RemoteUnlockCommand("pass");

        byte[] recivePackets = [
            0x44, 0x30, 0x30, 0x30,                                     // Sub Header
            0x30, 0x30, 0x46, 0x46, 0x30, 0x33, 0x46, 0x46, 0x30, 0x30, // Route
            0x30, 0x30, 0x30, 0x34,                                     // Content Length
            0x30, 0x30, 0x30, 0x30                                      // Error Code
        ];

        plcMock.Setup(x => x.RequestAsync(It.IsAny<byte[]>(), It.IsAny<IReceiveLengthParser>(), It.IsAny<CancellationToken>())).ReturnsAsync(recivePackets);

        Assert.AreEqual(true, await command.ExecuteAsync(plcMock.Object, CancellationToken.None));
    }
}
