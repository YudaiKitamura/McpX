using McpXLib.Commands;
using McpXLib.Enums;
using McpXLib.Interfaces;
using Moq;
using Bogus;
using McpXLib.Builders;

namespace TestMcpX;

[TestClass]
public sealed class TestBitRandomWriteCommand
{
    private readonly Faker faker;
    private Mock<IPlc> plcMock;

    public TestBitRandomWriteCommand()
    {
        faker = new Faker();
        plcMock = new Mock<IPlc>();
        plcMock.SetupProperty(x => x.Route);
        plcMock.Object.Route = new RoutePacketBuilder();
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
            0x11, 0x00,                     // Content Length
            0x00, 0x00,                     // Monitoring Timer
            0x02, 0x14, 0x01, 0x00,         // Command + SubCommand
            0x02,                           // Bit Access Points
            0x32, 0x00, 0x00,               // Device Address1 (M50)
            0x90,                           // Device Prefix1 (M)
            0x00,                           // Set/Reset1 (OFF)
            0x2F, 0x00, 0x00,               // Device Address2 (Y2F)
            0x9D,                           // Device Prefix2 (Y)
            0x01,                           // Set/Reset2 (ON)
        ];

        CollectionAssert.AreEqual(repuestPacketExpected, command.ToBinaryBytes());
    }

    [TestMethod]
    public void TestToBytesForiQR()
    {
        var command = new BitRandomWriteCommand(
            [
                (Prefix.M, "50", false),
                (Prefix.Y, "2F", true)
            ],
            series: ProcessorSeries.iQR
        );

        byte[] repuestPacketExpected = [
            0x15, 0x00,                                 // Content Length
            0x00, 0x00,                                 // Monitoring Timer
            0x02, 0x14, 0x03, 0x00,                     // Command + SubCommand (iQ-R bit = 0003)
            0x02,                                       // Bit Access Points
            0x32, 0x00, 0x00, 0x00, 0x90, 0x00,         // M50 (Address 4Byte + Code 1Byte + 0x00)
            0x00,                                       // Set/Reset1 (OFF)
            0x2F, 0x00, 0x00, 0x00, 0x9D, 0x00,         // Y2F
            0x01,                                       // Set/Reset2 (ON)
        ];

        CollectionAssert.AreEqual(repuestPacketExpected, command.ToBinaryBytes());
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
            0xD0, 0x00,                     // Sub Header
            0x00, 0xFF, 0xFF, 0x03, 0x00,   // Route
            0x02, 0x00,                     // Content Length
            0x00, 0x00,                     // Error Code
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
            0xD0, 0x00,                     // Sub Header
            0x00, 0xFF, 0xFF, 0x03, 0x00,   // Route
            0x02, 0x00,                     // Content Length
            0x00, 0x00,                     // Error Code
        ];

        plcMock.Setup(x => x.RequestAsync(It.IsAny<byte[]>(), It.IsAny<IReceiveLengthParser>())).ReturnsAsync(recivePackets);

        Assert.AreEqual(true, await command.ExecuteAsync(plcMock.Object));
    }

    [TestMethod]
    public void TestException()
    {
        var ex = Assert.ThrowsException<ArgumentException>(() => {
            _ = new BitRandomWriteCommand([]);
        });

        Assert.IsInstanceOfType<ArgumentException>(ex);

        // Q/L series: over MAX_BIT_LENGTH (188) throws
        ex = Assert.ThrowsException<ArgumentException>(() => {
            var bitDevices = Enumerable.Range(0, BitRandomWriteCommand.MAX_BIT_LENGTH + 1)
                .Select(_ => (faker.PickRandom<Prefix>(), faker.Random.UShort().ToString(), faker.Random.Bool()))
                .ToArray();

            _ = new BitRandomWriteCommand(bitDevices);
        });

        Assert.IsInstanceOfType<ArgumentException>(ex);

        // iQ-R series: over 94 points throws (95 points)
        ex = Assert.ThrowsException<ArgumentException>(() => {
            var bitDevices = Enumerable.Range(0, BitRandomWriteCommand.GetMaxBitLength(ProcessorSeries.iQR) + 1)
                .Select(_ => (faker.PickRandom<Prefix>(), faker.Random.UShort().ToString(), faker.Random.Bool()))
                .ToArray();

            _ = new BitRandomWriteCommand(bitDevices, series: ProcessorSeries.iQR);
        });

        Assert.IsInstanceOfType<ArgumentException>(ex);
    }
}
