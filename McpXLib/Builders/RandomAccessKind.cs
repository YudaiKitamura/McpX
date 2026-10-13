using McpXLib.Enums;
using McpXLib.Utils;

namespace McpXLib.Builders;

/// <summary>
/// ランダムアクセス時に型から決まるアクセス単位。
/// </summary>
internal enum RandomAccessKind
{
    Bit,
    Word,
    DoubleWord,
}

internal static class RandomAccessKindResolver
{
    /// <summary>
    /// 型<c>T</c>からランダムアクセスの単位を決定します。
    /// bool→ビット / short,ushort→ワード / int,uint,float→ダブルワード。
    /// long,double は MCプロトコルのランダムアクセスに存在しないため非対応。
    /// </summary>
    internal static RandomAccessKind Resolve<T>() where T : unmanaged
    {
        var type = typeof(T);

        if (type == typeof(bool))
        {
            return RandomAccessKind.Bit;
        }

        if (type == typeof(short) || type == typeof(ushort))
        {
            return RandomAccessKind.Word;
        }

        if (type == typeof(int) || type == typeof(uint) || type == typeof(float))
        {
            return RandomAccessKind.DoubleWord;
        }

        throw new NotSupportedException(
            $"Type {type} is not supported for random access. " +
            "Supported types are bool, short, ushort, int, uint, and float. " +
            "(long and double are not available for random read/write.)"
        );
    }

    /// <summary>
    /// ランダム読出し・モニタで読み込むデバイスを、型<c>T</c>のアクセス単位に応じてワード／ダブルワードのエントリに追加します。
    /// </summary>
    /// <remarks>
    /// ビットはワードで読み、先頭ビット(bit0)を抽出する。
    /// </remarks>
    internal static void AddReadEntry<T>(
        List<(Prefix prefix, string address, Action<ushort> apply)> wordEntries,
        List<(Prefix prefix, string address, Action<uint> apply)> doubleWordEntries,
        Prefix prefix, string address, Action<T> onRead) where T : unmanaged
    {
        if (onRead == null)
        {
            throw new ArgumentNullException(nameof(onRead));
        }

        switch (Resolve<T>())
        {
            case RandomAccessKind.Bit:
                wordEntries.Add((prefix, address, raw => onRead((T)(object)((raw & 0x0001) != 0))));
                break;
            case RandomAccessKind.Word:
                wordEntries.Add((prefix, address, raw => onRead(DeviceConverter.ConvertValueArray<T>(BitConverter.GetBytes(raw))[0])));
                break;
            default:
                doubleWordEntries.Add((prefix, address, raw => onRead(DeviceConverter.ConvertValueArray<T>(BitConverter.GetBytes(raw))[0])));
                break;
        }
    }
}
