namespace McpXLib.Interfaces;

internal interface IPlcTransport : IDisposable
{
    byte[] Request(byte[] packet, IReceiveLengthParser receiveLengthParser);
    
    Task<byte[]> RequestAsync(byte[] packet, IReceiveLengthParser receiveLengthParser, CancellationToken cancellationToken);
}
