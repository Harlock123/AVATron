#!/usr/bin/env bash
# Publishes release builds into ./publish/<rid>.
#   Windows: self-contained single folder (no .NET install needed).
#   Linux/macOS: framework-dependent (requires the .NET 10 runtime).
# Usage: build/publish.sh [rid ...]   (default: all targets below)
set -euo pipefail
cd "$(dirname "$0")/.."
RIDS=("$@")
[ ${#RIDS[@]} -eq 0 ] && RIDS=(win-x64 linux-x64 linux-arm64 osx-arm64 osx-x64)
for rid in "${RIDS[@]}"; do
  case "$rid" in
    win-*) sc=true ;;
    *)     sc=false ;;
  esac
  echo "==> $rid (self-contained=$sc)"
  dotnet publish src/AVATron.Avalonia/AVATron.Avalonia.csproj -c Release -r "$rid" --self-contained "$sc" \
    -p:DebugType=None -o "publish/$rid"
  cp README.md THIRD_PARTY.md LICENSE "publish/$rid/" 2>/dev/null || true
done
