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
}
