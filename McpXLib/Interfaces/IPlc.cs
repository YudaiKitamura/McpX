using McpXLib.Enums;

namespace McpXLib.Interfaces;

public interface IPlc 
{
    bool IsAscii { get; set; }
    IPacketBuilder Route { get; set; }
    RequestFrame RequestFrame { get; set; }
    
    [Obsolete]
    Task<byte[]> RequestAsync(byte[] packet); 
    
    [Obsolete]
    byte[] Request(byte[] packet);
    
    byte[] Request(byte[] packet, IReceiveLengthParser receiveLengthParser);
    
    Task<byte[]> RequestAsync(byte[] packet, IReceiveLengthParser receiveLengthParser);
}
