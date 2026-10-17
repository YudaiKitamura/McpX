namespace McpXLib.Exceptions;

/// <summary>
/// McpX の例外の基底クラス
/// </summary>
/// <remarks>
/// McpX が独自にスローする例外（<see cref="DeviceAddressException"/>、<see cref="McProtocolException"/>、<see cref="ReceivePacketException"/>）の基底クラスです。<br/>
/// 通信の失敗は、.NET の標準の例外（<see cref="TimeoutException"/>、<see cref="System.IO.IOException"/>、<see cref="System.Net.Sockets.SocketException"/>）でスローします。
/// </remarks>
public class McpXException : Exception
{
    /// <summary>
    /// 例外の初期化
    /// </summary>
    public McpXException()
    {
    }

    /// <summary>
    /// 例外の初期化（メッセージ指定）
    /// </summary>
    /// <param name="message">詳細の内容</param>
    public McpXException(string message) : base(message)
    {
    }

    /// <summary>
    /// 例外の初期化（メッセージ・原因となった例外の指定）
    /// </summary>
    /// <param name="message">詳細の内容</param>
    /// <param name="innerException">原因となった例外</param>
    public McpXException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
