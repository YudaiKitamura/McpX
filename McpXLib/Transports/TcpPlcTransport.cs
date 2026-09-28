using System.Diagnostics;
using System.Net.Sockets;
using McpXLib.Interfaces;
using McpXLib.Utils;

namespace McpXLib.Transports;

internal class TcpPlcTransport : IPlcTransport
{
    private readonly TcpClient client;
    private readonly NetworkStream stream;
    // 同期・非同期の要求を1つずつ処理する（送受信の交錯を防ぐ）。
    private readonly SemaphoreSlim gate = new(1, 1);

    internal TcpPlcTransport(string ip, int port, ushort timeout)
    {
        client = new TcpClient();
        client.SendTimeout = timeout;
        client.ReceiveTimeout = timeout;

        var task = client.ConnectAsync(ip, port);
        if (!task.Wait(timeout))
        {
            task.ObserveException();
            client.Close();
            throw new TimeoutException("Connection Timeout");
        }
        stream = client.GetStream();
    }

    public void Dispose()
    {
        stream.Dispose();
        client.Dispose();
    }

    public byte[] Request(byte[] packet, IReceiveLengthParser contentLength)
    {
        gate.Wait();
        try
        {
            stream.Write(packet, 0, packet.Length);

            var headerBytes = GetReceivePacket(contentLength.GetHeaderLength());

            var length = contentLength.ParseContentLength(headerBytes);

            return headerBytes.Concat(GetReceivePacket(length)).ToArray();
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<byte[]> RequestAsync(byte[] packet, IReceiveLengthParser contentLength)
    {
        await gate.WaitAsync();
        try
        {
            await stream.WriteAsync(packet, 0, packet.Length);

            var headerBytes = await GetReceivePacketAsync(contentLength.GetHeaderLength());

            var length = contentLength.ParseContentLength(headerBytes);

            return headerBytes.Concat(await GetReceivePacketAsync(length)).ToArray();
        }
        finally
        {
            gate.Release();
        }
    }

    private byte[] GetReceivePacket(int expectedLength)
    {
        var memoryStream = new MemoryStream();
        var buffer = new byte[1024];
        int totalRead = 0;

        while (totalRead < expectedLength)
        {
            int bytesRead = stream.Read(buffer, 0, Math.Min(buffer.Length, expectedLength - totalRead));
            if (bytesRead == 0)
            {
                throw new IOException("Connection closed unexpectedly");
            }

            memoryStream.Write(buffer, 0, bytesRead);
            totalRead += bytesRead;
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
            int bytesRead = await stream.ReadAsync(buffer, 0, Math.Min(buffer.Length, expectedLength - totalRead));
            if (bytesRead == 0)
            {
                throw new IOException("Connection closed unexpectedly");
            }

            if (stopwatch.ElapsedMilliseconds > client.ReceiveTimeout)
            {
                throw new IOException("Unable to read data from the transport connection: Connection timed out.");
            }

            await memoryStream.WriteAsync(buffer, 0, bytesRead);
            totalRead += bytesRead;
        }

        return memoryStream.ToArray();
    }
}
