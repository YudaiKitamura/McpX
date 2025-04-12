using System.Diagnostics;
using System.Net.Sockets;
using McpXLib.Interfaces;

namespace McpXLib.Transports;

internal class TcpPlcTransport : IPlcTransport
{
    private readonly TcpClient client;
    private readonly NetworkStream stream;
    private readonly object syncLock = new();

    internal TcpPlcTransport(string ip, int port)
    {
        client = new TcpClient();
        client.Connect(ip, port);
        stream = client.GetStream();
    }

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
}
