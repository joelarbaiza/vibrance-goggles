# Keeps assets/icon.ico in sync with assets/icon-dark.ico.
#
# The icons are designed outside this repo; nothing here generates or redraws them.
#   assets/icon-dark.ico   window + tray icon when Windows uses dark mode (embedded in the .exe)
#   assets/icon-light.ico  window + tray icon when Windows uses light mode (embedded in the .exe)
#   assets/icon.ico        fixed icon of the .exe, the MSI installer and the shortcuts = copy of icon-dark.ico
#
# Usage (from the repo root):
#   pwsh -NoProfile -File tools/make-icon.ps1          # copies icon-dark.ico over icon.ico if they differ
#   pwsh -NoProfile -File tools/make-icon.ps1 -Check   # only verifies; exit code 1 if out of sync

param([switch]$Check)

$assets = Join-Path $PSScriptRoot '..\assets'
$dark = Join-Path $assets 'icon-dark.ico'
$light = Join-Path $assets 'icon-light.ico'
$fixed = Join-Path $assets 'icon.ico'

foreach ($f in $dark, $light) {
    if (-not (Test-Path $f)) { Write-Error "Missing $f"; exit 1 }
    $bytes = [System.IO.File]::ReadAllBytes($f)
    if ($bytes.Length -lt 6 -or [BitConverter]::ToUInt16($bytes, 2) -ne 1) { Write-Error "$f is not a valid .ico file"; exit 1 }
}

$inSync = (Test-Path $fixed) -and ((Get-FileHash $fixed).Hash -eq (Get-FileHash $dark).Hash)

if ($inSync) {
    Write-Host 'assets/icon.ico is in sync with assets/icon-dark.ico'
    exit 0
}

if ($Check) {
    Write-Host 'assets/icon.ico differs from assets/icon-dark.ico. Run tools/make-icon.ps1 to sync it.' -ForegroundColor Red
    exit 1
}

Copy-Item $dark $fixed -Force
Write-Host 'Copied assets/icon-dark.ico -> assets/icon.ico'
