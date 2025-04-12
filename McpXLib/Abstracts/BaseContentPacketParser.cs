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

    public BaseContentPacketParser(IPacketParser prevPacketParser, bool isAscii = false) : base (
        isAscii: isAscii,
        prevPacketParser: prevPacketParser
    )
    {
    }

    internal override void Validation(byte[] bytes)
    {
        if (bytes.Length != Length) 
        {
            throw new RecivePacketException("Received packet had an invalid content.");
        }
    }
}
