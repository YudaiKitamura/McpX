"""McpX: 三菱電機 PLC と MC プロトコルで通信するライブラリ（Python バインディング）。

>>> from mcpx import McpX, Prefix, int16
>>> with McpX("192.168.12.88", 10000) as plc:
...     values = plc.batch_read(Prefix.D, "100", 10, int16)
"""

from ._native import version
from .builders import MonitorBuilder, ReadBuilder, WriteBuilder
from .client import McpX, McpXSimulator, MonitorSession
from .enums import Prefix, ProcessorSeries, RemoteRunClearMode, RequestFrame, parse_device
from .errors import (
    ClosedError,
    DeviceAddressError,
    DisconnectedError,
    InternalError,
    InvalidArgumentError,
    InvalidOperationError,
    McProtocolError,
    McpXConnectionRefusedError,
    McpXError,
    McpXTimeoutError,
    NetworkError,
    ReceivePacketError,
    UnsupportedError,
)
from .types import DataType, bool_, float32, float64, int8, int16, int32, int64, uint8, uint16, uint32, uint64

__version__ = "0.12.0"

__all__ = [
    "McpX", "McpXSimulator", "MonitorSession",
    "ReadBuilder", "WriteBuilder", "MonitorBuilder",
    "Prefix", "RequestFrame", "ProcessorSeries", "RemoteRunClearMode", "parse_device",
    "DataType", "bool_", "int8", "uint8", "int16", "uint16", "int32", "uint32", "int64", "uint64", "float32", "float64",
    "McpXError", "InvalidArgumentError", "ClosedError", "McpXTimeoutError", "DisconnectedError",
    "McpXConnectionRefusedError", "NetworkError", "McProtocolError", "ReceivePacketError", "DeviceAddressError",
    "UnsupportedError", "InvalidOperationError", "InternalError",
    "version",
]
