using McpXLib.Abstructs;
using McpXLib.Interfaces;

namespace McpXLib.Parsers;

internal class RoutePacketParser : BasePacketParser
{
    internal override int BinaryLength => 5;
    internal override int AsciiLength => 10;

    internal RoutePacketParser(IPacketParser? prevPacketParser = null, bool isAscii = false) : base(
        isAscii: isAscii,
        prevPacketParser: prevPacketParser,
        isReverse: false
    )
    {
    }
}
