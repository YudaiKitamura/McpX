namespace McpXLib.Interfaces;

internal interface IPacketParser
{
    byte[] ParsePacket(byte[] bytes);

    int GetIndex();

    int GetLength();    
}
