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
    
    // 图片缓存
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
        
        if (!string.IsNullOrEmpty(strokeColor) && strokeWidth > 0)
        {
            using var strokePaint = new SKPaint
            {
                IsAntialias = true,
                Color = SKColor.TryParse(strokeColor, out var c) ? c : SKColors.Transparent,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = (float)strokeWidth
            };
            
            var rect = new SKRect((float)bounds.X, (float)bounds.Y, (float)(bounds.X + bounds.Width), (float)(bounds.Y + bounds.Height));
            
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
    
    public void DrawLine(double x1, double y1, double x2, double y2, string color, double strokeWidth)
    {
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color = SKColor.TryParse(color, out var c) ? c : SKColors.Black,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = (float)strokeWidth
        };
        
        _canvas.DrawLine((float)x1, (float)y1, (float)x2, (float)y2, paint);
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
        
        // 获取字体度量信息
        var metrics = font.Metrics;
        
        // y 参数表示文本视觉中心
        // 视觉中心 = (Top + Bottom) / 2，Top 是负数
        // 基线位置 = y - 视觉中心
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
    
    /// <summary>
    /// 加载图片并返回缓存键
    /// </summary>
    public string? LoadImage(string source)
    {
        if (string.IsNullOrEmpty(source))
            return null;
        
        // 使用路径作为缓存键
        var cacheKey = source;
        
        // 如果已缓存，直接返回
        if (_imageCache.ContainsKey(cacheKey))
            return cacheKey;
        
        // 尝试加载图片
        try
        {
            SKImage? image = null;
            
            // 检查是否是文件路径
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
            // 检查是否是 HTTP URL（未来扩展）
            else if (source.StartsWith("http://") || source.StartsWith("https://"))
            {
                // 暂不支持 HTTP 加载
                return null;
            }
            // 检查是否是资源路径（未来扩展）
            else if (source.StartsWith("res://"))
            {
                // 暂不支持资源加载
                return null;
            }
            
            if (image != null)
            {
                _imageCache[cacheKey] = image;
                return cacheKey;
            }
        }
        catch (Exception)
        {
            // 加载失败
            return null;
        }
        
        return null;
    }
    
    /// <summary>
    /// 获取图片原始尺寸
    /// </summary>
    public Size GetImageSize(string imageKey)
    {
        if (string.IsNullOrEmpty(imageKey) || !_imageCache.TryGetValue(imageKey, out var image))
            return Size.Zero;
        
        return new Size(image.Width, image.Height);
    }
    
    /// <summary>
    /// 绘制图片
    /// </summary>
    public void DrawImage(string imageKey, Rect bounds, Stretch stretch = Stretch.Uniform)
    {
        if (string.IsNullOrEmpty(imageKey) || !_imageCache.TryGetValue(imageKey, out var image))
            return;
        
        // 计算绘制区域
        var destRect = CalculateStretchRect(
            new Size(image.Width, image.Height),
            bounds,
            stretch);
        
        // 绘制图片
        _canvas.DrawImage(image, destRect);
    }
    
    /// <summary>
    /// 根据拉伸模式计算绘制区域
    /// </summary>
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
                // 不拉伸，居中显示
                var x = bounds.X + (boundsWidth - imageWidth) / 2;
                var y = bounds.Y + (boundsHeight - imageHeight) / 2;
                return new SKRect((float)x, (float)y, (float)(x + imageWidth), (float)(y + imageHeight));
            
            case Stretch.Fill:
                // 填充整个区域，可能改变比例
                return new SKRect((float)bounds.X, (float)bounds.Y, (float)(bounds.X + boundsWidth), (float)(bounds.Y + boundsHeight));
            
            case Stretch.Uniform:
                // 保持比例，适应区域（可能留空白）
                var uniformScale = Math.Min(boundsWidth / imageWidth, boundsHeight / imageHeight);
                var uniformWidth = imageWidth * uniformScale;
                var uniformHeight = imageHeight * uniformScale;
                var uniformX = bounds.X + (boundsWidth - uniformWidth) / 2;
                var uniformY = bounds.Y + (boundsHeight - uniformHeight) / 2;
                return new SKRect((float)uniformX, (float)uniformY, (float)(uniformX + uniformWidth), (float)(uniformY + uniformHeight));
            
            case Stretch.UniformToFill:
                // 保持比例，填充区域（可能裁剪）
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
    
    private static SKColor ParseColor(string? color, SKColor defaultColor)
    {
        if (string.IsNullOrEmpty(color))
            return defaultColor;
        return SKColor.TryParse(color, out var result) ? result : defaultColor;
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
}