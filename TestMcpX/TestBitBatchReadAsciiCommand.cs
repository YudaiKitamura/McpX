using McpXLib.Commands;
using McpXLib.Enums;
using McpXLib.Interfaces;
using Moq;
using Bogus;
using McpXLib.Builders;

namespace TestMcpX;

[TestClass]
public sealed class TestBitBatchReadAsciiCommand
{
    private readonly Faker faker;
    private Mock<IPlc> plcMock;

    public TestBitBatchReadAsciiCommand()
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
        var command = new BitBatchReadCommand(Prefix.M, "0", 6);

        byte[] repuestPacketExpected = [
            0x30, 0x30, 0x31, 0x38,                                     // Content Length
            0x30, 0x30, 0x30, 0x30,                                     // Monitoring Timer
            0x30, 0x34, 0x30, 0x31,                                     // Command
            0x30, 0x30, 0x30, 0x31,                                     // SubCommand
            0x4D, 0x2A,                                                 // Device Prefix
            0x30, 0x30, 0x30, 0x30, 0x30, 0x30,                         // Device Address
            0x30, 0x30, 0x30, 0x36                                      // Device Length
        ];

        CollectionAssert.AreEqual(repuestPacketExpected, command.ToAsciiBytes());
    }

    [TestMethod]
    public void TestExecute()
    {
        var command = new BitBatchReadCommand(Prefix.M, "0", 6);

        byte[] recivePackets = [
            0x44, 0x30, 0x30, 0x30,                                     // Sub Header
            0x30, 0x30, 0x46, 0x46, 0x30, 0x33, 0x46, 0x46, 0x30, 0x30, // Route
            0x30, 0x30, 0x30, 0x41,                                     // Content Length
            0x30, 0x30, 0x30, 0x30,                                     // Error Code
            0x30,                                                       // Value1
            0x31,                                                       // Value2
            0x31,                                                       // Value3
            0x31,                                                       // Value4
            0x31,                                                       // Value5
            0x30,                                                       // Value6
        ];

        plcMock.Setup(x => x.Request(It.IsAny<byte[]>())).Returns(recivePackets);

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
            0x44, 0x30, 0x30, 0x30,                                     // Sub Header
            0x30, 0x30, 0x46, 0x46, 0x30, 0x33, 0x46, 0x46, 0x30, 0x30, // Route
            0x30, 0x30, 0x30, 0x41,                                     // Content Length
            0x30, 0x30, 0x30, 0x30,                                     // Error Code
            0x30,                                                       // Value1
            0x31,                                                       // Value2
            0x31,                                                       // Value3
            0x31,                                                       // Value4
            0x31,                                                       // Value5
            0x30,                                                       // Value6
        ];

        plcMock.Setup(x => x.RequestAsync(It.IsAny<byte[]>())).ReturnsAsync(recivePackets);

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
