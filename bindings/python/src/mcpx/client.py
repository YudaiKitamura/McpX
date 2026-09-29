"""PLC との接続（C# の McpX / McpXSimulator / MonitorSession に相当）。"""

import asyncio
import ctypes
import weakref
from typing import Any, Callable, List, Optional, Sequence, Tuple

from . import _native
from ._native import ITEM_RANGE, ITEM_SINGLE, Item
from .builders import Device, MonitorBuilder, ReadBuilder, WriteBuilder, _Entry, build, resolve_device
from .enums import Prefix, ProcessorSeries, RemoteRunClearMode, RequestFrame
from .errors import ClosedError, InvalidArgumentError
from .types import DataType, require


def _close_handle(handle: int) -> None:
    # weakref.finalize から呼ばれる（close し忘れたインスタンスの後始末）
    try:
        _native.lib().mcpx_close(handle, None)
    except Exception:
        pass


class _Items:
    """ビルダーの項目から mcpx_item の配列とバッファを作ります（呼び出しが終わるまで参照を保持する）。"""

    def __init__(self, entries: List[_Entry], write: bool, single_only: bool = False, range_only: bool = False) -> None:
        if not entries:
            raise InvalidArgumentError("No devices were added to the builder.", 1)

        self.entries = entries
        self.buffers: List[Any] = []
        self.array = (Item * len(entries))()

        for i, entry in enumerate(entries):
            is_range = entry.count is not None
            if single_only and is_range:
                raise InvalidArgumentError(f"items[{i}]: count cannot be specified for random access.", 1)
            if range_only and not is_range:
                raise InvalidArgumentError(f"items[{i}]: count is required for multiple block access.", 1)

            length = entry.count if is_range else 1
            buffer = (entry.type.ctype * max(length, 1))()
            if write:
                values = entry.value if is_range else [entry.value]
                for j, value in enumerate(values):
                    buffer[j] = entry.type.to_native(value)
            self.buffers.append(buffer)

            self.array[i] = Item(
                address=entry.address.encode(),
                buffer=ctypes.cast(buffer, ctypes.c_void_p),
                buffer_size=ctypes.sizeof(buffer),
                count=length,
                type=entry.type.code,
                prefix=int(entry.prefix),
                kind=ITEM_RANGE if is_range else ITEM_SINGLE,
            )

    def results(self) -> List[Any]:
        """読み込んだ値を返し、各項目のコールバックを呼びます。"""
        values = []
        for entry, buffer in zip(self.entries, self.buffers):
            value = [entry.type.to_python(v) for v in buffer] if entry.count is not None else entry.type.to_python(buffer[0])
            if entry.on_read is not None:
                entry.on_read(value)
            values.append(value)
        return values


class McpX:
    """PLC との接続。

    ``with McpX("192.168.12.88", 10000) as plc:`` のように使い、終了時に接続を閉じます。
    """

    def __init__(
        self,
        host: str,
        port: int,
        password: Optional[str] = None,
        is_ascii: bool = False,
        is_udp: bool = False,
        request_frame: RequestFrame = RequestFrame.E3,
        timeout_ms: int = 5000,
        processor_series: ProcessorSeries = ProcessorSeries.Q,
        use_multi_block_access: bool = False,
    ) -> None:
        lib = _native.lib()
        options = _native.ConnectOptions()
        lib.mcpx_connect_options_init(ctypes.byref(options))
        options.host = host.encode()
        options.port = port
        options.password = password.encode() if password is not None else None
        options.timeout_ms = timeout_ms
        options.is_ascii = int(is_ascii)
        options.is_udp = int(is_udp)
        options.frame = int(request_frame)
        options.series = int(processor_series)
        options.use_multi_block = int(use_multi_block_access)
        self._open(lambda handle, err: lib.mcpx_connect(ctypes.byref(options), handle, err), use_multi_block_access)

    def _open(self, connect: Callable[[Any, Any], int], use_multi_block_access: bool) -> None:
        handle = _native.c_client()
        err = _native.new_error()
        _native.check(connect(ctypes.byref(handle), ctypes.byref(err)), err)
        self._handle: int = handle.value
        self._use_multi_block_access = use_multi_block_access
        self._finalizer = weakref.finalize(self, _close_handle, self._handle)

    # ---- 接続 ----

    @classmethod
    async def connect_async(cls, *args: Any, **kwargs: Any) -> "McpX":
        """接続を別スレッドで行います（``await McpX.connect_async(...)``）。"""
        return await asyncio.to_thread(cls, *args, **kwargs)

    def close(self) -> None:
        """接続を閉じます（2回目以降は何もしません）。"""
        if self._finalizer.detach() is not None:
            err = _native.new_error()
            _native.check(_native.lib().mcpx_close(self._handle, ctypes.byref(err)), err)

    async def close_async(self) -> None:
        await asyncio.to_thread(self.close)

    @property
    def closed(self) -> bool:
        return not self._finalizer.alive

    def __enter__(self) -> "McpX":
        return self

    def __exit__(self, *exc: Any) -> None:
        self.close()

    async def __aenter__(self) -> "McpX":
        return self

    async def __aexit__(self, *exc: Any) -> None:
        await self.close_async()

    @property
    def use_multi_block_access(self) -> bool:
        """統合アクセス（read / write のビルダー）で、複数ブロック一括読み書きを使うか。"""
        return self._use_multi_block_access

    @use_multi_block_access.setter
    def use_multi_block_access(self, value: bool) -> None:
        self._call("mcpx_set_multi_block", int(value))
        self._use_multi_block_access = bool(value)

    def _call(self, name: str, *args: Any) -> None:
        if self.closed:
            raise ClosedError("The connection has already been closed.", 2)
        err = _native.new_error()
        _native.check(getattr(_native.lib(), name)(self._handle, *args, ctypes.byref(err)), err)

    # ---- 単一・連続デバイス ----

    def read(self, prefix: Any, address: Any = None, type: Optional[DataType] = None) -> Any:
        """単一デバイスを読み込みます（``plc.read(Prefix.D, "100", int16)``、``plc.read("D100", int16)``）。

        ビルダーを渡すと、連続・ランダムアクセスをまとめて読み込み、登録順の値のリストを返します
        （``plc.read(lambda b: b.add("D100", int16, count=10).add("M0", bool_))``）。
        """
        if address is None and type is None:
            return self._read_items(build(ReadBuilder, prefix)._entries, "mcpx_read_items")

        prefix, address, rest = resolve_device(prefix, address, (type,))
        data_type = require(rest[0])
        value = data_type.ctype()
        self._call("mcpx_read", int(prefix), address.encode(), data_type.code, ctypes.byref(value), data_type.size)
        return data_type.to_python(value.value)

    def write(self, prefix: Any, address: Any = None, value: Any = None, type: Optional[DataType] = None) -> None:
        """単一デバイスに書き込みます（``plc.write(Prefix.D, "100", 5, int16)``、``plc.write("D100", 5, int16)``）。

        ビルダーを渡すと、連続・ランダムアクセスをまとめて書き込みます
        （``plc.write(lambda b: b.add("D100", [1, 2, 3], int16).add("M0", True, bool_))``）。
        """
        if address is None and value is None and type is None:
            self._write_items(build(WriteBuilder, prefix)._entries, "mcpx_write_items")
            return

        prefix, address, rest = resolve_device(prefix, address, (value, type))
        value, data_type = rest[0], require(rest[1])
        native = data_type.ctype(data_type.to_native(value))
        self._call("mcpx_write", int(prefix), address.encode(), data_type.code, ctypes.byref(native), data_type.size)

    def batch_read(self, prefix: Device, address: Any, length: Any, type: Optional[DataType] = None) -> List[Any]:
        """先頭デバイスから length 要素を読み込みます（``plc.batch_read(Prefix.D, "100", 10, int16)``、``plc.batch_read("D100", 10, int16)``）。"""
        prefix, address, rest = resolve_device(prefix, address, (length, type))
        count, data_type = rest[0], require(rest[1])
        buffer = (data_type.ctype * max(count, 1))()
        self._call("mcpx_batch_read", int(prefix), address.encode(), data_type.code, count, buffer, ctypes.sizeof(buffer))
        return [data_type.to_python(v) for v in buffer[:count]]

    def batch_write(self, prefix: Device, address: Any, values: Any, type: Optional[DataType] = None) -> None:
        """先頭デバイスから values を書き込みます（``plc.batch_write(Prefix.D, "100", [1, 2, 3], int16)``）。"""
        prefix, address, rest = resolve_device(prefix, address, (values, type))
        items, data_type = list(rest[0]), require(rest[1])
        buffer = (data_type.ctype * max(len(items), 1))(*[data_type.to_native(v) for v in items])
        self._call("mcpx_batch_write", int(prefix), address.encode(), data_type.code, len(items), buffer, ctypes.sizeof(buffer))

    # ---- 文字列 ----

    def read_string(self, prefix: Device, address: Any, length: Any = None) -> str:
        """length ワードを文字列（Shift_JIS）として読み込みます。"""
        prefix, address, rest = resolve_device(prefix, address, (length,))
        words: int = rest[0]
        buffer = ctypes.create_string_buffer(words * 6 + 1)
        size = ctypes.c_size_t()
        self._call("mcpx_read_string", int(prefix), address.encode(), words, buffer, ctypes.sizeof(buffer), ctypes.byref(size))
        return buffer.raw[: size.value].decode("utf-8")

    def write_string(self, prefix: Device, address: Any, value: Any = None) -> None:
        """文字列を Shift_JIS に変換して書き込みます（終端の NUL も書き込まれます）。"""
        prefix, address, rest = resolve_device(prefix, address, (value,))
        self._call("mcpx_write_string", int(prefix), address.encode(), str(rest[0]).encode("utf-8"))

    # ---- ランダム・複数ブロック ----

    def random_read(self, build_or_builder: Any) -> List[Any]:
        """非連続のデバイスを読み込みます（count は指定しない）。"""
        return self._read_items(build(ReadBuilder, build_or_builder)._entries, "mcpx_read_items", single_only=True)

    def random_write(self, build_or_builder: Any) -> None:
        """非連続のデバイスに書き込みます（値は単一の値のみ）。"""
        self._write_items(build(WriteBuilder, build_or_builder)._entries, "mcpx_write_items", single_only=True)

    def block_read(self, build_or_builder: Any) -> List[Any]:
        """複数ブロック一括読み込み（コマンド 0406。count は必須）。"""
        return self._read_items(build(ReadBuilder, build_or_builder)._entries, "mcpx_block_read", range_only=True)

    def block_write(self, build_or_builder: Any) -> None:
        """複数ブロック一括書き込み（コマンド 1406。値はリストのみ）。"""
        self._write_items(build(WriteBuilder, build_or_builder)._entries, "mcpx_block_write", range_only=True)

    def _read_items(self, entries: List[_Entry], function: str, **kwargs: bool) -> List[Any]:
        items = _Items(entries, write=False, **kwargs)
        self._call(function, items.array, len(entries))
        return items.results()

    def _write_items(self, entries: List[_Entry], function: str, **kwargs: bool) -> None:
        items = _Items(entries, write=True, **kwargs)
        self._call(function, items.array, len(entries))

    # ---- モニタ ----

    def monitor_regist(self, build_or_builder: Any) -> "MonitorSession":
        """モニタ登録し、登録したデバイスを繰り返し読み出すセッションを返します。

        別のモニタ登録を行うと、既存のセッションは使えなくなります（InvalidOperationError）。
        """
        entries = build(MonitorBuilder, build_or_builder)._entries
        items = _Items(entries, write=False, single_only=True)
        session = _native.c_session()
        self._call("mcpx_monitor_register", items.array, len(entries), ctypes.byref(session))
        return MonitorSession(self, session.value, items)

    # ---- リモート操作 ----

    def remote_run(self, force: bool = False, clear_mode: RemoteRunClearMode = RemoteRunClearMode.NONE) -> None:
        self._call("mcpx_remote_run", int(force), int(clear_mode))

    def remote_stop(self) -> None:
        self._call("mcpx_remote_stop")

    def remote_pause(self, force: bool = False) -> None:
        self._call("mcpx_remote_pause", int(force))

    def remote_latch_clear(self) -> None:
        self._call("mcpx_remote_latch_clear")

    def remote_reset(self, reconnect_timeout_ms: Optional[int] = None) -> None:
        """リモート RESET。リセットで切れた接続を接続し直します（既定 30000ms、0 は接続し直さない）。"""
        self._call("mcpx_remote_reset", -1 if reconnect_timeout_ms is None else reconnect_timeout_ms)

    # ---- 非同期版（別スレッドで実行する） ----

    async def read_async(self, *args: Any, **kwargs: Any) -> Any:
        return await asyncio.to_thread(self.read, *args, **kwargs)

    async def write_async(self, *args: Any, **kwargs: Any) -> None:
        await asyncio.to_thread(self.write, *args, **kwargs)

    async def batch_read_async(self, *args: Any, **kwargs: Any) -> List[Any]:
        return await asyncio.to_thread(self.batch_read, *args, **kwargs)

    async def batch_write_async(self, *args: Any, **kwargs: Any) -> None:
        await asyncio.to_thread(self.batch_write, *args, **kwargs)

    async def read_string_async(self, *args: Any, **kwargs: Any) -> str:
        return await asyncio.to_thread(self.read_string, *args, **kwargs)

    async def write_string_async(self, *args: Any, **kwargs: Any) -> None:
        await asyncio.to_thread(self.write_string, *args, **kwargs)

    async def random_read_async(self, build_or_builder: Any) -> List[Any]:
        return await asyncio.to_thread(self.random_read, build_or_builder)

    async def random_write_async(self, build_or_builder: Any) -> None:
        await asyncio.to_thread(self.random_write, build_or_builder)

    async def block_read_async(self, build_or_builder: Any) -> List[Any]:
        return await asyncio.to_thread(self.block_read, build_or_builder)

    async def block_write_async(self, build_or_builder: Any) -> None:
        await asyncio.to_thread(self.block_write, build_or_builder)

    async def monitor_regist_async(self, build_or_builder: Any) -> "MonitorSession":
        return await asyncio.to_thread(self.monitor_regist, build_or_builder)

    async def remote_run_async(self, force: bool = False, clear_mode: RemoteRunClearMode = RemoteRunClearMode.NONE) -> None:
        await asyncio.to_thread(self.remote_run, force, clear_mode)

    async def remote_stop_async(self) -> None:
        await asyncio.to_thread(self.remote_stop)

    async def remote_pause_async(self, force: bool = False) -> None:
        await asyncio.to_thread(self.remote_pause, force)

    async def remote_latch_clear_async(self) -> None:
        await asyncio.to_thread(self.remote_latch_clear)

    async def remote_reset_async(self, reconnect_timeout_ms: Optional[int] = None) -> None:
        await asyncio.to_thread(self.remote_reset, reconnect_timeout_ms)


class McpXSimulator(McpX):
    """GX Simulator3 への接続（ポート 5500 + システムNo. × 10 + 号機No.）。"""

    def __init__(
        self,
        system_no: int = 1,
        cpu_no: int = 1,
        host: str = "127.0.0.1",
        request_frame: RequestFrame = RequestFrame.E3,
        timeout_ms: int = 5000,
        processor_series: ProcessorSeries = ProcessorSeries.iQR,
        use_multi_block_access: bool = False,
    ) -> None:
        lib = _native.lib()
        options = _native.SimulatorOptions()
        lib.mcpx_simulator_options_init(ctypes.byref(options))
        options.system_no = system_no
        options.cpu_no = cpu_no
        options.host = host.encode()
        options.timeout_ms = timeout_ms
        options.frame = int(request_frame)
        options.series = int(processor_series)
        options.use_multi_block = int(use_multi_block_access)
        self._open(lambda handle, err: lib.mcpx_connect_simulator(ctypes.byref(options), handle, err), use_multi_block_access)


class MonitorSession:
    """モニタ登録したデバイスを繰り返し読み出すセッション（``monitor_regist`` が返す）。"""

    def __init__(self, client: McpX, handle: int, items: "_Items") -> None:
        self._client = client
        self._handle = handle
        self._items = items

    def read(self) -> List[Any]:
        """最新の値を読み出し、登録順の値のリストを返します（各項目の on_read も呼びます）。"""
        err = _native.new_error()
        _native.check(_native.lib().mcpx_monitor_read(self._handle, self._items.array, len(self._items.entries), ctypes.byref(err)), err)
        return self._items.results()

    async def read_async(self) -> List[Any]:
        return await asyncio.to_thread(self.read)

    def close(self) -> None:
        """セッションを解放します（接続を close した後は何もしません）。"""
        if self._handle and not self._client.closed:
            _native.lib().mcpx_session_free(self._handle, None)
        self._handle = 0

    def __enter__(self) -> "MonitorSession":
        return self

    def __exit__(self, *exc: Any) -> None:
        self.close()
