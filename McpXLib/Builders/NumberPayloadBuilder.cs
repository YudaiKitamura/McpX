using McpXLib.Interfaces;

namespace McpXLib.Builders;

/// <summary>
/// 数値だけで構成される要求データ。
/// </summary>
/// <remarks>
/// 各数値を、バイナリ交信時は指定バイト数で下位バイトから、ASCII交信時は16進数（バイト数×2桁）で上位桁から送信する。
/// </remarks>
internal class NumberPayloadBuilder(params (uint value, int byteLength)[] numbers) : IPayloadBuilder
{
    public void AppendPayload(List<byte> packets, bool isAscii)
    {
        foreach (var (value, byteLength) in numbers)
        {
            var bytes = BitConverter.GetBytes(value).Take(byteLength).ToArray();
            packets.AddRange(isAscii
                ? CommandPacketBuilder.BinaryBytesToAsciiBytes(bytes, isReverse: true)
                : bytes);
        }
    }
}
