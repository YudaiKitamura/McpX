using McpXLib.Commands;
using McpXLib.Enums;
using McpXLib.Interfaces;
using Moq;
using Bogus;
using McpXLib.Builders;

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
        plcMock.SetupProperty(x => x.IsAscii);
        plcMock.Object.Route = new RoutePacketBuilder();
        plcMock.Object.IsAscii = true;
    }

    [TestMethod]
    public void TestToBytes()
    {
        var command = new WordRandomReadCommand<short, int>([(Prefix.D, "0"),(Prefix.D, "1")],[(Prefix.D, "2"),(Prefix.D, "4")]);

        byte[] repuestPacketExpected = [
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

        CollectionAssert.AreEqual(repuestPacketExpected, command.ToAsciiBytes());
    }

    [TestMethod]
    public void TestExecute()
    {
        var command = new WordRandomReadCommand<short, int>([(Prefix.D, "0"),(Prefix.D, "1")],[(Prefix.D, "2"),(Prefix.D, "4")]);

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

        plcMock.Setup(x => x.Request(It.IsAny<byte[]>(), It.IsAny<IReceiveLengthParser>())).Returns(recivePackets);

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
            0x44, 0x30, 0x30, 0x30,                                     // Sub Header
            0x30, 0x30, 0x46, 0x46, 0x30, 0x33, 0x46, 0x46, 0x30, 0x30, // Route
            0x30, 0x30, 0x31, 0x43,                                     // Content Length
            0x30, 0x30, 0x30, 0x30,                                     // Error Code
            0x30, 0x30, 0x30, 0x31,                                     // Value1
            0x30, 0x30, 0x30, 0x32,                                     // Value2
            0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x33,             // Value3
            0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x34,             // Value4
        ];

        plcMock.Setup(x => x.RequestAsync(It.IsAny<byte[]>(), It.IsAny<IReceiveLengthParser>())).ReturnsAsync(recivePackets);

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

    // Regression: word/dword access point counts >= 16 must be encoded as exactly 2 hex chars, not 3 ("010").
    [TestMethod]
    public void TestAccessPointCountEncodingNotCorruptedAt16Points()
    {
        var wordAddresses = Enumerable.Range(0, 16)
            .Select(i => (Prefix.D, i.ToString()))
            .ToArray();

        var command = new WordRandomReadCommand<short, int>(wordAddresses, []);

        byte[] result = command.ToAsciiBytes();

        // Content Length(4) + Monitoring(4) + Command(4) + SubCommand(4) = offset 16
        // Word count "10", Double word count "00", then first device "D*...".
        Assert.AreEqual((byte)0x31, result[16]); // '1'
        Assert.AreEqual((byte)0x30, result[17]); // '0'
        Assert.AreEqual((byte)0x30, result[18]); // '0' (double word count high)
        Assert.AreEqual((byte)0x30, result[19]); // '0' (double word count low)
        Assert.AreEqual((byte)0x44, result[20]); // 'D' (first device, not shifted)
    }
}
