﻿using McpXLib.Commands;
using McpXLib.Enums;
using McpXLib.Interfaces;
using Moq;
using Bogus;
using McpXLib.Builders;

namespace TestMcpX;

[TestClass]
public sealed class TestBitBatchWriteCommand
{
    private readonly Faker faker;
    private Mock<IPlc> plcMock;

    public TestBitBatchWriteCommand()
    {
        faker = new Faker();
        plcMock = new Mock<IPlc>();
        plcMock.SetupProperty(x => x.Route);
        plcMock.Object.Route = new RoutePacketBuilder();
    }

    [TestMethod]
    public void TestToBytes()
    {
        var command = new BitBatchWriteCommand(Prefix.M, "0", [true, false, true, true]);

        byte[] repuestPacketExpected = [
            0x0E, 0x00,                     // Content Length
            0x00, 0x00,                     // Monitoring Timer
            0x01, 0x14, 0x01, 0x00,         // Command
            0x00, 0x00, 0x00,               // Device Address
            0x90,                           // Device Prefix
            0x04, 0x00,                     // Device Length
            0x10, 0x11                      // Device Value
        ];

        CollectionAssert.AreEqual(repuestPacketExpected, command.ToBinaryBytes());
    }

    [TestMethod]
    public void TestExecute()
    {
        var command = new BitBatchWriteCommand(Prefix.M, "0", [true, false, true, true]);

        byte[] recivePackets = [
            0xD0, 0x00,                     // Sub Header
            0x00, 0xFF, 0xFF, 0x03, 0x00,   // Route
            0x02, 0x00,                     // Content Length
            0x00, 0x00,                     // Error Code
        ];

        plcMock.Setup(x => x.Request(It.IsAny<byte[]>(), It.IsAny<IReceiveLengthParser>())).Returns(recivePackets);

        Assert.AreEqual(true, command.Execute(plcMock.Object));
    }

    [TestMethod]
    public async Task TestExecuteAsync()
    {
        var command = new BitBatchWriteCommand(Prefix.M, "0", [true, false, true, true]);

        byte[] recivePackets = [
            0xD0, 0x00,                     // Sub Header
            0x00, 0xFF, 0xFF, 0x03, 0x00,   // Route
            0x02, 0x00,                     // Content Length
            0x00, 0x00,                     // Error Code
        ];

        plcMock.Setup(x => x.RequestAsync(It.IsAny<byte[]>(), It.IsAny<IReceiveLengthParser>())).ReturnsAsync(recivePackets);

        Assert.AreEqual(true, await command.ExecuteAsync(plcMock.Object));
    }

    [TestMethod]
    public void TestException()
    {
        var ex = Assert.ThrowsException<ArgumentException>(() => {
            _ = new BitBatchWriteCommand(faker.PickRandom<Prefix>(), faker.Random.UShort().ToString(), []);
        });
        
        Assert.IsInstanceOfType<ArgumentException>(ex);

        ex = Assert.ThrowsException<ArgumentException>(() => {
            var randomBoolValues = Enumerable.Range(0, BitBatchWriteCommand.MAX_BIT_LENGTH + 1)
                .Select(_ => faker.Random.Bool())
                .ToArray();
            _ = new BitBatchWriteCommand(faker.PickRandom<Prefix>(), faker.Random.UShort().ToString(), randomBoolValues);
        });

        Assert.IsInstanceOfType<ArgumentException>(ex);
    }
}
