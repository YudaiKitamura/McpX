using McpXLib.Abstructs;
using McpXLib.Exceptions;
using McpXLib.Interfaces;

namespace McpXLib.Parsers;

internal class SerialPacketParser : BasePacketParser, IPacketParser
{
    internal override int BinaryLength => 4;
    internal override int AsciiLength => 8;
    private ushort serialNumber;

    internal SerialPacketParser(ushort serialNumber, IPacketParser? prevPacketParser = null, bool isAscii = false) : base(
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
            throw new RecivePacketException("Received packet had an invalid serial number.");
        }
    }
}
