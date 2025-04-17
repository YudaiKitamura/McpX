namespace McpXLib.Interfaces;

public interface IPlcTransport : IDisposable
{
    [Obsolete]
    Task<byte[]> RequestAsync(byte[] packet);
    
    [Obsolete]
    byte[] Request(byte[] packet);

    byte[] Request(byte[] packet, IReceiveLengthParser receiveLengthParser);
    
    Task<byte[]> RequestAsync(byte[] packet, IReceiveLengthParser receiveLengthParser);
}
