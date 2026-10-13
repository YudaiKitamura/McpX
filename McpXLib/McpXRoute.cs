namespace McpXLib;

/// <summary>
/// アクセス経路
/// </summary>
/// <remarks>
/// 要求を送るCPUユニット（交信先）を指定します。MCプロトコルの要求電文の「ネットワーク番号」「PC番号」「要求先ユニットI/O番号」「要求先ユニット局番号」です。<br/>
/// 既定（<see cref="Default"/>）は、接続したユニットの自局CPUです。<br/>
/// ・ネットワーク経由で他局にアクセスする場合は、ネットワーク番号とPC番号を指定します。<br/>
/// ・マルチCPUシステムの別の号機にアクセスする場合は、<see cref="ForCpu(int, byte, byte)"/> を使用します。<br/>
/// 指定できる値と組み合わせは、MCプロトコルのリファレンスマニュアル（アクセス経路）を参照してください。
/// </remarks>
public sealed class McpXRoute
{
    /// <summary>
    /// 自局CPU（ネットワーク番号 00H、PC番号 FFH、要求先ユニットI/O番号 03FFH、要求先ユニット局番号 00H）
    /// </summary>
    public static McpXRoute Default { get; } = new();

    /// <summary>
    /// アクセス経路の初期化
    /// </summary>
    /// <param name="networkNo">ネットワーク番号を指定します。（デフォルトは、自局:<c>0x00</c>です。）</param>
    /// <param name="pcNo">PC番号を指定します。（デフォルトは、自局:<c>0xFF</c>です。）</param>
    /// <param name="moduleIoNo">要求先ユニットI/O番号を指定します。（デフォルトは、自局CPU:<c>0x03FF</c>です。）</param>
    /// <param name="moduleStationNo">要求先ユニット局番号を指定します。（デフォルトは、<c>0x00</c>です。）</param>
    public McpXRoute(byte networkNo = 0x00, byte pcNo = 0xFF, ushort moduleIoNo = 0x03FF, byte moduleStationNo = 0x00)
    {
        NetworkNo = networkNo;
        PcNo = pcNo;
        ModuleIoNo = moduleIoNo;
        ModuleStationNo = moduleStationNo;
    }

    /// <summary>
    /// ネットワーク番号
    /// </summary>
    public byte NetworkNo { get; }

    /// <summary>
    /// PC番号
    /// </summary>
    public byte PcNo { get; }

    /// <summary>
    /// 要求先ユニットI/O番号
    /// </summary>
    public ushort ModuleIoNo { get; }

    /// <summary>
    /// 要求先ユニット局番号
    /// </summary>
    public byte ModuleStationNo { get; }

    /// <summary>
    /// マルチCPUシステムの号機を指定したアクセス経路
    /// </summary>
    /// <remarks>
    /// 接続したユニットと同じベースの、指定した号機のCPUユニットにアクセスします（要求先ユニットI/O番号 03E0H〜03E3H）。
    /// </remarks>
    /// <param name="cpuNo">号機No.（1〜4）を指定します。</param>
    /// <param name="networkNo">ネットワーク番号を指定します。（デフォルトは、自局:<c>0x00</c>です。）</param>
    /// <param name="pcNo">PC番号を指定します。（デフォルトは、自局:<c>0xFF</c>です。）</param>
    /// <exception cref="ArgumentOutOfRangeException">号機No.が1〜4以外の場合に例外をスローします。</exception>
    /// <returns>アクセス経路を返します。</returns>
    public static McpXRoute ForCpu(int cpuNo, byte networkNo = 0x00, byte pcNo = 0xFF)
    {
        if (cpuNo < 1 || cpuNo > 4)
        {
            throw new ArgumentOutOfRangeException(nameof(cpuNo), cpuNo, "CPU No. can be from 1 to 4.");
        }

        return new McpXRoute(networkNo, pcNo, (ushort)(0x03E0 + cpuNo - 1));
    }
}
