using McpXLib.Commands;
using McpXLib.Enums;
using McpXLib.Interfaces;
using Moq;
using Bogus;
using McpXLib.Builders;

namespace TestMcpX;

[TestClass]
public sealed class TestBitRandomWriteAsciiCommand
{
    private readonly Faker faker;
    private Mock<IPlc> plcMock;

    public TestBitRandomWriteAsciiCommand()
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
        var command = new BitRandomWriteCommand(
            [
                (Prefix.M, "50", false),
                (Prefix.Y, "2F", true)
            ]
        );

        byte[] repuestPacketExpected = [
            0x30, 0x30, 0x32, 0x32,                         // Content Length "0022"
            0x30, 0x30, 0x30, 0x30,                         // Monitoring Timer "0000"
            0x31, 0x34, 0x30, 0x32,                         // Command "1402"
            0x30, 0x30, 0x30, 0x31,                         // SubCommand "0001"
            0x30, 0x32,                                     // Bit Access Points "02"
            0x4D, 0x2A, 0x30, 0x30, 0x30, 0x30, 0x35, 0x30, // Device1 "M*000050"
            0x30, 0x30,                                     // Set/Reset1 "00" (OFF)
            0x59, 0x2A, 0x30, 0x30, 0x30, 0x30, 0x32, 0x46, // Device2 "Y*00002F"
            0x30, 0x31,                                     // Set/Reset2 "01" (ON)
        ];

        CollectionAssert.AreEqual(repuestPacketExpected, command.ToAsciiBytes());
    }

    // Regression: bit access points >= 16 must be encoded as exactly 2 hex chars ("10"), not "010".
    [TestMethod]
    public void TestBitAccessPointsEncodingNotCorruptedAt16Points()
    {
        var bitDevices = Enumerable.Range(0, 16)
            .Select(i => (Prefix.M, i.ToString(), true))
            .ToArray();

        var command = new BitRandomWriteCommand(bitDevices);

        byte[] result = command.ToAsciiBytes();

        // Content Length(4) + Monitoring(4) + Command(4) + SubCommand(4) = offset 16
        // Bit Access Points must be "10" (0x31, 0x30), then the first device "M*..." follows.
        Assert.AreEqual((byte)0x31, result[16]); // '1'
        Assert.AreEqual((byte)0x30, result[17]); // '0'
        Assert.AreEqual((byte)0x4D, result[18]); // 'M' (first device, not shifted)
        Assert.AreEqual((byte)0x2A, result[19]); // '*'
    }

    [TestMethod]
    public void TestExecute()
    {
        var command = new BitRandomWriteCommand(
            [
                (Prefix.M, "50", false),
                (Prefix.Y, "2F", true)
            ]
        );

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
        var command = new BitRandomWriteCommand(
            [
                (Prefix.M, "50", false),
                (Prefix.Y, "2F", true)
            ]
        );

        byte[] recivePackets = [
            0x44, 0x30, 0x30, 0x30,                                     // Sub Header
            0x30, 0x30, 0x46, 0x46, 0x30, 0x33, 0x46, 0x46, 0x30, 0x30, // Route
            0x30, 0x30, 0x30, 0x34,                                     // Content Length
            0x30, 0x30, 0x30, 0x30,                                     // Error Code
        ];

        plcMock.Setup(x => x.RequestAsync(It.IsAny<byte[]>(), It.IsAny<IReceiveLengthParser>())).ReturnsAsync(recivePackets);

        Assert.AreEqual(true, await command.ExecuteAsync(plcMock.Object));
    }
}
