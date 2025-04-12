using McpXLib.Abstructs;
using McpXLib.Exceptions;
using McpXLib.Interfaces;

namespace McpXLib.Parsers;

public class ErrorCodePacketParser : BasePacketParser2
{
    internal override int BinaryLength => 2;
    internal override int AsciiLength => 4;

    public ErrorCodePacketParser(IPacketParser? prevPacketParser = null, bool isAscii = false) : base(
        isAscii: isAscii,
        prevPacketParser: prevPacketParser,
        isReverse: true
    )
    {
    }

    internal override void Validation(byte[] errorCode)
    {
        if (!errorCode.SequenceEqual(new byte[] { 0x00, 0x00 })) 
        {
            throw new McProtocolException($"An error code was received from PLC. ({ BitConverter.ToUInt16(errorCode, 0).ToString("X") })");
        }
    }
}
