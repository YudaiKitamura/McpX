using McpXLib.Enums;
using McpXLib.Utils;

namespace McpXLib;

/// <summary>
/// 複数ブロック一括書き込みのリクエストビルダー。
/// </summary>
/// <remarks>
/// 追加した範囲（ブロック）を、複数ブロック一括書込み（コマンド: 1406）でまとめて書き込みます。
/// </remarks>
public sealed class BlockWriteBuilder
{
    internal readonly List<BlockWriteEntry> entries = new();

    /// <summary>
    /// 書き込むデバイス範囲と値を追加します。
    /// </summary>
    /// <typeparam name="T">
    /// 書き込むデータの型。<c>bool</c>（ビットデバイスのみ）、<c>short</c>、<c>int</c>、<c>float</c> などの値型を指定します。<br/>
    /// ビットデバイスをワード型で指定した場合は、1ワード=16点として書き込みます。
    /// </typeparam>
    /// <param name="prefix">書き込み対象の先頭デバイスコードを指定します。</param>
    /// <param name="address">書き込み対象の先頭アドレスを指定します。</param>
    /// <param name="values">書き込む値を配列で指定します。<c>bool</c> の場合は16の倍数の要素数を指定してください。</param>
    /// <exception cref="ArgumentException">ワードデバイスに <c>bool</c> を指定した場合、または <c>bool</c> の要素数が16の倍数でない場合にスローします。</exception>
    /// <exception cref="NotSupportedException">非対応の型を指定した場合にスローします。</exception>
    /// <returns>メソッドチェーン用に自身を返します。</returns>
    public BlockWriteBuilder Add<T>(Prefix prefix, string address, T[] values) where T : unmanaged
    {
        entries.Add(BlockWriteEntry.TryCreate(prefix, address, values)
            ?? throw new ArgumentException($"{typeof(T)}[{values.Length}] cannot be written to {prefix} with multiple block access. (bool is only for bit devices, in multiples of 16)", nameof(values)));
        return this;
    }
}

/// <summary>
/// 複数ブロックで書き込む1つの範囲（ワード単位。ビットデバイスは1ワード=16点）。
/// </summary>
internal sealed class BlockWriteEntry(Prefix prefix, string address, ushort[] words)
{
    internal Prefix Prefix { get; } = prefix;

    internal string Address { get; } = address;

    internal ushort[] Words { get; } = words;

    // 複数ブロックで書き込めない指定は null。
    // bool はビットデバイスの16点単位でしか書き込めない（端数を書くと、指定していないビットまで上書きしてしまう）。
    internal static BlockWriteEntry? TryCreate<T>(Prefix prefix, string address, T[] values) where T : unmanaged
    {
        if (typeof(T) == typeof(bool))
        {
            if (!DeviceConverter.IsBitDevice(prefix) || values.Length % 16 != 0)
            {
                return null;
            }

            var bits = (bool[])(object)values;
            return new BlockWriteEntry(prefix, address, Enumerable.Range(0, bits.Length / 16)
                .Select(w => (ushort)Enumerable.Range(0, 16).Sum(b => bits[w * 16 + b] ? 1 << b : 0))
                .ToArray());
        }

        var bytes = DeviceConverter.ConvertByteValueArray(values);
        return new BlockWriteEntry(prefix, address, Enumerable.Range(0, bytes.Length / 2)
            .Select(i => BitConverter.ToUInt16(bytes, i * 2))
            .ToArray());
    }
}
