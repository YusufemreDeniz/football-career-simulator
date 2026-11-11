param(
    [Parameter(Mandatory = $true)]
    [string]$SourcePath,

    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\src\FootballCareerSimulator.Presentation\assets\app-icon')
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$resolvedSource = (Resolve-Path -LiteralPath $SourcePath).Path
$resolvedOutput = [System.IO.Path]::GetFullPath($OutputDirectory)
[System.IO.Directory]::CreateDirectory($resolvedOutput) | Out-Null

function New-ArgbBitmap([int]$width, [int]$height) {
    return [System.Drawing.Bitmap]::new(
        $width,
        $height,
        [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
}

function Set-Quality([System.Drawing.Graphics]$graphics) {
    $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceOver
    $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
}

function Save-Png([System.Drawing.Bitmap]$bitmap, [string]$fileName) {
    $path = Join-Path $resolvedOutput $fileName
    $bitmap.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    return $path
}

function Add-RoundedRectangle(
    [System.Drawing.Drawing2D.GraphicsPath]$path,
    [System.Drawing.RectangleF]$bounds,
    [float]$radius) {
    $diameter = $radius * 2
    $arc = [System.Drawing.RectangleF]::new($bounds.X, $bounds.Y, $diameter, $diameter)
    $path.AddArc($arc, 180, 90)
    $arc.X = $bounds.Right - $diameter
    $path.AddArc($arc, 270, 90)
    $arc.Y = $bounds.Bottom - $diameter
    $path.AddArc($arc, 0, 90)
    $arc.X = $bounds.X
    $path.AddArc($arc, 90, 90)
    $path.CloseFigure()
}

$source = [System.Drawing.Bitmap]::new($resolvedSource)
try {
    $foreground = New-ArgbBitmap 432 432
    $graphics = [System.Drawing.Graphics]::FromImage($foreground)
    try {
        Set-Quality $graphics
        $graphics.Clear([System.Drawing.Color]::Transparent)
        $graphics.DrawImage($source, 0, 0, 432, 432)
    }
    finally {
        $graphics.Dispose()
    }
    Save-Png $foreground 'launcher-adaptive-foreground-432.png' | Out-Null

    $background = New-ArgbBitmap 432 432
    $graphics = [System.Drawing.Graphics]::FromImage($background)
    try {
        Set-Quality $graphics
        $bounds = [System.Drawing.RectangleF]::new(0, 0, 432, 432)
        $gradient = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
            $bounds,
            [System.Drawing.Color]::FromArgb(255, 3, 14, 26),
            [System.Drawing.Color]::FromArgb(255, 8, 48, 61),
            45)
        try {
            $graphics.FillRectangle($gradient, $bounds)
        }
        finally {
            $gradient.Dispose()
        }

        $glow = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(28, 54, 201, 255))
        try {
            $graphics.FillEllipse($glow, 48, -130, 336, 336)
        }
        finally {
            $glow.Dispose()
        }
    }
    finally {
        $graphics.Dispose()
    }
    Save-Png $background 'launcher-adaptive-background-432.png' | Out-Null

    $main = New-ArgbBitmap 192 192
    $graphics = [System.Drawing.Graphics]::FromImage($main)
    try {
        Set-Quality $graphics
        $graphics.Clear([System.Drawing.Color]::Transparent)
        $path = [System.Drawing.Drawing2D.GraphicsPath]::new()
        try {
            Add-RoundedRectangle $path ([System.Drawing.RectangleF]::new(4, 4, 184, 184)) 40
            $gradient = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
                [System.Drawing.RectangleF]::new(4, 4, 184, 184),
                [System.Drawing.Color]::FromArgb(255, 4, 19, 34),
                [System.Drawing.Color]::FromArgb(255, 7, 54, 62),
                45)
            try {
                $graphics.FillPath($gradient, $path)
            }
            finally {
                $gradient.Dispose()
            }
            $border = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(150, 54, 201, 255), 2)
            try {
                $graphics.DrawPath($border, $path)
            }
            finally {
                $border.Dispose()
            }
        }
        finally {
            $path.Dispose()
        }
        $graphics.DrawImage($source, 2, 2, 188, 188)
    }
    finally {
        $graphics.Dispose()
    }
    Save-Png $main 'launcher-main-192.png' | Out-Null

    $monochrome = $foreground.Clone(
        [System.Drawing.Rectangle]::new(0, 0, 432, 432),
        [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $area = [System.Drawing.Rectangle]::new(0, 0, 432, 432)
    $data = $monochrome.LockBits(
        $area,
        [System.Drawing.Imaging.ImageLockMode]::ReadWrite,
        [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    try {
        $bytes = [Math]::Abs($data.Stride) * $data.Height
        $pixels = [byte[]]::new($bytes)
        [System.Runtime.InteropServices.Marshal]::Copy($data.Scan0, $pixels, 0, $bytes)
        for ($index = 0; $index -lt $bytes; $index += 4) {
            if ($pixels[$index + 3] -gt 0) {
                $pixels[$index] = 255
                $pixels[$index + 1] = 255
                $pixels[$index + 2] = 255
            }
        }
        [System.Runtime.InteropServices.Marshal]::Copy($pixels, 0, $data.Scan0, $bytes)
    }
    finally {
        $monochrome.UnlockBits($data)
    }
    Save-Png $monochrome 'launcher-adaptive-monochrome-432.png' | Out-Null

    Copy-Item -LiteralPath $resolvedSource -Destination (Join-Path $resolvedOutput 'launcher-source.png') -Force

    $foreground.Dispose()
    $background.Dispose()
    $main.Dispose()
    $monochrome.Dispose()
}
finally {
    $source.Dispose()
}

Get-ChildItem -LiteralPath $resolvedOutput -Filter 'launcher-*.png' |
    Select-Object Name, Length, LastWriteTime
