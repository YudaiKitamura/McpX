using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Parsers;

namespace McpXLib.Utils;

public class ResponseFrameSelector
{
    private readonly IPacketParser packetParser;

    public ResponseFrameSelector(IPlc plc, ushort serialNumber, DeviceAccessMode deviceAccessMode)
    {
        if (plc.RequestFrame == RequestFrame.E3) 
        {
            packetParser = new E3FramePacketParser(
                plc: plc,
                deviceAccessMode: deviceAccessMode
            );
        }
        else
        {
            packetParser = new E4FramePacketParser(
                plc: plc,
                serialNumber: serialNumber,
                deviceAccessMode
            );
        }
    }

    public ResponseFrameSelector(IPlc plc, ushort serialNumber, int wordLength, int doubleWordLength)
    {
        if (plc.RequestFrame == RequestFrame.E3) 
        {
            packetParser = new E3FramePacketParser(
                plc: plc,
                wordLength: wordLength,
                doubleWordLength: doubleWordLength
            );
        }
        else
        {
            packetParser = new E4FramePacketParser(
                plc: plc,
                serialNumber: serialNumber,
                wordLength: wordLength,
                doubleWordLength: doubleWordLength
            );
        }
    }

    public byte[] ParsePacket(byte[] bytes)
    {
        return packetParser.ParsePacket(bytes);
    }
}
