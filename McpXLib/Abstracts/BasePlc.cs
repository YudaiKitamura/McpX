using System.Net.Sockets;
using McpXLib.Exceptions;

namespace McpXLib.Abstructs;

public abstract class BasePlc : IDisposable
{
    public const int READ_BUFFER_LENGTH = 1024;
    private readonly string ip;
    private readonly int port;
    private static readonly Dictionary<(string, int), SemaphoreSlim> semaphoreDict = new();
    private TcpClient client;
    private NetworkStream networkStream;

    public BasePlc(string ip, int port)
    {
        if (!semaphoreDict.ContainsKey((ip, port)))
        {
            semaphoreDict[(ip, port)] = new SemaphoreSlim(1, 1);
        }
        else
        {
            throw new PlcDuplicationException();
        }

        this.ip = ip;
        this.port = port;
        
        client = new TcpClient();
        client.Connect(ip, port);
        networkStream = client.GetStream();
    }

    ~ BasePlc()
    {
        semaphoreDict.Remove((ip, port));
    }

    public async Task<byte[]> RequestAsync(byte[] packet) 
    {
        var semaphore = semaphoreDict[(ip, port)];
        await semaphore.WaitAsync();

        try 
        {
            await networkStream.WriteAsync(packet, 0, packet.Length);

            await Task.Delay(100);
            
            using (MemoryStream memoryStream = new MemoryStream())
            {
                byte[] buffer = new byte[READ_BUFFER_LENGTH];
                int bytesRead;

                while (networkStream.DataAvailable && (bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await memoryStream.WriteAsync(buffer, 0, bytesRead);
                }

                return memoryStream.ToArray();
            }
        } 
        finally
        {
            semaphore.Release();
        }
    }

    public byte[] Request(byte[] packet) 
    {
        var semaphore = semaphoreDict[(ip, port)];
        semaphore.Wait();

        try 
        {
            networkStream.Write(packet, 0, packet.Length);

            Thread.Sleep(100);

            using (MemoryStream memoryStream = new MemoryStream())
            {
                byte[] buffer = new byte[READ_BUFFER_LENGTH];
                int bytesRead;

                while (networkStream.DataAvailable && (bytesRead = networkStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    memoryStream.Write(buffer, 0, bytesRead);
                }

                return memoryStream.ToArray();
            }
        } 
        finally
        {
            semaphore.Release();
        }
    }

    public virtual void Dispose()
    {
        client.Dispose();
    }
}
