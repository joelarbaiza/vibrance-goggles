# Generates assets/icon.ico (16-256 px) for Vibrance Goggles.
# Run from the repo root:  pwsh -NoProfile -File tools/make-icon.ps1
Add-Type -AssemblyName System.Drawing

$out = Join-Path $PSScriptRoot '..\assets\icon.ico'
$sizes = 16, 24, 32, 48, 64, 128, 256

function New-IconPng([int]$s) {
    $bmp = New-Object System.Drawing.Bitmap $s, $s
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.Clear([System.Drawing.Color]::Transparent)

    # Rounded tile with a vivid diagonal gradient.
    $r = [Math]::Max(2, $s * 0.22)
    $rect = New-Object System.Drawing.RectangleF 0, 0, ($s - 1), ($s - 1)
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $path.AddArc($rect.X, $rect.Y, $r * 2, $r * 2, 180, 90)
    $path.AddArc($rect.Right - $r * 2, $rect.Y, $r * 2, $r * 2, 270, 90)
    $path.AddArc($rect.Right - $r * 2, $rect.Bottom - $r * 2, $r * 2, $r * 2, 0, 90)
    $path.AddArc($rect.X, $rect.Bottom - $r * 2, $r * 2, $r * 2, 90, 90)
    $path.CloseFigure()
    $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush $rect, ([System.Drawing.Color]::FromArgb(255, 124, 58, 237)), ([System.Drawing.Color]::FromArgb(255, 255, 94, 58)), 45.0
    $blend = New-Object System.Drawing.Drawing2D.ColorBlend 3
    $blend.Colors = @([System.Drawing.Color]::FromArgb(255, 99, 102, 241), [System.Drawing.Color]::FromArgb(255, 236, 72, 153), [System.Drawing.Color]::FromArgb(255, 251, 146, 60))
    $blend.Positions = @(0.0, 0.5, 1.0)
    $brush.InterpolationColors = $blend
    $g.FillPath($brush, $path)

    # Goggles: two lenses joined by a bridge.
    $lens = $s * 0.30
    $cy = $s * 0.50
    $pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::White), ([Math]::Max(1.5, $s * 0.075))
    $fill = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(70, 255, 255, 255))
    foreach ($cx in ($s * 0.31), ($s * 0.69)) {
        $g.FillEllipse($fill, $cx - $lens / 2, $cy - $lens / 2, $lens, $lens)
        $g.DrawEllipse($pen, $cx - $lens / 2, $cy - $lens / 2, $lens, $lens)
    }
    $g.DrawLine($pen, $s * 0.31 + $lens / 2, $cy, $s * 0.69 - $lens / 2, $cy)

    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
    return , $ms.ToArray()
}

$images = foreach ($s in $sizes) { , (New-IconPng $s) }

$fs = [System.IO.File]::Create($out)
$w = New-Object System.IO.BinaryWriter $fs
$w.Write([uint16]0); $w.Write([uint16]1); $w.Write([uint16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $s = $sizes[$i]; $len = $images[$i].Length
    $w.Write([byte]($s % 256)); $w.Write([byte]($s % 256)); $w.Write([byte]0); $w.Write([byte]0)
    $w.Write([uint16]1); $w.Write([uint16]32); $w.Write([uint32]$len); $w.Write([uint32]$offset)
    $offset += $len
}
foreach ($img in $images) { $w.Write($img) }
$w.Close()
Write-Host "Wrote $out"
