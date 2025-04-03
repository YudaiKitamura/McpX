namespace McpXLib.Interfaces;

public interface IPlcTransport : IDisposable
{
    Task<byte[]> RequestAsync(byte[] packet);
    byte[] Request(byte[] packet);
}
