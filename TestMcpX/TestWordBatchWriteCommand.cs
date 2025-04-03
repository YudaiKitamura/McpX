using McpXLib.Commands;
using McpXLib.Enums;
using McpXLib.Interfaces;
using Moq;
using Bogus;
using McpXLib.Builders;

namespace TestMcpX;

[TestClass]
public sealed class TestWordBatchWriteCommand
{
    private readonly Faker faker;
    private Mock<IPlc> plcMock;

    public TestWordBatchWriteCommand()
    {
        faker = new Faker();
        plcMock = new Mock<IPlc>();
        plcMock.SetupProperty(x => x.Route);
        plcMock.Object.Route = new RoutePacketBuilder();
    }

    [TestMethod]
    public void TestToBytes()
    {
        var command = new WordBatchWriteCommand<short>(Prefix.D, "0", [1, 2, 3, 4, 5, 6]);

        byte[] repuestPacketExpected = [
            0x18, 0x00,                     // Content Length
            0x00, 0x00,                     // Monitoring Timer
            0x01, 0x14, 0x00, 0x00,         // Command
            0x00, 0x00, 0x00,               // Device Address
            0xA8,                           // Device Prefix
            0x06, 0x00,                     // Device Length
            0x01, 0x00,                     // Device Value1
            0x02, 0x00,                     // Device Value1
            0x03, 0x00,                     // Device Value1
            0x04, 0x00,                     // Device Value1
            0x05, 0x00,                     // Device Value1
            0x06, 0x00,                     // Device Value1
        ];

        CollectionAssert.AreEqual(repuestPacketExpected, command.ToBinaryBytes());
    }

    [TestMethod]
    public void TestExecute()
    {
        var command = new WordBatchWriteCommand<short>(Prefix.D, "0", [1, 2, 3, 4, 5, 6]);

        byte[] recivePackets = [
            0xD0, 0x00,                     // Sub Header
            0x00, 0xFF, 0xFF, 0x03, 0x00,   // Route
            0x02, 0x00,                     // Content Length
            0x00, 0x00,                     // Error Code
        ];

        plcMock.Setup(x => x.Request(It.IsAny<byte[]>())).Returns(recivePackets);

        Assert.AreEqual(true, command.Execute(plcMock.Object));
    }

    [TestMethod]
    public async Task TestExecuteAsync()
    {
        var command = new WordBatchWriteCommand<short>(Prefix.D, "0", [1, 2, 3, 4, 5, 6]);

        byte[] recivePackets = [
            0xD0, 0x00,                     // Sub Header
            0x00, 0xFF, 0xFF, 0x03, 0x00,   // Route
            0x02, 0x00,                     // Content Length
            0x00, 0x00,                     // Error Code
        ];

        plcMock.Setup(x => x.RequestAsync(It.IsAny<byte[]>())).ReturnsAsync(recivePackets);
        
        Assert.AreEqual(true, await command.ExecuteAsync(plcMock.Object));
    }

    [TestMethod]
    public void TestException()
    {
        var ex = Assert.ThrowsException<ArgumentException>(() => {
            _ = new WordBatchWriteCommand<short>(faker.PickRandom<Prefix>(), faker.Random.UShort().ToString(), []);
        });
        
        Assert.IsInstanceOfType<ArgumentException>(ex);

        ex = Assert.ThrowsException<ArgumentException>(() => {
            var randomValues = Enumerable.Range(0, WordBatchWriteCommand<short>.MAX_WORD_LENGTH + 1)
                .Select(_ => faker.Random.Short())
                .ToArray();
            _ = new WordBatchWriteCommand<short>(faker.PickRandom<Prefix>(), faker.Random.UShort().ToString(), randomValues);
        });

        Assert.IsInstanceOfType<ArgumentException>(ex);
    }
}
