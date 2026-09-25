using McpXLib.Enums;

namespace McpXLib;

/// <summary>
/// 連続／ランダム統合デバイス読み込みのリクエストビルダー。
/// </summary>
/// <remarks>
/// 点数を指定した <see cref="Add{T}(Prefix, string, ushort, Action{T[]})"/> は連続アクセス（一括読み込み）、
/// 点数を指定しない <see cref="Add{T}(Prefix, string, Action{T})"/> はランダムアクセスで読み込まれます。<br/>
/// 読み取り完了後、各コールバックに変換済みの値が渡されます。
/// </remarks>
public sealed class ReadBuilder
{
    // 単一指定（ランダムアクセス）のエントリ。
    internal readonly RandomReadBuilder random = new();

    // 範囲指定（連続アクセス）のエントリ。登録順に実行する。
    internal readonly List<(Action<McpX> read, Func<McpX, Task> readAsync)> batchEntries = new();

    /// <summary>
    /// ランダムアクセスで読み込むデバイスと、読み取り後に値を受け取るコールバックを追加します。
    /// </summary>
    /// <typeparam name="T">
    /// 読み込む値の型。<c>bool</c>(ビット) / <c>short</c>,<c>ushort</c>(ワード) /
    /// <c>int</c>,<c>uint</c>,<c>float</c>(ダブルワード) を指定できます。
    /// </typeparam>
    /// <param name="prefix">読み込み対象のデバイスコードを指定します。</param>
    /// <param name="address">読み込み対象のアドレスを指定します。</param>
    /// <param name="onRead">読み取り完了後に、変換済みの値<c>T</c>を受け取るコールバックを指定します。</param>
    /// <exception cref="NotSupportedException">ランダムアクセス非対応の型（<c>long</c>,<c>double</c> など）を指定した場合にスローします。</exception>
    /// <returns>メソッドチェーン用に自身を返します。</returns>
    public ReadBuilder Add<T>(Prefix prefix, string address, Action<T> onRead) where T : unmanaged
    {
        random.Add(prefix, address, onRead);
        return this;
    }

    /// <summary>
    /// 連続アクセスで読み込むデバイス範囲と、読み取り後に値を受け取るコールバックを追加します。
    /// </summary>
    /// <typeparam name="T">
    /// 読み込むデータの型。bool, short, int などの値型を指定します。
    /// `unmanaged` 制約があるため、参照型は使用できません。
    /// </typeparam>
    /// <param name="prefix">読み込み対象の先頭デバイスコードを指定します。</param>
    /// <param name="address">読み込み対象の先頭アドレスを指定します。</param>
    /// <param name="length">読み込み対象の要素数（<c>T</c>型の要素数）を指定します。</param>
    /// <param name="onRead">読み取り完了後に、変換済みの値<c>T[]</c>を受け取るコールバックを指定します。</param>
    /// <returns>メソッドチェーン用に自身を返します。</returns>
    public ReadBuilder Add<T>(Prefix prefix, string address, ushort length, Action<T[]> onRead) where T : unmanaged
    {
        batchEntries.Add((
            m => onRead(m.BatchRead<T>(prefix, address, length)),
            async m => onRead(await m.BatchReadAsync<T>(prefix, address, length))
        ));
        return this;
    }
}
