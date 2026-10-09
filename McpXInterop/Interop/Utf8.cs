using System.Text;

namespace McpXNative;

internal static unsafe class Utf8
{
    /// <summary>
    /// NUL 終端の UTF-8 文字列を読み込みます。NULL、または maxBytes 以内に NUL がない場合は false を返します。
    /// </summary>
    internal static bool TryRead(byte* source, int maxBytes, out string value)
    {
        value = string.Empty;
        if (source == null)
        {
            return false;
        }

        int length = 0;
        while (length <= maxBytes && source[length] != 0)
        {
            length++;
        }

        if (length > maxBytes)
        {
            return false;
        }

        value = Encoding.UTF8.GetString(source, length);
        return true;
    }

    /// <summary>
    /// 文字列を UTF-8 の NUL 終端文字列として書き込みます。収まらない場合は文字の途中で切れないよう切り詰めます。
    /// </summary>
    internal static void WriteTruncated(string value, byte* destination, int capacity)
    {
        if (destination == null || capacity <= 0)
        {
            return;
        }

        var bytes = Encoding.UTF8.GetBytes(value);
        int length = Math.Min(bytes.Length, capacity - 1);

        // UTF-8 の後続バイト（10xxxxxx）の途中で切らない
        while (length > 0 && length < bytes.Length && (bytes[length] & 0xC0) == 0x80)
        {
            length--;
        }

        bytes.AsSpan(0, length).CopyTo(new Span<byte>(destination, length));
        destination[length] = 0;
    }
}
