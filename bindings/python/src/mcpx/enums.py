"""列挙型（C# の McpXLib.Enums と同じ値）。"""

from enum import IntEnum


class Prefix(IntEnum):
    """デバイスコード。"""

    X = 0x9C
    Y = 0x9D
    M = 0x90
    L = 0x92
    F = 0x93
    V = 0x94
    B = 0xA0
    D = 0xA8
    W = 0xB4
    TS = 0xC1
    TC = 0xC0
    TN = 0xC2
    SS = 0xC7
    SC = 0xC6
    SN = 0xC8
    CS = 0xC4
    CC = 0xC3
    CN = 0xC5
    SB = 0xA1
    SW = 0xB5
    S = 0x98
    DX = 0xA2
    DY = 0xA3
    SM = 0x91
    SD = 0xA9
    Z = 0xCC
    R = 0xAF
    ZR = 0xB0


class RequestFrame(IntEnum):
    """フレーム（データ交信電文）の種類。"""

    E3 = 0
    E4 = 1


class ProcessorSeries(IntEnum):
    """PLC のシリーズ（デバイス指定の形式）。"""

    Q = 0
    iQR = 1


class RemoteRunClearMode(IntEnum):
    """リモート RUN のクリアモード。"""

    NONE = 0
    OUTSIDE_LATCH = 1
    ALL = 2


# 16進でアドレスを指定するデバイス
_HEX_PREFIXES = {Prefix.X, Prefix.Y, Prefix.B, Prefix.W, Prefix.SB, Prefix.SW, Prefix.DX, Prefix.DY}

# "SD100" のような文字列を解釈するため、長い名前から順に照合する
_PREFIX_NAMES = sorted(Prefix.__members__.keys(), key=len, reverse=True)


def parse_device(device: str) -> "tuple[Prefix, str]":
    """"D100" のようなデバイス文字列を (Prefix.D, "100") に分けます。"""
    text = device.strip().upper()
    for name in _PREFIX_NAMES:
        if text.startswith(name) and len(text) > len(name):
            prefix = Prefix[name]
            address = text[len(name):]
            valid = "0123456789ABCDEF" if prefix in _HEX_PREFIXES else "0123456789"
            if all(c in valid for c in address):
                return prefix, address
    raise ValueError(f"Invalid device: {device!r}")
