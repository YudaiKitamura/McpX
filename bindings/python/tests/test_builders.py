import pytest

import mcpx
from mcpx import Prefix


def test_read_builder_mixes_range_and_single(plc: mcpx.McpX) -> None:
    plc.batch_write(Prefix.D, "100", [1, 2, 3], mcpx.int16)
    plc.write(Prefix.D, "200", -7, mcpx.int32)
    plc.write(Prefix.M, "5", True, mcpx.bool_)

    received = []
    values = plc.read(lambda b: b
        .add(Prefix.D, "100", mcpx.int16, count=3, on_read=received.append)
        .add("D200", mcpx.int32)
        .add(Prefix.M, "5", mcpx.bool_))

    assert values == [[1, 2, 3], -7, True]
    assert received == [[1, 2, 3]]


def test_write_builder_mixes_range_and_single(plc: mcpx.McpX) -> None:
    plc.write(lambda b: b
        .add(Prefix.D, "10", [4, 5], mcpx.int16)
        .add("D20", 2.5, mcpx.float32)
        .add(Prefix.M, "1", True, mcpx.bool_))

    assert plc.batch_read(Prefix.D, "10", 2, mcpx.int16) == [4, 5]
    assert plc.read(Prefix.D, "20", mcpx.float32) == 2.5
    assert plc.read(Prefix.M, "1", mcpx.bool_) is True


def test_builder_object(plc: mcpx.McpX) -> None:
    builder = mcpx.WriteBuilder().add("D30", 9, mcpx.int16)
    plc.write(builder)
    assert plc.read(mcpx.ReadBuilder().add("D30", mcpx.int16)) == [9]


def test_random_access(plc: mcpx.McpX) -> None:
    plc.random_write(lambda b: b.add("D1", 11, mcpx.int16).add("D50", 12, mcpx.uint32))
    assert plc.random_read(lambda b: b.add("D1", mcpx.int16).add("D50", mcpx.uint32)) == [11, 12]

    with pytest.raises(mcpx.InvalidArgumentError):
        plc.random_read(lambda b: b.add("D1", mcpx.int16, count=2))


def test_block_access(plc: mcpx.McpX) -> None:
    plc.block_write(lambda b: b.add("D0", [1, 2], mcpx.int16).add("D100", [3, 4, 5], mcpx.int16))
    assert plc.block_read(lambda b: b.add("D0", mcpx.int16, count=2).add("D100", mcpx.int16, count=3)) == [[1, 2], [3, 4, 5]]

    with pytest.raises(mcpx.InvalidArgumentError):
        plc.block_read(lambda b: b.add("D0", mcpx.int16))


def test_monitor(plc: mcpx.McpX) -> None:
    plc.write("D10", 1, mcpx.int16)
    plc.write("D20", 100000, mcpx.int32)

    seen = []
    with plc.monitor_regist(lambda b: b.add("D10", mcpx.int16, on_read=seen.append).add("D20", mcpx.int32)) as session:
        assert session.read() == [1, 100000]

        # 登録し直さずに最新の値を読めること
        plc.write("D10", 2, mcpx.int16)
        assert session.read() == [2, 100000]
        assert seen == [1, 2]

        # 別の登録で置き換わった古いセッションは使えないこと
        other = plc.monitor_regist(lambda b: b.add("D10", mcpx.int16))
        with pytest.raises(mcpx.InvalidOperationError):
            session.read()
        assert other.read() == [2]


def test_monitor_unsupported_type(plc: mcpx.McpX) -> None:
    with pytest.raises(mcpx.UnsupportedError):
        plc.monitor_regist(lambda b: b.add("D0", mcpx.int64))


def test_empty_builder(plc: mcpx.McpX) -> None:
    with pytest.raises(mcpx.InvalidArgumentError):
        plc.read(lambda b: None)
