"""ビルダー（C# の ReadBuilder / WriteBuilder / RandomReadBuilder / BlockReadBuilder / MonitorBuilder に相当）。"""

from typing import Any, Callable, List, Optional, Sequence, Tuple, Union

from .enums import Prefix, parse_device
from .types import DataType, require

Device = Union[Prefix, str]


def resolve_device(prefix: Device, address: Any, rest: Tuple[Any, ...]) -> Tuple[Prefix, str, Tuple[Any, ...]]:
    """(Prefix.D, "100", ...) と ("D100", ...) の両方の書き方を (Prefix, アドレス, 残りの引数) にそろえます。"""
    if isinstance(prefix, str):
        parsed_prefix, parsed_address = parse_device(prefix)
        return parsed_prefix, parsed_address, (address,) + rest
    return Prefix(prefix), str(address), rest


class _Entry:
    __slots__ = ("prefix", "address", "type", "count", "value", "on_read")

    def __init__(self, prefix: Prefix, address: str, type: DataType, count: Optional[int], value: Any = None,
                 on_read: Optional[Callable[[Any], None]] = None) -> None:
        require(type)
        self.prefix = prefix
        self.address = address
        self.type = type
        # None は単一デバイス（ランダムアクセス）、数値は先頭からの要素数（連続アクセス）
        self.count = count
        self.value = value
        self.on_read = on_read


class ReadBuilder:
    """読み込むデバイスを並べるビルダー。

    ``count`` を指定した項目は連続アクセス、省略した項目はランダムアクセスで読み込みます。
    """

    def __init__(self) -> None:
        self._entries: List[_Entry] = []

    def add(self, prefix: Device, address: Any = None, type: Optional[DataType] = None, *,
            count: Optional[int] = None, on_read: Optional[Callable[[Any], None]] = None) -> "ReadBuilder":
        """読み込むデバイスを追加します（``b.add(Prefix.D, "100", int16)`` または ``b.add("D100", int16)``）。"""
        prefix, address, rest = resolve_device(prefix, address, (type,))
        self._entries.append(_Entry(prefix, address, rest[0], count, on_read=on_read))
        return self


class WriteBuilder:
    """書き込むデバイスと値を並べるビルダー。

    値にリスト（シーケンス）を指定した項目は連続アクセス、単一の値はランダムアクセスで書き込みます。
    """

    def __init__(self) -> None:
        self._entries: List[_Entry] = []

    def add(self, prefix: Device, address: Any = None, value: Any = None, type: Optional[DataType] = None) -> "WriteBuilder":
        """書き込むデバイスと値を追加します（``b.add(Prefix.D, "100", 5, int16)`` または ``b.add("D100", 5, int16)``）。"""
        prefix, address, rest = resolve_device(prefix, address, (value, type))
        value, data_type = rest[0], rest[1]
        is_range = isinstance(value, Sequence) and not isinstance(value, (str, bytes))
        values = list(value) if is_range else value
        self._entries.append(_Entry(prefix, address, data_type, len(values) if is_range else None, value=values))
        return self


class MonitorBuilder:
    """モニタするデバイスを並べるビルダー（単一デバイスのみ。型は bool_ / int16 / uint16 / int32 / uint32 / float32）。"""

    def __init__(self) -> None:
        self._entries: List[_Entry] = []

    def add(self, prefix: Device, address: Any = None, type: Optional[DataType] = None, *,
            on_read: Optional[Callable[[Any], None]] = None) -> "MonitorBuilder":
        prefix, address, rest = resolve_device(prefix, address, (type,))
        self._entries.append(_Entry(prefix, address, rest[0], None, on_read=on_read))
        return self


def build(builder_type: type, build: Any) -> Any:
    """ビルダーを受け取るか、ビルダーを組み立てる関数を実行してビルダーを返します。"""
    if isinstance(build, builder_type):
        return build
    if callable(build):
        builder = builder_type()
        build(builder)
        return builder
    raise TypeError(f"Expected a {builder_type.__name__} or a function that takes one.")
