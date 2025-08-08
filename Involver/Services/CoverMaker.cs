// CoverMaker.cs (已更新)
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;
using System.Numerics;

public class CoverMaker
{
    private readonly FontCollection _fontCollection;
    private readonly FontFamily _mainFontFamily;
    private readonly FontFamily _subFontFamily;

    public CoverMaker(string mainFontPath, string subFontPath)
    {
        _fontCollection = new FontCollection();
        // 確保能正確載入字體
        if (!File.Exists(mainFontPath)) throw new FileNotFoundException("找不到主要字體檔案！", mainFontPath);
        if (!File.Exists(subFontPath)) throw new FileNotFoundException("找不到副標題字體檔案！", subFontPath);

        _mainFontFamily = _fontCollection.Add(mainFontPath);
        _subFontFamily = _fontCollection.Add(subFontPath);
    }

    // --- 更新：方法簽名，接收 brandText, title, 和 subTitle ---
    public void Generate(string backgroundImagePath, string brandText, string title, string subTitle, string outputPath)
    {
        using (Image image = Image.Load(backgroundImagePath))
        {
            image.Mutate(ctx =>
            {
                // --- 更新：傳遞對應的參數 ---
                DrawTopLogo(ctx, brandText, title, image.Width, image.Height);
                DrawSubTitle(ctx, subTitle, image.Width, image.Height);
            });

            image.SaveAsPng(outputPath);
            Console.WriteLine($"封面已成功儲存至：{outputPath}");
        }
    }

    // --- 更新：接收 brandText 和 title 參數 ---
    private void DrawTopLogo(IImageProcessingContext ctx, string brandText, string title, int imageWidth, int imageHeight)
    {
        var kakuyomuFont = _subFontFamily.CreateFont(40, FontStyle.Bold);
        var nextFont = _subFontFamily.CreateFont(60, FontStyle.Bold);

        var blueColor = Color.ParseHex("00a0e9");
        var textColor = Color.Black;

        var topPadding = imageHeight * 0.05f;
        var leftPadding = imageWidth * 0.05f;

        var squareSize = 45;
        var square = new RectangularPolygon(leftPadding, topPadding, squareSize, squareSize);
        ctx.Fill(blueColor, square);

        // --- 更新：移除寫死的 brandText，改用參數 ---
        var brandTextOptions = new RichTextOptions(kakuyomuFont)
        {
            Origin = new PointF(leftPadding + squareSize + 10, topPadding + 5),
            Dpi = 72
        };
        ctx.DrawText(brandTextOptions, brandText, textColor);

        // --- 更新：移除寫死的 nextText，改用參數 title ---
        var nextTextOptions = new RichTextOptions(nextFont)
        {
            Origin = new PointF(leftPadding, topPadding + squareSize),
            Dpi = 72
        };
        ctx.DrawText(nextTextOptions, title, textColor);
    }

    // --- 更新：將參數 title 更名為 subTitle，使其語意更清晰 ---
    private void DrawSubTitle(IImageProcessingContext ctx, string subTitle, int imageWidth, int imageHeight)
    {
        // 中文字元較寬，調整字體大小比例。您可以依據喜好調整此處的除數。
        float fontSize = imageWidth / 10.0f;
        var font = _mainFontFamily.CreateFont(fontSize, FontStyle.Bold);

        var textOptions = new RichTextOptions(font)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            Origin = new PointF(imageWidth / 2f, imageHeight * 0.9f),
            Dpi = 72,
            WrappingLength = imageWidth * 0.8f,
            LineSpacing = 1.2f
        };

        var shadowOffset = new PointF(5, 5);
        var shadowColor = Color.Black.WithAlpha(0.6f);
        var shadowBrush = Brushes.Solid(shadowColor);

        var textColor = Color.White;
        var textBrush = Brushes.Solid(textColor);

        float rotationDegrees = -5.0f;
        var rotationMatrix = Matrix3x2.CreateRotation((float)(rotationDegrees * Math.PI / 180));

        var drawingOptions = new DrawingOptions { Transform = rotationMatrix };

        var shadowTransform = Matrix3x2.CreateTranslation(shadowOffset) * drawingOptions.Transform;
        ctx.SetDrawingTransform(shadowTransform);
        // --- 更新：使用 subTitle 參數 ---
        ctx.DrawText(textOptions, subTitle, shadowBrush);

        ctx.SetDrawingTransform(drawingOptions.Transform);
        // --- 更新：使用 subTitle 參數 ---
        ctx.DrawText(textOptions, subTitle, textBrush);

        ctx.SetDrawingTransform(Matrix3x2.Identity);
    }
}