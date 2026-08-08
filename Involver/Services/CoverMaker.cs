// CoverMaker.cs (已更新)
using System;
using System.IO;
using SkiaSharp;

public class CoverMaker : IDisposable
{
    private readonly SKTypeface _mainTypeface;
    private readonly SKTypeface _subTypeface;
    private bool _disposed;

    public CoverMaker(string mainFontPath, string subFontPath)
    {
        if (!File.Exists(mainFontPath)) throw new FileNotFoundException("找不到主要字體檔案！", mainFontPath);
        if (!File.Exists(subFontPath)) throw new FileNotFoundException("找不到副標題字體檔案！", subFontPath);

        _mainTypeface = SKTypeface.FromFile(mainFontPath) ?? throw new Exception("無法載入主要字體");
        _subTypeface = SKTypeface.FromFile(subFontPath) ?? throw new Exception("無法載入副標題字體");
    }

    public void Generate(string backgroundImagePath, string brandText, string title, string subTitle, string outputPath)
    {
        using var input = File.OpenRead(backgroundImagePath);
        using var bgBitmap = SKBitmap.Decode(input);
        if (bgBitmap == null) throw new FileNotFoundException("無法讀取背景圖像", backgroundImagePath);

        var info = new SKImageInfo(bgBitmap.Width, bgBitmap.Height);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        // Draw background (use sampling options to avoid obsolete overload)
        canvas.DrawBitmap(bgBitmap, 0, 0, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None));

        var topPadding = bgBitmap.Height * 0.05f;
        var leftPadding = bgBitmap.Width * 0.05f;

        // Draw blue square
        const float squareSize = 45f;
        using (var squarePaint = new SKPaint { Color = new SKColor(0x00, 0xA0, 0xE9), IsAntialias = true })
        {
            canvas.DrawRect(leftPadding, topPadding, squareSize, squareSize, squarePaint);
        }

        // Brand text
        using var brandFont = new SKFont(_subTypeface, 40);
        using var brandPaint = new SKPaint { Color = SKColors.Black, IsAntialias = true };
        {
            var brandX = leftPadding + squareSize + 10;
            var brandY = topPadding + brandFont.Size; // approximate baseline
            canvas.DrawText(brandText, brandX, brandY, SKTextAlign.Left, brandFont, brandPaint);
        }

        // Title text
        using var titleFont = new SKFont(_subTypeface, 60);
        using var titlePaint = new SKPaint { Color = SKColors.Black, IsAntialias = true };
        {
            var titleX = leftPadding;
            var titleY = topPadding + squareSize + titleFont.Size;
            canvas.DrawText(title, titleX, titleY, SKTextAlign.Left, titleFont, titlePaint);
        }

        // Subtitle (large, centered, rotated, with shadow)
        var fontSize = bgBitmap.Width / 10.0f;
        using var subFont = new SKFont(_mainTypeface, fontSize);
        using var subPaint = new SKPaint { Color = SKColors.White, IsAntialias = true };
        using var shadowPaint = new SKPaint { Color = new SKColor(0, 0, 0, 153), IsAntialias = true };

        var centerX = bgBitmap.Width / 2f;
        var centerY = bgBitmap.Height * 0.9f;

        // 支援多行文字（subTitle 內可能包含換行符號）
        var lines = (subTitle ?? string.Empty).Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        // 使用字型 metrics 計算更精確的行高與基線
        var metrics = subFont.Metrics;
        var ascent = Math.Abs(metrics.Ascent);
        var descent = metrics.Descent;
        var leading = metrics.Leading;
        // 行高：以 ascent+descent+leading 為基底，再乘上比例
        var lineSpacing = (ascent + descent + leading) * 1.2f;

        // 在 centerY 為基準的相對座標系中計算第一行 baseline
        // 預設置中排列（第一行 baseline 相對於 center 為負的偏移）
        var baselineFirstRelative = -((lines.Length - 1) * lineSpacing) / 2f;

        // 設定圖片底部保留距離，確保最後一行不會貼到或超出圖片底部
        var bottomPadding = Math.Max(40f, bgBitmap.Height * 0.05f);
        // 在相對座標中，底部可用空間為 (imageHeight - centerY)
        var maxBaselineFirstRelative = (bgBitmap.Height - centerY) - bottomPadding - descent - (lines.Length - 1) * lineSpacing;
        if (baselineFirstRelative > maxBaselineFirstRelative)
        {
            baselineFirstRelative = maxBaselineFirstRelative;
        }

        // 同樣避免第一行太靠近上方（留 topPadding）
        var minBaselineFirstRelative = -centerY + topPadding + ascent;
        if (baselineFirstRelative < minBaselineFirstRelative)
        {
            baselineFirstRelative = minBaselineFirstRelative;
        }

        canvas.Save();
        canvas.Translate(centerX, centerY);
        canvas.RotateDegrees(-5f);

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var y = baselineFirstRelative + i * lineSpacing;
            // Shadow
            canvas.DrawText(line, 5, y + 5, SKTextAlign.Center, subFont, shadowPaint);
            // Main
            canvas.DrawText(line, 0, y, SKTextAlign.Center, subFont, subPaint);
        }

        canvas.Restore();

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var outStream = File.OpenWrite(outputPath);
        data.SaveTo(outStream);

        Console.WriteLine($"封面已成功儲存至：{outputPath}");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _mainTypeface?.Dispose();
        _subTypeface?.Dispose();
        _disposed = true;
    }
}
