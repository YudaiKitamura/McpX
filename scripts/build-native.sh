#!/usr/bin/env bash
# ネイティブライブラリ（C ABI）を実行中の OS・CPU 向けに Native AOT でビルドし、
# 各言語のラッパー（bindings/*）から使えるように配置する（Linux / macOS 用）。
#
#   scripts/build-native.sh
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"

case "$(uname -s)-$(uname -m)" in
    Linux-x86_64)  rid=linux-x64;   lib=mcpx.so ;;
    Linux-aarch64) rid=linux-arm64; lib=mcpx.so ;;
    Darwin-arm64)  rid=osx-arm64;   lib=mcpx.dylib ;;
    *) echo "unsupported platform: $(uname -s)-$(uname -m)" >&2; exit 1 ;;
esac

dotnet publish "$root/McpXInterop/McpXInterop.csproj" -c Release -r "$rid" /p:PublishAot=true

source="$root/McpXInterop/bin/Release/net9.0/$rid/publish/$lib"
mkdir -p "$root/bindings/python/src/mcpx/_native"
cp "$source" "$root/bindings/python/src/mcpx/_native/$lib"
echo "copied $lib to bindings/python/src/mcpx/_native/"
