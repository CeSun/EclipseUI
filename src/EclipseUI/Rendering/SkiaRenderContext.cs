using SkiaSharp;

namespace EclipseUI.Rendering;

/// <summary>
/// SkiaSharp 渲染上下文实现
/// </summary>
public class SkiaRenderContext : IRenderContext
{
    private readonly SKCanvas _canvas;
    private readonly SKPaint _defaultPaint;
    
    public SkiaRenderContext(SKCanvas canvas)
    {
        _canvas = canvas;
        _defaultPaint = new SKPaint { IsAntialias = true };
    }
    
    public void Clear(Color color)
    {
        _canvas.Clear(new SKColor(color.R, color.G, color.B, color.A));
    }
    
    public void DrawRectangle(float x, float y, float width, float height, IBrush? brush = null, IPen? pen = null)
    {
        var rect = new SKRect(x, y, x + width, y + height);
        
        if (brush != null)
        {
            using var paint = CreatePaint(brush);
            _canvas.DrawRect(rect, paint);
        }
        
        if (pen != null)
        {
            using var paint = CreatePaint(pen);
            _canvas.DrawRect(rect, paint);
        }
    }
    
    public void DrawRoundedRectangle(float x, float y, float width, float height, float cornerRadius, IBrush? brush = null, IPen? pen = null)
    {
        var rect = new SKRect(x, y, x + width, y + height);
        using var path = new SKPath();
        path.AddRoundRect(rect, cornerRadius, cornerRadius);
        
        if (brush != null)
        {
            using var paint = CreatePaint(brush);
            _canvas.DrawPath(path, paint);
        }
        
        if (pen != null)
        {
            using var paint = CreatePaint(pen);
            _canvas.DrawPath(path, paint);
        }
    }
    
    public void DrawText(string text, float x, float y, IFont font, Color color)
    {
        var fontStyle = SKFontStyle.Normal;
        if (font.IsBold && font.IsItalic) fontStyle = SKFontStyle.BoldItalic;
        else if (font.IsBold) fontStyle = SKFontStyle.Bold;
        else if (font.IsItalic) fontStyle = SKFontStyle.Italic;
        
        using var typeface = SKTypeface.FromFamilyName(font.FamilyName, fontStyle);
        using var paint = new SKPaint
        {
            TextSize = font.Size,
            IsAntialias = true,
            Color = new SKColor(color.R, color.G, color.B, color.A),
            Typeface = typeface
        };
        
        _canvas.DrawText(text, x, y, paint);
    }
    
    public void DrawImage(IImage image, float x, float y, float? width = null, float? height = null)
    {
        if (image is SkiaImage skiaImage)
        {
            var destWidth = width ?? image.Width;
            var destHeight = height ?? image.Height;
            var dest = new SKRect(x, y, x + destWidth, y + destHeight);
            _canvas.DrawImage(skiaImage.Image, dest);
        }
    }
    
    public void Save()
    {
        _canvas.Save();
    }
    
    public void Restore()
    {
        _canvas.Restore();
    }
    
    public void Translate(float dx, float dy)
    {
        _canvas.Translate(dx, dy);
    }
    
    public void Rotate(float degrees)
    {
        _canvas.RotateDegrees(degrees);
    }
    
    public void Scale(float sx, float sy)
    {
        _canvas.Scale(sx, sy);
    }
    
    private SKPaint CreatePaint(IBrush brush)
    {
        return new SKPaint
        {
            Color = new SKColor(brush.Color.R, brush.Color.G, brush.Color.B, brush.Color.A),
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
    }
    
    private SKPaint CreatePaint(IPen pen)
    {
        return new SKPaint
        {
            Color = new SKColor(pen.Color.R, pen.Color.G, pen.Color.B, pen.Color.A),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = pen.StrokeWidth
        };
    }
}

/// <summary>
/// SkiaSharp 画刷实现
/// </summary>
public class SkiaBrush : IBrush
{
    public Color Color { get; set; }
    
    public SkiaBrush(Color color)
    {
        Color = color;
    }
    
    public static implicit operator SkiaBrush(SKColor color)
        => new SkiaBrush(new Color(color.Red, color.Green, color.Blue, color.Alpha));
}

/// <summary>
/// SkiaSharp 画笔实现
/// </summary>
public class SkiaPen : IPen
{
    public Color Color { get; set; }
    public float StrokeWidth { get; set; } = 1;
    
    public SkiaPen(Color color, float strokeWidth = 1)
    {
        Color = color;
        StrokeWidth = strokeWidth;
    }
}

/// <summary>
/// SkiaSharp 字体实现
/// </summary>
public class SkiaFont : IFont
{
    public string FamilyName { get; }
    public float Size { get; }
    public bool IsBold { get; }
    public bool IsItalic { get; }
    
    public SkiaFont(string familyName, float size, bool isBold = false, bool isItalic = false)
    {
        FamilyName = familyName;
        Size = size;
        IsBold = isBold;
        IsItalic = isItalic;
    }
}

/// <summary>
/// SkiaSharp 图片实现
/// </summary>
public class SkiaImage : IImage
{
    public SKImage Image { get; }
    public int Width => Image.Width;
    public int Height => Image.Height;
    
    public SkiaImage(SKImage image)
    {
        Image = image;
    }
}
