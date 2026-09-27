using McpXLib.Enums;
using McpXLib.Utils;

namespace McpXLib.Commands;

/// <summary>
/// 複数ブロック一括読出し・書込み（0406 / 1406）の1ブロック。
/// </summary>
/// <remarks>
/// ワードデバイスは1点1ワード、ビットデバイスは1点16ビット（1ワード）。
/// </remarks>
internal sealed class DeviceBlock(Prefix prefix, string address, ushort points, ushort[]? words = null)
{
    internal Prefix Prefix { get; } = prefix;

    internal string Address { get; } = address;

    internal ushort Points { get; } = points;

    // 書込みデータ（読出し時は null）
    internal ushort[]? Words { get; } = words;

    internal bool IsBitDevice => DeviceConverter.IsBitDevice(Prefix);
}
