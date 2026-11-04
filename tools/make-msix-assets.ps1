# Generates the Store / MSIX logos in packaging/msix/Assets from assets/icon-dark.ico and icon-light.ico.
# Each size uses the closest image stored in the .ico, so small icons stay sharp.
# Run from the repo root after changing the icon:  pwsh -NoProfile -File tools/make-msix-assets.ps1
Add-Type -AssemblyName System.Drawing

$root = Join-Path $PSScriptRoot '..'
$out = Join-Path $root 'packaging\msix\Assets'
Remove-Item (Join-Path $out '*.png') -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force $out | Out-Null

$darkIco = Join-Path $root 'assets\icon-dark.ico'
$lightIco = Join-Path $root 'assets\icon-light.ico'

function Save-Logo([string]$name, [int]$width, [int]$height, [double]$iconScale, [string]$ico = $darkIco) {
    $size = [int]([Math]::Min($width, $height) * $iconScale)
    $source = (New-Object System.Drawing.Icon $ico, $size, $size).ToBitmap()
    $bmp = New-Object System.Drawing.Bitmap $width, $height
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = 'HighQualityBicubic'
    $g.SmoothingMode = 'AntiAlias'
    $g.Clear([System.Drawing.Color]::Transparent)
    $g.DrawImage($source, [int](($width - $size) / 2), [int](($height - $size) / 2), $size, $size)
    $g.Dispose(); $source.Dispose()
    $bmp.Save((Join-Path $out $name), [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
}

Save-Logo 'StoreLogo.png'          50  50  1.0
Save-Logo 'Square44x44Logo.png'    44  44  1.0
Save-Logo 'Square150x150Logo.png' 150 150  0.7
Save-Logo 'Wide310x150Logo.png'   310 150  0.7

# Taskbar / Start / title bar icons. Without the "unplated" variants Windows draws the icon on a
# square of the user's accent color (blue by default). altform-lightunplated is used in light mode.
foreach ($size in 16, 20, 24, 30, 32, 36, 40, 48, 60, 64, 72, 80, 96, 256) {
    Save-Logo "Square44x44Logo.targetsize-$size.png"                       $size $size 1.0
    Save-Logo "Square44x44Logo.targetsize-${size}_altform-unplated.png"      $size $size 1.0
    Save-Logo "Square44x44Logo.targetsize-${size}_altform-lightunplated.png" $size $size 1.0 $lightIco
}

Write-Host ("{0} images in {1}" -f (Get-ChildItem $out -Filter *.png).Count, $out)
