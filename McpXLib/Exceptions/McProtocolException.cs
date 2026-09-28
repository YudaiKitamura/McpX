namespace McpXLib.Exceptions;

/// <summary>
/// MCプロトコルの交信例外
/// </summary>
/// <remarks>
/// PLCとの更新時にエラーコードが返ってきた場合に例外をスローします。
/// </remarks>
/// <param name="message">詳細の内容</param>
public class McProtocolException(string message) : Exception(message)
{
    /// <summary>
    /// 例外の初期化（エラーコード指定）
    /// </summary>
    /// <param name="message">詳細の内容</param>
    /// <param name="errorCode">PLCから受信したエラーコード（終了コード）</param>
    public McProtocolException(string message, ushort errorCode) : this(message)
    {
        ErrorCode = errorCode;
    }

    /// <summary>
    /// PLCから受信したエラーコード（終了コード）。不明な場合は <c>0</c> です。
    /// </summary>
    /// <remarks>
    /// 例えば <c>0xC059</c> は、コマンドまたはサブコマンドに対応していないことを示します。
    /// </remarks>
    public ushort ErrorCode { get; }
}
