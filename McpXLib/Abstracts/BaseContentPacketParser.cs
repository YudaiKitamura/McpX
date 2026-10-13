using McpXLib.Exceptions;
using McpXLib.Interfaces;

namespace McpXLib.Abstracts;

internal class BaseContentPacketParser : BasePacketParser
{
    internal override int BinaryLength => binaryLength;
    internal override int AsciiLength => asciiLength;
    private int binaryLength;
    private int asciiLength;

    internal int Length
    {
        get 
        {
            return IsAscii ? AsciiLength : BinaryLength;
        }
        set
        {
            _ = IsAscii ? asciiLength = value : binaryLength = value;
        }
    }

    internal BaseContentPacketParser(IPacketParser prevPacketParser, bool isAscii = false, bool? isReverse = null) : base (
        isAscii: isAscii,
        prevPacketParser: prevPacketParser,
        isReverse: isReverse
    )
    {
    }

    internal override void Validation(byte[] bytes)
    {
        if (bytes.Length != (IsAscii ? Length / 2 : Length)) 
        {
            throw new ReceivePacketException("Received packet had an invalid content.");
        }
    }
}
