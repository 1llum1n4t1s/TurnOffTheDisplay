# .NETを使用してPNGファイルからICOファイルを生成するスクリプト
Add-Type -AssemblyName System.Drawing

$sizes = @(16, 32, 48, 256)

function Create-IconFromPng {
    param([string]$PngPath, [string]$OutputIcoPath, [string]$IconName)

    if (-not (Test-Path $PngPath)) {
        Write-Host "ERROR: PNGファイルが見つかりません: $PngPath" -ForegroundColor Red
        return $false
    }

    Write-Host "Processing: $PngPath -> $OutputIcoPath"

    try {
        $sourceBitmap = [System.Drawing.Image]::FromFile($PngPath)
        $bitmap = [System.Drawing.Bitmap]$sourceBitmap
        $images = @{}

        foreach ($size in $sizes) {
            $resizedBitmap = New-Object System.Drawing.Bitmap($size, $size)
            $graphics = [System.Drawing.Graphics]::FromImage($resizedBitmap)
            $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
            $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $graphics.Clear([System.Drawing.Color]::Transparent)
            $graphics.DrawImage($bitmap, 0, 0, $size, $size)
            $graphics.Dispose()
            $images[$size] = $resizedBitmap
        }

        $icoFile = [System.IO.File]::Create($OutputIcoPath)
        $writer = New-Object System.IO.BinaryWriter($icoFile)

        $writer.Write([byte]0)
        $writer.Write([byte]0)
        $writer.Write([byte]1)
        $writer.Write([byte]0)
        $writer.Write([System.Int16]$sizes.Count)

        $offset = 6 + (16 * $sizes.Count)
        foreach ($size in $sizes) {
            $bitmap = $images[$size]
            $stream = New-Object System.IO.MemoryStream
            $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
            $pngData = $stream.ToArray()

            $w = if ($size -eq 256) { [byte]0 } else { [byte]$size }
            $h = if ($size -eq 256) { [byte]0 } else { [byte]$size }

            $writer.Write($w)
            $writer.Write($h)
            $writer.Write([byte]0)
            $writer.Write([byte]0)
            $writer.Write([System.Int16]1)
            $writer.Write([System.Int16]32)
            $writer.Write([System.Int32]$pngData.Length)
            $writer.Write([System.Int32]$offset)

            $offset += $pngData.Length
            $stream.Dispose()
        }

        foreach ($size in $sizes) {
            $bitmap = $images[$size]
            $stream = New-Object System.IO.MemoryStream
            $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
            $pngData = $stream.ToArray()
            $writer.Write($pngData)
            $stream.Dispose()
        }

        $writer.Close()
        $icoFile.Close()

        foreach ($img in $images.Values) {
            $img.Dispose()
        }
        $bitmap.Dispose()
        $sourceBitmap.Dispose()

        Write-Host "✓ Successfully created: $OutputIcoPath with sizes: $($sizes -join ', ')" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Host "ERROR: $($_)" -ForegroundColor Red
        return $false
    }
}

$scriptDir = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }

if (-not $scriptDir) {
    Write-Host "ERROR: スクリプトディレクトリを確認できません" -ForegroundColor Red
    exit 1
}

$appIconPath = Join-Path -Path $scriptDir -ChildPath "app_icon.png"
$appIcoPath = Join-Path -Path $scriptDir -ChildPath "app.ico"

$allSuccess = $true

if (Test-Path $appIconPath) {
    $success = Create-IconFromPng -PngPath $appIconPath -OutputIcoPath $appIcoPath -IconName "アプリケーション"
    if (-not $success) { $allSuccess = $false }
} else {
    Write-Host "WARNING: app_icon.png が見つかりません: $appIconPath" -ForegroundColor Yellow
}

if ($allSuccess) {
    Write-Host "`nIcon generation completed successfully!" -ForegroundColor Green
} else {
    Write-Host "`nIcon generation completed with some warnings." -ForegroundColor Yellow
}
