namespace McpXLib.Enums;

/// <summary>
/// 統合アクセス（<see cref="McpX.Read(System.Action{ReadBuilder})"/> / <see cref="McpX.Write(System.Action{WriteBuilder})"/>）で
/// 複数ブロック一括読出し・書込み（コマンド: 0406 / 1406）を使うかどうか
/// </summary>
public enum MultiBlockAccessMode
{
    /// <summary>
    /// 自動（デフォルト）。複数ブロックで送信し、接続先が非対応（エラーコード <c>C059</c>）の場合は、
    /// 以降そのインスタンスでは範囲ごとの一括読出し・書込み（0401 / 1401）を使います。
    /// </summary>
    Auto,

    /// <summary>
    /// 常に複数ブロックで送信します。接続先が非対応の場合は例外をスローします。
    /// </summary>
    Always,

    /// <summary>
    /// 複数ブロックを使わず、範囲ごとに一括読出し・書込み（0401 / 1401）を送信します。
    /// </summary>
    Never,
}
