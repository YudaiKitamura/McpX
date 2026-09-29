/*
 * McpX native library (C ABI)
 *
 * 三菱電機 PLC と MC プロトコルで通信する McpX（.NET）を、C の関数として公開したライブラリです。
 * Python（ctypes）、Node.js（koffi）など、FFI を持つ言語から利用できます。
 *
 * 規約
 * - 関数はすべて cdecl、64bit のみ対応です。
 * - 文字列はすべて UTF-8 の NUL 終端文字列です。
 * - bool 値は uint8_t（入力は 0 以外を true、出力は 0 / 1）です。
 * - 状態を返す関数は mcpx_status_t（MCPX_OK = 0）を返し、最後の引数 err（NULL 可）に詳細を書き込みます。
 * - 構造体は先頭の struct_size に sizeof を設定してから渡してください（前方互換のため）。
 * - 同じクライアントへの同時呼び出しは内部で1つずつ処理されます。別のクライアント同士は並列に動作します。
 * - 呼び出し側が渡したバッファ・文字列は、その関数が戻るまで有効である必要があります。
 */
#ifndef MCPX_H
#define MCPX_H

#include <stddef.h>
#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

/* ABI のバージョン（上位16ビット: major、下位16ビット: minor）。major が異なるライブラリとは互換性がありません。 */
#define MCPX_ABI_VERSION ((1u << 16) | 0u)

/* クライアント（PLC への接続）のハンドル。0 は無効値です。 */
typedef uint64_t mcpx_client_t;

typedef int32_t mcpx_status_t;

/* 状態コード（値は固定で、今後も変更しません） */
enum {
    MCPX_OK = 0,
    MCPX_E_INVALID_ARGUMENT = 1,         /* 引数が不正（NULL、範囲外、未知の値など） */
    MCPX_E_INVALID_HANDLE = 2,           /* ハンドルが無効（close 済みを含む） */
    MCPX_E_TIMEOUT = 3,                  /* 通信がタイムアウトした */
    MCPX_E_DISCONNECTED = 4,             /* 接続が切れている（タイムアウト・通信エラー後の要求を含む） */
    MCPX_E_CONNECTION_REFUSED = 5,       /* 接続が拒否された */
    MCPX_E_NETWORK = 6,                  /* その他のネットワークエラー（socket_error を参照） */
    MCPX_E_PROTOCOL = 7,                 /* PLC がエラーを返した（end_code を参照） */
    MCPX_E_RECEIVE_PACKET = 8,           /* 受信したパケットが不正 */
    MCPX_E_INVALID_DEVICE_ADDRESS = 9,   /* デバイスのアドレスが不正 */
    MCPX_E_UNSUPPORTED = 10,             /* 対応していない型・操作 */
    MCPX_E_INVALID_OPERATION = 11,       /* 現在の状態では実行できない操作 */
    MCPX_E_BUFFER_TOO_SMALL = 12,        /* バッファが小さい */
    MCPX_E_CLOSED = 13,                  /* 呼び出し中にクライアントが close された */
    MCPX_E_INTERNAL = 99                 /* 内部エラー */
};

/* 値の型 */
enum {
    MCPX_TYPE_BOOL = 1,   /* uint8_t（0 / 1） */
    MCPX_TYPE_I8 = 2,     /* int8_t（1ワードの下位バイト） */
    MCPX_TYPE_U8 = 3,     /* uint8_t（1ワードの下位バイト） */
    MCPX_TYPE_I16 = 4,
    MCPX_TYPE_U16 = 5,
    MCPX_TYPE_I32 = 6,
    MCPX_TYPE_U32 = 7,
    MCPX_TYPE_I64 = 8,
    MCPX_TYPE_U64 = 9,
    MCPX_TYPE_F32 = 10,
    MCPX_TYPE_F64 = 11
};

/* フレーム */
enum { MCPX_FRAME_3E = 0, MCPX_FRAME_4E = 1 };

/* PLC のシリーズ（デバイス指定の形式） */
enum { MCPX_SERIES_Q = 0, MCPX_SERIES_IQR = 1 };

/* デバイスコード（McpX の Prefix と同じ値） */
enum {
    MCPX_PREFIX_X = 0x9C,  MCPX_PREFIX_Y = 0x9D,  MCPX_PREFIX_M = 0x90,  MCPX_PREFIX_L = 0x92,
    MCPX_PREFIX_F = 0x93,  MCPX_PREFIX_V = 0x94,  MCPX_PREFIX_B = 0xA0,  MCPX_PREFIX_D = 0xA8,
    MCPX_PREFIX_W = 0xB4,  MCPX_PREFIX_TS = 0xC1, MCPX_PREFIX_TC = 0xC0, MCPX_PREFIX_TN = 0xC2,
    MCPX_PREFIX_SS = 0xC7, MCPX_PREFIX_SC = 0xC6, MCPX_PREFIX_SN = 0xC8, MCPX_PREFIX_CS = 0xC4,
    MCPX_PREFIX_CC = 0xC3, MCPX_PREFIX_CN = 0xC5, MCPX_PREFIX_SB = 0xA1, MCPX_PREFIX_SW = 0xB5,
    MCPX_PREFIX_S = 0x98,  MCPX_PREFIX_DX = 0xA2, MCPX_PREFIX_DY = 0xA3, MCPX_PREFIX_SM = 0x91,
    MCPX_PREFIX_SD = 0xA9, MCPX_PREFIX_Z = 0xCC,  MCPX_PREFIX_R = 0xAF,  MCPX_PREFIX_ZR = 0xB0
};

/* mcpx_struct_size() に渡す構造体の種類 */
enum {
    MCPX_STRUCT_ERROR = 1,
    MCPX_STRUCT_CONNECT_OPTIONS = 2,
    MCPX_STRUCT_SIMULATOR_OPTIONS = 3
};

/* エラーの詳細 */
typedef struct mcpx_error {
    uint32_t struct_size;   /* sizeof(mcpx_error) を設定する */
    int32_t  status;        /* 戻り値と同じ状態コード */
    uint16_t end_code;      /* PLC の終了コード（MCPX_E_PROTOCOL のとき） */
    uint16_t reserved;
    int32_t  socket_error;  /* ソケットのエラーコード（該当しない場合は 0） */
    char     message[512];  /* エラーメッセージ（UTF-8、NUL 終端） */
} mcpx_error;

/* 接続のオプション（mcpx_connect_options_init で既定値を設定してから変更する） */
typedef struct mcpx_connect_options {
    uint32_t    struct_size;      /* sizeof(mcpx_connect_options) */
    int32_t     port;             /* 1〜65535 */
    const char* host;             /* IP アドレスまたはホスト名（必須） */
    const char* password;         /* リモートパスワード（NULL = なし） */
    uint32_t    timeout_ms;       /* 0〜65535（0 = 無期限）。既定 5000 */
    uint8_t     is_ascii;         /* ASCII コードで交信する。既定 0（バイナリ） */
    uint8_t     is_udp;           /* UDP で交信する。既定 0（TCP） */
    uint8_t     frame;            /* MCPX_FRAME_*。既定 MCPX_FRAME_3E */
    uint8_t     series;           /* MCPX_SERIES_*。既定 MCPX_SERIES_Q */
    uint8_t     use_multi_block;  /* 統合アクセスで複数ブロック一括読み書きを使う。既定 0 */
    uint8_t     reserved[3];
} mcpx_connect_options;

/* GX Simulator3 への接続のオプション（mcpx_simulator_options_init で既定値を設定してから変更する） */
typedef struct mcpx_simulator_options {
    uint32_t    struct_size;      /* sizeof(mcpx_simulator_options) */
    int32_t     system_no;        /* システムNo.。既定 1 */
    int32_t     cpu_no;           /* 号機No.。既定 1 */
    const char* host;             /* NULL = "127.0.0.1" */
    uint32_t    timeout_ms;       /* 0〜65535（0 = 無期限）。既定 5000 */
    uint8_t     frame;            /* 既定 MCPX_FRAME_3E */
    uint8_t     series;           /* 既定 MCPX_SERIES_IQR */
    uint8_t     use_multi_block;  /* 既定 0 */
    uint8_t     reserved;
} mcpx_simulator_options;

/* ---- ライブラリ情報 ---- */

/* ABI のバージョン（MCPX_ABI_VERSION と比較して互換性を確認する） */
uint32_t mcpx_abi_version(void);

/* McpX のバージョン文字列（静的な文字列。解放不要） */
const char* mcpx_version(void);

/* 構造体のサイズ（MCPX_STRUCT_*）。ライブラリ側の定義とラッパー側の定義が一致するかの確認に使う。未知の値は 0 */
uint32_t mcpx_struct_size(uint32_t which);

/* オプションに既定値を設定する（struct_size も設定される） */
void mcpx_connect_options_init(mcpx_connect_options* options);
void mcpx_simulator_options_init(mcpx_simulator_options* options);

/* ---- 接続 ---- */

/* PLC に接続し、クライアントのハンドルを out に返す */
mcpx_status_t mcpx_connect(const mcpx_connect_options* options, mcpx_client_t* out, mcpx_error* err);

/* GX Simulator3 に接続し、クライアントのハンドルを out に返す */
mcpx_status_t mcpx_connect_simulator(const mcpx_simulator_options* options, mcpx_client_t* out, mcpx_error* err);

/* 接続を閉じ、ハンドルを無効にする（0 を渡した場合は何もせず MCPX_OK）。
 * 通信中の呼び出しは速やかに MCPX_E_CLOSED で戻る。 */
mcpx_status_t mcpx_close(mcpx_client_t client, mcpx_error* err);

/* 統合アクセスで複数ブロック一括読み書きを使うかを切り替える */
mcpx_status_t mcpx_set_multi_block(mcpx_client_t client, uint8_t enable, mcpx_error* err);

/* ---- 単一・連続デバイスの読み書き ---- */

/* 単一デバイスを読み込み、out（out_size バイト以上）に書き込む */
mcpx_status_t mcpx_read(mcpx_client_t client, uint8_t prefix, const char* address, uint8_t type,
                        void* out, size_t out_size, mcpx_error* err);

/* 単一デバイスに value（size バイト以上）の値を書き込む */
mcpx_status_t mcpx_write(mcpx_client_t client, uint8_t prefix, const char* address, uint8_t type,
                         const void* value, size_t size, mcpx_error* err);

/* 先頭デバイスから count 要素（1〜65535）を読み込み、out（count × 型のサイズ以上）に書き込む */
mcpx_status_t mcpx_batch_read(mcpx_client_t client, uint8_t prefix, const char* address, uint8_t type,
                              uint32_t count, void* out, size_t out_size, mcpx_error* err);

/* 先頭デバイスから count 要素（1〜65535）を書き込む */
mcpx_status_t mcpx_batch_write(mcpx_client_t client, uint8_t prefix, const char* address, uint8_t type,
                               uint32_t count, const void* values, size_t size, mcpx_error* err);

#ifdef __cplusplus
}
#endif

#endif /* MCPX_H */
