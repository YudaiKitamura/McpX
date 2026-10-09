using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using McpXLib.Enums;

namespace McpXNative;

/// <summary>
/// 単一・連続デバイスの読み書き（mcpx_read / mcpx_write / mcpx_batch_read / mcpx_batch_write）。
/// </summary>
internal static unsafe class AccessExports
{
    [UnmanagedCallersOnly(EntryPoint = "mcpx_read", CallConvs = [typeof(CallConvCdecl)])]
    public static int Read(ulong client, byte prefix, byte* address, byte type, void* output, nuint outputSize, NativeError* err)
    {
        ClientEntry? entry = null;
        try
        {
            if (!TryPrepare(client, prefix, address, type, output, 1, outputSize, err, out entry, out var addressText, out var status))
            {
                return status;
            }

            TypeDispatch.Read(entry!.Client, (Prefix)prefix, addressText, (McpxType)type, output);
            return Errors.Ok(err);
        }
        catch (Exception ex)
        {
            return Errors.FromException(err, ex, entry);
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "mcpx_write", CallConvs = [typeof(CallConvCdecl)])]
    public static int Write(ulong client, byte prefix, byte* address, byte type, void* value, nuint valueSize, NativeError* err)
    {
        ClientEntry? entry = null;
        try
        {
            if (!TryPrepare(client, prefix, address, type, value, 1, valueSize, err, out entry, out var addressText, out var status))
            {
                return status;
            }

            TypeDispatch.Write(entry!.Client, (Prefix)prefix, addressText, (McpxType)type, value);
            return Errors.Ok(err);
        }
        catch (Exception ex)
        {
            return Errors.FromException(err, ex, entry);
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "mcpx_batch_read", CallConvs = [typeof(CallConvCdecl)])]
    public static int BatchRead(ulong client, byte prefix, byte* address, byte type, uint count, void* output, nuint outputSize, NativeError* err)
    {
        ClientEntry? entry = null;
        try
        {
            if (!TryPrepare(client, prefix, address, type, output, count, outputSize, err, out entry, out var addressText, out var status))
            {
                return status;
            }

            TypeDispatch.BatchRead(entry!.Client, (Prefix)prefix, addressText, (McpxType)type, (ushort)count, output);
            return Errors.Ok(err);
        }
        catch (Exception ex)
        {
            return Errors.FromException(err, ex, entry);
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "mcpx_batch_write", CallConvs = [typeof(CallConvCdecl)])]
    public static int BatchWrite(ulong client, byte prefix, byte* address, byte type, uint count, void* values, nuint valuesSize, NativeError* err)
    {
        ClientEntry? entry = null;
        try
        {
            if (!TryPrepare(client, prefix, address, type, values, count, valuesSize, err, out entry, out var addressText, out var status))
            {
                return status;
            }

            TypeDispatch.BatchWrite(entry!.Client, (Prefix)prefix, addressText, (McpxType)type, (ushort)count, values);
            return Errors.Ok(err);
        }
        catch (Exception ex)
        {
            return Errors.FromException(err, ex, entry);
        }
    }

    /// <summary>
    /// 引数を検証し、ハンドルとアドレスを取り出します。McpX を呼ぶ前に不正な値を弾く（範囲外の点数・NULL・短いバッファなど）。
    /// </summary>
    private static bool TryPrepare(
        ulong client, byte prefix, byte* address, byte type, void* buffer, uint count, nuint bufferSize,
        NativeError* err, out ClientEntry? entry, out string addressText, out int status)
    {
        entry = null;
        addressText = string.Empty;
        status = (int)McpxStatus.Ok;

        entry = HandleTable.Get(client);
        if (entry == null)
        {
            status = Errors.InvalidHandle(err);
            return false;
        }

        if (!Validation.IsKnownPrefix(prefix))
        {
            status = Errors.InvalidArgument(err, $"Unknown device prefix 0x{prefix:X2}.");
            return false;
        }

        int size = TypeDispatch.SizeOf((McpxType)type);
        if (size == 0)
        {
            status = Errors.InvalidArgument(err, $"Unknown value type {type}.");
            return false;
        }

        if (!Utf8.TryRead(address, Validation.MaxAddressBytes, out addressText) || addressText.Length == 0)
        {
            status = Errors.InvalidArgument(err, $"address must be a non-empty string of at most {Validation.MaxAddressBytes} bytes.");
            return false;
        }

        if (count < 1 || count > ushort.MaxValue)
        {
            status = Errors.InvalidArgument(err, "count must be 1 to 65535.");
            return false;
        }

        if (buffer == null)
        {
            status = Errors.InvalidArgument(err, "The buffer must not be null.");
            return false;
        }

        if (bufferSize < (nuint)count * (nuint)size)
        {
            status = Errors.Fail(err, McpxStatus.BufferTooSmall, $"The buffer must be at least {(ulong)count * (ulong)size} bytes.");
            return false;
        }

        return true;
    }
}
