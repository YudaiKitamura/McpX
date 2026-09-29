import asyncio

import mcpx
from mcpx import Prefix


def test_async_api(start_server) -> None:
    port = start_server()

    async def main() -> None:
        async with await mcpx.McpX.connect_async("127.0.0.1", port) as plc:
            await plc.batch_write_async(Prefix.D, "0", list(range(20)), mcpx.int16)

            # 同じ接続への並行呼び出しは1つずつ処理され、それぞれ正しい値を返す
            results = await asyncio.gather(*[plc.read_async(Prefix.D, str(i), mcpx.int16) for i in range(20)])
            assert results == list(range(20))

            values = await plc.read_async(lambda b: b.add("D0", mcpx.int16, count=3))
            assert values == [[0, 1, 2]]

    asyncio.run(main())
