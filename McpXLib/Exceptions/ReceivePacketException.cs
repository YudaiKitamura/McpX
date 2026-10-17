namespace McpXLib.Exceptions;

/// <summary>
/// 受信パケットの例外
/// </summary>
/// <remarks>
/// PLCから受け取ったパケットが変換できない場合（サブヘッダ・シリアル番号の不一致、応答データの長さや内容の不正など）に例外をスローします。
/// </remarks>
public class ReceivePacketException : McpXException
{
    /// <summary>
    /// 例外の初期化
    /// </summary>
    public ReceivePacketException()
    {
    }

    /// <summary>
    /// 例外の初期化（メッセージ指定）
    /// </summary>
    /// <param name="message">詳細の内容</param>
    public ReceivePacketException(string message) : base(message)
    {
    }

    /// <summary>
    /// 例外の初期化（メッセージ・原因となった例外の指定）
    /// </summary>
    /// <param name="message">詳細の内容</param>
    /// <param name="innerException">原因となった例外</param>
    public ReceivePacketException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
