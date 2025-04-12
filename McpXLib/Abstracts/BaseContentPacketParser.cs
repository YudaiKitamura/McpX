using McpXLib.Exceptions;
using McpXLib.Interfaces;

namespace McpXLib.Abstructs;

public class BaseContentPacketParser : BasePacketParser2
{
    internal override int BinaryLength => binaryLength;
    internal override int AsciiLength => asciiLength;
    private int binaryLength;
    private int asciiLength;

    public int Length
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

    public BaseContentPacketParser(IPacketParser prevPacketParser, bool isAscii = false, bool? isReverse = null) : base (
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
            throw new RecivePacketException("Received packet had an invalid content.");
        }
    }
}
