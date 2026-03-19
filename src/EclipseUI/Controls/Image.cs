using SkiaSharp;
using Microsoft.AspNetCore.Components;
using EclipseUI.Core;

namespace EclipseUI.Controls;

/// <summary>
/// 图片组件
/// </summary>
public class Image : ComponentBase, IElementHandler, IDisposable
{
    /// <summary>
    /// 图片路径
    /// </summary>
    [Parameter] public string? Source { get; set; }
    
    /// <summary>
    /// 图片拉伸模式
    /// </summary>
    [Parameter] public Stretch Stretch { get; set; } = Stretch.Uniform;
    
    [Parameter] public float? Width { get; set; }
    [Parameter] public float? Height { get; set; }
    [Parameter] public float? MinWidth { get; set; }
    [Parameter] public float? MinHeight { get; set; }
    [Parameter] public float? MaxWidth { get; set; }
    [Parameter] public float? MaxHeight { get; set; }
    
    [Parameter] public float MarginLeft { get; set; }
    [Parameter] public float MarginTop { get; set; }
    [Parameter] public float MarginRight { get; set; }
    [Parameter] public float MarginBottom { get; set; }
    
    [Parameter] public HorizontalAlignment HorizontalAlignment { get; set; } = HorizontalAlignment.Stretch;
    [Parameter] public VerticalAlignment VerticalAlignment { get; set; } = VerticalAlignment.Stretch;
    
    [Parameter] public EventCallback OnClick { get; set; }
    
    private ImageElement? _element;
    private bool _disposed;
    
    EclipseElement IElementHandler.Element
    {
        get
        {
            if (_element == null)
            {
                _element = new ImageElement();
                UpdateElementFromParameters();
            }
            return _element;
        }
    }
    
    protected override void OnInitialized()
    {
        base.OnInitialized();
        _ = ((IElementHandler)this).Element;
    }
    
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        UpdateElementFromParameters();
    }
    
    private void UpdateElementFromParameters()
    {
        if (_element == null) return;
        
        _element.Source = Source;
        _element.Stretch = Stretch;
        _element.RequestedWidth = Width;
        _element.RequestedHeight = Height;
        _element.MinWidth = MinWidth;
        _element.MinHeight = MinHeight;
        _element.MaxWidth = MaxWidth;
        _element.MaxHeight = MaxHeight;
        _element.MarginLeft = MarginLeft;
        _element.MarginTop = MarginTop;
        _element.MarginRight = MarginRight;
        _element.MarginBottom = MarginBottom;
        _element.HorizontalAlignment = HorizontalAlignment;
        _element.VerticalAlignment = VerticalAlignment;
        _element.OnClick = OnClick.HasDelegate ? async (e, p) => await OnClick.InvokeAsync() : null;
    }
    
    void IDisposable.Dispose()
    {
        if (!_disposed)
        {
            _element?.Dispose();
            _element = null;
            _disposed = true;
        }
    }
}

/// <summary>
/// 图片拉伸模式
/// </summary>
public enum Stretch
{
    /// <summary>
    /// 不拉伸，保持原始尺寸
    /// </summary>
    None,
    
    /// <summary>
    /// 填充整个区域，可能变形
    /// </summary>
    Fill,
    
    /// <summary>
    /// 保持比例，完整显示图片
    /// </summary>
    Uniform,
    
    /// <summary>
    /// 保持比例，填满区域（可能裁剪）
    /// </summary>
    UniformToFill
}

/// <summary>
/// 图片元素
/// </summary>
public class ImageElement : EclipseElement, IDisposable
{
    private string? _source;
    private SKBitmap? _bitmap;
    private bool _disposed;
    
    public string? Source
    {
        get => _source;
        set
        {
            if (_source != value)
            {
                _source = value;
                LoadImage();
            }
        }
    }
    
    public Stretch Stretch { get; set; } = Stretch.Uniform;
    
    private void LoadImage()
    {
        _bitmap?.Dispose();
        _bitmap = null;
        
        if (string.IsNullOrEmpty(_source)) return;
        
        try
        {
            if (File.Exists(_source))
            {
                using var stream = File.OpenRead(_source);
                _bitmap = SKBitmap.Decode(stream);
            }
        }
        catch
        {
            // 加载失败，忽略
        }
    }
    
    public override SKSize Measure(SKCanvas canvas, float availableWidth, float availableHeight)
    {
        float imageWidth = _bitmap?.Width ?? 100;
        float imageHeight = _bitmap?.Height ?? 100;
        
        float finalWidth = RequestedWidth ?? imageWidth;
        float finalHeight = RequestedHeight ?? imageHeight;
        
        // 如果只设置了一个维度，按比例计算另一个
        if (RequestedWidth.HasValue && !RequestedHeight.HasValue && _bitmap != null)
        {
            finalHeight = RequestedWidth.Value * imageHeight / imageWidth;
        }
        else if (!RequestedWidth.HasValue && RequestedHeight.HasValue && _bitmap != null)
        {
            finalWidth = RequestedHeight.Value * imageWidth / imageHeight;
        }
        
        if (MinWidth.HasValue) finalWidth = Math.Max(finalWidth, MinWidth.Value);
        if (MinHeight.HasValue) finalHeight = Math.Max(finalHeight, MinHeight.Value);
        if (MaxWidth.HasValue) finalWidth = Math.Min(finalWidth, MaxWidth.Value);
        if (MaxHeight.HasValue) finalHeight = Math.Min(finalHeight, MaxHeight.Value);
        
        return new SKSize(finalWidth, finalHeight);
    }
    
    public override void Render(SKCanvas canvas)
    {
        if (!IsVisible) return;
        
        // 绘制背景
        if (BackgroundColor.HasValue)
        {
            using var bgPaint = new SKPaint { Color = BackgroundColor.Value };
            canvas.DrawRect(X, Y, Width, Height, bgPaint);
        }
        
        if (_bitmap == null) return;
        
        var destRect = CalculateDestRect();
        
        using var paint = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High };
        canvas.DrawBitmap(_bitmap, destRect, paint);
    }
    
    private SKRect CalculateDestRect()
    {
        if (_bitmap == null) return new SKRect(X, Y, X + Width, Y + Height);
        
        float imageWidth = _bitmap.Width;
        float imageHeight = _bitmap.Height;
        
        switch (Stretch)
        {
            case Stretch.None:
                // 居中显示原始尺寸
                float x = X + (Width - imageWidth) / 2;
                float y = Y + (Height - imageHeight) / 2;
                return new SKRect(x, y, x + imageWidth, y + imageHeight);
                
            case Stretch.Fill:
                // 填充整个区域
                return new SKRect(X, Y, X + Width, Y + Height);
                
            case Stretch.Uniform:
                // 保持比例，完整显示
                float scale = Math.Min(Width / imageWidth, Height / imageHeight);
                float w = imageWidth * scale;
                float h = imageHeight * scale;
                return new SKRect(
                    X + (Width - w) / 2,
                    Y + (Height - h) / 2,
                    X + (Width + w) / 2,
                    Y + (Height + h) / 2
                );
                
            case Stretch.UniformToFill:
                // 保持比例，填满区域
                float scaleFill = Math.Max(Width / imageWidth, Height / imageHeight);
                float wFill = imageWidth * scaleFill;
                float hFill = imageHeight * scaleFill;
                return new SKRect(
                    X + (Width - wFill) / 2,
                    Y + (Height - hFill) / 2,
                    X + (Width + wFill) / 2,
                    Y + (Height + hFill) / 2
                );
                
            default:
                return new SKRect(X, Y, X + Width, Y + Height);
        }
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            _bitmap?.Dispose();
            _bitmap = null;
            _disposed = true;
        }
    }
}
