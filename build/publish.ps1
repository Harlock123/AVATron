# Windows equivalent of publish.sh. Usage: pwsh build/publish.ps1 [-Rids win-x64,win-arm64]
param([string[]]$Rids = @("win-x64"))
$ErrorActionPreference = "Stop"
Set-Location (Join-Path $PSScriptRoot "..")
foreach ($rid in $Rids) {
  $sc = if ($rid -like "win-*") { "true" } else { "false" }
  Write-Host "==> $rid (self-contained=$sc)"
  dotnet publish src/Robotron.Avalonia/Robotron.Avalonia.csproj -c Release -r $rid --self-contained $sc -p:DebugType=None -o "publish/$rid"
  Copy-Item README.md, THIRD_PARTY.md "publish/$rid/" -ErrorAction SilentlyContinue
}
