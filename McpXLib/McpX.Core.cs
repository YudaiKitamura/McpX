using McpXLib.Enums;
using McpXLib.Commands;
using McpXLib.Builders;
using McpXLib.Interfaces;

namespace McpXLib;

// MCプロトコルの各コマンドを送受信する内部実装（旧 Mcp / BasePlc）。
public partial class McpX : IPlc
{
    /// <summary>
    /// ASCIIコードで交信する場合は<c>true</c>、バイナリコードの場合は<c>false</c>です。
    /// </summary>
    public bool IsAscii => isAscii;

    /// <summary>
    /// フレーム（データ交信電文）の種類です。
    /// </summary>
    public RequestFrame RequestFrame => requestFrame;

    /// <summary>
    /// PLCのシリーズ（デバイス指定フォーマット）です。
    /// </summary>
    public ProcessorSeries ProcessorSeries => processorSeries;

    IPacketBuilder IPlc.Route => route;

    private readonly bool isAscii;
    private readonly IPacketBuilder route;
    private readonly RequestFrame requestFrame;
    private readonly ProcessorSeries processorSeries;

    // 各コマンドの監視タイマ（ミリ秒）。0 は PLC 側で無限待ち。
    private readonly ushort monitoringTimer;

    private IPlcTransport transport;

    // 再接続用のトランスポート生成処理（トランスポートを直接指定した場合は null）
    private readonly Func<IPlcTransport>? transportFactory;

    // 破棄とトランスポートの差し替えを排他し、破棄後に再接続した接続が残らないようにする。
    private readonly object transportLock = new();
    private bool transportDisposed;

    // PLC に残るモニタ登録は最後の1つだけのため、登録のたびに世代を進めて古い MonitorSession を検出する。
    // 登録と世代の更新、世代の確認とモニタ読み出しを、それぞれ monitorGate の内側で一まとめに行う。
    private readonly SemaphoreSlim monitorGate = new(1, 1);
    private int monitorGeneration;

    // transport と transportFactory はどちらか一方を指定する（transportFactory を指定した場合は再接続できる）。
    private McpX(
        IPlcTransport? transport,
        Func<IPlcTransport>? transportFactory,
        ushort timeout,
        bool isAscii,
        RequestFrame requestFrame,
        ProcessorSeries processorSeries,
        IPacketBuilder? route = null
    )
    {
        this.transportFactory = transportFactory;
        this.transport = transport ?? transportFactory?.Invoke() ?? throw new ArgumentNullException(nameof(transport));
        this.isAscii = isAscii;
        this.requestFrame = requestFrame;
        this.processorSeries = processorSeries;
        this.route = route ?? new RoutePacketBuilder();
        this.monitoringTimer = ToMonitoringTimer(timeout);
    }

    private static Func<IPlcTransport> CreateTransportFactory(string ip, int port, ushort timeout, bool isUdp)
    {
        return () => isUdp ? new Transports.UdpPlcTransport(ip, port, timeout) : new Transports.TcpPlcTransport(ip, port, timeout);
    }

    /// <summary>
    /// 通信タイムアウトから監視タイマを決めます。
    /// </summary>
    /// <remarks>
    /// PLC のエラー応答（監視タイマ切れ）がクライアント側のタイムアウトより先に届くよう、
    /// タイムアウトより 250ms 短い 250ms 単位の値にする（例：5000ms → 4750ms）。
    /// 500ms 未満は 0（PLC 側は無限待ち）とし、クライアント側のタイムアウトで打ち切る。
    /// </remarks>
    private static ushort ToMonitoringTimer(ushort timeout)
    {
        int monitoringTimer = timeout / 250 * 250 - 250;
        return (ushort)Math.Max(0, monitoringTimer);
    }

    byte[] IPlc.Request(byte[] packet, IReceiveLengthParser receiveLengthParser)
    {
        return transport.Request(packet, receiveLengthParser);
    }

    async Task<byte[]> IPlc.RequestAsync(byte[] packet, IReceiveLengthParser receiveLengthParser)
    {
        return await transport.RequestAsync(packet, receiveLengthParser);
    }

    internal async Task RemoteUnlockAsync(string password)
    {
        await new RemoteUnlockCommand(password, monitoringTimer).ExecuteAsync(this);
    }

    internal void RemoteUnlock(string password)
    {
        new RemoteUnlockCommand(password, monitoringTimer).Execute(this);
    }

    internal async Task RemoteLockAsync(string password)
    {
        await new RemoteLockCommand(password, monitoringTimer).ExecuteAsync(this);
    }

    internal void RemoteLock(string password)
    {
        new RemoteLockCommand(password, monitoringTimer).Execute(this);
    }

    // 通信トランスポートを解放する（リモートロックは送らない）。
    private void DisposeTransport()
    {
        lock (transportLock)
        {
            transportDisposed = true;
            transport.Dispose();
        }
    }

    // トランスポートを直接指定した場合は再接続できない
    internal bool CanReconnect => transportFactory != null;

    // トランスポートを作り直して接続し直す。
    internal void Reconnect()
    {
        if (transportFactory == null)
        {
            throw new InvalidOperationException("This instance cannot reconnect.");
        }

        // 接続（最大でタイムアウト時間かかる）はロックの外で行う
        var newTransport = transportFactory();

        lock (transportLock)
        {
            if (transportDisposed)
            {
                // RemoteReset の再接続中に Dispose された場合、新しい接続を残さない
                newTransport.Dispose();
                throw new ObjectDisposedException(GetType().Name);
            }

            var oldTransport = transport;
            transport = newTransport;
            oldTransport.Dispose();
        }

        // 新しい接続には PLC のモニタ登録が引き継がれないため、既存の MonitorSession を無効にする
        monitorGate.Wait();
        try
        {
            monitorGeneration++;
        }
        finally
        {
            monitorGate.Release();
        }
    }

    internal async Task RemoteOperationAsync(Func<ushort, RemoteOperationCommand> create)
    {
        await create(monitoringTimer).ExecuteAsync(this);
    }

    internal void RemoteOperation(Func<ushort, RemoteOperationCommand> create)
    {
        create(monitoringTimer).Execute(this);
    }

    internal async Task<ushort[][]> MultiBlockReadAsync(DeviceBlock[] blocks)
    {
        return await new MultiBlockReadCommand(blocks, monitoringTimer, processorSeries).ExecuteAsync(this);
    }

    internal ushort[][] MultiBlockRead(DeviceBlock[] blocks)
    {
        return new MultiBlockReadCommand(blocks, monitoringTimer, processorSeries).Execute(this);
    }

    internal async Task MultiBlockWriteAsync(DeviceBlock[] blocks)
    {
        await new MultiBlockWriteCommand(blocks, monitoringTimer, processorSeries).ExecuteAsync(this);
    }

    internal void MultiBlockWrite(DeviceBlock[] blocks)
    {
        new MultiBlockWriteCommand(blocks, monitoringTimer, processorSeries).Execute(this);
    }

    internal async Task<bool[]> BitBatchReadAsync(Prefix prefix, string address, ushort bitLength)
    {
        return await new BitBatchReadCommand(prefix, address, bitLength, monitoringTimer, processorSeries).ExecuteAsync(this);
    }

    internal bool[] BitBatchRead(Prefix prefix, string address, ushort bitLength)
    {
        return new BitBatchReadCommand(prefix, address, bitLength, monitoringTimer, processorSeries).Execute(this);
    }

    internal async Task BitBatchWriteAsync(Prefix prefix, string address, bool[]values)
    {
        await new BitBatchWriteCommand(prefix, address, values, monitoringTimer, processorSeries).ExecuteAsync(this);
    }

    internal void BitBatchWrite(Prefix prefix, string address, bool[]values)
    {
        new BitBatchWriteCommand(prefix, address, values, monitoringTimer, processorSeries).Execute(this);
    }
    
    // wordLength は T 型の要素数（送信ワード数は WordBatchReadCommand 内で GetWordLength<T>() 倍される）。
    internal async Task<T[]> WordBatchReadAsync<T>(Prefix prefix, string address, ushort wordLength) where T : unmanaged
    {
        return await new WordBatchReadCommand<T>(prefix, address, wordLength, monitoringTimer, processorSeries).ExecuteAsync(this);
    }

    // wordLength は T 型の要素数（送信ワード数は WordBatchReadCommand 内で GetWordLength<T>() 倍される）。
    internal T[] WordBatchRead<T>(Prefix prefix, string address, ushort wordLength) where T : unmanaged
    {
        return new WordBatchReadCommand<T>(prefix, address, wordLength, monitoringTimer, processorSeries).Execute(this);
    }

    internal async Task WordBatchWriteAsync<T>(Prefix prefix, string address, T[]values) where T : unmanaged
    {
        await new WordBatchWriteCommand<T>(prefix, address, values, monitoringTimer, processorSeries).ExecuteAsync(this);
    }

    internal void WordBatchWrite<T>(Prefix prefix, string address, T[]values) where T : unmanaged
    {
        new WordBatchWriteCommand<T>(prefix, address, values, monitoringTimer, processorSeries).Execute(this);
    }

    internal async Task<(T1[] wordValues, T2[] doubleValues)> WordRandomReadAsync<T1,T2>((Prefix, string)[] wordAddresses, (Prefix, string)[] doubleWordAddresses) 
        where T1 : unmanaged
        where T2 : unmanaged
    {
        return await new WordRandomReadCommand<T1, T2>(wordAddresses, doubleWordAddresses, monitoringTimer, processorSeries).ExecuteAsync(this);
    }

    internal (T1[] wordValues, T2[] doubleValues) WordRandomRead<T1,T2>((Prefix, string)[] wordAddresses, (Prefix, string)[] doubleWordAddresses) 
        where T1 : unmanaged
        where T2 : unmanaged
    {
        return new WordRandomReadCommand<T1, T2>(wordAddresses, doubleWordAddresses, monitoringTimer, processorSeries).Execute(this);
    }

    internal async Task WordRandomWriteAsync<T1,T2>((Prefix prefix, string address, T1 value)[] wordDevices, (Prefix prefix, string address, T2 value)[] doubleWorsDevices) 
        where T1 : unmanaged
        where T2 : unmanaged
    {
        await new WordRandomWriteCommand<T1, T2>(wordDevices, doubleWorsDevices, monitoringTimer, processorSeries).ExecuteAsync(this);
    }

    internal void WordRandomWrite<T1,T2>((Prefix prefix, string address, T1 value)[] wordDevices, (Prefix prefix, string address, T2 value)[] doubleWorsDevices)
        where T1 : unmanaged
        where T2 : unmanaged
    {
        new WordRandomWriteCommand<T1, T2>(wordDevices, doubleWorsDevices, monitoringTimer, processorSeries).Execute(this);
    }

    internal async Task BitRandomWriteAsync((Prefix prefix, string address, bool value)[] bitDevices)
    {
        await new BitRandomWriteCommand(bitDevices, monitoringTimer, processorSeries).ExecuteAsync(this);
    }

    internal void BitRandomWrite((Prefix prefix, string address, bool value)[] bitDevices)
    {
        new BitRandomWriteCommand(bitDevices, monitoringTimer, processorSeries).Execute(this);
    }

    /// <summary>
    /// モニタ登録を行い、登録後の世代を返します。
    /// </summary>
    internal int RegisterMonitor((Prefix, string)[] wordAddresses, (Prefix, string)[] doubleWordAddresses)
    {
        // 点数などの検証はコマンド生成時に行われる（検証エラーでは PLC の登録は変わらないため世代も進めない）
        var command = new MonitorRegistCommand(wordAddresses, doubleWordAddresses, monitoringTimer, processorSeries);

        monitorGate.Wait();
        try
        {
            // 送信した時点で PLC の登録は置き換わりうるため、登録が失敗しても古いセッションは無効にする
            monitorGeneration++;
            command.Execute(this);
            return monitorGeneration;
        }
        finally
        {
            monitorGate.Release();
        }
    }

    internal async Task<int> RegisterMonitorAsync((Prefix, string)[] wordAddresses, (Prefix, string)[] doubleWordAddresses)
    {
        var command = new MonitorRegistCommand(wordAddresses, doubleWordAddresses, monitoringTimer, processorSeries);

        await monitorGate.WaitAsync();
        try
        {
            monitorGeneration++;
            await command.ExecuteAsync(this);
            return monitorGeneration;
        }
        finally
        {
            monitorGate.Release();
        }
    }

    /// <summary>
    /// 指定した世代のモニタ登録が有効な場合に、モニタ（0802）で値を読み出します。
    /// </summary>
    /// <exception cref="InvalidOperationException">その後に別のモニタ登録が行われ、指定した世代の登録が無効になっている場合にスローします。</exception>
    internal (ushort[] wordValues, uint[] doubleValues) MonitorRegistered(int generation, (Prefix, string)[] wordAddresses, (Prefix, string)[] doubleWordAddresses)
    {
        monitorGate.Wait();
        try
        {
            ThrowIfStaleMonitor(generation);
            return Monitor(wordAddresses, doubleWordAddresses);
        }
        finally
        {
            monitorGate.Release();
        }
    }

    internal async Task<(ushort[] wordValues, uint[] doubleValues)> MonitorRegisteredAsync(int generation, (Prefix, string)[] wordAddresses, (Prefix, string)[] doubleWordAddresses)
    {
        await monitorGate.WaitAsync();
        try
        {
            ThrowIfStaleMonitor(generation);
            return await MonitorAsync(wordAddresses, doubleWordAddresses);
        }
        finally
        {
            monitorGate.Release();
        }
    }

    private void ThrowIfStaleMonitor(int generation)
    {
        if (generation != monitorGeneration)
        {
            throw new InvalidOperationException(
                "This MonitorSession is no longer valid because the monitor registration was replaced by another MonitorRegister call."
            );
        }
    }

    private async Task<(ushort[] wordValues, uint[] doubleValues)> MonitorAsync((Prefix, string)[] wordAddresses, (Prefix, string)[] doubleWordAddresses)
    {
        return await new MonitorCommand<ushort, uint>(wordAddresses, doubleWordAddresses, monitoringTimer, processorSeries).ExecuteAsync(this);
    }

    private (ushort[] wordValues, uint[] doubleValues) Monitor((Prefix, string)[] wordAddresses, (Prefix, string)[] doubleWordAddresses)
    {
        return new MonitorCommand<ushort, uint>(wordAddresses, doubleWordAddresses, monitoringTimer, processorSeries).Execute(this);
    }
}
