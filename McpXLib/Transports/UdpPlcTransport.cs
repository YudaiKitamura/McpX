using System.Net;
using System.Net.Sockets;
using McpXLib.Interfaces;

namespace McpXLib.Transports;

internal class UdpPlcTransport : IPlcTransport
{
    private readonly UdpClient udp;
    private readonly IPEndPoint remoteEndPoint;

    internal UdpPlcTransport(string ip, int port, ushort timeout)
    {
        udp = new UdpClient();
        udp.Client.ReceiveTimeout = timeout;
        remoteEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
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
        var delayTask = Task.Delay(udp.Client.ReceiveTimeout, cts.Token);

        var completed = await Task.WhenAny(receiveTask, delayTask);

        if (completed == delayTask)
        {
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
