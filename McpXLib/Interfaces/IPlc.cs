using McpXLib.Builders;

namespace McpXLib.Interfaces;

public interface IPlc 
{
    bool IsAscii { get; set; }
    RoutePacketBuilder Route { get; set; }
    Task<byte[]> RequestAsync(byte[] packet); 
    byte[] Request(byte[] packet);
}
