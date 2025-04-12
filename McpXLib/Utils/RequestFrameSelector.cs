using McpXLib.Builders;
using McpXLib.Enums;
using McpXLib.Interfaces;

namespace McpXLib.Utils;

public class RequestFrameSelector 
{
    private readonly RequestPacketBuilder requestPacketBuilder;
    private static ushort uniqSerialNumber;
    private ushort serialNumber;
    private IPlc plc;

    public RequestFrameSelector(IPlc plc, CommandPacketBuilder commandPacketBuilder)
    {
        this.plc = plc;

        serialNumber = uniqSerialNumber;

        requestPacketBuilder = new RequestPacketBuilder(
            subHeaderPacketBuilder: this.plc.RequestFrame == RequestFrame.E3 ? new SubHeaderPacketBuilder() : new SubHeader4EPacketBuilder(uniqSerialNumber),
            routePacketBuilder: this.plc.Route,
            commandPacketBuilder: commandPacketBuilder
        );

        if (uniqSerialNumber == ushort.MaxValue) 
        {
            uniqSerialNumber = ushort.MinValue;
        }
        else
        {
            uniqSerialNumber++;
        }
    }

    public byte[] GetRequestPacket()
    {
        return plc.IsAscii ? requestPacketBuilder.ToAsciiBytes() : requestPacketBuilder.ToBinaryBytes();
    }

    public ushort GetSerialNumber()
    {
        return serialNumber;
    }
}
