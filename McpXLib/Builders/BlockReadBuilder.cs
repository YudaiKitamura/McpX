using McpXLib.Enums;
using McpXLib.Utils;

namespace McpXLib;

/// <summary>
/// 複数ブロック一括読み込みのリクエストビルダー。
/// </summary>
/// <remarks>
/// 追加した範囲（ブロック）を、複数ブロック一括読出し（コマンド: 0406）でまとめて読み込みます。<br/>
/// 読み取り完了後、各コールバックに変換済みの値が渡されます。
/// </remarks>
public sealed class BlockReadBuilder
{
    internal readonly List<BlockReadEntry> entries = new();

    /// <summary>
    /// 読み込むデバイス範囲と、読み取り後に値を受け取るコールバックを追加します。
    /// </summary>
    /// <typeparam name="T">
    /// 読み込むデータの型。<c>bool</c>（ビットデバイスのみ）、<c>short</c>、<c>int</c>、<c>float</c> などの値型を指定します。<br/>
    /// ビットデバイスをワード型で指定した場合は、1ワード=16点として読み込みます。
    /// </typeparam>
    /// <param name="prefix">読み込み対象の先頭デバイスコードを指定します。</param>
    /// <param name="address">読み込み対象の先頭アドレスを指定します。</param>
    /// <param name="length">読み込み対象の要素数（<c>T</c>型の要素数）を指定します。</param>
    /// <param name="onRead">読み取り完了後に、変換済みの値<c>T[]</c>を受け取るコールバックを指定します。</param>
    /// <exception cref="ArgumentException">ワードデバイスに <c>bool</c> を指定した場合にスローします。</exception>
    /// <exception cref="NotSupportedException">非対応の型を指定した場合にスローします。</exception>
    /// <returns>メソッドチェーン用に自身を返します。</returns>
    public BlockReadBuilder Add<T>(Prefix prefix, string address, ushort length, Action<T[]> onRead) where T : unmanaged
    {
        entries.Add(BlockReadEntry.TryCreate(prefix, address, length, onRead)
            ?? throw new ArgumentException($"{typeof(T)} cannot be read from {prefix} with multiple block access.", nameof(prefix)));
        return this;
    }
}

/// <summary>
/// 複数ブロックで読み込む1つの範囲。ワード単位（ビットデバイスは1ワード=16点）で読み、<c>T</c> に変換して通知する。
/// </summary>
internal sealed class BlockReadEntry(Prefix prefix, string address, int points, Action<ushort[]> onWords)
{
    internal Prefix Prefix { get; } = prefix;

    internal string Address { get; } = address;

    internal int Points { get; } = points;

    internal Action<ushort[]> OnWords { get; } = onWords;

    // 複数ブロックで読み込めない指定（ワードデバイスへの bool）は null
    internal static BlockReadEntry? TryCreate<T>(Prefix prefix, string address, ushort length, Action<T[]> onRead) where T : unmanaged
    {
        if (typeof(T) == typeof(bool))
        {
            if (!DeviceConverter.IsBitDevice(prefix))
            {
                return null;
            }

            // 1ワード=16点。余分に読んだビットは捨てる。
            return new BlockReadEntry(prefix, address, (length + 15) / 16, words => onRead((T[])(object)
                Enumerable.Range(0, length).Select(i => (words[i / 16] >> (i % 16) & 1) == 1).ToArray()));
        }

        return new BlockReadEntry(prefix, address, DeviceConverter.GetWordLength<T>() * length, words => onRead(
            DeviceConverter.ConvertValueArray<T>(words.SelectMany(BitConverter.GetBytes).ToArray())));
    }
}
