using McpXLib.Abstructs;
using McpXLib.Interfaces;

namespace McpXLib.Parsers;

public class RoutePacketParser : BasePacketParser2
{
    internal override int BinaryLength => 5;
    internal override int AsciiLength => 10;

    public RoutePacketParser(IPacketParser? prevPacketParser = null, bool isAscii = false) : base(
        isAscii: isAscii,
        prevPacketParser: prevPacketParser,
        isReverse: false
    )
    {
    }
}
