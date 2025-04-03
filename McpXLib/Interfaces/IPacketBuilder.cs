namespace McpXLib.Interfaces;

public interface IPacketBuilder
{
    byte[] ToBinaryBytes();
    byte[] ToAsciiBytes();
}
