using McpXLib.Abstructs;
using McpXLib.Interfaces;

namespace McpXLib.Parsers;

internal class ContentLengthPacketParser : BasePacketParser
{
    internal override int BinaryLength => 2;
    internal override int AsciiLength => 4;

    internal ContentLengthPacketParser(IPacketParser? prevPacketParser = null, bool isAscii = false) : base(
        isAscii: isAscii,
        prevPacketParser: prevPacketParser,
        isReverse: true
    )
    {
    }
}
