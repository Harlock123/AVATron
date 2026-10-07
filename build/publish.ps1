# Windows equivalent of publish.sh: self-contained single-file builds, as CI makes them.
# Usage: pwsh build/publish.ps1 [-Rids win-x64,win-arm64]
param([string[]]$Rids = @("win-x64"))
$ErrorActionPreference = "Stop"
Set-Location (Join-Path $PSScriptRoot "..")
foreach ($rid in $Rids) {
  Write-Host "==> $rid"
  dotnet publish src/AVATron.Avalonia/AVATron.Avalonia.csproj -c Release -r $rid --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:IncludeAllContentForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true -p:DebugType=none -o "publish/$rid"
  Copy-Item README.md, THIRD_PARTY.md, LICENSE "publish/$rid/"
}
