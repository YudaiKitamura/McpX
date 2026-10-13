using McpXLib.Builders;
using McpXLib.Exceptions;
using McpXLib.Interfaces;
using McpXLib.Utils;

namespace McpXLib.Commands;

/// <summary>
/// コマンドの共通処理（要求の組み立て・送信・応答の解析）
/// </summary>
/// <remarks>
/// 派生クラスはコンストラクタで <see cref="commandPacketBuilder"/> を設定し、
/// 応答の解析方法（<see cref="CreateResponseSelector"/>）と応答データの変換（<see cref="Parse"/>）だけを実装する。
/// </remarks>
internal abstract class PlcCommandBase<T> : IPacketBuilder
{
    protected CommandPacketBuilder commandPacketBuilder = null!;

    /// <summary>
    /// 応答の解析に使うセレクタを生成します。既定は応答データを持たないコマンド用です。
    /// </summary>
    protected virtual ResponseFrameSelector CreateResponseSelector(IPlc plc, ushort serialNumber)
        => new(plc, serialNumber);

    /// <summary>
    /// 応答データ（ヘッダ・終了コードを除いた部分）を戻り値に変換します。
    /// </summary>
    protected abstract T Parse(byte[] responseContent);

    public T Execute(IPlc plc)
    {
        var requestFrameSelector = new RequestFrameSelector(plc, commandPacketBuilder);
        var responseFrameSelector = CreateResponseSelector(plc, requestFrameSelector.GetSerialNumber());

        var responseContent = responseFrameSelector.ParsePacket(
            plc.Request(requestFrameSelector.GetRequestPacket(), responseFrameSelector)
        );

        return ResponseFrameSelector.WrapInvalidPacket(() => Parse(responseContent));
    }

    public async Task<T> ExecuteAsync(IPlc plc, CancellationToken cancellationToken)
    {
        var requestFrameSelector = new RequestFrameSelector(plc, commandPacketBuilder);
        var responseFrameSelector = CreateResponseSelector(plc, requestFrameSelector.GetSerialNumber());

        var responseContent = responseFrameSelector.ParsePacket(
            await plc.RequestAsync(requestFrameSelector.GetRequestPacket(), responseFrameSelector, cancellationToken).ConfigureAwait(false)
        );

        return ResponseFrameSelector.WrapInvalidPacket(() => Parse(responseContent));
    }

    public byte[] ToBinaryBytes()
    {
        return commandPacketBuilder.ToBinaryBytes();
    }

    public byte[] ToAsciiBytes()
    {
        return commandPacketBuilder.ToAsciiBytes();
    }

    /// <summary>
    /// ワード・ダブルワードを並べた応答データを、それぞれの型の配列に分けます（ランダム読出し・モニタ用）。
    /// </summary>
    protected static (T1[] wordValues, T2[] doubleValues) SplitWordAndDoubleWord<T1, T2>(byte[] responseContent, int wordLength, int doubleWordLength)
        where T1 : unmanaged
        where T2 : unmanaged
    {
        int wordBytes = wordLength * DeviceConverter.GetWordLength<T1>() * 2;
        int doubleWordBytes = doubleWordLength * DeviceConverter.GetWordLength<T2>() * 2;
        ThrowIfLengthMismatch(responseContent, wordBytes + doubleWordBytes);

        return (
            wordValues: DeviceConverter.ConvertValueArray<T1>(responseContent.Take(wordBytes).ToArray()),
            doubleValues: DeviceConverter.ConvertValueArray<T2>(responseContent.Skip(wordBytes).Take(doubleWordBytes).ToArray())
        );
    }

    /// <summary>
    /// 応答データの長さが、要求した点数から決まる長さと一致しない場合に <see cref="ReceivePacketException"/> をスローします。
    /// </summary>
    /// <remarks>
    /// 応答のヘッダに書かれた長さとの一致は解析時に確認済みのため、ここでは「要求した点数分のデータがあるか」を確認する。
    /// </remarks>
    protected static void ThrowIfLengthMismatch(byte[] responseContent, int expectedLength)
    {
        if (responseContent.Length != expectedLength)
        {
            throw new ReceivePacketException($"Received packet had {responseContent.Length} bytes of data, but {expectedLength} bytes were expected.");
        }
    }
}
