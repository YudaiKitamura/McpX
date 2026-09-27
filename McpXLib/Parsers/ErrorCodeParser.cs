using McpXLib.Abstructs;
using McpXLib.Exceptions;
using McpXLib.Interfaces;

namespace McpXLib.Parsers;

internal class ErrorCodePacketParser : BasePacketParser
{
    internal override int BinaryLength => 2;
    internal override int AsciiLength => 4;

    internal ErrorCodePacketParser(IPacketParser? prevPacketParser = null, bool isAscii = false) : base(
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
            ushort code = BitConverter.ToUInt16(errorCode, 0);
            throw new McProtocolException($"An error code was received from PLC. ({ code.ToString("X") })", code);
        }
    }
}
