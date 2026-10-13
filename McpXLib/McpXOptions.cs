using McpXLib.Enums;

namespace McpXLib;

/// <summary>
/// 接続設定
/// </summary>
/// <remarks>
/// <see cref="McpX.Connect(McpXOptions)"/> / <see cref="McpX.ConnectAsync(McpXOptions, CancellationToken)"/> に渡す、PLCの接続情報です。<br/>
/// PLCのパラメータ設定（交信データコード、プロトコル、ポート番号など）に合わせて指定してください。
/// </remarks>
public sealed class McpXOptions
{
    /// <summary>
    /// 既定の通信タイムアウト時間（5秒）
    /// </summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// PLCのIPアドレスまたはホスト名を指定します。
    /// </summary>
    public string Host { get; set; } = "";

    /// <summary>
    /// PLCのポート番号（1〜65535）を指定します。
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// PLCのリモートパスワードを指定します。（リモートパスワードを設定している場合のみ）
    /// </summary>
    /// <remarks>
    /// 指定した場合、接続時にリモートパスワードを解除し、<see cref="McpX.Dispose"/> / <see cref="McpX.DisposeAsync"/> でロックします。
    /// </remarks>
    public string? Password { get; set; }

    /// <summary>
    /// ASCIIコードで交信する場合に<c>true</c>を指定します。（既定は、バイナリコード:<c>false</c>です。）
    /// </summary>
    public bool IsAscii { get; set; }

    /// <summary>
    /// UDPで交信する場合に<c>true</c>を指定します。（既定は、TCP:<c>false</c>です。）
    /// </summary>
    public bool IsUdp { get; set; }

    /// <summary>
    /// フレーム（データ交信電文）の種類を指定します。（既定は、3Eフレーム:<see cref="RequestFrame.E3"/>です。）
    /// </summary>
    public RequestFrame RequestFrame { get; set; } = RequestFrame.E3;

    /// <summary>
    /// PLCのシリーズ（デバイス指定フォーマット）を指定します。
    /// MELSEC iQ-Rシリーズへ拡張デバイス指定で交信する場合は <see cref="ProcessorSeries.iQR"/> を指定します。（既定は、MELSEC-Q/Lシリーズ:<see cref="ProcessorSeries.Q"/>です。）
    /// </summary>
    public ProcessorSeries ProcessorSeries { get; set; } = ProcessorSeries.Q;

    /// <summary>
    /// 通信タイムアウト時間を指定します。（既定は、5秒です。）
    /// </summary>
    /// <remarks>
    /// 接続と、各要求（送信開始から応答の受信完了まで）に適用します。<see cref="System.Threading.Timeout.InfiniteTimeSpan"/> を指定すると無期限になります。<br/>
    /// PLC側の監視タイマは、タイムアウト時間より 250ms 短い値（250ms 単位）を設定します。
    /// </remarks>
    public TimeSpan Timeout { get; set; } = DefaultTimeout;

    /// <summary>
    /// 統合アクセスで、複数ブロック一括読出し・書込み（コマンド: 0406 / 1406）を使う場合に<c>true</c>を指定します。（既定は、<c>false</c>です。）
    /// </summary>
    /// <remarks>
    /// 詳細は <see cref="McpX.UseMultiBlockAccess"/> を参照してください。
    /// </remarks>
    public bool UseMultiBlockAccess { get; set; }

    /// <summary>
    /// 設定を検証し、通信タイムアウト時間をミリ秒（0 は無期限）で返します。
    /// </summary>
    internal int Validate()
    {
        if (string.IsNullOrWhiteSpace(Host))
        {
            throw new ArgumentException("Host must not be empty.", nameof(Host));
        }

        if (Port < 1 || Port > ushort.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(Port), Port, "Port must be from 1 to 65535.");
        }

        if (!Enum.IsDefined(typeof(RequestFrame), RequestFrame))
        {
            throw new ArgumentOutOfRangeException(nameof(RequestFrame), RequestFrame, "Unknown request frame.");
        }

        if (!Enum.IsDefined(typeof(ProcessorSeries), ProcessorSeries))
        {
            throw new ArgumentOutOfRangeException(nameof(ProcessorSeries), ProcessorSeries, "Unknown processor series.");
        }

        if (Timeout == System.Threading.Timeout.InfiniteTimeSpan)
        {
            return 0;
        }

        if (Timeout <= TimeSpan.Zero || Timeout.TotalMilliseconds > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(Timeout), Timeout, "Timeout must be positive and at most int.MaxValue milliseconds, or Timeout.InfiniteTimeSpan.");
        }

        // 1ms 未満は 1ms に切り上げる（0 は無期限を表すため）
        return Math.Max(1, (int)Timeout.TotalMilliseconds);
    }
}
