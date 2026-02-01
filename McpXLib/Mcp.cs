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

    private bool isAscii;
    private IPacketBuilder route;
    private RequestFrame requestFrame;

    private ushort timeout;

    private Mcp(
        IPlcTransport transport, 
        ushort timeout,
        IPacketBuilder? route = null, 
        bool isAscii = false, 
        RequestFrame requestFrame = RequestFrame.E3
    ) : base (
        transport
    )
    {
        this.isAscii = isAscii;
        this.requestFrame = requestFrame;
        this.timeout = timeout;

        if (route != null) 
        {
            this.route = route;
        }
        else
        {
            this.route = new RoutePacketBuilder();
        }
    }

    internal Mcp(
        string ip, 
        int port,
        ushort timeout,
        bool isUdp = false,
        IPacketBuilder? route = null,
        bool isAscii = false,
        RequestFrame requestFrame = RequestFrame.E3
    ) : this (
        transport: isUdp ? new Transports.UdpPlcTransport(ip, port) : new Transports.TcpPlcTransport(ip, port, timeout),
        route: route,
        isAscii: isAscii,
        requestFrame: requestFrame,
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
            new RemoteUnlockCommand(password, timeout),
            this
        );
    }

    internal void RemoteUnlock(string password)
    {
        new PlcCommandHandler<bool>().Execute(
            new RemoteUnlockCommand(password, timeout),
            this
        );
    }

    internal async Task RemoteLockAsync(string password)
    {
        await new PlcCommandHandler<bool>().ExecuteAsync(
            new RemoteLockCommand(password, timeout),
            this
        );
    }

    internal void RemoteLock(string password)
    {
        new PlcCommandHandler<bool>().Execute(
            new RemoteLockCommand(password, timeout),
            this
        );
    }

    internal async Task<bool[]> BitBatchReadAsync(Prefix prefix, string address, ushort bitLength)
    {
        return await new PlcCommandHandler<bool[]>().ExecuteAsync(
            new BitBatchReadCommand(prefix, address, bitLength, timeout),
            this
        );
    }

    internal bool[] BitBatchRead(Prefix prefix, string address, ushort bitLength)
    {
        return new PlcCommandHandler<bool[]>().Execute(
            new BitBatchReadCommand(prefix, address, bitLength, timeout),
            this
        );
    }

    internal async Task BitBatchWriteAsync(Prefix prefix, string address, bool[]values)
    {
        await new PlcCommandHandler<bool>().ExecuteAsync(
            new BitBatchWriteCommand(prefix, address, values, timeout),
            this
        );
    }

    internal void BitBatchWrite(Prefix prefix, string address, bool[]values)
    {
        new PlcCommandHandler<bool>().Execute(
            new BitBatchWriteCommand(prefix, address, values, timeout),
            this
        );
    }
    
    internal async Task<T[]> WordBatchReadAsync<T>(Prefix prefix, string address, ushort wordLength) where T : unmanaged
    {
        return await new PlcCommandHandler<T[]>().ExecuteAsync(
            new WordBatchReadCommand<T>(prefix, address, wordLength, timeout),
            this
        );
    }

    internal T[] WordBatchRead<T>(Prefix prefix, string address, ushort wordLength) where T : unmanaged
    {
        return new PlcCommandHandler<T[]>().Execute(
            new WordBatchReadCommand<T>(prefix, address, wordLength, timeout),
            this
        );
    }

    internal async Task WordBatchWriteAsync<T>(Prefix prefix, string address, T[]values) where T : unmanaged
    {
        await new PlcCommandHandler<bool>().ExecuteAsync(
            new WordBatchWriteCommand<T>(prefix, address, values, timeout),
            this
        );
    }

    internal void WordBatchWrite<T>(Prefix prefix, string address, T[]values) where T : unmanaged
    {
        new PlcCommandHandler<bool>().Execute(
            new WordBatchWriteCommand<T>(prefix, address, values, timeout),
            this
        );
    }

    internal async Task<(T1[] wordValues, T2[] doubleValues)> WordRandomReadAsync<T1,T2>((Prefix, string)[] wordAddresses, (Prefix, string)[] doubleWordAddresses) 
        where T1 : unmanaged
        where T2 : unmanaged
    {
        return await new PlcCommandHandler<(T1[], T2[])>().ExecuteAsync(
            new WordRandomReadCommand<T1, T2>(wordAddresses, doubleWordAddresses, timeout),
            this
        );
    }

    internal (T1[] wordValues, T2[] doubleValues) WordRandomRead<T1,T2>((Prefix, string)[] wordAddresses, (Prefix, string)[] doubleWordAddresses) 
        where T1 : unmanaged
        where T2 : unmanaged
    {
        return new PlcCommandHandler<(T1[], T2[])>().Execute(
            new WordRandomReadCommand<T1, T2>(wordAddresses, doubleWordAddresses, timeout),
            this
        );
    }

    internal async Task WordRandomWriteAsync<T1,T2>((Prefix prefix, string address, T1 value)[] wordDevices, (Prefix prefix, string address, T2 value)[] doubleWorsDevices) 
        where T1 : unmanaged
        where T2 : unmanaged
    {
        await new PlcCommandHandler<bool>().ExecuteAsync(
            new WordRandomWriteCommand<T1, T2>(wordDevices, doubleWorsDevices, timeout),
            this
        );
    }

    internal void WordRandomWrite<T1,T2>((Prefix prefix, string address, T1 value)[] wordDevices, (Prefix prefix, string address, T2 value)[] doubleWorsDevices) 
        where T1 : unmanaged
        where T2 : unmanaged
    {
        new PlcCommandHandler<bool>().Execute(
            new WordRandomWriteCommand<T1, T2>(wordDevices, doubleWorsDevices, timeout),
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
        await new PlcCommandHandler<bool>().ExecuteAsync(
            new MonitorRegistCommand(wordAddresses, doubleWordAddresses, timeout),
            this
        );
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
        new PlcCommandHandler<bool>().Execute(
            new MonitorRegistCommand(wordAddresses, doubleWordAddresses, timeout),
            this
        );
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
            new MonitorCommand<T1, T2>(wordAddresses, doubleWordAddresses, timeout),
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
            new MonitorCommand<T1, T2>(wordAddresses, doubleWordAddresses, timeout),
            this
        );
    }
}
