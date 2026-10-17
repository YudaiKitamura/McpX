using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using McpXLib.Exceptions;
using McpXLib.Interfaces;
using McpXLib.Utils;

namespace McpXLib.Transports;

internal class UdpPlcTransport : IPlcTransport
{
    private UdpClient udp;
    private readonly IPEndPoint remoteEndPoint;
    // 1要求（送信開始〜応答受信完了）の期限（ミリ秒）。0 は無期限。
    private readonly int timeout;
    // 同期・非同期の要求を1つずつ処理する（応答の取り違えを防ぐ）。
    private readonly SemaphoreSlim gate = new(1, 1);
    // ソケットの作り直しと破棄を排他し、破棄後に作り直したソケットが残らないようにする。
    private readonly object socketLock = new();
    private bool disposed;

    private UdpPlcTransport(IPEndPoint remoteEndPoint, int timeout)
    {
        this.remoteEndPoint = remoteEndPoint;
        this.timeout = timeout;
        udp = CreateClient();
    }

    /// <summary>
    /// 接続先を決めてソケットを作ります。<paramref name="host"/> は IP アドレスまたはホスト名です。
    /// </summary>
    internal static UdpPlcTransport Create(string host, int port, int timeout)
    {
        var address = IPAddress.TryParse(host, out var parsed) ? parsed : SelectAddress(Dns.GetHostAddresses(host), host);
        return new UdpPlcTransport(new IPEndPoint(address, port), timeout);
    }

    internal static async Task<UdpPlcTransport> CreateAsync(string host, int port, int timeout, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var address = IPAddress.TryParse(host, out var parsed) ? parsed : SelectAddress(await Dns.GetHostAddressesAsync(host).ConfigureAwait(false), host);
        return new UdpPlcTransport(new IPEndPoint(address, port), timeout);
    }

    // ホスト名は IPv4 のアドレスを優先し、なければ IPv6 のアドレスを使う。
    private static IPAddress SelectAddress(IPAddress[] addresses, string host)
    {
        return addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork)
            ?? addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetworkV6)
            ?? throw new SocketException((int)SocketError.HostNotFound);
    }

    private const string TimeoutMessage = "The request timed out.";

    // 接続先のアドレスファミリー（IPv4 / IPv6）でソケットを作る。
    private UdpClient CreateClient()
    {
        return new UdpClient(remoteEndPoint.AddressFamily);
    }

    // 失敗した要求の応答が遅れて届いても次の要求が受け取らないよう、ソケットを作り直す（新しい送信元ポートになる）。
    private void RecreateClient()
    {
        lock (socketLock)
        {
            udp.Dispose();
            if (!disposed)
            {
                udp = CreateClient();
            }
        }
    }

    private int GetRemaining(Stopwatch elapsed)
    {
        if (timeout == 0)
        {
            return 0; // Socket.ReceiveTimeout の 0 は無期限
        }

        long remaining = timeout - elapsed.ElapsedMilliseconds;
        if (remaining <= 0)
        {
            throw new TimeoutException(TimeoutMessage, new SocketException((int)SocketError.TimedOut));
        }

        return (int)remaining;
    }

    private byte[] SendReceive(byte[] packet, IReceiveLengthParser receiveLengthParser)
    {
        var elapsed = Stopwatch.StartNew();
        udp.Send(packet, packet.Length, remoteEndPoint);

        while (true)
        {
            udp.Client.ReceiveTimeout = GetRemaining(elapsed);

            IPEndPoint remote = remoteEndPoint;
            byte[] datagram;
            try
            {
                datagram = udp.Receive(ref remote);
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
            {
                throw new TimeoutException(TimeoutMessage, ex);
            }

            // 接続先以外から届いたデータグラムは無視する
            if (remote.Equals(remoteEndPoint))
            {
                return Validate(datagram, receiveLengthParser);
            }
        }
    }

    private async Task<byte[]> SendReceiveAsync(byte[] packet, IReceiveLengthParser receiveLengthParser, CancellationToken cancellationToken)
    {
        var elapsed = Stopwatch.StartNew();
        await udp.SendAsync(packet, packet.Length, remoteEndPoint).ConfigureAwait(false);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        // timeout が 0 の場合は、同期版や TCP と同じく無期限とする
        var delayTask = Task.Delay(timeout == 0 ? Timeout.Infinite : Math.Max(0, timeout - (int)elapsed.ElapsedMilliseconds), cts.Token);

        try
        {
            while (true)
            {
                var receiveTask = udp.ReceiveAsync();
                var completed = await Task.WhenAny(receiveTask, delayTask).ConfigureAwait(false);

                if (completed == delayTask)
                {
                    // 保留中の受信は、ソケットの作り直し（呼び出し元）で打ち切られる
                    receiveTask.ObserveException();
                    cancellationToken.ThrowIfCancellationRequested();
                    throw new TimeoutException(TimeoutMessage, new SocketException((int)SocketError.TimedOut));
                }

                var result = await receiveTask.ConfigureAwait(false);

                // 接続先以外から届いたデータグラムは無視する
                if (result.RemoteEndPoint.Equals(remoteEndPoint))
                {
                    return Validate(result.Buffer, receiveLengthParser);
                }
            }
        }
        finally
        {
            // 受信エラー・不正な応答で抜けた場合も、待機用のタイマーを止める
            cts.Cancel();
        }
    }

    // データグラムの長さが、ヘッダに書かれた応答データ長と一致するかを確認する。
    private static byte[] Validate(byte[] datagram, IReceiveLengthParser receiveLengthParser)
    {
        int headerLength = receiveLengthParser.GetHeaderLength();
        if (datagram.Length < headerLength)
        {
            throw new ReceivePacketException("Received packet was shorter than the header.");
        }

        var header = new byte[headerLength];
        Array.Copy(datagram, header, headerLength);
        if (datagram.Length != headerLength + receiveLengthParser.ParseContentLength(header))
        {
            throw new ReceivePacketException("Received packet length did not match the response data length.");
        }

        return datagram;
    }

    public void Dispose()
    {
        lock (socketLock)
        {
            disposed = true;
            udp.Dispose();
        }
    }

    public byte[] Request(byte[] packet, IReceiveLengthParser receiveLengthParser)
    {
        gate.Wait();
        try
        {
            try
            {
                return SendReceive(packet, receiveLengthParser);
            }
            catch
            {
                // タイムアウト・受信エラー・不正な応答のいずれでも、応答が遅れて届く可能性があるため作り直す
                RecreateClient();
                throw;
            }
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<byte[]> RequestAsync(byte[] packet, IReceiveLengthParser receiveLengthParser, CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            try
            {
                return await SendReceiveAsync(packet, receiveLengthParser, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // タイムアウト・キャンセル・受信エラー・不正な応答のいずれでも、応答が遅れて届く可能性があるため作り直す
                RecreateClient();
                throw;
            }
        }
        finally
        {
            gate.Release();
        }
    }
}
