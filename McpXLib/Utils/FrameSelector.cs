using McpXLib.Builders;
using McpXLib.Enums;
using McpXLib.Interfaces;

namespace McpXLib.Utils;

public class FrameSelector 
{
    private RequestPacketBuilder requestPacketBuilder;
    public static ushort serialNumber;

    public FrameSelector(IPlc plc, CommandPacketBuilder commandPacketBuilder)
    {
        switch (plc.RequestFrame)
        {
            default:
                requestPacketBuilder = new RequestPacketBuilder(
                    subHeaderPacketBuilder: new SubHeaderPacketBuilder(),
                    routePacketBuilder: plc.Route,
                    commandPacketBuilder: commandPacketBuilder
                );
                break;

            case RequestFrame.E4:
                requestPacketBuilder = new RequestPacketBuilder(
                    subHeaderPacketBuilder: new SubHeader4EPacketBuilder(serialNumber),
                    routePacketBuilder: plc.Route,
                    commandPacketBuilder: commandPacketBuilder
                );
                break;
        }

        if (serialNumber == ushort.MaxValue) 
        {
            serialNumber = ushort.MinValue;
        }
        else
        {
            serialNumber++;
        }
    }

    public RequestPacketBuilder GetPacketBuilder()
    {
        return requestPacketBuilder;
    }

    public ushort GetSerialNumber()
    {
        return (ushort)(serialNumber - 1);
    }
}
