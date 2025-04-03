using McpXLib.Commands;
using McpXLib.Enums;
using McpXLib.Interfaces;
using Moq;
using Bogus;
using McpXLib.Builders;

namespace TestMcpX;

[TestClass]
public sealed class TestWordRandomReadCommand
{
    private readonly Faker faker;
    private Mock<IPlc> plcMock;

    public TestWordRandomReadCommand()
    {
        faker = new Faker();
        plcMock = new Mock<IPlc>();
        plcMock.SetupProperty(x => x.Route);
        plcMock.Object.Route = new RoutePacketBuilder();
    }

    [TestMethod]
    public void TestToBytes()
    {
        var command = new WordRandomReadCommand<short, int>([(Prefix.D, "0"),(Prefix.D, "1")],[(Prefix.D, "2"),(Prefix.D, "4")]);

        byte[] repuestPacketExpected = [
            0x18, 0x00,                     // Content Length
            0x00, 0x00,                     // Monitoring Timer
            0x03, 0x04, 0x00, 0x00,         // Command
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
        var command = new WordRandomReadCommand<short, int>([(Prefix.D, "0"),(Prefix.D, "1")],[(Prefix.D, "2"),(Prefix.D, "4")]);

        byte[] recivePackets = [
            0xD0, 0x00,                     // Sub Header
            0x00, 0xFF, 0xFF, 0x03, 0x00,   // Route
            0x0E, 0x00,                     // Content Length
            0x00, 0x00,                     // Error Code
            0x01, 0x00,                     // Value1
            0x02, 0x00,                     // Value2
            0x03, 0x00, 0x00, 0x00,         // Value3
            0x04, 0x00, 0x00, 0x00,         // Value4
        ];

        plcMock.Setup(x => x.Request(It.IsAny<byte[]>())).Returns(recivePackets);

        short[] wordDeviceExpected = [
            1,
            2,
        ];

        CollectionAssert.AreEqual(wordDeviceExpected, command.Execute(plcMock.Object).wordValues);

        int[] doubleDeviceExpected = [
            3,
            4,
        ];

        CollectionAssert.AreEqual(doubleDeviceExpected, command.Execute(plcMock.Object).doubleValues);
    }

    [TestMethod]
    public async Task TestExecuteAsync()
    {
        var command = new WordRandomReadCommand<short, int>([(Prefix.D, "0"),(Prefix.D, "1")],[(Prefix.D, "2"),(Prefix.D, "4")]);

        byte[] recivePackets = [
            0xD0, 0x00,                     // Sub Header
            0x00, 0xFF, 0xFF, 0x03, 0x00,   // Route
            0x0E, 0x00,                     // Content Length
            0x00, 0x00,                     // Error Code
            0x01, 0x00,                     // Value1
            0x02, 0x00,                     // Value2
            0x03, 0x00, 0x00, 0x00,         // Value3
            0x04, 0x00, 0x00, 0x00,         // Value4
        ];

        plcMock.Setup(x => x.RequestAsync(It.IsAny<byte[]>())).ReturnsAsync(recivePackets);

        short[] wordDeviceExpected = [
            1,
            2,
        ];

        CollectionAssert.AreEqual(wordDeviceExpected, (await command.ExecuteAsync(plcMock.Object)).wordValues);

        int[] doubleDeviceExpected = [
            3,
            4,
        ];

        CollectionAssert.AreEqual(doubleDeviceExpected, (await command.ExecuteAsync(plcMock.Object)).doubleValues);
    }

    [TestMethod]
    public void TestException()
    {
        var ex = Assert.ThrowsException<ArgumentException>(() => {
            _ = new WordRandomReadCommand<short, int>([], []);
        });
        
        Assert.IsInstanceOfType<ArgumentException>(ex);

        ex = Assert.ThrowsException<ArgumentException>(() => {
            var wordAddresses = Enumerable.Range(0, WordRandomReadCommand<short, int>.MAX_WORD_LENGTH + 1)
                .Select(_ => (faker.PickRandom<Prefix>(), faker.Random.UShort().ToString()))
                .ToArray();

            _ = new WordRandomReadCommand<short, int>(wordAddresses, []);
        });

        Assert.IsInstanceOfType<ArgumentException>(ex);

        ex = Assert.ThrowsException<ArgumentException>(() => {
            var doubleWordAddresses = Enumerable.Range(0, WordRandomReadCommand<short, int>.MAX_WORD_LENGTH + 1)
                .Select(_ => (faker.PickRandom<Prefix>(), faker.Random.UShort().ToString()))
                .ToArray();

            _ = new WordRandomReadCommand<short, int>([], doubleWordAddresses);
        });

        Assert.IsInstanceOfType<ArgumentException>(ex);

        ex = Assert.ThrowsException<ArgumentException>(() => {
            var wordAddresses = Enumerable.Range(0, WordRandomReadCommand<short, int>.MAX_WORD_LENGTH / 2)
                .Select(_ => (faker.PickRandom<Prefix>(), faker.Random.UShort().ToString()))
                .ToArray();

            var doubleWordAddresses = Enumerable.Range(0, WordRandomReadCommand<short, int>.MAX_WORD_LENGTH / 2 + 1)
                .Select(_ => (faker.PickRandom<Prefix>(), faker.Random.UShort().ToString()))
                .ToArray();

            _ = new WordRandomReadCommand<short, int>(wordAddresses, doubleWordAddresses);
        });

        Assert.IsInstanceOfType<ArgumentException>(ex);
    }
}
