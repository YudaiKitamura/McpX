using McpXLib.Abstracts;
using McpXLib.Interfaces;

namespace McpXLib.Parsers;

/// <summary>
/// 応答電文（3E / 4E フレーム）の解析
/// </summary>
/// <remarks>
/// サブヘッダ → シリアル番号（4E のみ）→ 経路 → 応答データ長 → 終了コード → 応答データ の順に解析する。
/// </remarks>
internal sealed class FramePacketParser : IReceiveLengthParser
{
    private readonly SubHeaderPacketParser subHeaderPacketParser;
    private readonly SerialPacketParser? serialPacketParser;
    private readonly RoutePacketParser routePacketParser;
    private readonly ContentLengthPacketParser contentLengthPacketParser;
    private readonly ErrorCodePacketParser errorCodePacketParser;
    private readonly BaseContentPacketParser? contentPacketParser;

    /// <param name="plc">交信の設定（フレーム、ASCII / バイナリ）</param>
    /// <param name="serialNumber">要求のシリアル番号（4E フレームのみ。3E フレームは null）</param>
    /// <param name="createContentParser">応答データの解析を生成する処理（応答データがないコマンドは null）。引数は直前の解析と ASCII か否か。</param>
    internal FramePacketParser(IPlc plc, ushort? serialNumber, Func<BasePacketParser, bool, BaseContentPacketParser>? createContentParser)
    {
        bool isAscii = plc.IsAscii;

        subHeaderPacketParser = new SubHeaderPacketParser(requestFrame: plc.RequestFrame, isAscii: isAscii);

        BasePacketParser beforeRoute = subHeaderPacketParser;
        if (serialNumber is ushort serial)
        {
            serialPacketParser = new SerialPacketParser(serialNumber: serial, prevPacketParser: subHeaderPacketParser, isAscii: isAscii);
            beforeRoute = serialPacketParser;
        }

        routePacketParser = new RoutePacketParser(prevPacketParser: beforeRoute, isAscii: isAscii);
        contentLengthPacketParser = new ContentLengthPacketParser(prevPacketParser: routePacketParser, isAscii: isAscii);
        errorCodePacketParser = new ErrorCodePacketParser(prevPacketParser: contentLengthPacketParser, isAscii: isAscii);
        contentPacketParser = createContentParser?.Invoke(errorCodePacketParser, isAscii);
    }

    /// <summary>
    /// 応答電文を検証し、応答データ（ヘッダ・終了コードを除いた部分）を返します。
    /// </summary>
    internal byte[] ParsePacket(byte[] bytes)
    {
        subHeaderPacketParser.ParsePacket(bytes);
        serialPacketParser?.ParsePacket(bytes);
        errorCodePacketParser.ParsePacket(bytes);

        if (contentPacketParser == null)
        {
            return [];
        }

        contentPacketParser.Length = ParseContentLength(bytes) - (ushort)errorCodePacketParser.GetLength();
        return contentPacketParser.ParsePacket(bytes);
    }

    public ushort GetHeaderLength()
    {
        return (ushort)(subHeaderPacketParser.GetLength() + (serialPacketParser?.GetLength() ?? 0) + routePacketParser.GetLength() + contentLengthPacketParser.GetLength());
    }

    public ushort ParseContentLength(byte[] bytes)
    {
        return BitConverter.ToUInt16(contentLengthPacketParser.ParsePacket(bytes), 0);
    }
}
