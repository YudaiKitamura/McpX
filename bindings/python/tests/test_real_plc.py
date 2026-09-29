import os

import pytest

import mcpx
from mcpx import Prefix

pytestmark = pytest.mark.real_plc

HOST = os.environ.get("MCPX_PLC_IP", "192.168.12.88")
PORT = int(os.environ.get("MCPX_PLC_PORT", "10000"))


def test_real_plc_round_trip() -> None:
    # 実機の D300〜D399 を上書きする
    with mcpx.McpX(HOST, PORT) as plc:
        plc.batch_write(Prefix.D, "300", [1, -2, 3], mcpx.int16)
        assert plc.batch_read(Prefix.D, "300", 3, mcpx.int16) == [1, -2, 3]

        plc.write_string(Prefix.D, "310", "三菱PLCテスト")
        assert plc.read_string(Prefix.D, "310", 10) == "三菱PLCテスト"

        values = plc.read(lambda b: b.add("D300", mcpx.int16, count=3).add("D301", mcpx.int16))
        assert values == [[1, -2, 3], -2]
