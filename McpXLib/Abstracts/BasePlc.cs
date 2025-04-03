using McpXLib.Interfaces;

namespace McpXLib.Abstructs;

public abstract class BasePlc : IDisposable
{
    private readonly IPlcTransport transport;

    protected BasePlc(IPlcTransport transport)
    {
        this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
    }

    public async Task<byte[]> RequestAsync(byte[] packet)
    {
        return await transport.RequestAsync(packet);
    }

    public byte[] Request(byte[] packet)
    {
        return transport.Request(packet);
    }

    public virtual void Dispose()
    {
        transport.Dispose();
    }
}
