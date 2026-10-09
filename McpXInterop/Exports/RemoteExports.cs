using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using McpXLib.Enums;

namespace McpXNative;

/// <summary>
/// リモート操作（mcpx_remote_run / stop / pause / latch_clear / reset）。
/// </summary>
internal static unsafe class RemoteExports
{
    private const int DefaultReconnectTimeoutMs = 30000;

    [UnmanagedCallersOnly(EntryPoint = "mcpx_remote_run", CallConvs = [typeof(CallConvCdecl)])]
    public static int Run(ulong client, byte force, byte clearMode, NativeError* err)
    {
        if (clearMode > (byte)RemoteRunClearMode.All)
        {
            return Errors.InvalidArgument(err, "clear_mode must be MCPX_CLEAR_NONE, MCPX_CLEAR_OUTSIDE_LATCH or MCPX_CLEAR_ALL.");
        }

        return Execute(client, err, entry => entry.Client.RemoteRun(force != 0, (RemoteRunClearMode)clearMode));
    }

    [UnmanagedCallersOnly(EntryPoint = "mcpx_remote_stop", CallConvs = [typeof(CallConvCdecl)])]
    public static int Stop(ulong client, NativeError* err)
        => Execute(client, err, entry => entry.Client.RemoteStop());

    [UnmanagedCallersOnly(EntryPoint = "mcpx_remote_pause", CallConvs = [typeof(CallConvCdecl)])]
    public static int Pause(ulong client, byte force, NativeError* err)
        => Execute(client, err, entry => entry.Client.RemotePause(force != 0));

    [UnmanagedCallersOnly(EntryPoint = "mcpx_remote_latch_clear", CallConvs = [typeof(CallConvCdecl)])]
    public static int LatchClear(ulong client, NativeError* err)
        => Execute(client, err, entry => entry.Client.RemoteLatchClear());

    [UnmanagedCallersOnly(EntryPoint = "mcpx_remote_reset", CallConvs = [typeof(CallConvCdecl)])]
    public static int Reset(ulong client, int reconnectTimeoutMs, NativeError* err)
        => Execute(client, err, entry => entry.Client.RemoteReset(reconnectTimeoutMs < 0 ? DefaultReconnectTimeoutMs : reconnectTimeoutMs));

    private static int Execute(ulong client, NativeError* err, Action<ClientEntry> operation)
    {
        ClientEntry? entry = null;
        try
        {
            entry = HandleTable.Get(client);
            if (entry == null)
            {
                return Errors.InvalidHandle(err);
            }

            operation(entry);
            return Errors.Ok(err);
        }
        catch (Exception ex)
        {
            return Errors.FromException(err, ex, entry);
        }
    }
}
