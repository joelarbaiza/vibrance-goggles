# Generates the Store / MSIX logos in packaging/msix/Assets from assets/icon-dark.ico (its 256 px image).
# Run from the repo root after changing the icon:  pwsh -NoProfile -File tools/make-msix-assets.ps1
Add-Type -AssemblyName System.Drawing

$root = Join-Path $PSScriptRoot '..'
$out = Join-Path $root 'packaging\msix\Assets'
New-Item -ItemType Directory -Force $out | Out-Null

$icon = New-Object System.Drawing.Icon (Join-Path $root 'assets\icon-dark.ico'), 256, 256
$source = $icon.ToBitmap()

function Save-Logo([string]$name, [int]$width, [int]$height, [double]$iconScale) {
    $bmp = New-Object System.Drawing.Bitmap $width, $height
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = 'HighQualityBicubic'
    $g.SmoothingMode = 'AntiAlias'
    $g.Clear([System.Drawing.Color]::Transparent)
    $size = [int]([Math]::Min($width, $height) * $iconScale)
    $g.DrawImage($source, [int](($width - $size) / 2), [int](($height - $size) / 2), $size, $size)
    $g.Dispose()
    $bmp.Save((Join-Path $out $name), [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Host "  $name ${width}x$height"
}

Save-Logo 'StoreLogo.png'          50  50  1.0
Save-Logo 'Square44x44Logo.png'    44  44  1.0
Save-Logo 'Square150x150Logo.png' 150 150  0.7
Save-Logo 'Wide310x150Logo.png'   310 150  0.7
