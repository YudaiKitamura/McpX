"""ネイティブライブラリ（mcpx.h の C ABI）の読み込みと ctypes の定義。"""

import ctypes
import os
import platform
import sys
from pathlib import Path
from typing import Any, Optional

from .errors import McpXError, from_status

ABI_MAJOR = 1

c_client = ctypes.c_uint64
c_session = ctypes.c_uint64
c_status = ctypes.c_int32


class Error(ctypes.Structure):
    _fields_ = [
        ("struct_size", ctypes.c_uint32),
        ("status", ctypes.c_int32),
        ("end_code", ctypes.c_uint16),
        ("reserved", ctypes.c_uint16),
        ("socket_error", ctypes.c_int32),
        ("message", ctypes.c_char * 512),
    ]


class ConnectOptions(ctypes.Structure):
    _fields_ = [
        ("struct_size", ctypes.c_uint32),
        ("port", ctypes.c_int32),
        ("host", ctypes.c_char_p),
        ("password", ctypes.c_char_p),
        ("timeout_ms", ctypes.c_uint32),
        ("is_ascii", ctypes.c_uint8),
        ("is_udp", ctypes.c_uint8),
        ("frame", ctypes.c_uint8),
        ("series", ctypes.c_uint8),
        ("use_multi_block", ctypes.c_uint8),
        ("reserved", ctypes.c_uint8 * 3),
    ]


class SimulatorOptions(ctypes.Structure):
    _fields_ = [
        ("struct_size", ctypes.c_uint32),
        ("system_no", ctypes.c_int32),
        ("cpu_no", ctypes.c_int32),
        ("host", ctypes.c_char_p),
        ("timeout_ms", ctypes.c_uint32),
        ("frame", ctypes.c_uint8),
        ("series", ctypes.c_uint8),
        ("use_multi_block", ctypes.c_uint8),
        ("reserved", ctypes.c_uint8),
    ]


class Item(ctypes.Structure):
    _fields_ = [
        ("address", ctypes.c_char_p),
        ("buffer", ctypes.c_void_p),
        ("buffer_size", ctypes.c_size_t),
        ("count", ctypes.c_uint32),
        ("type", ctypes.c_uint8),
        ("prefix", ctypes.c_uint8),
        ("kind", ctypes.c_uint8),
        ("reserved", ctypes.c_uint8),
    ]


ITEM_SINGLE = 0
ITEM_RANGE = 1

_P = ctypes.POINTER
_E = _P(Error)

# 関数名: (戻り値, 引数)
_PROTOTYPES = {
    "mcpx_abi_version": (ctypes.c_uint32, []),
    "mcpx_version": (ctypes.c_char_p, []),
    "mcpx_struct_size": (ctypes.c_uint32, [ctypes.c_uint32]),
    "mcpx_connect_options_init": (None, [_P(ConnectOptions)]),
    "mcpx_simulator_options_init": (None, [_P(SimulatorOptions)]),
    "mcpx_connect": (c_status, [_P(ConnectOptions), _P(c_client), _E]),
    "mcpx_connect_simulator": (c_status, [_P(SimulatorOptions), _P(c_client), _E]),
    "mcpx_close": (c_status, [c_client, _E]),
    "mcpx_set_multi_block": (c_status, [c_client, ctypes.c_uint8, _E]),
    "mcpx_read": (c_status, [c_client, ctypes.c_uint8, ctypes.c_char_p, ctypes.c_uint8, ctypes.c_void_p, ctypes.c_size_t, _E]),
    "mcpx_write": (c_status, [c_client, ctypes.c_uint8, ctypes.c_char_p, ctypes.c_uint8, ctypes.c_void_p, ctypes.c_size_t, _E]),
    "mcpx_batch_read": (c_status, [c_client, ctypes.c_uint8, ctypes.c_char_p, ctypes.c_uint8, ctypes.c_uint32, ctypes.c_void_p, ctypes.c_size_t, _E]),
    "mcpx_batch_write": (c_status, [c_client, ctypes.c_uint8, ctypes.c_char_p, ctypes.c_uint8, ctypes.c_uint32, ctypes.c_void_p, ctypes.c_size_t, _E]),
    "mcpx_read_items": (c_status, [c_client, _P(Item), ctypes.c_size_t, _E]),
    "mcpx_write_items": (c_status, [c_client, _P(Item), ctypes.c_size_t, _E]),
    "mcpx_block_read": (c_status, [c_client, _P(Item), ctypes.c_size_t, _E]),
    "mcpx_block_write": (c_status, [c_client, _P(Item), ctypes.c_size_t, _E]),
    "mcpx_read_string": (c_status, [c_client, ctypes.c_uint8, ctypes.c_char_p, ctypes.c_uint32, ctypes.c_char_p, ctypes.c_size_t, _P(ctypes.c_size_t), _E]),
    "mcpx_write_string": (c_status, [c_client, ctypes.c_uint8, ctypes.c_char_p, ctypes.c_char_p, _E]),
    "mcpx_monitor_register": (c_status, [c_client, _P(Item), ctypes.c_size_t, _P(c_session), _E]),
    "mcpx_monitor_read": (c_status, [c_session, _P(Item), ctypes.c_size_t, _E]),
    "mcpx_session_free": (c_status, [c_session, _E]),
    "mcpx_remote_run": (c_status, [c_client, ctypes.c_uint8, ctypes.c_uint8, _E]),
    "mcpx_remote_stop": (c_status, [c_client, _E]),
    "mcpx_remote_pause": (c_status, [c_client, ctypes.c_uint8, _E]),
    "mcpx_remote_latch_clear": (c_status, [c_client, _E]),
    "mcpx_remote_reset": (c_status, [c_client, ctypes.c_int32, _E]),
}


def _library_name() -> str:
    if sys.platform == "win32":
        return "mcpx.dll"
    if sys.platform == "darwin":
        return "mcpx.dylib"
    return "mcpx.so"


def _find_library() -> Path:
    env = os.environ.get("MCPX_LIBRARY_PATH")
    if env:
        return Path(env)

    bundled = Path(__file__).parent / "_native" / _library_name()
    if bundled.exists():
        return bundled

    raise McpXError(
        f"The McpX native library ({_library_name()}) was not found for {sys.platform}/{platform.machine()}. "
        "Install a wheel for your platform, or set MCPX_LIBRARY_PATH to the library path."
    )


def _load() -> Any:
    lib = ctypes.CDLL(str(_find_library()))
    for name, (restype, argtypes) in _PROTOTYPES.items():
        function = getattr(lib, name)
        function.restype = restype
        function.argtypes = argtypes

    abi = lib.mcpx_abi_version()
    if abi >> 16 != ABI_MAJOR:
        raise McpXError(f"Incompatible McpX native library (ABI {abi >> 16}.{abi & 0xFFFF}, expected {ABI_MAJOR}.x).")

    # ライブラリ側の構造体の定義とここでの定義が一致するか
    for which, struct in ((1, Error), (2, ConnectOptions), (3, SimulatorOptions), (4, Item)):
        if lib.mcpx_struct_size(which) != ctypes.sizeof(struct):
            raise McpXError(f"Structure size mismatch with the McpX native library ({struct.__name__}).")

    return lib


_lib: Optional[Any] = None


def lib() -> Any:
    """ネイティブライブラリ（初回に読み込む）。"""
    global _lib
    if _lib is None:
        _lib = _load()
    return _lib


def new_error() -> Error:
    return Error(struct_size=ctypes.sizeof(Error))


def check(status: int, err: Error) -> None:
    """状態コードが MCPX_OK 以外なら、対応する例外をスローします。"""
    if status != 0:
        raise from_status(status, err.message.decode("utf-8", "replace"), err.end_code, err.socket_error)


def version() -> str:
    """ネイティブライブラリ（McpX）のバージョン。"""
    return lib().mcpx_version().decode()
