using McpXLib.Builders;
using McpXLib.Enums;
using McpXLib.Utils;

namespace McpXLib;

/// <summary>
/// ランダムデバイス書き込みのリクエストビルダー。
/// </summary>
/// <remarks>
/// <see cref="Add{T}(Prefix, string, T)"/> でデバイスと値を並べるだけで、
/// 型に応じてビット／ワード／ダブルワードのアクセス単位が自動的に振り分けられます。
/// </remarks>
public sealed class RandomWriteBuilder
{
    internal readonly List<(Prefix prefix, string address, ushort value)> wordDevices = new();
    internal readonly List<(Prefix prefix, string address, uint value)> doubleWordDevices = new();
    internal readonly List<(Prefix prefix, string address, bool value)> bitDevices = new();

    /// <summary>
    /// 書き込むデバイスと値を追加します。
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
    public RandomWriteBuilder Add<T>(Prefix prefix, string address, T value) where T : unmanaged
    {
        switch (RandomAccessKindResolver.Resolve<T>())
        {
            case RandomAccessKind.Bit:
                bitDevices.Add((prefix, address, (bool)(object)value));
                break;
            case RandomAccessKind.Word:
                wordDevices.Add((prefix, address, BitConverter.ToUInt16(DeviceConverter.StructToBytes(value), 0)));
                break;
            default:
                doubleWordDevices.Add((prefix, address, BitConverter.ToUInt32(DeviceConverter.StructToBytes(value), 0)));
                break;
        }

        return this;
    }
}
