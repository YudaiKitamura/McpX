using McpXLib.Commands;
using McpXLib.Enums;
using McpXLib.Helpers;
using McpXLib.Interfaces;
using Moq;
using Bogus;

namespace TestMcpX;

[TestClass]
public sealed class TestWordRandomReadAsciiCommand
{
    private readonly Faker faker;
    private Mock<IPlc> plcMock;

    public TestWordRandomReadAsciiCommand()
    {
        faker = new Faker();
        plcMock = new Mock<IPlc>();
        plcMock.SetupProperty(x => x.Route);
        plcMock.Object.Route = new RoutePacketHelper();
    }

    [TestMethod]
    public void TestToBytes()
    {
        var command = new WordRandomReadAsciiCommand<short, int>([(Prefix.D, "0"),(Prefix.D, "1")],[(Prefix.D, "2"),(Prefix.D, "4")]);

        byte[] repuestPacketExpected = [
            0x35, 0x30, 0x30, 0x30,                                     // Sub Header
            0x30, 0x30, 0x46, 0x46, 0x30, 0x33, 0x46, 0x46, 0x30, 0x30, // Route
            0x30, 0x30, 0x33, 0x30,                                     // Content Length
            0x30, 0x30, 0x30, 0x30,                                     // Monitoring Timer
            0x30, 0x34, 0x30, 0x33,                                     // Command
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

        CollectionAssert.AreEqual(repuestPacketExpected, command.ToBytes());
    }

    [TestMethod]
    public void TestExecute()
    {
        var command = new WordRandomReadAsciiCommand<short, int>([(Prefix.D, "0"),(Prefix.D, "1")],[(Prefix.D, "2"),(Prefix.D, "4")]);

        byte[] recivePackets = [
            0x44, 0x30, 0x30, 0x30,                                     // Sub Header
            0x30, 0x30, 0x46, 0x46, 0x30, 0x33, 0x46, 0x46, 0x30, 0x30, // Route
            0x30, 0x30, 0x31, 0x43,                                     // Content Length
            0x30, 0x30, 0x30, 0x30,                                     // Error Code
            0x30, 0x30, 0x30, 0x31,                                     // Value1
            0x30, 0x30, 0x30, 0x32,                                     // Value2
            0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x33,             // Value3
            0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x34,             // Value4
        ];

        plcMock.Setup(x => x.Request(command.ToBytes())).Returns(recivePackets);

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
        var command = new WordRandomReadAsciiCommand<short, int>([(Prefix.D, "0"),(Prefix.D, "1")],[(Prefix.D, "2"),(Prefix.D, "4")]);

        byte[] recivePackets = [
            0x44, 0x30, 0x30, 0x30,                                     // Sub Header
            0x30, 0x30, 0x46, 0x46, 0x30, 0x33, 0x46, 0x46, 0x30, 0x30, // Route
            0x30, 0x30, 0x31, 0x43,                                     // Content Length
            0x30, 0x30, 0x30, 0x30,                                     // Error Code
            0x30, 0x30, 0x30, 0x31,                                     // Value1
            0x30, 0x30, 0x30, 0x32,                                     // Value2
            0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x33,             // Value3
            0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x34,             // Value4
        ];

        plcMock.Setup(x => x.RequestAsync(command.ToBytes())).ReturnsAsync(recivePackets);

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
            _ = new WordRandomReadAsciiCommand<short, int>([], []);
        });
        
        Assert.IsInstanceOfType<ArgumentException>(ex);

        ex = Assert.ThrowsException<ArgumentException>(() => {
            var wordAddresses = Enumerable.Range(0, WordRandomReadAsciiCommand<short, int>.MAX_WORD_LENGTH + 1)
                .Select(_ => (faker.PickRandom<Prefix>(), faker.Random.UShort().ToString()))
                .ToArray();

            _ = new WordRandomReadAsciiCommand<short, int>(wordAddresses, []);
        });

        Assert.IsInstanceOfType<ArgumentException>(ex);

        ex = Assert.ThrowsException<ArgumentException>(() => {
            var doubleWordAddresses = Enumerable.Range(0, WordRandomReadAsciiCommand<short, int>.MAX_WORD_LENGTH + 1)
                .Select(_ => (faker.PickRandom<Prefix>(), faker.Random.UShort().ToString()))
                .ToArray();

            _ = new WordRandomReadAsciiCommand<short, int>([], doubleWordAddresses);
        });

        Assert.IsInstanceOfType<ArgumentException>(ex);

        ex = Assert.ThrowsException<ArgumentException>(() => {
            var wordAddresses = Enumerable.Range(0, WordRandomReadAsciiCommand<short, int>.MAX_WORD_LENGTH / 2)
                .Select(_ => (faker.PickRandom<Prefix>(), faker.Random.UShort().ToString()))
                .ToArray();

            var doubleWordAddresses = Enumerable.Range(0, WordRandomReadAsciiCommand<short, int>.MAX_WORD_LENGTH / 2 + 1)
                .Select(_ => (faker.PickRandom<Prefix>(), faker.Random.UShort().ToString()))
                .ToArray();

            _ = new WordRandomReadAsciiCommand<short, int>(wordAddresses, doubleWordAddresses);
        });

        Assert.IsInstanceOfType<ArgumentException>(ex);
    }
}
