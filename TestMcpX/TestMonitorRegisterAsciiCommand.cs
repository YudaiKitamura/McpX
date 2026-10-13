using McpXLib.Commands;
using McpXLib.Enums;
using McpXLib.Interfaces;
using Moq;
using Bogus;
using McpXLib.Builders;

namespace TestMcpX;

[TestClass]
public sealed class TestMonitorRegisterAsciiCommand
{
    private readonly Faker faker;
    private Mock<IPlc> plcMock;

    public TestMonitorRegisterAsciiCommand()
    {
        faker = new Faker();
        plcMock = new Mock<IPlc>();
        plcMock.SetupGet(x => x.Route).Returns(new RoutePacketBuilder());
        plcMock.SetupGet(x => x.IsAscii).Returns(true);
    }

    [TestMethod]
    public void TestToBytes()
    {
        var command = new MonitorRegisterCommand([(Prefix.D, "0"),(Prefix.D, "1")], [(Prefix.D, "2"),(Prefix.D, "4")]);

        byte[] requestPacketExpected = [
            0x30, 0x30, 0x33, 0x30,                                     // Content Length
            0x30, 0x30, 0x30, 0x30,                                     // Monitoring Timer
            0x30, 0x38, 0x30, 0x31,                                     // Command
            0x30, 0x30, 0x30, 0x30,                                     // SubCommand
            0x30, 0x32,                                                 // Word Device Length
            0x30, 0x32,                                                 // Double Word Device Length
            0x44, 0x2A,                                                 // Device Prefix 1
            0x30, 0x30, 0x30, 0x30, 0x30, 0x30,                         // Device Address 1
            0x44, 0x2A,                                                 // Device Prefix 2
            0x30, 0x30, 0x30, 0x30, 0x30, 0x31,                         // Device Address 2
            0x44, 0x2A,                                                 // Device Prefix 3
            0x30, 0x30, 0x30, 0x30, 0x30, 0x32,                         // Device Address 3
            0x44, 0x2A,                                                 // Device Prefix 4
            0x30, 0x30, 0x30, 0x30, 0x30, 0x34,                         // Device Address 4
        ];

        CollectionAssert.AreEqual(requestPacketExpected, command.ToAsciiBytes());
    }

    [TestMethod]
    public void TestExecute()
    {
        var command = new MonitorRegisterCommand([(Prefix.D, "0"),(Prefix.D, "1")], [(Prefix.D, "2"),(Prefix.D, "4")]);

        byte[] recivePackets = [
            0x44, 0x30, 0x30, 0x30,                                     // Sub Header
            0x30, 0x30, 0x46, 0x46, 0x30, 0x33, 0x46, 0x46, 0x30, 0x30, // Route
            0x30, 0x30, 0x30, 0x34,                                     // Content Length
            0x30, 0x30, 0x30, 0x30,                                     // Error Code
        ];

        plcMock.Setup(x => x.Request(It.IsAny<byte[]>(), It.IsAny<IReceiveLengthParser>())).Returns(recivePackets);

        Assert.AreEqual(true, command.Execute(plcMock.Object));
    }

    [TestMethod]
    public async Task TestExecuteAsync()
    {
        var command = new MonitorRegisterCommand([(Prefix.D, "0"),(Prefix.D, "1")], [(Prefix.D, "2"),(Prefix.D, "4")]);

        byte[] recivePackets = [
            0x44, 0x30, 0x30, 0x30,                                     // Sub Header
            0x30, 0x30, 0x46, 0x46, 0x30, 0x33, 0x46, 0x46, 0x30, 0x30, // Route
            0x30, 0x30, 0x30, 0x34,                                     // Content Length
            0x30, 0x30, 0x30, 0x30,                                     // Error Code
        ];

        plcMock.Setup(x => x.RequestAsync(It.IsAny<byte[]>(), It.IsAny<IReceiveLengthParser>(), It.IsAny<CancellationToken>())).ReturnsAsync(recivePackets);

        Assert.AreEqual(true, await command.ExecuteAsync(plcMock.Object, CancellationToken.None));
    }

    [TestMethod]
    public void TestException()
    {
        var ex = Assert.ThrowsException<ArgumentException>(() => {
            _ = new MonitorRegisterCommand([], []);
        });
        
        Assert.IsInstanceOfType<ArgumentException>(ex);

        ex = Assert.ThrowsException<ArgumentException>(() => {
            var wordAddresses = Enumerable.Range(0, MonitorRegisterCommand.MAX_WORD_LENGTH + 1)
                .Select(_ => (faker.PickRandom<Prefix>(), faker.Random.UShort().ToString()))
                .ToArray();

            _ = new MonitorRegisterCommand(wordAddresses, []);
        });

        Assert.IsInstanceOfType<ArgumentException>(ex);

        ex = Assert.ThrowsException<ArgumentException>(() => {
            var doubleWordAddresses = Enumerable.Range(0, MonitorRegisterCommand.MAX_WORD_LENGTH + 1)
                .Select(_ => (faker.PickRandom<Prefix>(), faker.Random.UShort().ToString()))
                .ToArray();

            _ = new MonitorRegisterCommand([], doubleWordAddresses);
        });

        Assert.IsInstanceOfType<ArgumentException>(ex);

        ex = Assert.ThrowsException<ArgumentException>(() => {
            var wordAddresses = Enumerable.Range(0, MonitorRegisterCommand.MAX_WORD_LENGTH / 2)
                .Select(_ => (faker.PickRandom<Prefix>(), faker.Random.UShort().ToString()))
                .ToArray();

            var doubleWordAddresses = Enumerable.Range(0, MonitorRegisterCommand.MAX_WORD_LENGTH / 2 + 1)
                .Select(_ => (faker.PickRandom<Prefix>(), faker.Random.UShort().ToString()))
                .ToArray();

            _ = new MonitorRegisterCommand(wordAddresses, doubleWordAddresses);
        });

        Assert.IsInstanceOfType<ArgumentException>(ex);
    }
}
