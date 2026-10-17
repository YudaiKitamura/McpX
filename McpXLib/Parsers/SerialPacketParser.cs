using McpXLib.Abstracts;
using McpXLib.Exceptions;

namespace McpXLib.Parsers;

internal class SerialPacketParser : BasePacketParser
{
    internal override int BinaryLength => 4;
    internal override int AsciiLength => 8;
    private ushort serialNumber;

    internal SerialPacketParser(ushort serialNumber, BasePacketParser? prevPacketParser = null, bool isAscii = false) : base(
        isAscii: isAscii,
        prevPacketParser: prevPacketParser,
        isReverse: false
    )
    {
        this.serialNumber = serialNumber;
    }

    internal override void Validation(byte[] bytes)
    {
        if (BitConverter.ToUInt16(bytes, 0) != serialNumber) 
        {
            throw new ReceivePacketException("Received packet had an invalid serial number.");
        }
    }
}
