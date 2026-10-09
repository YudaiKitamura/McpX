using McpXLib.Enums;
using McpXLib.Abstructs;
using McpXLib.Commands;
using McpXLib.Builders;
using McpXLib.Interfaces;
using McpXLib.Exceptions;

namespace McpXLib;

/// <summary>
/// MCプロトコル実装クラス（コマンド追加以外は、<see cref="McpX"/>クラスを使用してください。）
/// </summary>
public class Mcp : BasePlc, IPlc
{
    /// <summary>
    /// ASCIIコードによる交信を行う場合に<c>true</c>を指定します。
    /// </summary>
    public bool IsAscii
    { 
        get 
        {
            return isAscii;
        }
        set 
        {
            isAscii = value; 
        }
    }
    
    /// <summary>
    /// アクセス経路を指定します。
    /// </summary>
    public IPacketBuilder Route
    {
        get
        {
            return route;
        }
        set
        {
            route = value;
        }
    }

    /// <summary>
    /// フレーム（データ交信電文）の種類を指定します。
    /// </summary>
    public RequestFrame RequestFrame
    {
        get
        {
            return requestFrame;
        }
        set
        {
            requestFrame = value;
        }
    }

    /// <summary>
    /// PLCのシリーズ（デバイス指定フォーマット）を指定します。
    /// </summary>
    public ProcessorSeries ProcessorSeries
    {
        get
        {
            return processorSeries;
        }
        set
        {
            processorSeries = value;
        }
    }

    private bool isAscii;
    private IPacketBuilder route;
    private RequestFrame requestFrame;
    private ProcessorSeries processorSeries;

    // 各コマンドの監視タイマ（ミリ秒）。0 は PLC 側で無限待ち。
    private readonly ushort monitoringTimer;

    // 再接続用のトランスポート生成処理（トランスポートを直接指定した場合は null）
    private readonly Func<IPlcTransport>? transportFactory;

    // 破棄とトランスポートの差し替えを排他し、破棄後に再接続した接続が残らないようにする。
    private readonly object transportLock = new();
    private bool disposed;

    // PLC に残るモニタ登録は最後の1つだけのため、登録のたびに世代を進めて古い MonitorSession を検出する。
    // 登録と世代の更新、世代の確認とモニタ読み出しを、それぞれ monitorGate の内側で一まとめに行う。
    private readonly SemaphoreSlim monitorGate = new(1, 1);
    private int monitorGeneration;

    internal Mcp(
        IPlcTransport transport,
        ushort timeout,
        IPacketBuilder? route = null,
        bool isAscii = false,
        RequestFrame requestFrame = RequestFrame.E3,
        ProcessorSeries processorSeries = ProcessorSeries.Q
    ) : base (
        transport
    )
    {
        this.isAscii = isAscii;
        this.requestFrame = requestFrame;
        this.processorSeries = processorSeries;
        this.monitoringTimer = ToMonitoringTimer(timeout);

        if (route != null) 
        {
            this.route = route;
        }
        else
        {
            this.route = new RoutePacketBuilder();
        }
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

    internal Mcp(
        Func<IPlcTransport> transportFactory,
        ushort timeout,
        IPacketBuilder? route = null,
        bool isAscii = false,
        RequestFrame requestFrame = RequestFrame.E3,
        ProcessorSeries processorSeries = ProcessorSeries.Q
    ) : this (
        transport: transportFactory(),
        timeout: timeout,
        route: route,
        isAscii: isAscii,
        requestFrame: requestFrame,
        processorSeries: processorSeries
    )
    {
        this.transportFactory = transportFactory;
    }

    internal Mcp(
        string ip,
        int port,
        ushort timeout,
        bool isUdp = false,
        IPacketBuilder? route = null,
        bool isAscii = false,
        RequestFrame requestFrame = RequestFrame.E3,
        ProcessorSeries processorSeries = ProcessorSeries.Q
    ) : this (
        transportFactory: () => isUdp ? new Transports.UdpPlcTransport(ip, port, timeout) : (IPlcTransport)new Transports.TcpPlcTransport(ip, port, timeout),
        route: route,
        isAscii: isAscii,
        requestFrame: requestFrame,
        processorSeries: processorSeries,
        timeout: timeout
    )
    {
        if (route != null) 
        {
            this.route = route;
        }
        else
        {
            this.route = new RoutePacketBuilder();
        }
    }

    internal async Task RemoteUnlockAsync(string password)
    {
        await new PlcCommandHandler<bool>().ExecuteAsync(
            new RemoteUnlockCommand(password, monitoringTimer),
            this
        );
    }

    internal void RemoteUnlock(string password)
    {
        new PlcCommandHandler<bool>().Execute(
            new RemoteUnlockCommand(password, monitoringTimer),
            this
        );
    }

    internal async Task RemoteLockAsync(string password)
    {
        await new PlcCommandHandler<bool>().ExecuteAsync(
            new RemoteLockCommand(password, monitoringTimer),
            this
        );
    }

    internal void RemoteLock(string password)
    {
        new PlcCommandHandler<bool>().Execute(
            new RemoteLockCommand(password, monitoringTimer),
            this
        );
    }

    /// <summary>
    /// インスタンス破棄
    /// </summary>
    /// <remarks>
    /// 通信トランスポートを解放します。
    /// </remarks>
    public override void Dispose()
    {
        lock (transportLock)
        {
            disposed = true;
            base.Dispose();
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
            if (disposed)
            {
                // RemoteReset の再接続中に Dispose された場合、新しい接続を残さない
                newTransport.Dispose();
                throw new ObjectDisposedException(GetType().Name);
            }

            ReplaceTransport(newTransport);
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
        await new PlcCommandHandler<bool>().ExecuteAsync(create(monitoringTimer), this);
    }

    internal void RemoteOperation(Func<ushort, RemoteOperationCommand> create)
    {
        new PlcCommandHandler<bool>().Execute(create(monitoringTimer), this);
    }

    internal async Task<ushort[][]> MultiBlockReadAsync(DeviceBlock[] blocks)
    {
        return await new PlcCommandHandler<ushort[][]>().ExecuteAsync(
            new MultiBlockReadCommand(blocks, monitoringTimer, processorSeries),
            this
        );
    }

    internal ushort[][] MultiBlockRead(DeviceBlock[] blocks)
    {
        return new PlcCommandHandler<ushort[][]>().Execute(
            new MultiBlockReadCommand(blocks, monitoringTimer, processorSeries),
            this
        );
    }

    internal async Task MultiBlockWriteAsync(DeviceBlock[] blocks)
    {
        await new PlcCommandHandler<bool>().ExecuteAsync(
            new MultiBlockWriteCommand(blocks, monitoringTimer, processorSeries),
            this
        );
    }

    internal void MultiBlockWrite(DeviceBlock[] blocks)
    {
        new PlcCommandHandler<bool>().Execute(
            new MultiBlockWriteCommand(blocks, monitoringTimer, processorSeries),
            this
        );
    }

    internal async Task<bool[]> BitBatchReadAsync(Prefix prefix, string address, ushort bitLength)
    {
        return await new PlcCommandHandler<bool[]>().ExecuteAsync(
            new BitBatchReadCommand(prefix, address, bitLength, monitoringTimer, processorSeries),
            this
        );
    }

    internal bool[] BitBatchRead(Prefix prefix, string address, ushort bitLength)
    {
        return new PlcCommandHandler<bool[]>().Execute(
            new BitBatchReadCommand(prefix, address, bitLength, monitoringTimer, processorSeries),
            this
        );
    }

    internal async Task BitBatchWriteAsync(Prefix prefix, string address, bool[]values)
    {
        await new PlcCommandHandler<bool>().ExecuteAsync(
            new BitBatchWriteCommand(prefix, address, values, monitoringTimer, processorSeries),
            this
        );
    }

    internal void BitBatchWrite(Prefix prefix, string address, bool[]values)
    {
        new PlcCommandHandler<bool>().Execute(
            new BitBatchWriteCommand(prefix, address, values, monitoringTimer, processorSeries),
            this
        );
    }
    
    // wordLength は T 型の要素数（送信ワード数は WordBatchReadCommand 内で GetWordLength<T>() 倍される）。
    internal async Task<T[]> WordBatchReadAsync<T>(Prefix prefix, string address, ushort wordLength) where T : unmanaged
    {
        return await new PlcCommandHandler<T[]>().ExecuteAsync(
            new WordBatchReadCommand<T>(prefix, address, wordLength, monitoringTimer, processorSeries),
            this
        );
    }

    // wordLength は T 型の要素数（送信ワード数は WordBatchReadCommand 内で GetWordLength<T>() 倍される）。
    internal T[] WordBatchRead<T>(Prefix prefix, string address, ushort wordLength) where T : unmanaged
    {
        return new PlcCommandHandler<T[]>().Execute(
            new WordBatchReadCommand<T>(prefix, address, wordLength, monitoringTimer, processorSeries),
            this
        );
    }

    internal async Task WordBatchWriteAsync<T>(Prefix prefix, string address, T[]values) where T : unmanaged
    {
        await new PlcCommandHandler<bool>().ExecuteAsync(
            new WordBatchWriteCommand<T>(prefix, address, values, monitoringTimer, processorSeries),
            this
        );
    }

    internal void WordBatchWrite<T>(Prefix prefix, string address, T[]values) where T : unmanaged
    {
        new PlcCommandHandler<bool>().Execute(
            new WordBatchWriteCommand<T>(prefix, address, values, monitoringTimer, processorSeries),
            this
        );
    }

    internal async Task<(T1[] wordValues, T2[] doubleValues)> WordRandomReadAsync<T1,T2>((Prefix, string)[] wordAddresses, (Prefix, string)[] doubleWordAddresses) 
        where T1 : unmanaged
        where T2 : unmanaged
    {
        return await new PlcCommandHandler<(T1[], T2[])>().ExecuteAsync(
            new WordRandomReadCommand<T1, T2>(wordAddresses, doubleWordAddresses, monitoringTimer, processorSeries),
            this
        );
    }

    internal (T1[] wordValues, T2[] doubleValues) WordRandomRead<T1,T2>((Prefix, string)[] wordAddresses, (Prefix, string)[] doubleWordAddresses) 
        where T1 : unmanaged
        where T2 : unmanaged
    {
        return new PlcCommandHandler<(T1[], T2[])>().Execute(
            new WordRandomReadCommand<T1, T2>(wordAddresses, doubleWordAddresses, monitoringTimer, processorSeries),
            this
        );
    }

    internal async Task WordRandomWriteAsync<T1,T2>((Prefix prefix, string address, T1 value)[] wordDevices, (Prefix prefix, string address, T2 value)[] doubleWorsDevices) 
        where T1 : unmanaged
        where T2 : unmanaged
    {
        await new PlcCommandHandler<bool>().ExecuteAsync(
            new WordRandomWriteCommand<T1, T2>(wordDevices, doubleWorsDevices, monitoringTimer, processorSeries),
            this
        );
    }

    internal void WordRandomWrite<T1,T2>((Prefix prefix, string address, T1 value)[] wordDevices, (Prefix prefix, string address, T2 value)[] doubleWorsDevices)
        where T1 : unmanaged
        where T2 : unmanaged
    {
        new PlcCommandHandler<bool>().Execute(
            new WordRandomWriteCommand<T1, T2>(wordDevices, doubleWorsDevices, monitoringTimer, processorSeries),
            this
        );
    }

    internal async Task BitRandomWriteAsync((Prefix prefix, string address, bool value)[] bitDevices)
    {
        await new PlcCommandHandler<bool>().ExecuteAsync(
            new BitRandomWriteCommand(bitDevices, monitoringTimer, processorSeries),
            this
        );
    }

    internal void BitRandomWrite((Prefix prefix, string address, bool value)[] bitDevices)
    {
        new PlcCommandHandler<bool>().Execute(
            new BitRandomWriteCommand(bitDevices, monitoringTimer, processorSeries),
            this
        );
    }

    /// <summary>
    /// デバイスモニター登録（非同期）
    /// </summary>
    /// <remarks>
    /// モニターするデバイスを非同期でPLCに登録します。
    /// </remarks>
    /// <param name="wordAddresses">
    /// 16ビット単位でモニターするデバイスアドレスの配列を指定します。<br />
    /// ・<c>prefix</c>:モニター対象のデバイスコードを指定します。<br/>
    /// ・<c>address</c>:モニター対象のアドレスを指定します。
    /// </param>
    /// <param name="doubleWordAddresses">
    /// 32ビット単位でモニターするデバイスの配列を指定します。<br/>
    /// ・<c>prefix</c>:モニター対象のデバイスコードを指定します。<br/>
    /// ・<c>address</c>:モニター対象のアドレスを指定します。
    /// </param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正の場合に例外をスローします。</exception>
    /// <exception cref="ArgumentException">モニター登録のデバイス範囲を超過した場合に例外をスローします。</exception>
    /// <exception cref="RecivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    public async Task MonitorRegistAsync((Prefix, string)[] wordAddresses, (Prefix, string)[] doubleWordAddresses)
    {
        await RegisterMonitorAsync(wordAddresses, doubleWordAddresses);
    }

    /// <summary>
    /// デバイスモニター登録
    /// </summary>
    /// <remarks>
    /// モニターするデバイスをPLCに登録します。
    /// </remarks>
    /// <param name="wordAddresses">
    /// 16ビット単位でモニターするデバイスアドレスの配列を指定します。<br />
    /// ・<c>prefix</c>:モニター対象のデバイスコードを指定します。<br/>
    /// ・<c>address</c>:モニター対象のアドレスを指定します。
    /// </param>
    /// <param name="doubleWordAddresses">
    /// 32ビット単位でモニターするデバイスの配列を指定します。<br/>
    /// ・<c>prefix</c>:モニター対象のデバイスコードを指定します。<br/>
    /// ・<c>address</c>:モニター対象のアドレスを指定します。
    /// </param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正の場合に例外をスローします。</exception>
    /// <exception cref="ArgumentException">モニター登録のデバイス範囲を超過した場合に例外をスローします。</exception>
    /// <exception cref="RecivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    public void MonitorRegist((Prefix, string)[] wordAddresses, (Prefix, string)[] doubleWordAddresses)
    {
        RegisterMonitor(wordAddresses, doubleWordAddresses);
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
            new PlcCommandHandler<bool>().Execute(command, this);
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
            await new PlcCommandHandler<bool>().ExecuteAsync(command, this);
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
            return Monitor<ushort, uint>(wordAddresses, doubleWordAddresses);
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
            return await MonitorAsync<ushort, uint>(wordAddresses, doubleWordAddresses);
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
                "This MonitorSession is no longer valid because the monitor registration was replaced by another MonitorRegist call."
            );
        }
    }

    /// <summary>
    /// デバイスモニター（非同期）
    /// </summary>
    /// <remarks>
    /// モニター登録したデバイスの値を非同期でPLCから読み込みます。<br/>
    /// 指定された型<c>T1</c>、<c>T2</c>に応じて、内部的に読み込むデバイス点数は自動的に調整されます。
    /// </remarks>
    /// <typeparam name="T1">
    /// 16ビット単位で読み込むデータの型。bool, short, int などの値型を指定します。
    /// `unmanaged` 制約があるため、参照型は使用できません。
    /// </typeparam>
    /// <typeparam name="T2">
    /// 32ビット単位で読み込むデータの型。bool, short, int などの値型を指定します。
    /// `unmanaged` 制約があるため、参照型は使用できません。
    /// </typeparam>
    /// <param name="wordAddresses">
    /// 16ビット単位でモニターするデバイスアドレスの配列を指定します。<br />
    /// ・<c>prefix</c>:モニター対象のデバイスコードを指定します。<br/>
    /// ・<c>address</c>:モニター対象のアドレスを指定します。
    /// </param>
    /// <param name="doubleWordAddresses">
    /// 32ビット単位でモニターするデバイスの配列を指定します。<br/>
    /// ・<c>prefix</c>:モニター対象のデバイスコードを指定します。<br/>
    /// ・<c>address</c>:モニター対象のアドレスを指定します。
    /// </param>
    /// <exception cref="ArgumentException">モニター登録のデバイス範囲を超過した場合に例外をスローします。</exception>
    /// <exception cref="RecivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    /// <returns>
    /// PLCから読み込んだ値を指定した型<c>T1</c>、<c>T2</c>に変換して返します。<br/>
    /// ・<c>wordValues</c>: 16ビット単位で読み込まれた <c>T1</c>型の値の配列<br/>
    /// ・<c>doubleValues</c>: 32ビット単位で読み込まれた <c>T2</c>型の値の配列
    /// </returns>
    public async Task<(T1[] wordValues, T2[] doubleValues)> MonitorAsync<T1,T2>((Prefix, string)[] wordAddresses, (Prefix, string)[] doubleWordAddresses) 
        where T1 : unmanaged
        where T2 : unmanaged
    {
        return await new PlcCommandHandler<(T1[], T2[])>().ExecuteAsync(
            new MonitorCommand<T1, T2>(wordAddresses, doubleWordAddresses, monitoringTimer, processorSeries),
            this
        );
    }

    /// <summary>
    /// デバイスモニター
    /// </summary>
    /// <remarks>
    /// モニター登録したデバイスの値をPLCから読み込みます。<br/>
    /// 指定された型<c>T1</c>、<c>T2</c>に応じて、内部的に読み込むデバイス点数は自動的に調整されます。
    /// </remarks>
    /// <typeparam name="T1">
    /// 16ビット単位で読み込むデータの型。bool, short, int などの値型を指定します。
    /// `unmanaged` 制約があるため、参照型は使用できません。
    /// </typeparam>
    /// <typeparam name="T2">
    /// 32ビット単位で読み込むデータの型。bool, short, int などの値型を指定します。
    /// `unmanaged` 制約があるため、参照型は使用できません。
    /// </typeparam>
    /// <param name="wordAddresses">
    /// 16ビット単位でモニターするデバイスアドレスの配列を指定します。<br />
    /// ・<c>prefix</c>:モニター対象のデバイスコードを指定します。<br/>
    /// ・<c>address</c>:モニター対象のアドレスを指定します。
    /// </param>
    /// <param name="doubleWordAddresses">
    /// 32ビット単位でモニターするデバイスの配列を指定します。<br/>
    /// ・<c>prefix</c>:モニター対象のデバイスコードを指定します。<br/>
    /// ・<c>address</c>:モニター対象のアドレスを指定します。
    /// </param>
    /// <exception cref="ArgumentException">モニター登録のデバイス範囲を超過した場合に例外をスローします。</exception>
    /// <exception cref="RecivePacketException">受信したパケットの内容が不正な値の場合に例外をスローします。</exception>
    /// <exception cref="McProtocolException">PLCからエラーコードを受信した場合に例外をスローします。</exception>
    /// <returns>
    /// PLCから読み込んだ値を指定した型<c>T1</c>、<c>T2</c>に変換して返します。<br/>
    /// ・<c>wordValues</c>: 16ビット単位で読み込まれた <c>T1</c>型の値の配列<br/>
    /// ・<c>doubleValues</c>: 32ビット単位で読み込まれた <c>T2</c>型の値の配列
    /// </returns>
    public (T1[] wordValues, T2[] doubleValues) Monitor<T1,T2>((Prefix, string)[] wordAddresses, (Prefix, string)[] doubleWordAddresses) 
        where T1 : unmanaged
        where T2 : unmanaged
    {
        return new PlcCommandHandler<(T1[], T2[])>().Execute(
            new MonitorCommand<T1, T2>(wordAddresses, doubleWordAddresses, monitoringTimer, processorSeries),
            this
        );
    }
}
