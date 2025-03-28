using McpXLib.Commands;
using McpXLib.Enums;
using McpXLib.Helpers;
using McpXLib.Interfaces;
using Moq;
using Bogus;

namespace TestMcpX;

[TestClass]
public sealed class TestWordBatchReadAsciiCommand
{
    private readonly Faker faker;
    private Mock<IPlc> plcMock;

    public TestWordBatchReadAsciiCommand()
    {
        faker = new Faker();
        plcMock = new Mock<IPlc>();
        plcMock.SetupProperty(x => x.Route);
        plcMock.Object.Route = new RoutePacketHelper();
    }

    [TestMethod]
    public void TestToBytes()
    {
        var command = new WordBatchReadAsciiCommand<short>(Prefix.D, "0", 6);

        byte[] repuestPacketExpected = [
            0x35, 0x30, 0x30, 0x30,                                     // Sub Header
            0x30, 0x30, 0x46, 0x46, 0x30, 0x33, 0x46, 0x46, 0x30, 0x30, // Route
            0x30, 0x30, 0x31, 0x38,                                     // Content Length
            0x30, 0x30, 0x30, 0x30,                                     // Monitoring Timer
            0x30, 0x34, 0x30, 0x31,                                     // Command
            0x30, 0x30, 0x30, 0x30,                                     // SubCommand
            0x44, 0x2A,                                                 // Device Prefix
            0x30, 0x30, 0x30, 0x30, 0x30, 0x30,                         // Device Address
            0x30, 0x30, 0x30, 0x36                                      // Device Length
        ];

        CollectionAssert.AreEqual(repuestPacketExpected, command.ToBytes());
    }

    [TestMethod]
    public void TestExecute()
    {
        var command = new WordBatchReadAsciiCommand<short>(Prefix.D, "0", 6);

        byte[] recivePackets = [
            0x44, 0x30, 0x30, 0x30,                                     // Sub Header
            0x30, 0x30, 0x46, 0x46, 0x30, 0x33, 0x46, 0x46, 0x30, 0x30, // Route
            0x30, 0x30, 0x31, 0x43,                                     // Content Length
            0x30, 0x30, 0x30, 0x30,                                     // Error Code
            0x30, 0x30, 0x30, 0x31,                                     // Value1
            0x30, 0x30, 0x30, 0x32,                                     // Value2
            0x30, 0x30, 0x30, 0x33,                                     // Value3
            0x30, 0x30, 0x30, 0x34,                                     // Value4
            0x30, 0x30, 0x30, 0x35,                                     // Value5
            0x30, 0x30, 0x30, 0x36,                                     // Value6
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
        var command = new WordBatchReadAsciiCommand<short>(Prefix.D, "0", 6);

        byte[] recivePackets = [
            0x44, 0x30, 0x30, 0x30,                                     // Sub Header
            0x30, 0x30, 0x46, 0x46, 0x30, 0x33, 0x46, 0x46, 0x30, 0x30, // Route
            0x30, 0x30, 0x31, 0x43,                                     // Content Length
            0x30, 0x30, 0x30, 0x30,                                     // Error Code
            0x30, 0x30, 0x30, 0x31,                                     // Value1
            0x30, 0x30, 0x30, 0x32,                                     // Value2
            0x30, 0x30, 0x30, 0x33,                                     // Value3
            0x30, 0x30, 0x30, 0x34,                                     // Value4
            0x30, 0x30, 0x30, 0x35,                                     // Value5
            0x30, 0x30, 0x30, 0x36,                                     // Value6
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
            _ = new WordBatchReadAsciiCommand<short>(faker.PickRandom<Prefix>(), faker.Random.UShort().ToString(), WordBatchReadCommand<short>.MIN_WORD_LENGTH - 1);
        });
        
        Assert.IsInstanceOfType<ArgumentException>(ex);

        ex = Assert.ThrowsException<ArgumentException>(() => {
            _ = new WordBatchReadAsciiCommand<short>(faker.PickRandom<Prefix>(), faker.Random.UShort().ToString(), WordBatchReadCommand<short>.MAX_WORD_LENGTH + 1);
        });

        Assert.IsInstanceOfType<ArgumentException>(ex);
    }
}
