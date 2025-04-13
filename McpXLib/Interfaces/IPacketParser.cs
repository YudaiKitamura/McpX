namespace McpXLib.Interfaces;

public interface IPacketParser
{
    byte[] ParsePacket(byte[] bytes);

    int GetIndex();

    int GetLength();    
}
