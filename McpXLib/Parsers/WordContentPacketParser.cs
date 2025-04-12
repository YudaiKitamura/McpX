using McpXLib.Abstructs;
using McpXLib.Interfaces;

namespace McpXLib.Parsers;

public class WordContentPacketParser : BaseContentPacketParser
{
    public WordContentPacketParser(IPacketParser prevPacketParser, bool isAscii = false) : base (
        isAscii: isAscii,
        prevPacketParser: prevPacketParser
    )
    {
    }
}
