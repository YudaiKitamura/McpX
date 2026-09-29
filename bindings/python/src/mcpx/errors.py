"""例外（C ABI の状態コードから変換する）。"""


class McpXError(Exception):
    """McpX の例外の基底クラス。"""

    def __init__(self, message: str, status: int = 0, end_code: int = 0, socket_error: int = 0) -> None:
        super().__init__(message)
        self.message = message
        #: C ABI の状態コード（MCPX_E_*）
        self.status = status
        #: PLC の終了コード（McProtocolError のとき）
        self.end_code = end_code
        #: ソケットのエラーコード（該当しない場合は 0）
        self.socket_error = socket_error


class InvalidArgumentError(McpXError, ValueError):
    """引数が不正（範囲外、未知の値など）。"""


class ClosedError(McpXError):
    """接続が close 済み、または呼び出し中に close された。"""


class McpXTimeoutError(McpXError, TimeoutError):
    """通信がタイムアウトした。"""


class DisconnectedError(McpXError, ConnectionError):
    """接続が切れている（タイムアウト・通信エラーの後は、接続し直す必要がある）。"""


class McpXConnectionRefusedError(McpXError, ConnectionRefusedError):
    """接続が拒否された。"""


class NetworkError(McpXError, OSError):
    """その他のネットワークエラー（socket_error を参照）。"""


class McProtocolError(McpXError):
    """PLC がエラーを返した（end_code を参照）。"""


class ReceivePacketError(McpXError):
    """受信したパケットが不正。"""


class DeviceAddressError(McpXError, ValueError):
    """デバイスのアドレスが不正。"""


class UnsupportedError(McpXError, NotImplementedError):
    """対応していない型・操作。"""


class InvalidOperationError(McpXError):
    """現在の状態では実行できない操作（登録が置き換わったモニタセッションなど）。"""


class InternalError(McpXError):
    """内部エラー。"""


_BY_STATUS = {
    1: InvalidArgumentError,
    2: ClosedError,
    3: McpXTimeoutError,
    4: DisconnectedError,
    5: McpXConnectionRefusedError,
    6: NetworkError,
    7: McProtocolError,
    8: ReceivePacketError,
    9: DeviceAddressError,
    10: UnsupportedError,
    11: InvalidOperationError,
    12: InternalError,
    13: ClosedError,
    99: InternalError,
}


def from_status(status: int, message: str, end_code: int, socket_error: int) -> McpXError:
    return _BY_STATUS.get(status, InternalError)(message, status, end_code, socket_error)
