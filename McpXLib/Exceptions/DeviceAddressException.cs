namespace McpXLib.Exceptions;


/// <summary>
/// デバイスアドレスの例外
/// </summary>
/// <remarks>
/// 下記、場合に例外をスローします。<br/>
/// ・10進のデバイスに対して、アドレスに数字以外の文字列を含んでいる。<br/>
/// ・16進のデバイスに対して、アドレスに16進に変換できない文字列を含んでいる。
/// </remarks>
/// <param name="message">詳細の内容</param>
public class DeviceAddressException(string message) : Exception(message)
{
}
