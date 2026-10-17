using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace TestMcpX;

/// <summary>
/// テスト用の UDP サーバーのソケットを作ります。
/// </summary>
internal static class UdpTestServer
{
    // WSAIoctl の SIO_UDP_CONNRESET
    private const int SIO_UDP_CONNRESET = -1744830452;

    /// <summary>
    /// <paramref name="endPoint"/> で待ち受ける <see cref="UdpClient"/> を作ります。
    /// </summary>
    /// <remarks>
    /// Windows では、送信先のポートが閉じていると ICMP の到達不能通知で次の受信が <see cref="SocketError.ConnectionReset"/> になる。
    /// クライアントがソケットを作り直した後に、古いポートへ遅れて応答するテストでサーバーが止まらないよう、この通知を無効にする。
    /// </remarks>
    internal static UdpClient Create(IPEndPoint endPoint)
    {
        var server = new UdpClient(endPoint);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            server.Client.IOControl(SIO_UDP_CONNRESET, [0, 0, 0, 0], null);
        }

        return server;
    }

    internal static UdpClient Create() => Create(new IPEndPoint(IPAddress.Any, 0));
}
