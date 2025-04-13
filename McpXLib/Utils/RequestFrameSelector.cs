using McpXLib.Builders;
using McpXLib.Enums;
using McpXLib.Interfaces;

namespace McpXLib.Utils;

internal class RequestFrameSelector 
{
    private readonly RequestPacketBuilder requestPacketBuilder;
    private static int uniqSerialNumber;
    private ushort serialNumber;
    private IPlc plc;

    internal RequestFrameSelector(IPlc plc, CommandPacketBuilder commandPacketBuilder)
    {
        this.plc = plc;

        serialNumber = (ushort)uniqSerialNumber;

        requestPacketBuilder = new RequestPacketBuilder(
            subHeaderPacketBuilder: this.plc.RequestFrame == RequestFrame.E3 ? new SubHeaderPacketBuilder() : new SubHeader4EPacketBuilder(serialNumber),
            routePacketBuilder: this.plc.Route,
            commandPacketBuilder: commandPacketBuilder
        );

        var current = Interlocked.Increment(ref uniqSerialNumber);
        if (current > ushort.MaxValue)
        {
            Interlocked.Exchange(ref uniqSerialNumber, ushort.MinValue);
        }
    }

    internal byte[] GetRequestPacket()
    {
        return plc.IsAscii ? requestPacketBuilder.ToAsciiBytes() : requestPacketBuilder.ToBinaryBytes();
    }

    internal ushort GetSerialNumber()
    {
        return serialNumber;
    }
}
