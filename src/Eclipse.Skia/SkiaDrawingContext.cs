using Eclipse.Core.Abstractions;
using Eclipse.Input;
using Eclipse.Rendering;
using Eclipse.Skia.Text;
using SkiaSharp;

namespace Eclipse.Skia;

/// <summary>
/// Skia 绘制上下文实现
/// </summary>
public class SkiaDrawingContext : IDrawingContext
{
    private readonly SKCanvas _canvas;
    private static readonly HarfBuzzTextRenderer _textRenderer = new();
    private static SKTypeface? _chineseTypeface;
    
    public double Scale { get; }
    public double Width { get; }
    public double Height { get; }
    
    public SkiaDrawingContext(SKCanvas canvas, double width, double height, double scale = 1.0)
    {
        _canvas = canvas;
        Width = width;
        Height = height;
        Scale = scale;
    }
    
    public void Clear(string? color = null)
    {
        var bgColor = string.IsNullOrEmpty(color) 
            ? SKColors.White 
            : SKColor.TryParse(color, out var c) ? c : SKColors.White;
        _canvas.Clear(bgColor);
    }
    
    public void DrawRectangle(Rect bounds, string? fillColor, string? strokeColor = null, double strokeWidth = 0, double cornerRadius = 0)
    {
        if (!string.IsNullOrEmpty(fillColor))
        {
            using var paint = new SKPaint
            {
                IsAntialias = true,
                Color = SKColor.TryParse(fillColor, out var c) ? c : SKColors.Transparent,
                Style = SKPaintStyle.Fill
            };
            
            var rect = new SKRect((float)bounds.X, (float)bounds.Y, (float)(bounds.X + bounds.Width), (float)(bounds.Y + bounds.Height));
            
            if (cornerRadius > 0)
            {
                _canvas.DrawRoundRect(rect, (float)cornerRadius, (float)cornerRadius, paint);
            }
            else
            {
                _canvas.DrawRect(rect, paint);
            }
        }
    }
    
    public void DrawRoundRect(Rect bounds, string fillColor, double cornerRadius)
    {
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color = SKColor.TryParse(fillColor, out var c) ? c : SKColors.Gray,
            Style = SKPaintStyle.Fill
        };
        
        var rect = new SKRect((float)bounds.X, (float)bounds.Y, (float)(bounds.X + bounds.Width), (float)(bounds.Y + bounds.Height));
        _canvas.DrawRoundRect(rect, (float)cornerRadius, (float)cornerRadius, paint);
    }
    
    public void DrawText(string text, double x, double y, double fontSize, string? fontFamily = null, string? fontWeight = null, string? color = null)
    {
        if (string.IsNullOrEmpty(text))
            return;
        
        using var font = new SKFont
        {
            Size = (float)fontSize,
            Edging = SKFontEdging.SubpixelAntialias,
            Subpixel = true
        };
        
        font.Typeface = GetTypeface(fontFamily, fontWeight);
        
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color = ParseColor(color, SKColors.Black)
        };
        
        _textRenderer.DrawText(_canvas, text, (float)x, (float)(y + font.Spacing), font, paint);
    }
    
    public double MeasureText(string text, double fontSize, string? fontFamily = null)
    {
        if (string.IsNullOrEmpty(text))
            return 0;
        
        using var font = new SKFont { Size = (float)fontSize };
        font.Typeface = GetTypeface(fontFamily, null);
        return _textRenderer.MeasureText(text, font);
    }
    
    private static SKTypeface GetTypeface(string? fontFamily, string? fontWeight)
    {
        if (!string.IsNullOrEmpty(fontFamily))
        {
            var weight = fontWeight == "Bold" ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal;
            var typeface = SKTypeface.FromFamilyName(fontFamily, weight, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
            if (typeface != null)
                return typeface;
        }
        
        return GetChineseTypeface();
    }
    
    private static SKTypeface GetChineseTypeface()
    {
        if (_chineseTypeface != null)
            return _chineseTypeface;
        
        var chineseFonts = new[] { 
            "Microsoft YaHei", "PingFang SC", "SimSun", 
            "Noto Sans CJK SC", "Segoe UI"
        };
        
        foreach (var fontName in chineseFonts)
        {
            var typeface = SKTypeface.FromFamilyName(fontName, 
                SKFontStyleWeight.Normal, 
                SKFontStyleWidth.Normal, 
                SKFontStyleSlant.Upright);
            if (typeface != null && typeface.CountGlyphs("中") > 0)
            {
                _chineseTypeface = typeface;
                return typeface;
            }
        }
        
        _chineseTypeface = SKTypeface.Default;
        return _chineseTypeface;
    }
    
    private static SKColor ParseColor(string? color, SKColor defaultColor)
    {
        if (string.IsNullOrEmpty(color))
            return defaultColor;
        return SKColor.TryParse(color, out var result) ? result : defaultColor;
    }
}