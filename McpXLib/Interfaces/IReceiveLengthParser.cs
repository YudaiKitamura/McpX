namespace McpXLib.Interfaces;

internal interface IReceiveLengthParser
{
    ushort GetHeaderLength();
    ushort ParseContentLength(byte[] bytes);
}
