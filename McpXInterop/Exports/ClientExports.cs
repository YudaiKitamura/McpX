using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using McpXLib;
using McpXLib.Enums;

namespace McpXNative;

/// <summary>
/// 接続・切断（mcpx_connect / mcpx_connect_simulator / mcpx_close / mcpx_set_multi_block）。
/// </summary>
internal static unsafe class ClientExports
{
    private const int MaxHostBytes = 253;
    private const int MaxPasswordBytes = 64;

    [UnmanagedCallersOnly(EntryPoint = "mcpx_connect", CallConvs = [typeof(CallConvCdecl)])]
    public static int Connect(NativeConnectOptions* options, ulong* output, NativeError* err)
    {
        try
        {
            if (options == null || output == null)
            {
                return Errors.InvalidArgument(err, "options and out must not be null.");
            }

            *output = 0;

            if (options->StructSize < (uint)sizeof(NativeConnectOptions))
            {
                return Errors.InvalidArgument(err, "options->struct_size is too small. Initialize it with mcpx_connect_options_init.");
            }

            if (!Utf8.TryRead((byte*)options->Host, MaxHostBytes, out var host) || host.Length == 0)
            {
                return Errors.InvalidArgument(err, $"host must be a non-empty string of at most {MaxHostBytes} bytes.");
            }

            string? password = null;
            if (options->Password != IntPtr.Zero && !Utf8.TryRead((byte*)options->Password, MaxPasswordBytes, out password))
            {
                return Errors.InvalidArgument(err, $"password must be at most {MaxPasswordBytes} bytes.");
            }

            if (options->Port < 1 || options->Port > ushort.MaxValue)
            {
                return Errors.InvalidArgument(err, "port must be 1 to 65535.");
            }

            if (!TryValidateCommon(options->TimeoutMs, options->Frame, options->Series, err, out var status))
            {
                return status;
            }

            var client = new McpX(
                ip: host,
                port: options->Port,
                password: password,
                isAscii: options->IsAscii != 0,
                isUdp: options->IsUdp != 0,
                requestFrame: (RequestFrame)options->Frame,
                timeoutMilliseconds: (ushort)options->TimeoutMs,
                processorSeries: (ProcessorSeries)options->Series,
                useMultiBlockAccess: options->UseMultiBlock != 0
            );

            *output = HandleTable.Add(client);
            return Errors.Ok(err);
        }
        catch (Exception ex)
        {
            return Errors.FromException(err, ex, null);
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "mcpx_connect_simulator", CallConvs = [typeof(CallConvCdecl)])]
    public static int ConnectSimulator(NativeSimulatorOptions* options, ulong* output, NativeError* err)
    {
        try
        {
            if (options == null || output == null)
            {
                return Errors.InvalidArgument(err, "options and out must not be null.");
            }

            *output = 0;

            if (options->StructSize < (uint)sizeof(NativeSimulatorOptions))
            {
                return Errors.InvalidArgument(err, "options->struct_size is too small. Initialize it with mcpx_simulator_options_init.");
            }

            var host = "127.0.0.1";
            if (options->Host != IntPtr.Zero && (!Utf8.TryRead((byte*)options->Host, MaxHostBytes, out host) || host.Length == 0))
            {
                return Errors.InvalidArgument(err, $"host must be a non-empty string of at most {MaxHostBytes} bytes.");
            }

            if (!TryValidateCommon(options->TimeoutMs, options->Frame, options->Series, err, out var status))
            {
                return status;
            }

            var client = new McpXSimulator(
                systemNo: options->SystemNo,
                cpuNo: options->CpuNo,
                ip: host,
                requestFrame: (RequestFrame)options->Frame,
                timeoutMilliseconds: (ushort)options->TimeoutMs,
                processorSeries: (ProcessorSeries)options->Series,
                useMultiBlockAccess: options->UseMultiBlock != 0
            );

            *output = HandleTable.Add(client);
            return Errors.Ok(err);
        }
        catch (Exception ex)
        {
            return Errors.FromException(err, ex, null);
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "mcpx_close", CallConvs = [typeof(CallConvCdecl)])]
    public static int Close(ulong client, NativeError* err)
    {
        try
        {
            if (client == 0)
            {
                return Errors.Ok(err);
            }

            // 表から先に外し、以降の呼び出しを無効にする（二重 close も INVALID_HANDLE になる）
            var entry = HandleTable.Remove(client);
            if (entry == null)
            {
                return Errors.InvalidHandle(err);
            }

            // 通信中の呼び出しは、ソケットが閉じられて失敗し、Closed を見て MCPX_E_CLOSED を返す
            entry.Closed = true;
            entry.Client.Dispose();
            return Errors.Ok(err);
        }
        catch (Exception ex)
        {
            return Errors.FromException(err, ex, null);
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "mcpx_set_multi_block", CallConvs = [typeof(CallConvCdecl)])]
    public static int SetMultiBlock(ulong client, byte enable, NativeError* err)
    {
        try
        {
            var entry = HandleTable.Get(client);
            if (entry == null)
            {
                return Errors.InvalidHandle(err);
            }

            entry.Client.UseMultiBlockAccess = enable != 0;
            return Errors.Ok(err);
        }
        catch (Exception ex)
        {
            return Errors.FromException(err, ex, null);
        }
    }

    private static bool TryValidateCommon(uint timeoutMs, byte frame, byte series, NativeError* err, out int status)
    {
        status = (int)McpxStatus.Ok;

        if (timeoutMs > ushort.MaxValue)
        {
            status = Errors.InvalidArgument(err, "timeout_ms must be 0 to 65535.");
            return false;
        }

        if (frame > (byte)RequestFrame.E4)
        {
            status = Errors.InvalidArgument(err, "frame must be MCPX_FRAME_3E or MCPX_FRAME_4E.");
            return false;
        }

        if (series > (byte)ProcessorSeries.iQR)
        {
            status = Errors.InvalidArgument(err, "series must be MCPX_SERIES_Q or MCPX_SERIES_IQR.");
            return false;
        }

        return true;
    }
}
