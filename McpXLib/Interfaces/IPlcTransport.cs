namespace McpXLib.Interfaces;

public interface IPlcTransport : IDisposable
{
    byte[] Request(byte[] packet, IReceiveLengthParser receiveLengthParser);
    
    Task<byte[]> RequestAsync(byte[] packet, IReceiveLengthParser receiveLengthParser);
}
