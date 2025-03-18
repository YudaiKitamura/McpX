﻿using McpXLib.Commands;
using McpXLib.Helpers;
using McpXLib.Interfaces;
using Moq;

namespace TestMcpX;

[TestClass]
public sealed class TestRemoteUnLockCommand
{
    private Mock<IPlc> plcMock;

    public TestRemoteUnLockCommand()
    {
        plcMock = new Mock<IPlc>();
        plcMock.SetupProperty(x => x.Route);
        plcMock.Object.Route = new RoutePacketHelper();
    }

    [TestMethod]
    public void TestToBytes()
    {
        var command = new RemoteUnlockCommand("pass");

        byte[] repuestPacketExpected = [
            0x50, 0x00,                     // Sub Header
            0x00, 0xFF, 0xFF, 0x03, 0x00,   // Route
            0x0C, 0x00,                     // Content Length
            0x00, 0x00,                     // Monitoring Timer
            0x30, 0x16, 0x00, 0x00,         // Command
            0x04, 0x00,                     // Password Length
            0x70, 0x61, 0x73, 0x73,         // Password ASCII
        ];

        CollectionAssert.AreEqual(repuestPacketExpected, command.ToBytes());
    }

    [TestMethod]
    public void TestExecute()
    {
        var command = new RemoteUnlockCommand("pass");

        byte[] recivePackets = [
            0xD0, 0x00,                     // Sub Header
            0x00, 0xFF, 0xFF, 0x03, 0x00,   // Route
            0x02, 0x00,                     // Content Length
            0x00, 0x00,                     // Error Code
        ];

        plcMock.Setup(x => x.Request(command.ToBytes())).Returns(recivePackets);

        Assert.AreEqual(true, command.Execute(plcMock.Object));
    }

    [TestMethod]
    public async Task TestExecuteAsync()
    {
        var command = new RemoteUnlockCommand("pass");

        byte[] recivePackets = [
            0xD0, 0x00,                     // Sub Header
            0x00, 0xFF, 0xFF, 0x03, 0x00,   // Route
            0x02, 0x00,                     // Content Length
            0x00, 0x00,                     // Error Code
        ];

        plcMock.Setup(x => x.RequestAsync(command.ToBytes())).ReturnsAsync(recivePackets);

        Assert.AreEqual(true, await command.ExecuteAsync(plcMock.Object));
    }
}
