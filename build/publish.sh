#!/usr/bin/env bash
# Publishes self-contained single-file builds into ./publish/<rid>, the same way CI does
# (.github/workflows/build.yml). No .NET install is needed to run them.
# Usage: build/publish.sh [rid ...]   (default: all supported targets)
set -euo pipefail
cd "$(dirname "$0")/.."
RIDS=("$@")
[ ${#RIDS[@]} -eq 0 ] && RIDS=(win-x64 win-x86 win-arm64 linux-x64 linux-arm64 linux-arm osx-x64 osx-arm64)
for rid in "${RIDS[@]}"; do
  echo "==> $rid"
  dotnet publish src/AVATron.Avalonia/AVATron.Avalonia.csproj -c Release -r "$rid" --self-contained true \
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:IncludeAllContentForSelfExtract=true \
    -p:EnableCompressionInSingleFile=true -p:DebugType=none -o "publish/$rid"
  cp README.md THIRD_PARTY.md LICENSE "publish/$rid/"
done
