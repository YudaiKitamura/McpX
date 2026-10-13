using McpXLib.Abstracts;

namespace McpXLib.Parsers;

internal class RoutePacketParser : BasePacketParser
{
    internal override int BinaryLength => 5;
    internal override int AsciiLength => 10;

    internal RoutePacketParser(BasePacketParser? prevPacketParser = null, bool isAscii = false) : base(
        isAscii: isAscii,
        prevPacketParser: prevPacketParser,
        isReverse: false
    )
    {
    }
}
