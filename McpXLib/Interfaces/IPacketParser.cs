namespace McpXLib.Interfaces;

public interface IPacketParser
{
    public byte[] ParsePacket(byte[] bytes);

    public int GetIndex();

    public int GetLength();    
}
