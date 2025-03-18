using McpXLib.Commands;
using McpXLib.Enums;
using McpXLib.Helpers;
using McpXLib.Interfaces;
using Moq;
using Bogus;

namespace TestMcpX;

[TestClass]
public sealed class TestBitBatchReadCommand
{
    private readonly Faker faker;
    private Mock<IPlc> plcMock;

    public TestBitBatchReadCommand()
    {
        faker = new Faker();
        plcMock = new Mock<IPlc>();
        plcMock.SetupProperty(x => x.Route);
        plcMock.Object.Route = new RoutePacketHelper();
    }

    [TestMethod]
    public void TestToBytes()
    {
        var command = new BitBatchReadCommand(Prefix.M, "0", 6);

        byte[] repuestPacketExpected = [
            0x50, 0x00,                     // Sub Header
            0x00, 0xFF, 0xFF, 0x03, 0x00,   // Route
            0x0C, 0x00,                     // Content Length
            0x00, 0x00,                     // Monitoring Timer
            0x01, 0x04, 0x01, 0x00,         // Command
            0x00, 0x00, 0x00,               // Device Address
            0x90,                           // Device Prefix
            0x06, 0x00                      // Device Length
        ];

        CollectionAssert.AreEqual(repuestPacketExpected, command.ToBytes());
    }

    [TestMethod]
    public void TestExecute()
    {
        var command = new BitBatchReadCommand(Prefix.M, "0", 6);

        byte[] recivePackets = [
            0xD0, 0x00,                     // Sub Header
            0x00, 0xFF, 0xFF, 0x03, 0x00,   // Route
            0x05, 0x00,                     // Content Length
            0x00, 0x00,                     // Error Code
            0x01, 0x11, 0x10                // Value
        ];

        plcMock.Setup(x => x.Request(command.ToBytes())).Returns(recivePackets);

        bool[] deviceExpected = [
            false,
            true,
            true,
            true,
            true,
            false
        ];

        CollectionAssert.AreEqual(deviceExpected, command.Execute(plcMock.Object));
    }

    [TestMethod]
    public async Task TestExecuteAsync()
    {
        var command = new BitBatchReadCommand(Prefix.M, "0", 6);

        byte[] recivePackets = [
            0xD0, 0x00,                     // Sub Header
            0x00, 0xFF, 0xFF, 0x03, 0x00,   // Route
            0x05, 0x00,                     // Content Length
            0x00, 0x00,                     // Error Code
            0x01, 0x11, 0x10                // Value
        ];

        plcMock.Setup(x => x.RequestAsync(command.ToBytes())).ReturnsAsync(recivePackets);

        bool[] deviceExpected = [
            false,
            true,
            true,
            true,
            true,
            false
        ];

        CollectionAssert.AreEqual(deviceExpected, await command.ExecuteAsync(plcMock.Object));
    }

    [TestMethod]
    public void TestException()
    {
        var ex = Assert.ThrowsException<ArgumentException>(() => {
            _ = new BitBatchReadCommand(faker.PickRandom<Prefix>(), faker.Random.UShort().ToString(), BitBatchReadCommand.MIN_BIT_LENGTH - 1);
        });
        
        Assert.IsInstanceOfType<ArgumentException>(ex);

        ex = Assert.ThrowsException<ArgumentException>(() => {
            _ = new BitBatchReadCommand(faker.PickRandom<Prefix>(), faker.Random.UShort().ToString(), BitBatchReadCommand.MAX_BIT_LENGTH + 1);
        });

        Assert.IsInstanceOfType<ArgumentException>(ex);
    }
}
