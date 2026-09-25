using McpXLib.Enums;

namespace McpXLib;

/// <summary>
/// 連続／ランダム統合デバイス書き込みのリクエストビルダー。
/// </summary>
/// <remarks>
/// 配列を指定した <see cref="Add{T}(Prefix, string, T[])"/> は連続アクセス（一括書き込み）、
/// 単一の値を指定した <see cref="Add{T}(Prefix, string, T)"/> はランダムアクセスで書き込まれます。
/// </remarks>
public sealed class WriteBuilder
{
    // 単一指定（ランダムアクセス）のエントリ。
    internal readonly RandomWriteBuilder random = new();

    // 範囲指定（連続アクセス）のエントリ。登録順に実行する。
    internal readonly List<(Action<McpX> write, Func<McpX, Task> writeAsync)> batchEntries = new();

    /// <summary>
    /// ランダムアクセスで書き込むデバイスと値を追加します。
    /// </summary>
    /// <typeparam name="T">
    /// 書き込む値の型。<c>bool</c>(ビット) / <c>short</c>,<c>ushort</c>(ワード) /
    /// <c>int</c>,<c>uint</c>,<c>float</c>(ダブルワード) を指定できます。
    /// </typeparam>
    /// <param name="prefix">書き込み対象のデバイスコードを指定します。</param>
    /// <param name="address">書き込み対象のアドレスを指定します。</param>
    /// <param name="value">書き込む値を指定します。</param>
    /// <exception cref="NotSupportedException">ランダムアクセス非対応の型（<c>long</c>,<c>double</c> など）を指定した場合にスローします。</exception>
    /// <returns>メソッドチェーン用に自身を返します。</returns>
    public WriteBuilder Add<T>(Prefix prefix, string address, T value) where T : unmanaged
    {
        random.Add(prefix, address, value);
        return this;
    }

    /// <summary>
    /// 連続アクセスで書き込むデバイス範囲と値を追加します。
    /// </summary>
    /// <typeparam name="T">
    /// 書き込むデータの型。bool, short, int などの値型を指定します。
    /// `unmanaged` 制約があるため、参照型は使用できません。
    /// </typeparam>
    /// <param name="prefix">書き込み対象の先頭デバイスコードを指定します。</param>
    /// <param name="address">書き込み対象の先頭アドレスを指定します。</param>
    /// <param name="values">書き込む値を配列で指定します。</param>
    /// <returns>メソッドチェーン用に自身を返します。</returns>
    public WriteBuilder Add<T>(Prefix prefix, string address, T[] values) where T : unmanaged
    {
        batchEntries.Add((
            m => m.BatchWrite(prefix, address, values),
            m => m.BatchWriteAsync(prefix, address, values)
        ));
        return this;
    }
}
