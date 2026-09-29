# McpX for Python

三菱電機 PLC と MC プロトコルで通信する [McpX](https://github.com/YudaiKitamura/McpX) の Python バインディングです。
McpX のネイティブライブラリ（C ABI）を `ctypes` で呼び出すため、.NET ランタイムは不要です。

## 使用例
```python
import mcpx
from mcpx import McpX, Prefix

with McpX("192.168.12.88", 10000) as plc:
    # 単一・連続デバイス
    value = plc.read(Prefix.D, "100", mcpx.int16)
    plc.write("D100", 1234, mcpx.int16)            # "D100" の形式でも指定できる
    values = plc.batch_read(Prefix.D, "100", 10, mcpx.int32)
    plc.batch_write(Prefix.M, "0", [True, False], mcpx.bool_)

    # デバイスを並べるだけ。点数（count）を指定すると連続アクセス、単一デバイスはランダムアクセス
    block, total = plc.read(lambda b: b
        .add("D400", mcpx.int16, count=100)
        .add("D2000", mcpx.int32))

    # 文字列（PLC 側は Shift_JIS）
    plc.write_string("D300", "三菱")
    text = plc.read_string("D300", 10)

    # モニタ：登録1回で繰り返し読み出し
    with plc.monitor_regist(lambda b: b.add("D0", mcpx.int16).add("M0", mcpx.bool_)) as session:
        d0, m0 = session.read()
```

非同期版（`read_async`、`batch_read_async` など）は、別スレッドで実行します。

```python
async with await McpX.connect_async("192.168.12.88", 10000) as plc:
    value = await plc.read_async("D100", mcpx.int16)
```

C# の McpX との対応、値の型、例外の一覧は [README.md](README.md) を参照してください。

## 例外
すべての例外は `McpXError` の派生です。`TimeoutError`・`ConnectionError`・`ValueError` など標準の例外でも捕まえられます。
タイムアウト（`McpXTimeoutError`）や通信エラーの後は接続が閉じられ、以降の呼び出しは `DisconnectedError` になります。インスタンスを作り直してください。

## 開発
```sh
scripts/build-native.sh               # ネイティブライブラリをビルドし、src/mcpx/_native/ に配置する
cd bindings/python
uv venv && uv pip install -e ".[test]"
.venv/bin/python -m pytest            # 偽 PLC サーバー（tools/FakePlcServer、.NET SDK が必要）でテストする
MCPX_REAL_PLC=1 MCPX_PLC_IP=192.168.12.88 MCPX_PLC_PORT=10000 .venv/bin/python -m pytest -m real_plc
```
別の場所のネイティブライブラリを使う場合は、`MCPX_LIBRARY_PATH` を設定してください。
