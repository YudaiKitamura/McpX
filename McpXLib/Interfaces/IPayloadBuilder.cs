namespace McpXLib.Interfaces;

internal interface IPayloadBuilder
{
    void AppendPayload(List<byte> packets, bool isAscii);
}
