import socket

import pytest

import mcpx
from mcpx import Prefix


def test_connection_refused() -> None:
    with socket.socket() as s:
        s.bind(("127.0.0.1", 0))
        port = s.getsockname()[1]

    with pytest.raises(mcpx.McpXConnectionRefusedError) as info:
        mcpx.McpX("127.0.0.1", port)
    assert isinstance(info.value, ConnectionRefusedError)


def test_timeout_then_disconnected(start_server) -> None:
    port = start_server("--delay-ms", "2000")
    with mcpx.McpX("127.0.0.1", port, timeout_ms=500) as plc:
        with pytest.raises(mcpx.McpXTimeoutError) as info:
            plc.read(Prefix.D, "0", mcpx.int16)
        assert isinstance(info.value, TimeoutError)

        # タイムアウト後は接続が閉じられ、以降の呼び出しは DisconnectedError
        with pytest.raises(mcpx.DisconnectedError):
            plc.read(Prefix.D, "0", mcpx.int16)


def test_protocol_error_has_end_code(start_server) -> None:
    with mcpx.McpX("127.0.0.1", start_server("--end-code", "0401=C051")) as plc:
        with pytest.raises(mcpx.McProtocolError) as info:
            plc.read(Prefix.D, "0", mcpx.int16)
    assert info.value.end_code == 0xC051
    assert "C051" in str(info.value)


def test_invalid_address(plc: mcpx.McpX) -> None:
    with pytest.raises(mcpx.DeviceAddressError) as info:
        plc.read(Prefix.D, "1A", mcpx.int16)
    assert isinstance(info.value, ValueError)


def test_invalid_arguments(plc: mcpx.McpX) -> None:
    with pytest.raises(mcpx.InvalidArgumentError):
        plc.batch_read(Prefix.D, "0", 0, mcpx.int16)
    with pytest.raises(TypeError):
        plc.read(Prefix.D, "0", int)


def test_closed(plc: mcpx.McpX) -> None:
    plc.close()
    plc.close()  # 2回目は何もしない
    assert plc.closed
    with pytest.raises(mcpx.ClosedError):
        plc.read(Prefix.D, "0", mcpx.int16)
