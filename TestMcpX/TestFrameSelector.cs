using McpXLib.Enums;
using McpXLib.Interfaces;
using Moq;
using Bogus;
using McpXLib.Builders;
using McpXLib.Utils;

namespace TestMcpX;

[TestClass]
public sealed class TestFrameSelector
{
    private readonly Faker faker;
    private Mock<IPlc> plcMock;
    private CommandPacketBuilder commandPacketBuilder;

    public TestFrameSelector()
    {
        faker = new Faker();
        plcMock = new Mock<IPlc>();
        plcMock.SetupProperty(x => x.RequestFrame);
        plcMock.SetupProperty(x => x.Route);
        plcMock.SetupProperty(x => x.IsAscii);
        plcMock.Object.Route = new RoutePacketBuilder();
        plcMock.Object.IsAscii = faker.Random.Bool();

        commandPacketBuilder = new CommandPacketBuilder(
            command: faker.Random.Bytes(2),
            subCommand: faker.Random.Bytes(2),
            monitoringTimer: faker.Random.Bytes(2)
        );
    }

    [TestMethod]
    public void TestGetSerialNumber()
    {
        for (int i = 0; i <= ushort.MaxValue; i++) 
        {
            plcMock.Object.RequestFrame = faker.PickRandom<RequestFrame>();
            var frameSelector1 = new FrameSelector(
                plc: plcMock.Object,
                commandPacketBuilder: commandPacketBuilder
            );

            Assert.AreEqual(i, frameSelector1.GetSerialNumber());
        }

        plcMock.Object.RequestFrame = faker.PickRandom<RequestFrame>();
        var frameSelector2 = new FrameSelector(
            plc: plcMock.Object,
            commandPacketBuilder: commandPacketBuilder
        );

        Assert.AreEqual(0, frameSelector2.GetSerialNumber());
    }

    [TestMethod]
    public void TestGetPacketBuilder()
    {
        plcMock.Object.RequestFrame = RequestFrame.E3;
        var e3FrameSelector = new FrameSelector(
            plc: plcMock.Object,
            commandPacketBuilder: commandPacketBuilder
        );

        CollectionAssert.AreEqual(new byte[] { 0x50, 0x00 }, e3FrameSelector.GetPacketBuilder().ToBinaryBytes().Take(2).ToArray());

        plcMock.Object.RequestFrame = RequestFrame.E4;
        var e4FrameSelector = new FrameSelector(
            plc: plcMock.Object,
            commandPacketBuilder: commandPacketBuilder
        );

        CollectionAssert.AreEqual(new byte[] { 0x54, 0x00, 0x01, 0x00, 0x00, 0x00 }, e4FrameSelector.GetPacketBuilder().ToBinaryBytes().Take(6).ToArray());

        plcMock.Object.RequestFrame = RequestFrame.E4;
        var e4FrameSelector2 = new FrameSelector(
            plc: plcMock.Object,
            commandPacketBuilder: commandPacketBuilder
        );

        CollectionAssert.AreEqual(new byte[] { 0x54, 0x00, 0x02, 0x00, 0x00, 0x00 }, e4FrameSelector2.GetPacketBuilder().ToBinaryBytes().Take(6).ToArray());
    }
}
