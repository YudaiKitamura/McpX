# McpX for Python

Python bindings for [McpX](https://github.com/YudaiKitamura/McpX), a library for communicating with Mitsubishi Electric PLCs using the MC protocol.
It calls the McpX native library (C ABI) through `ctypes`, so no .NET runtime is required.

[日本語](README_JA.md)

## Usage
```python
import mcpx
from mcpx import McpX, Prefix

with McpX("192.168.12.88", 10000) as plc:
    # Single device / consecutive devices
    value = plc.read(Prefix.D, "100", mcpx.int16)
    plc.write("D100", 1234, mcpx.int16)            # "D100" style is also accepted
    values = plc.batch_read(Prefix.D, "100", 10, mcpx.int32)
    plc.batch_write(Prefix.M, "0", [True, False], mcpx.bool_)

    # Just list the devices: ranges (count) use batch access, single devices use random access
    block, total = plc.read(lambda b: b
        .add("D400", mcpx.int16, count=100)
        .add("D2000", mcpx.int32))

    # Strings (Shift_JIS on the PLC side)
    plc.write_string("D300", "ABC")
    text = plc.read_string("D300", 10)

    # Monitor: register once, read many
    with plc.monitor_regist(lambda b: b.add("D0", mcpx.int16).add("M0", mcpx.bool_)) as session:
        d0, m0 = session.read()
```

Async versions (`read_async`, `batch_read_async`, ...) run the call in a worker thread:

```python
async with await McpX.connect_async("192.168.12.88", 10000) as plc:
    value = await plc.read_async("D100", mcpx.int16)
```

## Features
| C# (McpX) | Python |
|---|---|
| `Read<T>` / `Write<T>` | `read` / `write` |
| `BatchRead<T>` / `BatchWrite<T>` | `batch_read` / `batch_write` |
| `Read(Action<ReadBuilder>)` / `Write(Action<WriteBuilder>)` | `read(builder)` / `write(builder)` |
| `RandomRead` / `RandomWrite` | `random_read` / `random_write` |
| `BlockRead` / `BlockWrite` | `block_read` / `block_write` |
| `ReadString` / `WriteString` | `read_string` / `write_string` |
| `MonitorRegist` → `MonitorSession.Read` | `monitor_regist` → `MonitorSession.read` |
| `RemoteRun` / `RemoteStop` / `RemotePause` / `RemoteLatchClear` / `RemoteReset` | `remote_run` / `remote_stop` / `remote_pause` / `remote_latch_clear` / `remote_reset` |
| `McpXSimulator` | `McpXSimulator` |

Value types: `bool_`, `int8`, `uint8`, `int16`, `uint16`, `int32`, `uint32`, `int64`, `uint64`, `float32`, `float64`.

## Exceptions
All exceptions derive from `McpXError`. Standard exception types can also be used to catch them.

| Exception | Also an instance of | When |
|---|---|---|
| `McpXTimeoutError` | `TimeoutError` | The request timed out (the connection is closed afterwards) |
| `DisconnectedError` | `ConnectionError` | The connection is closed (after a timeout or an error) |
| `McpXConnectionRefusedError` | `ConnectionRefusedError` | The connection was refused |
| `McProtocolError` | | The PLC returned an error (`end_code`) |
| `DeviceAddressError` | `ValueError` | Invalid device address |
| `InvalidArgumentError` | `ValueError` | Invalid argument |
| `InvalidOperationError` | | The monitor session was replaced by another registration |
| `ClosedError` | | The instance has already been closed |

## Development
```sh
scripts/build-native.sh               # build the native library and copy it into src/mcpx/_native/
cd bindings/python
uv venv && uv pip install -e ".[test]"
.venv/bin/python -m pytest            # tests use the fake PLC server (tools/FakePlcServer, requires .NET SDK)
MCPX_REAL_PLC=1 MCPX_PLC_IP=192.168.12.88 MCPX_PLC_PORT=10000 .venv/bin/python -m pytest -m real_plc
```
Set `MCPX_LIBRARY_PATH` to use a native library at another location.
