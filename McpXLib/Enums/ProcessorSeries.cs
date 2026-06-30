namespace McpXLib.Enums;

/// <summary>
/// PLCのシリーズ（デバイス指定フォーマット）
/// </summary>
/// <remarks>
/// MELSEC-Q/Lシリーズと、デバイス拡張指定が必要なMELSEC iQ-Rシリーズを区別します。<br/>
/// </remarks>
public enum ProcessorSeries
{
    /// <summary>
    /// MELSEC-Q/Lシリーズ（デバイス指定）
    /// </summary>
    Q,

    /// <summary>
    /// MELSEC iQ-Rシリーズ（デバイス拡張指定）
    /// </summary>
    iQR
}
