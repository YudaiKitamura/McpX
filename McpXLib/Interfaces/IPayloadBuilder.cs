namespace McpXLib.Interfaces;

public interface IPayloadBuilder
{
    void AppendPayload(List<byte> packets, bool isAscii);
}
