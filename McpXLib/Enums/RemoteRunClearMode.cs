namespace McpXLib.Enums;

/// <summary>
/// リモートRUN時のデバイスメモリのクリア範囲
/// </summary>
/// <remarks>
/// デバイス初期値が設定されている場合は、クリア後にデバイス初期値が反映されます。
/// </remarks>
public enum RemoteRunClearMode : byte
{
    /// <summary>
    /// クリアしない
    /// </summary>
    None = 0x00,

    /// <summary>
    /// ラッチ範囲外のみクリア
    /// </summary>
    OutsideLatch = 0x01,

    /// <summary>
    /// ラッチ範囲を含むすべてのデバイスメモリをクリア
    /// </summary>
    All = 0x02,
}
