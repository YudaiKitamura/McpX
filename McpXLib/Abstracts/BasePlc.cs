using McpXLib.Interfaces;

namespace McpXLib.Abstructs;

/// <summary>
/// PLC通信抽象クラス
/// </summary> 
public abstract class BasePlc : IDisposable
{
    private readonly IPlcTransport transport;

    /// <summary>
    /// インスタンス初期化
    /// </summary>
    /// <param name="transport">通信トランスポートを指定します。</param>
    protected BasePlc(IPlcTransport transport)
    {
        this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
    }

    /// <summary>
    /// リクエスト送信（非同期）
    /// </summary>
    /// <remarks>
    /// 指定されたパケットを非同期でPLCに送信します。
    /// </remarks>
    /// <param name="packet">PLCに送信するリクエストパケットを指定します。</param>
    /// <returns>PLCから受信したレスポンスパケットを返します。</returns>
    public async Task<byte[]> RequestAsync(byte[] packet)
    {
        return await transport.RequestAsync(packet);
    }

    /// <summary>
    /// リクエスト送信
    /// </summary>
    /// <remarks>
    /// 指定されたパケットをPLCに送信します。
    /// </remarks>
    /// <param name="packet">PLCに送信するリクエストパケットを指定します。</param>
    /// <returns>PLCから受信したレスポンスパケットを返します。</returns>
    public byte[] Request(byte[] packet)
    {
        return transport.Request(packet);
    }

    /// <summary>
    /// インスタンス破棄
    /// </summary>
    /// <remarks>
    /// 使用済みのリソースを解放します。
    /// </remarks>
    public virtual void Dispose()
    {
        transport.Dispose();
    }
}
