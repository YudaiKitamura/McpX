using McpXLib.Commands;
using McpXLib.Enums;
using McpXLib.Helpers;
using McpXLib.Interfaces;
using Moq;
using Bogus;

namespace TestMcpX;

[TestClass]
public sealed class TestWordBatchReadCommand
{
    private readonly Faker faker;
    private Mock<IPlc> plcMock;

    public TestWordBatchReadCommand()
    {
        faker = new Faker();
        plcMock = new Mock<IPlc>();
        plcMock.SetupProperty(x => x.Route);
        plcMock.Object.Route = new RoutePacketHelper();
    }

    [TestMethod]
    public void TestToBytes()
    {
        var command = new WordBatchReadCommand<short>(Prefix.D, "0", 6);

        byte[] repuestPacketExpected = [
            0x50, 0x00,                     // Sub Header
            0x00, 0xFF, 0xFF, 0x03, 0x00,   // Route
            0x0C, 0x00,                     // Content Length
            0x00, 0x00,                     // Monitoring Timer
            0x01, 0x04, 0x00, 0x00,         // Command
            0x00, 0x00, 0x00,               // Device Address
            0xA8,                           // Device Prefix
            0x06, 0x00                      // Device Length
        ];

        CollectionAssert.AreEqual(repuestPacketExpected, command.ToBytes());
    }

    [TestMethod]
    public void TestExecute()
    {
        var command = new WordBatchReadCommand<short>(Prefix.D, "0", 6);

        byte[] recivePackets = [
            0xD0, 0x00,                     // Sub Header
            0x00, 0xFF, 0xFF, 0x03, 0x00,   // Route
            0x0E, 0x00,                     // Content Length
            0x00, 0x00,                     // Error Code
            0x01, 0x00,                     // Value1
            0x02, 0x00,                     // Value2
            0x03, 0x00,                     // Value3
            0x04, 0x00,                     // Value4
            0x05, 0x00,                     // Value5
            0x06, 0x00,                     // Value6
        ];

        plcMock.Setup(x => x.Request(command.ToBytes())).Returns(recivePackets);

        short[] deviceExpected = [
            1,
            2,
            3,
            4,
            5,
            6
        ];

        CollectionAssert.AreEqual(deviceExpected, command.Execute(plcMock.Object));
    }

    [TestMethod]
    public async Task TestExecuteAsync()
    {
        var command = new WordBatchReadCommand<short>(Prefix.D, "0", 6);

        byte[] recivePackets = [
            0xD0, 0x00,                     // Sub Header
            0x00, 0xFF, 0xFF, 0x03, 0x00,   // Route
            0x0E, 0x00,                     // Content Length
            0x00, 0x00,                     // Error Code
            0x01, 0x00,                     // Value1
            0x02, 0x00,                     // Value2
            0x03, 0x00,                     // Value3
            0x04, 0x00,                     // Value4
            0x05, 0x00,                     // Value5
            0x06, 0x00,                     // Value6
        ];

        plcMock.Setup(x => x.RequestAsync(command.ToBytes())).ReturnsAsync(recivePackets);

        short[] deviceExpected = [
            1,
            2,
            3,
            4,
            5,
            6
        ];

        CollectionAssert.AreEqual(deviceExpected, await command.ExecuteAsync(plcMock.Object));
    }

    [TestMethod]
    public void TestException()
    {
        var ex = Assert.ThrowsException<ArgumentException>(() => {
            _ = new WordBatchReadCommand<short>(faker.PickRandom<Prefix>(), faker.Random.UShort().ToString(), WordBatchReadCommand<short>.MIN_WORD_LENGTH - 1);
        });
        
        Assert.IsInstanceOfType<ArgumentException>(ex);

        ex = Assert.ThrowsException<ArgumentException>(() => {
            _ = new WordBatchReadCommand<short>(faker.PickRandom<Prefix>(), faker.Random.UShort().ToString(), WordBatchReadCommand<short>.MAX_WORD_LENGTH + 1);
        });

        Assert.IsInstanceOfType<ArgumentException>(ex);
    }
}
