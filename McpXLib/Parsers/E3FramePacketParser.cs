using McpXLib.Abstructs;
using McpXLib.Enums;
using McpXLib.Interfaces;

namespace McpXLib.Parsers;

internal class E3FramePacketParser : IPacketParser
{
    private SubHeaderPacketParser subHeaderPacketParser;
    private RoutePacketParser routePacketParser;
    private ContentLengthPacketParser contentLengthPacketParser;
    private ErrorCodePacketParser errorCodePacketParser;
    private BaseContentPacketParser? contentPacketParser;

    internal E3FramePacketParser(IPlc plc)
    {
        subHeaderPacketParser = new SubHeaderPacketParser(
            requestFrame: plc.RequestFrame,
            isAscii: plc.IsAscii
        );

        routePacketParser = new RoutePacketParser(
            prevPacketParser: subHeaderPacketParser,
            isAscii: plc.IsAscii
        );

        contentLengthPacketParser = new ContentLengthPacketParser(
            prevPacketParser: routePacketParser,
            isAscii: plc.IsAscii
        );

        errorCodePacketParser = new ErrorCodePacketParser(
            prevPacketParser: contentLengthPacketParser,
            isAscii: plc.IsAscii
        );
    }

    internal E3FramePacketParser(IPlc plc, DeviceAccessMode deviceAccessMode = DeviceAccessMode.Word) : this (
        plc: plc
    )
    {
        switch (deviceAccessMode) 
        {
            default:
                contentPacketParser = new WordContentPacketParser(
                    prevPacketParser: errorCodePacketParser,
                    isAscii: plc.IsAscii
                );
                break;
            
            case DeviceAccessMode.Bit:
                contentPacketParser = new BitContentPacketParser(
                    prevPacketParser: errorCodePacketParser,
                    isAscii: plc.IsAscii
                );
                break;
        }
    }

    internal E3FramePacketParser(IPlc plc, int wordLength, int doubleWordLength) : this (
        plc: plc
    )
    {
        contentPacketParser = new RandomContentPacketParser(
            prevPacketParser: errorCodePacketParser,
            isAscii: plc.IsAscii,
            wordLength: wordLength,
            doubleWordLength: doubleWordLength
        );
    }

    public int GetIndex()
    {
        throw new NotImplementedException();
    }

    public int GetLength()
    {
        throw new NotImplementedException();
    }

    public byte[] ParsePacket(byte[] bytes)
    {
        subHeaderPacketParser.ParsePacket(bytes);
        errorCodePacketParser.ParsePacket(bytes);
        
        if (contentPacketParser != null) 
        {
            contentPacketParser.Length = BitConverter.ToUInt16(contentLengthPacketParser.ParsePacket(bytes), 0) - (ushort)errorCodePacketParser.GetLength();
            return contentPacketParser.ParsePacket(bytes);
        }

        return [];
    }
}
