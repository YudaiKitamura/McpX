using System.Diagnostics;
using System.Net.Sockets;
using McpXLib.Interfaces;

namespace McpXLib.Transports;

internal class TcpPlcTransport : IPlcTransport
{
    private const int RECIVE_TIMEOUT_MS = 1000; 
    private readonly TcpClient client;
    private readonly NetworkStream stream;
    private readonly object syncLock = new();

    internal TcpPlcTransport(string ip, int port)
    {
        client = new TcpClient();
        client.Connect(ip, port);
        stream = client.GetStream();
    }

    [Obsolete]
    public byte[] Request(byte[] packet)
    {
        lock (syncLock)
        {
            stream.Write(packet, 0, packet.Length);

            var memoryStream = new MemoryStream();
            var buffer = new byte[1024];
            int totalTimeout = 1000;
            int readTimeout = 100;
            var stopwatch = Stopwatch.StartNew();

            while (true)
            {
                if (stream.DataAvailable)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        memoryStream.Write(buffer, 0, bytesRead);
                        stopwatch.Restart();
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    if (stopwatch.ElapsedMilliseconds > readTimeout)
                    {
                        break;
                    }
                    Thread.Sleep(1);
                }

                if (stopwatch.ElapsedMilliseconds > totalTimeout)
                {
                    throw new TimeoutException("Recive Timeout");
                }
            }

            return memoryStream.ToArray();
        }
    }

    [Obsolete]
    public async Task<byte[]> RequestAsync(byte[] packet)
    {
        await stream.WriteAsync(packet, 0, packet.Length);
        await Task.Delay(100);

        using MemoryStream memoryStream = new();
        byte[] buffer = new byte[1024];
        int bytesRead;

        while (stream.DataAvailable && (bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            await memoryStream.WriteAsync(buffer, 0, bytesRead);
        }

        return memoryStream.ToArray();
    }

    public void Dispose()
    {
        stream.Dispose();
        client.Dispose();
    }

    public byte[] Request(byte[] packet, IReceiveLengthParser contentLength)
    {
        lock (syncLock)
        {
            stream.Write(packet, 0, packet.Length);

            var headerBytes = GetReceivePacket(contentLength.GetHeaderLength());

            var length = contentLength.ParseContentLength(headerBytes);

            return headerBytes.Concat(GetReceivePacket(length)).ToArray();

        }
    }

    public async Task<byte[]> RequestAsync(byte[] packet, IReceiveLengthParser contentLength)
    {
        await stream.WriteAsync(packet, 0, packet.Length);

        var headerBytes = await GetReceivePacketAsync(contentLength.GetHeaderLength());

        var length = contentLength.ParseContentLength(headerBytes);

        return headerBytes.Concat(await GetReceivePacketAsync(length)).ToArray();
    }

    private byte[] GetReceivePacket(int expectedLength)
    {
        var memoryStream = new MemoryStream();
        var buffer = new byte[1024];
        int totalRead = 0;
        var stopwatch = Stopwatch.StartNew();

        while (totalRead < expectedLength)
        {
            if (stopwatch.ElapsedMilliseconds > RECIVE_TIMEOUT_MS)
            {
                throw new TimeoutException("Recive Timeout");
            }

            if (stream.DataAvailable) 
            {
                int bytesRead = stream.Read(buffer, 0, Math.Min(buffer.Length, expectedLength - totalRead));
                if (bytesRead == 0)
                {
                    throw new IOException("Connection closed unexpectedly");
                }
                memoryStream.Write(buffer, 0, bytesRead);
                totalRead += bytesRead;
            }
        }

        return memoryStream.ToArray();
    }

    private async Task<byte[]> GetReceivePacketAsync(int expectedLength)
    {
        var memoryStream = new MemoryStream();
        var buffer = new byte[1024];
        int totalRead = 0;
        var stopwatch = Stopwatch.StartNew();

        while (totalRead < expectedLength)
        {
            if (stopwatch.ElapsedMilliseconds > RECIVE_TIMEOUT_MS)
            {
                throw new TimeoutException("Recive Timeout");
            }

            if (stream.DataAvailable) 
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, Math.Min(buffer.Length, expectedLength - totalRead));
                if (bytesRead == 0)
                {
                    throw new IOException("Connection closed unexpectedly");
                }
                await memoryStream.WriteAsync(buffer, 0, bytesRead);
                totalRead += bytesRead;
            }
        }

        return memoryStream.ToArray();
    }
}
