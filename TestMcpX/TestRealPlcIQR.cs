using System.Net.Sockets;
using McpXLib;
using McpXLib.Enums;

namespace TestMcpX;

/// <summary>
/// iQ-R 実機（R120CPU・CPU内蔵Ethernet・バイナリ）結合テストの接続先。
/// </summary>
/// <remarks>
/// 通常の <c>dotnet test</c> では実行されず（Inconclusive）、環境変数 <c>MCPX_REAL_IQR=1</c> を指定した場合のみ実行します。
/// <code>
/// MCPX_REAL_IQR=1 dotnet test TestMcpX --filter TestCategory=RealPlcIQR
/// </code>
/// 接続先は <c>MCPX_IQR_IP</c>（既定 192.168.12.89）、<c>MCPX_IQR_TCP_PORT</c>（既定 5007）/ <c>MCPX_IQR_UDP_PORT</c>（既定 5006）で変更できます。
/// </remarks>
internal static class RealIQRConnection
{
    internal const string EnableVar = "MCPX_REAL_IQR";

    internal static McpX Connect(RequestFrame requestFrame = RequestFrame.E3, ProcessorSeries processorSeries = ProcessorSeries.iQR, bool isUdp = false)
    {
        PlcIntegrationTestBase.EnsureEnabled(EnableVar);

        var ip = Environment.GetEnvironmentVariable("MCPX_IQR_IP") ?? "192.168.12.89";
        var port = isUdp
            ? PlcIntegrationTestBase.GetEnvironmentInt("MCPX_IQR_UDP_PORT", 5006)
            : PlcIntegrationTestBase.GetEnvironmentInt("MCPX_IQR_TCP_PORT", 5007);

        // CPU内蔵Ethernetは同時に1接続しか受け付けず、切断直後の再接続も拒否するため、しばらく再試行する
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (true)
        {
            try
            {
                return new McpX(ip, port, isUdp: isUdp, requestFrame: requestFrame, processorSeries: processorSeries);
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionRefused && DateTime.UtcNow < deadline)
            {
                Thread.Sleep(200);
            }
        }
    }
}

/// <summary>
/// iQ-R 実機結合テストの共通実装。
/// </summary>
/// <remarks>
/// 実機の D0〜D8999・W200〜W20F・M0〜M12287 を上書きします。<br/>
/// 実行条件・接続先は <see cref="RealIQRConnection"/> を参照してください。
/// </remarks>
public abstract class RealIQRTestBase : MultiBlockPlcTestBase
{
    protected abstract RequestFrame RequestFrame { get; }

    protected abstract ProcessorSeries ProcessorSeries { get; }

    protected virtual bool IsUdp => false;

    protected override McpX Connect() => RealIQRConnection.Connect(RequestFrame, ProcessorSeries, IsUdp);

    protected override int BitDeviceAsWordsPoints => 12288;
}

/// <summary>
/// iQ-R 実機結合テスト（TCP・3Eフレーム・iQ-Rシリーズの拡張デバイス指定）。
/// </summary>
[TestClass]
[TestCategory("RealPlcIQR")]
public sealed class TestRealPlcIQR : RealIQRTestBase
{
    protected override RequestFrame RequestFrame => RequestFrame.E3;

    protected override ProcessorSeries ProcessorSeries => ProcessorSeries.iQR;
}

/// <summary>
/// iQ-R 実機結合テスト（TCP・4Eフレーム・Q/Lシリーズ互換のデバイス指定）。
/// </summary>
[TestClass]
[TestCategory("RealPlcIQR")]
public sealed class TestRealPlcIQRE4Q : RealIQRTestBase
{
    protected override RequestFrame RequestFrame => RequestFrame.E4;

    protected override ProcessorSeries ProcessorSeries => ProcessorSeries.Q;
}

/// <summary>
/// iQ-R 実機結合テスト（UDP・4Eフレーム・iQ-Rシリーズの拡張デバイス指定）。
/// </summary>
[TestClass]
[TestCategory("RealPlcIQR")]
public sealed class TestRealPlcIQRUdpE4 : RealIQRTestBase
{
    protected override RequestFrame RequestFrame => RequestFrame.E4;

    protected override ProcessorSeries ProcessorSeries => ProcessorSeries.iQR;

    protected override bool IsUdp => true;
}

/// <summary>
/// iQ-R 実機結合テスト（UDP・3Eフレーム・Q/Lシリーズ互換のデバイス指定）。
/// </summary>
[TestClass]
[TestCategory("RealPlcIQR")]
public sealed class TestRealPlcIQRUdpE3Q : RealIQRTestBase
{
    protected override RequestFrame RequestFrame => RequestFrame.E3;

    protected override ProcessorSeries ProcessorSeries => ProcessorSeries.Q;

    protected override bool IsUdp => true;
}

/// <summary>
/// iQ-R 実機（R120CPU）の全デバイス範囲テスト。
/// </summary>
/// <remarks>
/// デバイス点数は実機のパラメータ（W 8K、その他は R120CPU の既定値）に合わせています。<br/>
/// 実行条件・接続先は <see cref="RealIQRConnection"/> を参照してください。
/// </remarks>
public abstract class RealIQRDeviceRangeTestBase : DeviceRangeTestBase
{
    protected override McpX Connect() => RealIQRConnection.Connect(RequestFrame, ProcessorSeries);

    protected override IReadOnlyDictionary<Prefix, int> DevicePoints { get; } = new Dictionary<Prefix, int>
    {
        [Prefix.D] = 18432, [Prefix.W] = 0x2000, [Prefix.SW] = 0x800, [Prefix.TN] = 1024, [Prefix.CN] = 512,
        [Prefix.M] = 12288, [Prefix.B] = 0x2000, [Prefix.SB] = 0x800, [Prefix.F] = 2048, [Prefix.V] = 2048, [Prefix.L] = 8192,
    };
}

/// <summary>
/// iQ-R 実機の全デバイス範囲テスト（3Eフレーム・Q/Lシリーズ互換のデバイス指定）。
/// </summary>
[TestClass]
[TestCategory("RealPlcIQR")]
public sealed class TestRealPlcIQRDeviceRange : RealIQRDeviceRangeTestBase
{
    protected override RequestFrame RequestFrame => RequestFrame.E3;

    protected override ProcessorSeries ProcessorSeries => ProcessorSeries.Q;
}

/// <summary>
/// iQ-R 実機の全デバイス範囲テスト（4Eフレーム・iQ-Rシリーズの拡張デバイス指定）。
/// </summary>
[TestClass]
[TestCategory("RealPlcIQR")]
public sealed class TestRealPlcIQRDeviceRangeE4iQR : RealIQRDeviceRangeTestBase
{
    protected override RequestFrame RequestFrame => RequestFrame.E4;

    protected override ProcessorSeries ProcessorSeries => ProcessorSeries.iQR;
}

/// <summary>
/// iQ-R 実機のリモート操作の結合テスト。
/// </summary>
/// <remarks>
/// 実機を STOP / PAUSE し、テスト後は RUN に戻します。ラッチクリアにより、実機のラッチデバイスがクリアされます。<br/>
/// RESET は環境変数 <c>MCPX_IQR_RESET=1</c> を指定した場合のみ実行します（CPUパラメータでリモートリセットを許可しておくこと。禁止の場合は 408B エラー）。<br/>
/// 実行条件・接続先は <see cref="RealIQRConnection"/> を参照してください。
/// </remarks>
[TestClass]
[TestCategory("RealPlcIQR")]
public sealed class TestRealPlcIQRRemoteControl : RemoteControlTestBase
{
    protected override McpX Connect() => RealIQRConnection.Connect();

    protected override string ResetEnableVar => "MCPX_IQR_RESET";
}
