using SkiaSharp;
using System;
using System.IO;
using System.Globalization;

class Program
{
    static void Main()
    {
        using var surface = SKSurface.Create(new SKImageInfo(600, 200));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        var fontManager = SKFontManager.Default;
        var typeface = fontManager.MatchFamily("Microsoft YaHei UI", SKFontStyle.Normal);
        Console.WriteLine($"Typeface: {typeface?.FamilyName}");
        if (typeface == null) return;

        string text = "你好世界Hello123";

        // 方式A: SKPaint 直接渲染 (SkiaSharp 3.x)
        using var paintA = new SKPaint {
            Typeface = typeface, TextSize = 24, IsAntialias = true, Color = SKColors.Black
        };
        canvas.DrawText(text, 10, 40, paintA);
        Console.WriteLine($"A paintA.MeasureText: {paintA.MeasureText(text)}");

        // 方式B: SKFont + canvas.DrawText
        using var fontB = new SKFont { Size = 24, Typeface = typeface, Edging = SKFontEdging.SubpixelAntialias, Subpixel = true };
        using var paintB = new SKPaint { IsAntialias = true, Color = SKColors.Red };
        canvas.DrawText(text, 10, 80, fontB, paintB);
        Console.WriteLine($"B fontB.MeasureText: {fontB.MeasureText(text)}");

        // 方式C: 测试 MeasureText 对每个字符
        foreach (char ch in text)
        {
            Console.WriteLine($"  MeasureText('{ch}'): {paintA.MeasureText(ch.ToString())}");
        }

        // 方式D: StringInfo 分段后分别渲染
        var si = new StringInfo(text);
        float x = 10;
        using var paintD = new SKPaint { Typeface = typeface, TextSize = 24, IsAntialias = true, Color = SKColors.Green };
        for (int i = 0; i < si.LengthInTextElements; i++)
        {
            var elem = si.SubstringByTextElements(i, 1);
            canvas.DrawText(elem, x, 130, paintD);
            x += paintD.MeasureText(elem);
        }

        // 方式E: SKTypeface.Default (无中文支持)
        using var paintE = new SKPaint {
            Typeface = SKTypeface.Default, TextSize = 24, IsAntialias = true, Color = SKColors.Blue
        };
        canvas.DrawText("Default字体: 你好", 10, 170, paintE);
        Console.WriteLine($"E Default.MeasureText('中'): {paintE.MeasureText("中")}");

        // 保存
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes("test_output.png", data.ToArray());
        Console.WriteLine("Done! Saved test_output.png");
    }
}
