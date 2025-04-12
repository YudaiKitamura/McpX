using System.Net;
using System.Net.Sockets;
using McpXLib.Interfaces;

namespace McpXLib.Transports;

internal class UdpPlcTransport : IPlcTransport
{
    private readonly UdpClient udp;
    private readonly IPEndPoint remoteEndPoint;

    internal UdpPlcTransport(string ip, int port)
    {
        udp = new UdpClient();
        remoteEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
    }

    public byte[] Request(byte[] packet)
    {
        udp.Send(packet, packet.Length, remoteEndPoint);
        udp.Client.ReceiveTimeout = 1000;

        IPEndPoint remote = remoteEndPoint;
        return udp.Receive(ref remote);
    }

    public async Task<byte[]> RequestAsync(byte[] packet)
    {
        await udp.SendAsync(packet, packet.Length, remoteEndPoint);
        var result = await udp.ReceiveAsync();
        return result.Buffer;
    }

    public void Dispose()
    {
        udp.Dispose();
    }
}
