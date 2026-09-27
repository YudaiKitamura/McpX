using McpXLib.Enums;
using McpXLib.Exceptions;

namespace McpXLib;

/// <summary>
/// GX Simulator3 接続クラス
/// </summary>
/// <remarks>
/// GX Simulator3（GX Works3 のシミュレーション機能）に、外部機器との通信（SLMP）で接続するクラスです。<br/>
/// 接続先のポート番号は、システムNo.と号機No.から <c>5500 + システムNo. × 10 + 号機No.</c> で決まります。（例: システム1・号機1 = 5511、システム2・号機1 = 5521）<br/>
/// システムNo.はシミュレーション（プロジェクト）ごとの番号、号機No.はマルチCPUシステムのCPU No.（1〜4）です。<br/>
/// 1インスタンスが1つのシミュレータ（CPU）への接続です。複数のシミュレータに接続する場合は、それぞれインスタンスを生成してください。<br/>
/// GX Simulator3 は 127.0.0.1 でのみ待ち受けるため、別のPCから接続する場合は、シミュレータ側のPCでポートフォワーディング（<c>netsh interface portproxy</c> など）を設定してください。<br/>
/// 交信はTCP・バイナリコードで行います。（GX Simulator3 はASCIIコードの交信に応答しません）
/// </remarks>
public sealed class McpXSimulator : McpX
{
    /// <summary>
    /// ポート番号の基準値
    /// </summary>
    public const int BasePort = 5500;

    /// <summary>
    /// 号機No.の最大値（マルチCPUシステムのCPU台数）
    /// </summary>
    public const int MaxCpuNo = 4;

    /// <summary>
    /// インスタンス初期化
    /// </summary>
    /// <remarks>
    /// システムNo.と号機No.から求めたポート番号で、GX Simulator3 に接続します。
    /// </remarks>
    /// <param name="systemNo">システムNo.を指定します。（デフォルトは、<c>1</c>です。）</param>
    /// <param name="cpuNo">号機No.（1〜4）を指定します。（デフォルトは、<c>1</c>です。）</param>
    /// <param name="ip">シミュレータが動作しているPCのIPアドレスを指定します。（デフォルトは、<c>127.0.0.1</c>です。）</param>
    /// <param name="requestFrame">フレーム（データ交信電文）の種類を指定します。（デフォルトは、3Eフレーム:<c>RequestFrame.E3</c>です。）</param>
    /// <param name="timeoutMilliseconds">通信タイムアウト時間（ミリ秒）を指定します。（デフォルトは、5秒です。）</param>
    /// <param name="processorSeries">PLCのシリーズを指定します。（デフォルトは、MELSEC iQ-Rシリーズ:<c>ProcessorSeries.iQR</c>です。）</param>
    /// <exception cref="ArgumentOutOfRangeException">システムNo.または号機No.が範囲外の場合に例外をスローします。</exception>
    /// <exception cref="RecivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    public McpXSimulator(
        int systemNo = 1,
        int cpuNo = 1,
        string ip = "127.0.0.1",
        RequestFrame requestFrame = RequestFrame.E3,
        ushort timeoutMilliseconds = 5000,
        ProcessorSeries processorSeries = ProcessorSeries.iQR
    ) : base (
        ip: ip,
        port: GetPort(systemNo, cpuNo),
        isAscii: false,
        isUdp: false,
        requestFrame: requestFrame,
        timeoutMilliseconds: timeoutMilliseconds,
        processorSeries: processorSeries
    )
    {
        SystemNo = systemNo;
        CpuNo = cpuNo;
    }

    /// <summary>
    /// 接続先のシステムNo.
    /// </summary>
    public int SystemNo { get; }

    /// <summary>
    /// 接続先の号機No.
    /// </summary>
    public int CpuNo { get; }

    /// <summary>
    /// ポート番号取得
    /// </summary>
    /// <remarks>
    /// システムNo.と号機No.から、GX Simulator3 のポート番号（<c>5500 + システムNo. × 10 + 号機No.</c>）を求めます。
    /// </remarks>
    /// <param name="systemNo">システムNo.を指定します。（デフォルトは、<c>1</c>です。）</param>
    /// <param name="cpuNo">号機No.（1〜4）を指定します。（デフォルトは、<c>1</c>です。）</param>
    /// <exception cref="ArgumentOutOfRangeException">システムNo.または号機No.が範囲外の場合に例外をスローします。</exception>
    /// <returns>ポート番号を返します。</returns>
    public static int GetPort(int systemNo = 1, int cpuNo = 1)
    {
        // システムNo.の上限はポート番号が 65535 を超えない値とする
        const int maxSystemNo = (ushort.MaxValue - BasePort - MaxCpuNo) / 10;
        if (systemNo < 1 || systemNo > maxSystemNo)
        {
            throw new ArgumentOutOfRangeException(nameof(systemNo), systemNo, $"System No. can be from 1 to {maxSystemNo}.");
        }

        if (cpuNo < 1 || cpuNo > MaxCpuNo)
        {
            throw new ArgumentOutOfRangeException(nameof(cpuNo), cpuNo, $"CPU No. can be from 1 to {MaxCpuNo}.");
        }

        return BasePort + systemNo * 10 + cpuNo;
    }
}
