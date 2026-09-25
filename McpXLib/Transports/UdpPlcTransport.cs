using System.Net;
using System.Net.Sockets;
using McpXLib.Interfaces;
using McpXLib.Utils;

namespace McpXLib.Transports;

internal class UdpPlcTransport : IPlcTransport
{
    private UdpClient udp;
    private readonly IPEndPoint remoteEndPoint;
    private readonly ushort timeout;

    internal UdpPlcTransport(string ip, int port, ushort timeout)
    {
        this.timeout = timeout;
        udp = CreateClient();
        remoteEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
    }

    private UdpClient CreateClient()
    {
        var client = new UdpClient();
        client.Client.ReceiveTimeout = timeout;
        return client;
    }

    public byte[] Request(byte[] packet)
    {
        udp.Send(packet, packet.Length, remoteEndPoint);

        IPEndPoint remote = remoteEndPoint;
        return udp.Receive(ref remote);
    }

    public async Task<byte[]> RequestAsync(byte[] packet)
    {
        await udp.SendAsync(packet, packet.Length, remoteEndPoint);

        using var cts = new CancellationTokenSource();
        var receiveTask = udp.ReceiveAsync();
        var delayTask = Task.Delay(timeout, cts.Token);

        var completed = await Task.WhenAny(receiveTask, delayTask);

        if (completed == delayTask)
        {
            // 保留中の受信が次の応答を横取りしないよう、ソケットを作り直す
            receiveTask.ObserveException();
            udp.Dispose();
            udp = CreateClient();
            throw new SocketException((int)SocketError.TimedOut);
        }

        cts.Cancel();
        return (await receiveTask).Buffer;
    }

    public void Dispose()
    {
        udp.Dispose();
    }

    public byte[] Request(byte[] packet, IReceiveLengthParser receiveLengthParser)
    {
        return Request(packet);
    }

    public async Task<byte[]> RequestAsync(byte[] packet, IReceiveLengthParser receiveLengthParser)
    {
        return await RequestAsync(packet);
    }
}
