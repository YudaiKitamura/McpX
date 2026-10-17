using McpXLib.Enums;

namespace McpXLib;

/// <summary>
/// GX Simulator3 接続
/// </summary>
/// <remarks>
/// GX Simulator3（GX Works3 のシミュレーション機能）に、外部機器との通信（SLMP）で接続します。<br/>
/// 接続先のポート番号は、システムNo.と号機No.から <c>5500 + システムNo. × 10 + 号機No.</c> で決まります。（例: システム1・号機1 = 5511、システム2・号機1 = 5521）<br/>
/// システムNo.はシミュレーション（プロジェクト）ごとの番号、号機No.はマルチCPUシステムのCPU No.（1〜4）です。<br/>
/// 1つの接続が1つのシミュレータ（CPU）への接続です。複数のシミュレータに接続する場合は、それぞれ接続してください。<br/>
/// GX Simulator3 は 127.0.0.1 でのみ待ち受けるため、別のPCから接続する場合は、シミュレータ側のPCでポートフォワーディング（<c>netsh interface portproxy</c> など）を設定してください。<br/>
/// GX Simulator3 はTCP・バイナリコードの交信にのみ応答します。
/// </remarks>
public static class McpXSimulator
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
    /// 接続設定の作成
    /// </summary>
    /// <remarks>
    /// システムNo.と号機No.から求めたポート番号と、MELSEC iQ-Rシリーズ（<see cref="ProcessorSeries.iQR"/>）を設定した接続設定を作成します。
    /// タイムアウト時間などを変更して <see cref="McpX.Connect(McpXOptions)"/> に渡せます。
    /// </remarks>
    /// <param name="systemNo">システムNo.を指定します。（デフォルトは、<c>1</c>です。）</param>
    /// <param name="cpuNo">号機No.（1〜4）を指定します。（デフォルトは、<c>1</c>です。）</param>
    /// <param name="host">シミュレータが動作しているPCのIPアドレスまたはホスト名を指定します。（デフォルトは、<c>127.0.0.1</c>です。）</param>
    /// <exception cref="ArgumentOutOfRangeException">システムNo.または号機No.が範囲外の場合に例外をスローします。</exception>
    /// <returns>接続設定を返します。</returns>
    public static McpXOptions CreateOptions(int systemNo = 1, int cpuNo = 1, string host = "127.0.0.1")
    {
        return new McpXOptions
        {
            Host = host,
            Port = GetPort(systemNo, cpuNo),
            ProcessorSeries = ProcessorSeries.iQR,
        };
    }

    /// <summary>
    /// 接続
    /// </summary>
    /// <remarks>
    /// システムNo.と号機No.から求めたポート番号で、GX Simulator3 に接続します（<see cref="CreateOptions"/> の設定で接続します）。
    /// </remarks>
    /// <param name="systemNo">システムNo.を指定します。（デフォルトは、<c>1</c>です。）</param>
    /// <param name="cpuNo">号機No.（1〜4）を指定します。（デフォルトは、<c>1</c>です。）</param>
    /// <param name="host">シミュレータが動作しているPCのIPアドレスまたはホスト名を指定します。（デフォルトは、<c>127.0.0.1</c>です。）</param>
    /// <exception cref="ArgumentOutOfRangeException">システムNo.または号機No.が範囲外の場合に例外をスローします。</exception>
    /// <exception cref="TimeoutException">タイムアウト時間内に接続できなかった場合に例外をスローします。</exception>
    /// <exception cref="System.Net.Sockets.SocketException">接続が拒否された場合など、接続に失敗した場合に例外をスローします。</exception>
    /// <returns>接続済みのインスタンスを返します。</returns>
    public static McpX Connect(int systemNo = 1, int cpuNo = 1, string host = "127.0.0.1")
        => McpX.Connect(CreateOptions(systemNo, cpuNo, host));

    /// <summary>
    /// 接続（非同期）
    /// </summary>
    /// <remarks>
    /// 詳細は <see cref="Connect"/> を参照してください。
    /// </remarks>
    /// <param name="systemNo">システムNo.を指定します。（デフォルトは、<c>1</c>です。）</param>
    /// <param name="cpuNo">号機No.（1〜4）を指定します。（デフォルトは、<c>1</c>です。）</param>
    /// <param name="host">シミュレータが動作しているPCのIPアドレスまたはホスト名を指定します。（デフォルトは、<c>127.0.0.1</c>です。）</param>
    /// <param name="cancellationToken">接続をキャンセルするトークンを指定します。キャンセルされると <see cref="OperationCanceledException"/> をスローします。</param>
    /// <exception cref="ArgumentOutOfRangeException">システムNo.または号機No.が範囲外の場合に例外をスローします。</exception>
    /// <exception cref="TimeoutException">タイムアウト時間内に接続できなかった場合に例外をスローします。</exception>
    /// <exception cref="System.Net.Sockets.SocketException">接続が拒否された場合など、接続に失敗した場合に例外をスローします。</exception>
    /// <returns>接続済みのインスタンスを返します。</returns>
    public static Task<McpX> ConnectAsync(int systemNo = 1, int cpuNo = 1, string host = "127.0.0.1", CancellationToken cancellationToken = default)
        => McpX.ConnectAsync(CreateOptions(systemNo, cpuNo, host), cancellationToken);

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
