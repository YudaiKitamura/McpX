using System.Text;
using McpXLib.Enums;
using McpXLib.Utils;

namespace McpXLib.Builders;

/// <summary>
/// 要求データを、交信データコード（バイナリ / ASCII）に合わせて書き込む。
/// </summary>
/// <remarks>
/// バイナリは数値を下位バイトから、ASCII は16進数（上位桁から）で書き込む。
/// </remarks>
internal readonly struct PacketWriter(List<byte> packets, bool isAscii, ProcessorSeries series = ProcessorSeries.Q)
{
    // 1バイトの数値（ASCII は16進2桁）
    internal void Byte(byte value) => Number(value, 1);

    // size バイトの数値（ASCII は16進 size×2 桁）
    internal void Number(ulong value, int size)
    {
        if (isAscii)
        {
            packets.AddRange(Encoding.ASCII.GetBytes(value.ToString("X" + size * 2)));
            return;
        }

        for (int i = 0; i < size; i++)
        {
            packets.Add((byte)(value >> (8 * i)));
        }
    }

    // デバイス（先頭デバイス番号とデバイスコード）
    internal void Device(Prefix prefix, string address)
    {
        packets.AddRange(isAscii
            ? Encoding.ASCII.GetBytes(DeviceConverter.ToASCIIAddress(prefix, address, series))
            : DeviceConverter.ToByteAddress(prefix, address, series));
    }

    // ワード単位の値の並び（リトルエンディアン）。ASCII はワードごとに16進4桁。
    internal void Words(byte[] littleEndian)
    {
        if (!isAscii)
        {
            packets.AddRange(littleEndian);
            return;
        }

        for (int i = 0; i < littleEndian.Length; i += 2)
        {
            Number((ulong)(littleEndian[i] | littleEndian[i + 1] << 8), 2);
        }
    }

    // 1つの値（リトルエンディアン）。ASCII は値全体を16進（上位桁から）で書き込む（ダブルワードは8桁）。
    internal void Value(byte[] littleEndian)
    {
        if (!isAscii)
        {
            packets.AddRange(littleEndian);
            return;
        }

        for (int i = littleEndian.Length - 1; i >= 0; i--)
        {
            packets.AddRange(Encoding.ASCII.GetBytes(littleEndian[i].ToString("X2")));
        }
    }

    // ビット単位の値の並び。バイナリは2点を1バイト（上位4ビット・下位4ビット）、ASCII は1点1文字（'0' / '1'）。
    internal void Bits(bool[] values)
    {
        if (isAscii)
        {
            foreach (var value in values)
            {
                packets.Add(value ? (byte)'1' : (byte)'0');
            }
            return;
        }

        for (int i = 0; i < values.Length; i += 2)
        {
            byte packed = values[i] ? (byte)0x10 : (byte)0x00;
            if (i + 1 < values.Length && values[i + 1])
            {
                packed |= 0x01;
            }
            packets.Add(packed);
        }
    }

    // そのまま書き込む（リモートパスワードなど、どちらの交信データコードでも文字列のデータ）
    internal void Raw(byte[] bytes) => packets.AddRange(bytes);
}
