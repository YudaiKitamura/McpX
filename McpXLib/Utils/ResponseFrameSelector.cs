using McpXLib.Abstracts;
using McpXLib.Enums;
using McpXLib.Exceptions;
using McpXLib.Interfaces;
using McpXLib.Parsers;

namespace McpXLib.Utils;

/// <summary>
/// 交信の設定（フレーム、ASCII / バイナリ）とコマンドの応答データの形式から、応答電文の解析を選ぶ。
/// </summary>
internal class ResponseFrameSelector : IReceiveLengthParser
{
    private readonly FramePacketParser packetParser;

    // 応答データがないコマンド
    internal ResponseFrameSelector(IPlc plc, ushort serialNumber)
        : this(plc, serialNumber, createContentParser: null)
    {
    }

    // ワード単位・ビット単位の応答データ
    internal ResponseFrameSelector(IPlc plc, ushort serialNumber, DeviceAccessMode deviceAccessMode)
        : this(plc, serialNumber, (prev, isAscii) => deviceAccessMode == DeviceAccessMode.Bit
            ? new BitContentPacketParser(prevPacketParser: prev, isAscii: isAscii)
            : new WordContentPacketParser(prevPacketParser: prev, isAscii: isAscii))
    {
    }

    // ワードとダブルワードを並べた応答データ（ランダム読出し・モニタ）
    internal ResponseFrameSelector(IPlc plc, ushort serialNumber, int wordLength, int doubleWordLength)
        : this(plc, serialNumber, (prev, isAscii) => new RandomContentPacketParser(
            prevPacketParser: prev,
            wordLength: wordLength,
            doubleWordLength: doubleWordLength,
            isAscii: isAscii))
    {
    }

    private ResponseFrameSelector(IPlc plc, ushort serialNumber, Func<IPacketParser, bool, BaseContentPacketParser>? createContentParser)
    {
        packetParser = new FramePacketParser(
            plc,
            plc.RequestFrame == RequestFrame.E4 ? serialNumber : null,
            createContentParser);
    }

    public ushort ParseContentLength(byte[] bytes)
    {
        return WrapInvalidPacket(() => packetParser.ParseContentLength(bytes));
    }

    public ushort GetHeaderLength()
    {
        return packetParser.GetHeaderLength();
    }

    internal byte[] ParsePacket(byte[] bytes)
    {
        return WrapInvalidPacket(() => packetParser.ParsePacket(bytes));
    }

    /// <summary>
    /// 壊れた応答（短い応答、ASCII の16進以外の文字など）の解析で発生した例外を、<see cref="ReceivePacketException"/> に変換します。
    /// </summary>
    internal static T WrapInvalidPacket<T>(Func<T> parse)
    {
        try
        {
            return parse();
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException or IndexOutOfRangeException or OverflowException)
        {
            throw new ReceivePacketException("Received packet had an invalid content.", ex);
        }
    }
}
