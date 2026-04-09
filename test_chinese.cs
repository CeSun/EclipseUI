// 临时测试文件，验证中文渲染
using SkiaSharp;
using System;

class Program
{
    static void Main()
    {
        var width = 400;
        var height = 100;
        
        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);
        
        // 测试 SKPaint 直接渲染中文
        using var paint = new SKPaint
        {
            Color = SKColors.Black,
            IsAntialias = true,
            TextSize = 24
        };
        
        // 尝试微软雅黑
        paint.Typeface = SKTypeface.FromFamilyName("Microsoft YaHei UI");
        Console.WriteLine($"Typeface: {paint.Typeface?.FamilyName}, Style: {paint.Typeface?.Style}");
        
        if (paint.Typeface != null)
        {
            var glyphCount = paint.Typeface.CountGlyphs("中");
            Console.WriteLine($"Glyph count for '中': {glyphCount}");
            
            var glyphId = paint.Typeface.GetGlyph('中');
            Console.WriteLine($"Glyph ID for '中': {glyphId}");
        }
        
        // 直接绘制
        canvas.DrawText("Hello 中文字体测试", 10, 40, paint);
        
        // 用 SKFont 方式测试
        using var font = new SKFont { Size = 24 };
        font.Typeface = SKTypeface.FromFamilyName("Microsoft YaHei UI");
        Console.WriteLine($"Font Typeface: {font.Typeface?.FamilyName}");
        
        if (font.Typeface != null)
        {
            var glyphId2 = font.Typeface.GetGlyph('中');
            Console.WriteLine($"Glyph ID via Font for '中': {glyphId2}");
        }
        
        using var paint2 = new SKPaint { IsAntialias = true, Color = SKColors.Blue };
        canvas.DrawText("Via SKFont + SKPaint", 10, 80, font, paint2);
        
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        System.IO.File.WriteAllBytes("test_chinese_output.png", data.ToArray());
        Console.WriteLine("Saved to test_chinese_output.png");
    }
}
