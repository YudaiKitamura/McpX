using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using McpXLib.Enums;

namespace McpXNative;

/// <summary>
/// 文字列の読み書き（mcpx_read_string / mcpx_write_string）。PLC 側は Shift_JIS、ABI 側は UTF-8。
/// </summary>
internal static unsafe class StringExports
{
    // 65535 ワード（131070 バイト）の Shift_JIS は、UTF-8 で最大 3 倍になる
    private const int MaxValueBytes = ushort.MaxValue * 2 * 3;

    [UnmanagedCallersOnly(EntryPoint = "mcpx_read_string", CallConvs = [typeof(CallConvCdecl)])]
    public static int ReadString(ulong client, byte prefix, byte* address, uint wordLength, byte* output, nuint outputSize, nuint* outputLength, NativeError* err)
    {
        ClientEntry? entry = null;
        try
        {
            if (outputLength != null)
            {
                *outputLength = 0;
            }

            if (!TryPrepare(client, prefix, address, err, out entry, out var addressText, out var status))
            {
                return status;
            }

            if (wordLength < 1 || wordLength > ushort.MaxValue)
            {
                return Errors.InvalidArgument(err, "word_length must be 1 to 65535.");
            }

            if (output == null && outputSize != 0)
            {
                return Errors.InvalidArgument(err, "out must not be null when out_size is not 0.");
            }

            var bytes = Encoding.UTF8.GetBytes(entry!.Client.ReadString((Prefix)prefix, addressText, (ushort)wordLength));
            if (outputLength != null)
            {
                *outputLength = (nuint)bytes.Length;
            }

            if (outputSize < (nuint)bytes.Length + 1)
            {
                return Errors.Fail(err, McpxStatus.BufferTooSmall, $"out_size must be at least {bytes.Length + 1} bytes.");
            }

            bytes.AsSpan().CopyTo(new Span<byte>(output, bytes.Length));
            output[bytes.Length] = 0;
            return Errors.Ok(err);
        }
        catch (Exception ex)
        {
            return Errors.FromException(err, ex, entry);
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "mcpx_write_string", CallConvs = [typeof(CallConvCdecl)])]
    public static int WriteString(ulong client, byte prefix, byte* address, byte* value, NativeError* err)
    {
        ClientEntry? entry = null;
        try
        {
            if (!TryPrepare(client, prefix, address, err, out entry, out var addressText, out var status))
            {
                return status;
            }

            if (!Utf8.TryRead(value, MaxValueBytes, out var text))
            {
                return Errors.InvalidArgument(err, $"value must not be null and must be at most {MaxValueBytes} bytes.");
            }

            entry!.Client.WriteString((Prefix)prefix, addressText, text);
            return Errors.Ok(err);
        }
        catch (Exception ex)
        {
            return Errors.FromException(err, ex, entry);
        }
    }

    private static bool TryPrepare(ulong client, byte prefix, byte* address, NativeError* err, out ClientEntry? entry, out string addressText, out int status)
    {
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

        if (!Utf8.TryRead(address, Validation.MaxAddressBytes, out addressText) || addressText.Length == 0)
        {
            status = Errors.InvalidArgument(err, $"address must be a non-empty string of at most {Validation.MaxAddressBytes} bytes.");
            return false;
        }

        return true;
    }
}
