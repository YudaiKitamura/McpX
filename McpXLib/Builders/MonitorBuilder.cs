using McpXLib.Builders;
using McpXLib.Enums;
using McpXLib.Utils;

namespace McpXLib;

/// <summary>
/// モニタデバイス登録のリクエストビルダー。
/// </summary>
/// <remarks>
/// <see cref="Add{T}(Prefix, string, Action{T})"/> でモニタ対象のデバイスと「読み取り後に呼ばれるコールバック」を並べます。
/// 型に応じてビット／ワード／ダブルワードのアクセス単位が自動的に振り分けられ、
/// <see cref="MonitorSession.Read"/> の実行完了後に各コールバックへ変換済みの値が渡されます。
/// </remarks>
public sealed class MonitorBuilder
{
    // ワードアクセス（1点=1ワード）で読み出すエントリ。ビットデバイスもワードとして読み、bit0 を抽出する。
    internal readonly List<(Prefix prefix, string address, Action<ushort> apply)> wordEntries = new();

    // ダブルワードアクセス（1点=2ワード）で読み出すエントリ。
    internal readonly List<(Prefix prefix, string address, Action<uint> apply)> doubleWordEntries = new();

    /// <summary>
    /// モニタ対象のデバイスと、モニタ実行後に値を受け取るコールバックを追加します。
    /// </summary>
    /// <typeparam name="T">
    /// 読み込む値の型。<c>bool</c>(ビット) / <c>short</c>,<c>ushort</c>(ワード) /
    /// <c>int</c>,<c>uint</c>,<c>float</c>(ダブルワード) を指定できます。
    /// </typeparam>
    /// <param name="prefix">モニタ対象のデバイスコードを指定します。</param>
    /// <param name="address">モニタ対象のアドレスを指定します。</param>
    /// <param name="onRead">モニタ実行後に、変換済みの値<c>T</c>を受け取るコールバックを指定します。</param>
    /// <exception cref="NotSupportedException">ランダムアクセス非対応の型（<c>long</c>,<c>double</c> など）を指定した場合にスローします。</exception>
    /// <returns>メソッドチェーン用に自身を返します。</returns>
    public MonitorBuilder Add<T>(Prefix prefix, string address, Action<T> onRead) where T : unmanaged
    {
        switch (RandomAccessKindResolver.Resolve<T>())
        {
            case RandomAccessKind.Bit:
                // ビットデバイスはワードで読み、先頭ビット(bit0)を抽出する。
                wordEntries.Add((prefix, address, raw => onRead((T)(object)((raw & 0x0001) != 0))));
                break;
            case RandomAccessKind.Word:
                wordEntries.Add((prefix, address, raw => onRead(DeviceConverter.ConvertValueArray<T>(BitConverter.GetBytes(raw))[0])));
                break;
            default:
                doubleWordEntries.Add((prefix, address, raw => onRead(DeviceConverter.ConvertValueArray<T>(BitConverter.GetBytes(raw))[0])));
                break;
        }

        return this;
    }
}
