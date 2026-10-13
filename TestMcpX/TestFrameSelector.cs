using McpXLib.Enums;
using McpXLib.Interfaces;
using Moq;
using Bogus;
using McpXLib.Builders;
using McpXLib.Utils;
using System.Reflection;

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
        plcMock.SetupGet(x => x.Route).Returns(new RoutePacketBuilder());
        plcMock.SetupGet(x => x.IsAscii).Returns(false);

        commandPacketBuilder = new CommandPacketBuilder(
            command: faker.Random.Bytes(2),
            subCommand: faker.Random.Bytes(2),
            monitoringTimer: (ushort)0
        );
    }

    private void ResetSerialNumber()
    {
        // MEMO:
        //  PrivateなuniqSerialNumberをリフレクションを使用してリセットする
        var type = typeof(RequestFrameSelector);
        var field = type.GetField("uniqSerialNumber", BindingFlags.Static | BindingFlags.NonPublic);
        field?.SetValue(null, 0);
    }

    [TestMethod]
    public void TestGetSerialNumber()
    {
        ResetSerialNumber();

        for (int i = 0; i <= ushort.MaxValue; i++) 
        {
            plcMock.SetupGet(x => x.RequestFrame).Returns(faker.PickRandom<RequestFrame>());
            var frameSelector1 = new RequestFrameSelector(
                plc: plcMock.Object,
                commandPacketBuilder: commandPacketBuilder
            );

            Assert.AreEqual(i, frameSelector1.GetSerialNumber());
        }

        plcMock.SetupGet(x => x.RequestFrame).Returns(faker.PickRandom<RequestFrame>());
        var frameSelector2 = new RequestFrameSelector(
            plc: plcMock.Object,
            commandPacketBuilder: commandPacketBuilder
        );

        Assert.AreEqual(0, frameSelector2.GetSerialNumber());
    }

    [TestMethod]
    public void TestGetPacketBuilder()
    {
        ResetSerialNumber();

        plcMock.SetupGet(x => x.IsAscii).Returns(false);
        plcMock.SetupGet(x => x.RequestFrame).Returns(RequestFrame.E3);
        var e3FrameSelector = new RequestFrameSelector(
            plc: plcMock.Object,
            commandPacketBuilder: commandPacketBuilder
        );

        CollectionAssert.AreEqual(new byte[] { 0x50, 0x00 }, e3FrameSelector.GetRequestPacket().Take(2).ToArray());

        plcMock.SetupGet(x => x.RequestFrame).Returns(RequestFrame.E4);
        var e4FrameSelector = new RequestFrameSelector(
            plc: plcMock.Object,
            commandPacketBuilder: commandPacketBuilder
        );

        CollectionAssert.AreEqual(new byte[] { 0x54, 0x00, 0x01, 0x00, 0x00, 0x00 }, e4FrameSelector.GetRequestPacket().Take(6).ToArray());

        plcMock.SetupGet(x => x.RequestFrame).Returns(RequestFrame.E4);
        var e4FrameSelector2 = new RequestFrameSelector(
            plc: plcMock.Object,
            commandPacketBuilder: commandPacketBuilder
        );

        CollectionAssert.AreEqual(new byte[] { 0x54, 0x00, 0x02, 0x00, 0x00, 0x00 }, e4FrameSelector2.GetRequestPacket().Take(6).ToArray());
    }
}
