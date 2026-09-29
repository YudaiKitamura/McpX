using McpXLib.Enums;

namespace McpXNative;

/// <summary>
/// 検証済みの項目（mcpx_item）。
/// </summary>
internal readonly record struct ParsedItem(Prefix Prefix, string Address, McpxType Type, ushort Count, bool IsRange, nint Buffer);

internal static unsafe class Items
{
    internal const int MaxItems = ushort.MaxValue;

    /// <summary>
    /// mcpx_item の配列を検証して読み取ります。
    /// </summary>
    /// <param name="requireAddress">アドレスを必須とするか（モニタの読み出しでは参照しない）。</param>
    /// <param name="allowSingle">SINGLE を許可するか。</param>
    /// <param name="allowRange">RANGE を許可するか。</param>
    internal static bool TryParse(
        NativeItem* items, nuint count, bool requireAddress, bool allowSingle, bool allowRange,
        NativeError* err, out ParsedItem[] parsed, out int status)
    {
        parsed = [];
        status = (int)McpxStatus.Ok;

        if (items == null || count == 0 || count > MaxItems)
        {
            status = Errors.InvalidArgument(err, $"items must not be null and count must be 1 to {MaxItems}.");
            return false;
        }

        var result = new ParsedItem[(int)count];
        for (int i = 0; i < result.Length; i++)
        {
            var item = items[i];

            if (!Validation.IsKnownPrefix(item.Prefix) && requireAddress)
            {
                status = Errors.InvalidArgument(err, $"items[{i}]: unknown device prefix 0x{item.Prefix:X2}.");
                return false;
            }

            int size = TypeDispatch.SizeOf((McpxType)item.Type);
            if (size == 0)
            {
                status = Errors.InvalidArgument(err, $"items[{i}]: unknown value type {item.Type}.");
                return false;
            }

            var kind = (McpxItemKind)item.Kind;
            if ((kind == McpxItemKind.Single && !allowSingle) || (kind == McpxItemKind.Range && !allowRange)
                || (kind != McpxItemKind.Single && kind != McpxItemKind.Range))
            {
                status = Errors.InvalidArgument(err, $"items[{i}]: kind {item.Kind} is not allowed here.");
                return false;
            }

            uint elements = kind == McpxItemKind.Single ? 1 : item.Count;
            if (kind == McpxItemKind.Single ? item.Count > 1 : elements < 1 || elements > ushort.MaxValue)
            {
                status = Errors.InvalidArgument(err, $"items[{i}]: count must be 1 to 65535 (0 or 1 for SINGLE).");
                return false;
            }

            string address = string.Empty;
            if (requireAddress && (!Utf8.TryRead((byte*)item.Address, Validation.MaxAddressBytes, out address) || address.Length == 0))
            {
                status = Errors.InvalidArgument(err, $"items[{i}]: address must be a non-empty string of at most {Validation.MaxAddressBytes} bytes.");
                return false;
            }

            if (item.Buffer == IntPtr.Zero)
            {
                status = Errors.InvalidArgument(err, $"items[{i}]: buffer must not be null.");
                return false;
            }

            if (item.BufferSize < (nuint)elements * (nuint)size)
            {
                status = Errors.Fail(err, McpxStatus.BufferTooSmall, $"items[{i}]: buffer must be at least {(ulong)elements * (ulong)size} bytes.");
                return false;
            }

            result[i] = new ParsedItem((Prefix)item.Prefix, address, (McpxType)item.Type, (ushort)elements, kind == McpxItemKind.Range, item.Buffer);
        }

        parsed = result;
        return true;
    }
}
