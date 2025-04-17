namespace McpXLib.Interfaces;

public interface IReceiveLengthParser
{
    ushort GetHeaderLength();
    ushort ParseContentLength(byte[] bytes);
}
