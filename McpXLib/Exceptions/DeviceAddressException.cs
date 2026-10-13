namespace McpXLib.Exceptions;

/// <summary>
/// デバイスアドレスの例外
/// </summary>
/// <remarks>
/// 下記、場合に例外をスローします。<br/>
/// ・10進のデバイスに対して、アドレスに数字以外の文字列を含んでいる。<br/>
/// ・16進のデバイスに対して、アドレスに16進に変換できない文字列を含んでいる。
/// </remarks>
public class DeviceAddressException : McpXException
{
    /// <summary>
    /// 例外の初期化
    /// </summary>
    public DeviceAddressException()
    {
    }

    /// <summary>
    /// 例外の初期化（メッセージ指定）
    /// </summary>
    /// <param name="message">詳細の内容</param>
    public DeviceAddressException(string message) : base(message)
    {
    }

    /// <summary>
    /// 例外の初期化（メッセージ・原因となった例外の指定）
    /// </summary>
    /// <param name="message">詳細の内容</param>
    /// <param name="innerException">原因となった例外</param>
    public DeviceAddressException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
