using McpXLib.Commands;
using McpXLib.Enums;
using McpXLib.Interfaces;
using Moq;
using Bogus;
using McpXLib.Builders;

namespace TestMcpX;

[TestClass]
public sealed class TestBitBatchWriteAsciiCommand
{
    private readonly Faker faker;
    private Mock<IPlc> plcMock;

    public TestBitBatchWriteAsciiCommand()
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
        var command = new BitBatchWriteCommand(Prefix.M, "0", [true, false, true, true]);

        byte[] repuestPacketExpected = [
            0x30, 0x30, 0x31, 0x43,                                     // Content Length
            0x30, 0x30, 0x30, 0x30,                                     // Monitoring Timer
            0x31, 0x34, 0x30, 0x31,                                     // Command
            0x30, 0x30, 0x30, 0x31,                                     // SubCommand
            0x4D, 0x2A,                                                 // Device Prefix
            0x30, 0x30, 0x30, 0x30, 0x30, 0x30,                         // Device Address
            0x30, 0x30, 0x30, 0x34,                                     // Device Length
            0x31,                                                       // Value1
            0x30,                                                       // Value2
            0x31,                                                       // Value3
            0x31,                                                       // Value4
        ];

        CollectionAssert.AreEqual(repuestPacketExpected, command.ToAsciiBytes());
    }

    [TestMethod]
    public void TestExecute()
    {
        var command = new BitBatchWriteCommand(Prefix.M, "0", [true, false, true, true]);

        byte[] recivePackets = [
            0x44, 0x30, 0x30, 0x30,                                     // Sub Header
            0x30, 0x30, 0x46, 0x46, 0x30, 0x33, 0x46, 0x46, 0x30, 0x30, // Route
            0x30, 0x30, 0x30, 0x34,                                     // Content Length
            0x30, 0x30, 0x30, 0x30,                                     // Error Code
        ];

        plcMock.Setup(x => x.Request(It.IsAny<byte[]>())).Returns(recivePackets);

        Assert.AreEqual(true, command.Execute(plcMock.Object));
    }

    [TestMethod]
    public async Task TestExecuteAsync()
    {
        var command = new BitBatchWriteCommand(Prefix.M, "0", [true, false, true, true]);

        byte[] recivePackets = [
            0x44, 0x30, 0x30, 0x30,                                     // Sub Header
            0x30, 0x30, 0x46, 0x46, 0x30, 0x33, 0x46, 0x46, 0x30, 0x30, // Route
            0x30, 0x30, 0x30, 0x34,                                     // Content Length
            0x30, 0x30, 0x30, 0x30,                                     // Error Code
        ];

        plcMock.Setup(x => x.RequestAsync(It.IsAny<byte[]>())).ReturnsAsync(recivePackets);

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
