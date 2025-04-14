namespace McpXLib.Enums;

/// <summary>
/// デバイスの接頭辞
/// </summary>
public enum Prefix : byte
{
    /// <summary>
    /// 入力
    /// </summary>
    X = 0x9C,

    /// <summary>
    /// 出力
    /// </summary>
    Y = 0x9D,

    /// <summary>
    /// 内部リレー
    /// </summary>
    M = 0x90,

    /// <summary>
    /// ラッチリレー
    /// </summary>
    L = 0x92,

    /// <summary>
    /// アナンシェータ
    /// </summary>
    F = 0x93,

    /// <summary>
    /// エッジリレー
    /// </summary>
    V = 0x94,

    /// <summary>
    /// リンクリレー
    /// </summary>
    B = 0xA0,

    /// <summary>
    /// データレジスタ
    /// </summary>
    D = 0xA8,

    /// <summary>
    /// リンクレジスタ
    /// </summary>
    W = 0xB4,

    /// <summary>
    /// タイマ接点
    /// </summary>
    TS = 0xC1,

    /// <summary>
    /// タイマコイル
    /// </summary>
    TC = 0xC0,

    /// <summary>
    /// タイマ現在値
    /// </summary>
    TN = 0xC2,

    /// <summary>
    /// 積算タイマ接点
    /// </summary>
    SS = 0xC7,

    /// <summary>
    /// 積算タイマコイル
    /// </summary>
    SC = 0xC6,

    /// <summary>
    /// 積算タイマ現在値
    /// </summary>
    SN = 0xC8,

    /// <summary>
    /// カウンタ接点
    /// </summary>
    CS = 0xC4,

    /// <summary>
    /// カウンタコイル
    /// </summary>
    CC = 0xC3,

    /// <summary>
    /// カウンタ現在値
    /// </summary>
    CN = 0xC5,

    /// <summary>
    /// リンク特殊リレー
    /// </summary>
    SB = 0xA1,

    /// <summary>
    /// リンク特殊レジスタ
    /// </summary>
    SW = 0xB5,

    /// <summary>
    /// ステップリレー
    /// </summary>
    S = 0x98,

    /// <summary>
    /// ダイレクトアクセス入力
    /// </summary>
    DX = 0xA2,

    /// <summary>
    /// ダイレクトアクセス出力
    /// </summary>
    DY = 0xA3,

    /// <summary>
    /// 特殊リレー
    /// </summary>
    SM = 0x91,

    /// <summary>
    /// 特殊レジスタ
    /// </summary>
    SD = 0xA9,

    /// <summary>
    /// インデックスレジスタ
    /// </summary>
    Z = 0xCC,

    /// <summary>
    /// ファイルレジスタ（ブロック切換え方式）
    /// </summary>
    R = 0xAF,

    /// <summary>
    /// ファイルレジスタ（連番アクセス方式）
    /// </summary>
    ZR = 0xB0,
}

