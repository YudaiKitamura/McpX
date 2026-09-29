"""値の型（C ABI の MCPX_TYPE_*）。"""

import ctypes
from typing import Any


class DataType:
    """PLC から読み書きする値の型。"""

    __slots__ = ("name", "code", "ctype", "size", "monitorable")

    def __init__(self, name: str, code: int, ctype: Any, monitorable: bool) -> None:
        self.name = name
        self.code = code
        self.ctype = ctype
        self.size = ctypes.sizeof(ctype)
        # ランダムアクセス（単一デバイス）・モニタで使える型か
        self.monitorable = monitorable

    def to_python(self, value: Any) -> Any:
        return bool(value) if self is bool_ else value

    def to_native(self, value: Any) -> Any:
        return 1 if (self is bool_ and value) else (0 if self is bool_ else value)

    def __repr__(self) -> str:
        return f"mcpx.{self.name}"


bool_ = DataType("bool_", 1, ctypes.c_uint8, True)
int8 = DataType("int8", 2, ctypes.c_int8, False)
uint8 = DataType("uint8", 3, ctypes.c_uint8, False)
int16 = DataType("int16", 4, ctypes.c_int16, True)
uint16 = DataType("uint16", 5, ctypes.c_uint16, True)
int32 = DataType("int32", 6, ctypes.c_int32, True)
uint32 = DataType("uint32", 7, ctypes.c_uint32, True)
int64 = DataType("int64", 8, ctypes.c_int64, False)
uint64 = DataType("uint64", 9, ctypes.c_uint64, False)
float32 = DataType("float32", 10, ctypes.c_float, True)
float64 = DataType("float64", 11, ctypes.c_double, False)

def require(data_type: Any) -> DataType:
    """型の引数を検証します（mcpx.int16 などの DataType 以外は TypeError）。"""
    if not isinstance(data_type, DataType):
        raise TypeError(f"type must be one of the mcpx data types (e.g. mcpx.int16), not {data_type!r}.")
    return data_type


ALL_TYPES = (bool_, int8, uint8, int16, uint16, int32, uint32, int64, uint64, float32, float64)
