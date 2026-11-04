# Builds the Microsoft Store package (MSIX) from the app's Release output.
#   pwsh -NoProfile -File tools/build-msix.ps1 -Version 1.2.2
# Output: packaging/msix/out/VibranceGoggles-<version>.msix (unsigned: the Store signs it).
# Needs makeappx.exe from the Windows SDK (installed on GitHub's windows-latest runners).

param(
    [Parameter(Mandatory)] [string]$Version,
    [string]$AppDir = (Join-Path $PSScriptRoot '..\src\VibranceGoggles\bin\Release')
)
$ErrorActionPreference = 'Stop'
$msix = Join-Path $PSScriptRoot '..\packaging\msix'

# The Store requires a four-part version whose last part is 0.
$parts = $Version.Split('.')
while ($parts.Count -lt 3) { $parts += '0' }
$fullVersion = ($parts[0..2] -join '.') + '.0'

$identity = Get-Content (Join-Path $msix 'store-identity.json') -Raw | ConvertFrom-Json

$makeappx = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin" -Recurse -Filter makeappx.exe |
    Where-Object FullName -match '\\x64\\' | Sort-Object FullName -Descending | Select-Object -First 1
if (-not $makeappx) { throw 'makeappx.exe not found: install the Windows SDK.' }

$stage = Join-Path $msix 'stage'
$outDir = Join-Path $msix 'out'
Remove-Item $stage -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force $stage, $outDir | Out-Null

Copy-Item (Join-Path $AppDir 'VibranceGoggles.exe'), (Join-Path $AppDir 'VibranceGoggles.exe.config') $stage
Copy-Item (Join-Path $msix 'Assets') $stage -Recurse

(Get-Content (Join-Path $msix 'AppxManifest.xml') -Raw).
    Replace('$IdentityName$', $identity.IdentityName).
    Replace('$Publisher$', $identity.Publisher).
    Replace('$PublisherDisplayName$', $identity.PublisherDisplayName).
    Replace('$Version$', $fullVersion) |
    Set-Content (Join-Path $stage 'AppxManifest.xml') -Encoding UTF8

# Resource index: lets Windows pick the right icon size and the "unplated" (no colored square) variants.
$makepri = Join-Path $makeappx.DirectoryName 'makepri.exe'
$priConfig = Join-Path $msix 'priconfig.xml'
& $makepri createconfig /cf $priConfig /dq en-US /o | Out-Null
& $makepri new /pr $stage /cf $priConfig /mn (Join-Path $stage 'AppxManifest.xml') /of (Join-Path $stage 'resources.pri') /o | Out-Null
if ($LASTEXITCODE -ne 0) { throw "makepri failed ($LASTEXITCODE)" }
Remove-Item $priConfig

$package = Join-Path $outDir "VibranceGoggles-$Version.msix"
& $makeappx.FullName pack /d $stage /p $package /o
if ($LASTEXITCODE -ne 0) { throw "makeappx failed ($LASTEXITCODE)" }
Remove-Item $stage -Recurse -Force
Write-Host "MSIX: $package (version $fullVersion, identity $($identity.IdentityName))"
