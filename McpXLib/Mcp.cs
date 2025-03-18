using McpXLib.Enums;
using McpXLib.Abstructs;
using McpXLib.Commands;
using McpXLib.Interfaces;
using McpXLib.Helpers;

namespace McpXLib;

public class Mcp : BasePlc, IPlc
{
    public IRoute Route
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

    private IRoute route;

    internal Mcp(string ip, int port, IRoute? route = null) : base(ip, port)
    {
        if (route != null) 
        {
            this.route = route;
        }
        else
        {
            this.route = new RoutePacketHelper();
        }
    }

    internal async Task RemoteUnlockAsync(string password)
    {
        await new PlcCommandHandler<bool>().ExecuteAsync(new RemoteUnlockCommand(password), this);
    }

    internal void RemoteUnlock(string password)
    {
        new PlcCommandHandler<bool>().Execute(new RemoteUnlockCommand(password), this);
    }

    internal async Task RemoteLockAsync(string password)
    {
        await new PlcCommandHandler<bool>().ExecuteAsync(new RemoteLockCommand(password), this);
    }

    internal void RemoteLock(string password)
    {
        new PlcCommandHandler<bool>().Execute(new RemoteLockCommand(password), this);
    }

    internal async Task<bool[]> BitBatchReadAsync(Prefix prefix, string address, ushort bitLength)
    {
        return await new PlcCommandHandler<bool[]>().ExecuteAsync(new BitBatchReadCommand(prefix, address, bitLength), this);
    }

    internal bool[] BitBatchRead(Prefix prefix, string address, ushort bitLength)
    {
        return new PlcCommandHandler<bool[]>().Execute(new BitBatchReadCommand(prefix, address, bitLength), this);
    }

    internal async Task BitBatchWriteAsync(Prefix prefix, string address, bool[]values)
    {
        await new PlcCommandHandler<bool>().ExecuteAsync(new BitBatchWriteCommand(prefix, address, values), this);
    }

    internal void BitBatchWrite(Prefix prefix, string address, bool[]values)
    {
        new PlcCommandHandler<bool>().Execute(new BitBatchWriteCommand(prefix, address, values), this);
    }
    
    internal async Task<T[]> WordBatchReadAsync<T>(Prefix prefix, string address, ushort wordLength) where T : unmanaged
    {
        return await new PlcCommandHandler<T[]>().ExecuteAsync(new WordBatchReadCommand<T>(prefix, address, wordLength), this);
    }

    internal T[] WordBatchRead<T>(Prefix prefix, string address, ushort wordLength) where T : unmanaged
    {
        return new PlcCommandHandler<T[]>().Execute(new WordBatchReadCommand<T>(prefix, address, wordLength), this);
    }

    internal async Task WordBatchWriteAsync<T>(Prefix prefix, string address, T[]values) where T : unmanaged
    {
        await new PlcCommandHandler<bool>().ExecuteAsync(new WordBatchWriteCommand<T>(prefix, address, values), this);
    }

    internal void WordBatchWrite<T>(Prefix prefix, string address, T[]values) where T : unmanaged
    {
        new PlcCommandHandler<bool>().Execute(new WordBatchWriteCommand<T>(prefix, address, values), this);
    }

    internal async Task<(T1[] wordValues, T2[] doubleValues)> WordRandomReadAsync<T1,T2>((Prefix, string)[] wordAddresses, (Prefix, string)[] doubleWordAddresses) 
        where T1 : unmanaged
        where T2 : unmanaged
    {
        return await new PlcCommandHandler<(T1[], T2[])>().ExecuteAsync(new WordRandomReadCommand<T1, T2>(wordAddresses, doubleWordAddresses), this);
    }

    internal (T1[] wordValues, T2[] doubleValues) WordRandomRead<T1,T2>((Prefix, string)[] wordAddresses, (Prefix, string)[] doubleWordAddresses) 
        where T1 : unmanaged
        where T2 : unmanaged
    {
        return new PlcCommandHandler<(T1[], T2[])>().Execute(new WordRandomReadCommand<T1, T2>(wordAddresses, doubleWordAddresses), this);
    }

    internal async Task WordRandomWriteAsync<T1,T2>((Prefix prefix, string address, T1 value)[] wordDevices, (Prefix prefix, string address, T2 value)[] doubleWorsDevices) 
        where T1 : unmanaged
        where T2 : unmanaged
    {
        await new PlcCommandHandler<bool>().ExecuteAsync(new WordRandomWriteCommand<T1, T2>(wordDevices, doubleWorsDevices), this);
    }

    internal void WordRandomWrite<T1,T2>((Prefix prefix, string address, T1 value)[] wordDevices, (Prefix prefix, string address, T2 value)[] doubleWorsDevices) 
        where T1 : unmanaged
        where T2 : unmanaged
    {
        new PlcCommandHandler<bool>().Execute(new WordRandomWriteCommand<T1, T2>(wordDevices, doubleWorsDevices), this);
    }

    public async Task MonitorRegistAsync((Prefix, string)[] wordAddresses, (Prefix, string)[] doubleWordAddresses)
    {
        await new PlcCommandHandler<bool>().ExecuteAsync(new MonitorRegistCommand(wordAddresses, doubleWordAddresses), this);
    }

    public void MonitorRegist((Prefix, string)[] wordAddresses, (Prefix, string)[] doubleWordAddresses)
    {
        new PlcCommandHandler<bool>().Execute(new MonitorRegistCommand(wordAddresses, doubleWordAddresses), this);
    }

    public async Task<(T1[] wordValues, T2[] doubleValues)> MonitorAsync<T1,T2>((Prefix, string)[] wordAddresses, (Prefix, string)[] doubleWordAddresses) 
        where T1 : unmanaged
        where T2 : unmanaged
    {
        return await new PlcCommandHandler<(T1[], T2[])>().ExecuteAsync(new MonitorCommand<T1, T2>(wordAddresses, doubleWordAddresses), this);
    }

    public (T1[] wordValues, T2[] doubleValues) Monitor<T1,T2>((Prefix, string)[] wordAddresses, (Prefix, string)[] doubleWordAddresses) 
        where T1 : unmanaged
        where T2 : unmanaged
    {
        return new PlcCommandHandler<(T1[], T2[])>().Execute(new MonitorCommand<T1, T2>(wordAddresses, doubleWordAddresses), this);
    }
}
