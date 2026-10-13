namespace McpXLib.Exceptions;

/// <summary>
/// 受信パケットの例外
/// </summary>
/// <remarks>
/// PLCから受け取ったパケットが変換できない場合に例外をスローします。
/// </remarks>
/// <param name="message">詳細の内容</param>
public class ReceivePacketException(string message) : Exception(message)
{
}
