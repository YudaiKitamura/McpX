import os
import subprocess
import sys
from pathlib import Path
from typing import Callable, Iterator, List

import pytest

import mcpx

ROOT = Path(__file__).resolve().parents[3]
SERVER_PROJECT = ROOT / "tools" / "FakePlcServer"
SERVER_DLL = SERVER_PROJECT / "bin" / "Debug" / "net9.0" / "FakePlcServer.dll"


@pytest.fixture(scope="session")
def fake_server_dll() -> Path:
    """偽 PLC サーバー（tools/FakePlcServer）をビルドする（セッションで1回）。"""
    subprocess.run(["dotnet", "build", str(SERVER_PROJECT), "-c", "Debug", "-nologo", "-v", "q"], check=True, stdout=subprocess.DEVNULL)
    return SERVER_DLL


@pytest.fixture
def start_server(fake_server_dll: Path) -> Iterator[Callable[..., int]]:
    """偽 PLC サーバーを起動してポート番号を返す関数（引数はサーバーのオプション）。テストの終わりに終了させる。"""
    processes: List[subprocess.Popen] = []

    def start(*args: str) -> int:
        process = subprocess.Popen(["dotnet", str(fake_server_dll), *args], stdin=subprocess.PIPE, stdout=subprocess.PIPE, text=True)
        processes.append(process)
        line = process.stdout.readline().strip()
        assert line.startswith("PORT="), line
        return int(line.split("=")[1])

    yield start

    for process in processes:
        process.stdin.close()
        process.wait(timeout=10)


@pytest.fixture
def plc(start_server: Callable[..., int]) -> Iterator[mcpx.McpX]:
    """偽 PLC サーバーに接続した McpX（書き込んだ値は読み込みで返る）。"""
    with mcpx.McpX("127.0.0.1", start_server()) as client:
        yield client


def pytest_collection_modifyitems(config: pytest.Config, items: List[pytest.Item]) -> None:
    if os.environ.get("MCPX_REAL_PLC") != "1":
        skip = pytest.mark.skip(reason="実機テストは MCPX_REAL_PLC=1 のときのみ実行します。")
        for item in items:
            if "real_plc" in item.keywords:
                item.add_marker(skip)
