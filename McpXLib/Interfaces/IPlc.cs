using McpXLib.Enums;

namespace McpXLib.Interfaces;

public interface IPlc 
{
    bool IsAscii { get; set; }
    IPacketBuilder Route { get; set; }
    RequestFrame RequestFrame { get; set; }
    Task<byte[]> RequestAsync(byte[] packet); 
    byte[] Request(byte[] packet);
}
