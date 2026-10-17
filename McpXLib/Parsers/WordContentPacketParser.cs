using McpXLib.Abstracts;

namespace McpXLib.Parsers;

internal class WordContentPacketParser : BaseContentPacketParser
{
    internal WordContentPacketParser(BasePacketParser prevPacketParser, bool isAscii = false) : base (
        isAscii: isAscii,
        prevPacketParser: prevPacketParser,
        isReverse: true
    )
    {
    }
}
