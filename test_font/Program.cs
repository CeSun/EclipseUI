using HarfBuzzSharp;
using SkiaSharp;
using System;
using System.IO;

class Program
{
    static void Main()
    {
        // 创建测试图片
        using var surface = SKSurface.Create(new SKImageInfo(400, 200));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        // 获取中文字体
        var fontManager = SKFontManager.Default;
        var typeface = fontManager.MatchFamily("Microsoft YaHei UI", SKFontStyle.Normal);
        Console.WriteLine($"Typeface: {typeface?.FamilyName}");

        if (typeface == null)
        {
            Console.WriteLine("ERROR: Could not find Microsoft YaHei UI");
            return;
        }

        // 测试 CountGlyphs
        var glyphCount = typeface.CountGlyphs("中");
        Console.WriteLine($"CountGlyphs('中'): {glyphCount}");

        // 测试 GetGlyph
        var glyphId = typeface.GetGlyph('中');
        Console.WriteLine($"GetGlyph('中'): {glyphId}");

        // 测试 MeasureText
        using var font = new SKFont { Size = 24, Typeface = typeface };
        var width = font.MeasureText("你好");
        Console.WriteLine($"MeasureText('你好'): {width}");

        // 直接用 SKPaint 渲染
        using var paint = new SKPaint
        {
            Typeface = typeface,
            TextSize = 24,
            IsAntialias = true,
            Color = SKColors.Black
        };
        canvas.DrawText("你好世界Hello", 10, 50, paint);

        // 用 HarfBuzz 渲染（模拟 HarfBuzzTextRenderer）
        var text = "你好";
        var shaper = new HarfBuzz.HbBlob();
        var face = HarfBuzz.HbFace.Create(typeface.Handle, 1);
        var hbFont = HarfBuzz.HbFont.Create(face);
        hbFont.SetScale((int)(24 * 64), (int)(24 * 64));

        Console.WriteLine($"HarfBuzz font scale set to 24px");

        // 直接用 SKCanvas.DrawText 渲染（最简单的方式）
        using var paint2 = new SKPaint
        {
            Typeface = typeface,
            TextSize = 24,
            IsAntialias = true,
            Color = SKColors.Red
        };
        canvas.DrawText("Red: 你好", 10, 100, paint2);

        // 保存测试结果
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes("test_font_output.png", data.ToArray());
        Console.WriteLine("Saved test_font_output.png");
    }
}
