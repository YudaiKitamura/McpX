using McpXLib.Enums;

namespace McpXLib.Interfaces;

internal interface IPlc 
{
    bool IsAscii { get; }
    IPacketBuilder Route { get; }
    RequestFrame RequestFrame { get; }
    
    byte[] Request(byte[] packet, IReceiveLengthParser receiveLengthParser);
    
    Task<byte[]> RequestAsync(byte[] packet, IReceiveLengthParser receiveLengthParser, CancellationToken cancellationToken);
}
