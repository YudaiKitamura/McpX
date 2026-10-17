namespace McpXLib.Interfaces;

internal interface IPacketBuilder
{
    byte[] ToBinaryBytes();
    byte[] ToAsciiBytes();
}
