using McpXLib.Abstructs;
using McpXLib.Interfaces;

namespace McpXLib.Parsers;

public class ContentLengthPacketParser : BasePacketParser2
{
    internal override int BinaryLength => 2;
    internal override int AsciiLength => 4;

    public ContentLengthPacketParser(IPacketParser? prevPacketParser = null, bool isAscii = false) : base(
        isAscii: isAscii,
        prevPacketParser: prevPacketParser,
        isReverse: true
    )
    {
    }
}
