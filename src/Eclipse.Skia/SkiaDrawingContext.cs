using Eclipse.Core.Abstractions;
using Eclipse.Input;
using Eclipse.Rendering;
using Eclipse.Skia.Text;
using SkiaSharp;
using System;
using System.Collections.Concurrent;
using System.IO;

namespace Eclipse.Skia;

/// <summary>
/// Skia 绘制上下文实现
/// </summary>
public class SkiaDrawingContext : IDrawingContext
{
    private readonly SKCanvas _canvas;
    private static readonly HarfBuzzTextRenderer _textRenderer = new();
    private static SKTypeface? _chineseTypeface;
    
    private static readonly ConcurrentDictionary<string, SKImage> _imageCache = new();
    
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
    
    public void Clear(Color color)
    {
        _canvas.Clear(ToSKColor(color));
    }
    
    public void DrawRectangle(Rect bounds, Color fillColor, Color? strokeColor = null, double strokeWidth = 0, double cornerRadius = 0)
    {
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color = ToSKColor(fillColor),
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
        
        if (strokeColor.HasValue && strokeWidth > 0)
        {
            using var strokePaint = new SKPaint
            {
                IsAntialias = true,
                Color = ToSKColor(strokeColor.Value),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = (float)strokeWidth
            };
            
            if (cornerRadius > 0)
            {
                _canvas.DrawRoundRect(rect, (float)cornerRadius, (float)cornerRadius, strokePaint);
            }
            else
            {
                _canvas.DrawRect(rect, strokePaint);
            }
        }
    }
    
    public void DrawRoundRect(Rect bounds, Color fillColor, double cornerRadius)
    {
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color = ToSKColor(fillColor),
            Style = SKPaintStyle.Fill
        };
        
        var rect = new SKRect((float)bounds.X, (float)bounds.Y, (float)(bounds.X + bounds.Width), (float)(bounds.Y + bounds.Height));
        _canvas.DrawRoundRect(rect, (float)cornerRadius, (float)cornerRadius, paint);
    }
    
    public void DrawLine(double x1, double y1, double x2, double y2, Color color, double strokeWidth)
    {
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color = ToSKColor(color),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = (float)strokeWidth
        };
        
        _canvas.DrawLine((float)x1, (float)y1, (float)x2, (float)y2, paint);
    }
    
    public void DrawText(string text, double x, double y, double fontSize, string? fontFamily = null, string? fontWeight = null, Color color = default)
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
            Color = color != default ? ToSKColor(color) : SKColors.Black
        };
        
        var metrics = font.Metrics;
        var visualCenter = (metrics.Top + metrics.Bottom) / 2;
        var baseline = (float)(y - visualCenter);
        
        _textRenderer.DrawText(_canvas, text, (float)x, baseline, font, paint);
    }
    
    public double MeasureText(string text, double fontSize, string? fontFamily = null)
    {
        if (string.IsNullOrEmpty(text))
            return 0;
        
        using var font = new SKFont { Size = (float)fontSize };
        font.Typeface = GetTypeface(fontFamily, null);
        return _textRenderer.MeasureText(text, font);
    }
    
    public string? LoadImage(string source)
    {
        if (string.IsNullOrEmpty(source))
            return null;
        
        var cacheKey = source;
        
        if (_imageCache.ContainsKey(cacheKey))
            return cacheKey;
        
        try
        {
            SKImage? image = null;
            
            if (File.Exists(source))
            {
                using var stream = File.OpenRead(source);
                using var codec = SKCodec.Create(stream);
                if (codec != null)
                {
                    var info = new SKImageInfo(codec.Info.Width, codec.Info.Height);
                    using var bitmap = SKBitmap.Decode(codec, info);
                    if (bitmap != null)
                    {
                        image = SKImage.FromBitmap(bitmap);
                    }
                }
            }
            
            if (image != null)
            {
                _imageCache[cacheKey] = image;
                return cacheKey;
            }
        }
        catch
        {
            return null;
        }
        
        return null;
    }
    
    public Size GetImageSize(string imageKey)
    {
        if (string.IsNullOrEmpty(imageKey) || !_imageCache.TryGetValue(imageKey, out var image))
            return Size.Zero;
        
        return new Size(image.Width, image.Height);
    }
    
    public void DrawImage(string imageKey, Rect bounds, Stretch stretch = Stretch.Uniform)
    {
        if (string.IsNullOrEmpty(imageKey) || !_imageCache.TryGetValue(imageKey, out var image))
            return;
        
        var destRect = CalculateStretchRect(
            new Size(image.Width, image.Height),
            bounds,
            stretch);
        
        _canvas.DrawImage(image, destRect);
    }
    
    private static SKRect CalculateStretchRect(Size imageSize, Rect bounds, Stretch stretch)
    {
        var imageWidth = imageSize.Width;
        var imageHeight = imageSize.Height;
        var boundsWidth = bounds.Width;
        var boundsHeight = bounds.Height;
        
        if (imageWidth <= 0 || imageHeight <= 0 || boundsWidth <= 0 || boundsHeight <= 0)
        {
            return new SKRect((float)bounds.X, (float)bounds.Y, (float)(bounds.X + boundsWidth), (float)(bounds.Y + boundsHeight));
        }
        
        switch (stretch)
        {
            case Stretch.None:
                var x = bounds.X + (boundsWidth - imageWidth) / 2;
                var y = bounds.Y + (boundsHeight - imageHeight) / 2;
                return new SKRect((float)x, (float)y, (float)(x + imageWidth), (float)(y + imageHeight));
            
            case Stretch.Fill:
                return new SKRect((float)bounds.X, (float)bounds.Y, (float)(bounds.X + boundsWidth), (float)(bounds.Y + boundsHeight));
            
            case Stretch.Uniform:
                var uniformScale = Math.Min(boundsWidth / imageWidth, boundsHeight / imageHeight);
                var uniformWidth = imageWidth * uniformScale;
                var uniformHeight = imageHeight * uniformScale;
                var uniformX = bounds.X + (boundsWidth - uniformWidth) / 2;
                var uniformY = bounds.Y + (boundsHeight - uniformHeight) / 2;
                return new SKRect((float)uniformX, (float)uniformY, (float)(uniformX + uniformWidth), (float)(uniformY + uniformHeight));
            
            case Stretch.UniformToFill:
                var fillScale = Math.Max(boundsWidth / imageWidth, boundsHeight / imageHeight);
                var fillWidth = imageWidth * fillScale;
                var fillHeight = imageHeight * fillScale;
                var fillX = bounds.X + (boundsWidth - fillWidth) / 2;
                var fillY = bounds.Y + (boundsHeight - fillHeight) / 2;
                return new SKRect((float)fillX, (float)fillY, (float)(fillX + fillWidth), (float)(fillY + fillHeight));
            
            default:
                return new SKRect((float)bounds.X, (float)bounds.Y, (float)(bounds.X + boundsWidth), (float)(bounds.Y + boundsHeight));
        }
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
    
    /// <summary>
    /// 将 Color 转换为 SKColor
    /// </summary>
    private static SKColor ToSKColor(Color color)
    {
        return new SKColor(color.R, color.G, color.B, color.A);
    }
    
    /// <summary>
    /// 清除图片缓存
    /// </summary>
    public static void ClearImageCache()
    {
        foreach (var image in _imageCache.Values)
        {
            image.Dispose();
        }
        _imageCache.Clear();
    }

    /// <summary>
    /// 推入裁剪区域
    /// </summary>
    public void PushClip(Rect bounds)
    {
        var skRect = new SKRect((float)bounds.X, (float)bounds.Y, (float)(bounds.X + bounds.Width), (float)(bounds.Y + bounds.Height));
        _canvas.Save();
        _canvas.ClipRect(skRect);
    }

    /// <summary>
    /// 弹出裁剪区域
    /// </summary>
    public void PopClip()
    {
        _canvas.Restore();
    }
}