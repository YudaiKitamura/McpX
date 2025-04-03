using McpXLib.Commands;
using McpXLib.Enums;
using McpXLib.Interfaces;
using Moq;
using Bogus;
using McpXLib.Builders;

namespace TestMcpX;

[TestClass]
public sealed class TestMonitorRegistCommand
{
    private readonly Faker faker;
    private Mock<IPlc> plcMock;

    public TestMonitorRegistCommand()
    {
        faker = new Faker();
        plcMock = new Mock<IPlc>();
        plcMock.SetupProperty(x => x.Route);
        plcMock.Object.Route = new RoutePacketBuilder();
    }

    [TestMethod]
    public void TestToBytes()
    {
        var command = new MonitorRegistCommand([(Prefix.D, "0"),(Prefix.D, "1")], [(Prefix.D, "2"),(Prefix.D, "4")]);

        byte[] repuestPacketExpected = [
            0x18, 0x00,                     // Content Length
            0x00, 0x00,                     // Monitoring Timer
            0x01, 0x08, 0x00, 0x00,         // Command
            0x02,                           // Word Device Length
            0x02,                           // Double Device Length
            0x00, 0x00, 0x00,               // Device Address1
            0xA8,                           // Device Prefix1
            0x01, 0x00, 0x00,               // Device Address2
            0xA8,                           // Device Prefix2
            0x02, 0x00, 0x00,               // Device Address3
            0xA8,                           // Device Prefix3
            0x04, 0x00, 0x00,               // Device Address4
            0xA8,                           // Device Prefix4
        ];

        CollectionAssert.AreEqual(repuestPacketExpected, command.ToBinaryBytes());
    }

    [TestMethod]
    public void TestExecute()
    {
        var command = new MonitorRegistCommand([(Prefix.D, "0"),(Prefix.D, "1")], [(Prefix.D, "2"),(Prefix.D, "4")]);

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
        var command = new MonitorRegistCommand([(Prefix.D, "0"),(Prefix.D, "1")], [(Prefix.D, "2"),(Prefix.D, "4")]);

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
            _ = new MonitorRegistCommand([], []);
        });
        
        Assert.IsInstanceOfType<ArgumentException>(ex);

        ex = Assert.ThrowsException<ArgumentException>(() => {
            var wordAddresses = Enumerable.Range(0, MonitorRegistCommand.MAX_WORD_LENGTH + 1)
                .Select(_ => (faker.PickRandom<Prefix>(), faker.Random.UShort().ToString()))
                .ToArray();

            _ = new MonitorRegistCommand(wordAddresses, []);
        });

        Assert.IsInstanceOfType<ArgumentException>(ex);

        ex = Assert.ThrowsException<ArgumentException>(() => {
            var doubleWordAddresses = Enumerable.Range(0, MonitorRegistCommand.MAX_WORD_LENGTH + 1)
                .Select(_ => (faker.PickRandom<Prefix>(), faker.Random.UShort().ToString()))
                .ToArray();

            _ = new MonitorRegistCommand([], doubleWordAddresses);
        });

        Assert.IsInstanceOfType<ArgumentException>(ex);

        ex = Assert.ThrowsException<ArgumentException>(() => {
            var wordAddresses = Enumerable.Range(0, MonitorRegistCommand.MAX_WORD_LENGTH / 2)
                .Select(_ => (faker.PickRandom<Prefix>(), faker.Random.UShort().ToString()))
                .ToArray();

            var doubleWordAddresses = Enumerable.Range(0, MonitorRegistCommand.MAX_WORD_LENGTH / 2 + 1)
                .Select(_ => (faker.PickRandom<Prefix>(), faker.Random.UShort().ToString()))
                .ToArray();

            _ = new MonitorRegistCommand(wordAddresses, doubleWordAddresses);
        });

        Assert.IsInstanceOfType<ArgumentException>(ex);
    }
}
