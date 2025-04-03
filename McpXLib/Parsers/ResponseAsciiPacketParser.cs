using McpXLib.Abstructs;

namespace McpXLib.Parsers;

public sealed class ResponseAsciiPacketParser(byte[] bytes) : BaseAsciiPacketParser(bytes)
{
}
