namespace McpXNative;

/// <summary>
/// C ABI の引数検証で共通に使う値。
/// </summary>
internal static class Validation
{
    internal const int MaxAddressBytes = 64;

    // 既知のデバイスコード（Prefix の値）
    private static readonly bool[] knownPrefixes = CreateKnownPrefixes();

    private static bool[] CreateKnownPrefixes()
    {
        var known = new bool[256];
        byte[] codes =
        [
            0x9C, 0x9D, 0x90, 0x92, 0x93, 0x94, 0xA0, 0xA8, 0xB4, 0xC1, 0xC0, 0xC2, 0xC7, 0xC6,
            0xC8, 0xC4, 0xC3, 0xC5, 0xA1, 0xB5, 0x98, 0xA2, 0xA3, 0x91, 0xA9, 0xCC, 0xAF, 0xB0,
        ];
        foreach (var code in codes)
        {
            known[code] = true;
        }
        return known;
    }

    internal static bool IsKnownPrefix(byte prefix) => knownPrefixes[prefix];
}
