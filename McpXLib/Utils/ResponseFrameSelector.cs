using McpXLib.Exceptions;
using McpXLib.Enums;
using McpXLib.Interfaces;
using McpXLib.Parsers;

namespace McpXLib.Utils;

internal class ResponseFrameSelector : IReceiveLengthParser
{
    private readonly IPacketParser packetParser;

    internal ResponseFrameSelector(IPlc plc, ushort serialNumber)
    {
        if (plc.RequestFrame == RequestFrame.E3) 
        {
            packetParser = new E3FramePacketParser(
                plc: plc
            );
        }
        else
        {
            packetParser = new E4FramePacketParser(
                plc: plc,
                serialNumber: serialNumber
            );
        }
    }

    internal ResponseFrameSelector(IPlc plc, ushort serialNumber, DeviceAccessMode deviceAccessMode)
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

    internal ResponseFrameSelector(IPlc plc, ushort serialNumber, int wordLength, int doubleWordLength)
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

    public ushort ParseContentLength(byte[] bytes)
    {
        return WrapInvalidPacket(() => ((IReceiveLengthParser)packetParser).ParseContentLength(bytes));
    }

    public ushort GetHeaderLength()
    {
        return ((IReceiveLengthParser)packetParser).GetHeaderLength();
    }

    internal byte[] ParsePacket(byte[] bytes)
    {
        return WrapInvalidPacket(() => packetParser.ParsePacket(bytes));
    }

    /// <summary>
    /// 壊れた応答（短い応答、ASCII の16進以外の文字など）の解析で発生した例外を、<see cref="ReceivePacketException"/> に変換します。
    /// </summary>
    internal static T WrapInvalidPacket<T>(Func<T> parse)
    {
        try
        {
            return parse();
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException or IndexOutOfRangeException or OverflowException)
        {
            throw new ReceivePacketException("Received packet had an invalid content.", ex);
        }
    }
}
