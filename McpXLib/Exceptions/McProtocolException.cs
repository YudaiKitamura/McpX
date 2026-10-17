namespace McpXLib.Exceptions;

/// <summary>
/// MCプロトコルの交信例外
/// </summary>
/// <remarks>
/// PLCから異常の終了コード（エラーコード）を受信した場合に例外をスローします。
/// </remarks>
public class McProtocolException : McpXException
{
    /// <summary>
    /// 例外の初期化
    /// </summary>
    public McProtocolException()
    {
    }

    /// <summary>
    /// 例外の初期化（メッセージ指定）
    /// </summary>
    /// <param name="message">詳細の内容</param>
    public McProtocolException(string message) : base(message)
    {
    }

    /// <summary>
    /// 例外の初期化（メッセージ・原因となった例外の指定）
    /// </summary>
    /// <param name="message">詳細の内容</param>
    /// <param name="innerException">原因となった例外</param>
    public McProtocolException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// 例外の初期化（エラーコード指定）
    /// </summary>
    /// <param name="message">詳細の内容</param>
    /// <param name="errorCode">PLCから受信したエラーコード（終了コード）</param>
    public McProtocolException(string message, ushort errorCode) : base(message)
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
