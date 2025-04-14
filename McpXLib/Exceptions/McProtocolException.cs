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
}
