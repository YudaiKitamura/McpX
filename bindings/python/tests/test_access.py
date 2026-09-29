import pytest

import mcpx
from mcpx import Prefix


def test_version() -> None:
    assert mcpx.version() == mcpx.__version__


@pytest.mark.parametrize(
    "data_type, value",
    [
        (mcpx.int8, -5), (mcpx.uint8, 250), (mcpx.int16, -1234), (mcpx.uint16, 60000),
        (mcpx.int32, -123456789), (mcpx.uint32, 4000000000), (mcpx.int64, -(2**40)), (mcpx.uint64, 2**63 + 5),
        (mcpx.float32, 1.5), (mcpx.float64, -2.25),
    ],
)
def test_single_round_trip(plc: mcpx.McpX, data_type: mcpx.DataType, value: object) -> None:
    plc.write(Prefix.D, "100", value, data_type)
    assert plc.read(Prefix.D, "100", data_type) == value


def test_bool_round_trip(plc: mcpx.McpX) -> None:
    plc.write(Prefix.M, "10", True, mcpx.bool_)
    assert plc.read(Prefix.M, "10", mcpx.bool_) is True
    plc.write(Prefix.M, "10", False, mcpx.bool_)
    assert plc.read(Prefix.M, "10", mcpx.bool_) is False


def test_device_string(plc: mcpx.McpX) -> None:
    plc.write("D200", 7, mcpx.int16)
    assert plc.read("D200", mcpx.int16) == 7
    plc.batch_write("D300", [1, 2, 3], mcpx.int16)
    assert plc.batch_read("D300", 3, mcpx.int16) == [1, 2, 3]


def test_batch_round_trip(plc: mcpx.McpX) -> None:
    plc.batch_write(Prefix.D, "100", [10, -20, 30], mcpx.int32)
    assert plc.batch_read(Prefix.D, "100", 3, mcpx.int32) == [10, -20, 30]

    bits = [True, False, True, True, False]
    plc.batch_write(Prefix.M, "0", bits, mcpx.bool_)
    assert plc.batch_read(Prefix.M, "0", 5, mcpx.bool_) == bits


def test_unwritten_word_is_device_number(plc: mcpx.McpX) -> None:
    # 偽 PLC は、未書き込みのワードにデバイス番号を返す
    assert plc.batch_read(Prefix.D, "500", 3, mcpx.int16) == [500, 501, 502]


def test_string_round_trip(plc: mcpx.McpX) -> None:
    plc.write_string(Prefix.D, "400", "三菱PLCテスト")
    assert plc.read_string(Prefix.D, "400", 10) == "三菱PLCテスト"
    plc.write_string("D400", "ABC")
    assert plc.read_string("D400", 10) == "ABC"


@pytest.mark.parametrize(
    "text, expected",
    [("D100", (Prefix.D, "100")), ("sd5", (Prefix.SD, "5")), ("X1F", (Prefix.X, "1F")), ("ZR10", (Prefix.ZR, "10"))],
)
def test_parse_device(text: str, expected: tuple) -> None:
    assert mcpx.parse_device(text) == expected


@pytest.mark.parametrize("text", ["D", "D1F", "Q100", "100"])
def test_parse_invalid_device(text: str) -> None:
    with pytest.raises(ValueError):
        mcpx.parse_device(text)
