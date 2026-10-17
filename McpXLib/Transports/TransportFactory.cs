using McpXLib.Interfaces;

namespace McpXLib.Transports;

/// <summary>
/// 接続（トランスポートの生成）処理。接続時と、リモートRESET 後の再接続に使う。
/// </summary>
internal sealed class TransportFactory(Func<IPlcTransport> create, Func<CancellationToken, Task<IPlcTransport>> createAsync)
{
    internal IPlcTransport Create() => create();

    internal Task<IPlcTransport> CreateAsync(CancellationToken cancellationToken) => createAsync(cancellationToken);

    internal static TransportFactory For(string host, int port, int timeout, bool isUdp)
    {
        return isUdp
            ? new TransportFactory(
                () => UdpPlcTransport.Create(host, port, timeout),
                async cancellationToken => await UdpPlcTransport.CreateAsync(host, port, timeout, cancellationToken).ConfigureAwait(false))
            : new TransportFactory(
                () => TcpPlcTransport.Connect(host, port, timeout),
                async cancellationToken => await TcpPlcTransport.ConnectAsync(host, port, timeout, cancellationToken).ConfigureAwait(false));
    }

    // 同期の生成処理だけを持つ場合（テスト用）
    internal static TransportFactory From(Func<IPlcTransport> create)
    {
        return new TransportFactory(create, _ => Task.FromResult(create()));
    }
}
