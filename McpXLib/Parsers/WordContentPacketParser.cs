using McpXLib.Abstructs;
using McpXLib.Interfaces;

namespace McpXLib.Parsers;

internal class WordContentPacketParser : BaseContentPacketParser
{
    internal WordContentPacketParser(IPacketParser prevPacketParser, bool isAscii = false) : base (
        isAscii: isAscii,
        prevPacketParser: prevPacketParser,
        isReverse: true
    )
    {
    }
}
