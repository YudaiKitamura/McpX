using McpXLib.Abstructs;
using McpXLib.Enums;
using McpXLib.Interfaces;

namespace McpXLib.Parsers;

internal class E4FramePacketParser : IPacketParser, IReceiveLengthParser
{
    private SubHeaderPacketParser subHeaderPacketParser;
    private SerialPacketParser serialPacketParser;
    private RoutePacketParser routePacketParser;
    private ContentLengthPacketParser contentLengthPacketParser;
    private ErrorCodePacketParser errorCodePacketParser;
    private BaseContentPacketParser? contentPacketParser;

    internal E4FramePacketParser(IPlc plc, ushort serialNumber)
    {
        subHeaderPacketParser = new SubHeaderPacketParser(
            requestFrame: plc.RequestFrame,
            isAscii: plc.IsAscii
        );

        serialPacketParser = new SerialPacketParser(
            serialNumber: serialNumber,
            prevPacketParser: subHeaderPacketParser,
            isAscii: plc.IsAscii
        );

        routePacketParser = new RoutePacketParser(
            prevPacketParser: serialPacketParser,
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

    internal E4FramePacketParser(IPlc plc, ushort serialNumber, DeviceAccessMode deviceAccessMode = DeviceAccessMode.Word) : this (
        plc: plc,
        serialNumber: serialNumber
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

    internal E4FramePacketParser(IPlc plc, ushort serialNumber, int wordLength, int doubleWordLength) : this (
        plc: plc,
        serialNumber: serialNumber
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
        serialPacketParser.ParsePacket(bytes);
        errorCodePacketParser.ParsePacket(bytes);
        
        if (contentPacketParser != null) 
        {
            contentPacketParser.Length = BitConverter.ToUInt16(contentLengthPacketParser.ParsePacket(bytes), 0) - (ushort)errorCodePacketParser.GetLength();
            return contentPacketParser.ParsePacket(bytes);
        }

        return [];
    }

    public ushort GetHeaderLength()
    {
        return (ushort)(subHeaderPacketParser.GetLength() + serialPacketParser.GetLength() + routePacketParser.GetLength() + contentLengthPacketParser.GetLength());
    } 

    public ushort ParseContentLength(byte[] bytes)
    {
        return BitConverter.ToUInt16(contentLengthPacketParser.ParsePacket(bytes), 0);
    } 
}
