using McpXLib.Abstructs;

namespace McpXLib.Parsers;

public sealed class ResponsePacketParser(byte[] bytes) : BasePacketParser(bytes)
{
}
