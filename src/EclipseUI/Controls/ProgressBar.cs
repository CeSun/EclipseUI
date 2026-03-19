using SkiaSharp;
using Microsoft.AspNetCore.Components;
using EclipseUI.Core;

namespace EclipseUI.Controls;

/// <summary>
/// 进度条组件
/// </summary>
public class ProgressBar : ComponentBase, IElementHandler, IDisposable
{
    /// <summary>
    /// 当前值
    /// </summary>
    [Parameter] public double Value { get; set; } = 0;
    
    /// <summary>
    /// 最小值
    /// </summary>
    [Parameter] public double Minimum { get; set; } = 0;
    
    /// <summary>
    /// 最大值
    /// </summary>
    [Parameter] public double Maximum { get; set; } = 100;
    
    /// <summary>
    /// 是否为不确定状态（循环动画）
    /// </summary>
    [Parameter] public bool IsIndeterminate { get; set; } = false;
    
    /// <summary>
    /// 进度条颜色
    /// </summary>
    [Parameter] public string? Foreground { get; set; }
    
    /// <summary>
    /// 轨道颜色
    /// </summary>
    [Parameter] public string? Background { get; set; }
    
    /// <summary>
    /// 圆角半径
    /// </summary>
    [Parameter] public float CornerRadius { get; set; } = 4;
    
    [Parameter] public float? Width { get; set; }
    [Parameter] public float? Height { get; set; } = 8;
    [Parameter] public float? MinWidth { get; set; } = 100;
    [Parameter] public float? MinHeight { get; set; }
    [Parameter] public float? MaxWidth { get; set; }
    [Parameter] public float? MaxHeight { get; set; }
    
    [Parameter] public float MarginLeft { get; set; }
    [Parameter] public float MarginTop { get; set; }
    [Parameter] public float MarginRight { get; set; }
    [Parameter] public float MarginBottom { get; set; }
    
    [Parameter] public HorizontalAlignment HorizontalAlignment { get; set; } = HorizontalAlignment.Stretch;
    [Parameter] public VerticalAlignment VerticalAlignment { get; set; } = VerticalAlignment.Center;
    
    private ProgressBarElement? _element;
    private bool _disposed;
    
    EclipseElement IElementHandler.Element
    {
        get
        {
            if (_element == null)
            {
                _element = new ProgressBarElement();
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
        
        _element.Value = Value;
        _element.Minimum = Minimum;
        _element.Maximum = Maximum;
        _element.IsIndeterminate = IsIndeterminate;
        _element.ForegroundColor = ParseColor(Foreground) ?? iOSTheme.SystemBlue;
        _element.TrackColor = ParseColor(Background) ?? iOSTheme.SystemGray5;
        _element.CornerRadius = CornerRadius;
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
    }
    
    private static SKColor? ParseColor(string? color)
    {
        if (!string.IsNullOrEmpty(color) && color.StartsWith('#') && color.Length == 7)
            return SKColor.Parse(color);
        return null;
    }
    
    void IDisposable.Dispose()
    {
        if (!_disposed)
        {
            _element = null;
            _disposed = true;
        }
    }
}

/// <summary>
/// 进度条元素 - iOS 风格
/// </summary>
public class ProgressBarElement : EclipseElement
{
    public double Value { get; set; } = 0;
    public double Minimum { get; set; } = 0;
    public double Maximum { get; set; } = 100;
    public bool IsIndeterminate { get; set; } = false;
    public SKColor ForegroundColor { get; set; } = iOSTheme.SystemBlue;
    public SKColor TrackColor { get; set; } = iOSTheme.SystemGray5;
    public float CornerRadius { get; set; } = 4;
    
    // 不确定状态动画
    private float _indeterminateOffset = 0;
    private DateTime _lastRenderTime = DateTime.Now;
    
    public override SKSize Measure(SKCanvas canvas, float availableWidth, float availableHeight)
    {
        float finalWidth = RequestedWidth ?? availableWidth;
        float finalHeight = RequestedHeight ?? 8;
        
        if (MinWidth.HasValue) finalWidth = Math.Max(finalWidth, MinWidth.Value);
        if (MinHeight.HasValue) finalHeight = Math.Max(finalHeight, MinHeight.Value);
        if (MaxWidth.HasValue) finalWidth = Math.Min(finalWidth, MaxWidth.Value);
        if (MaxHeight.HasValue) finalHeight = Math.Min(finalHeight, MaxHeight.Value);
        
        return new SKSize(finalWidth, finalHeight);
    }
    
    public override void Render(SKCanvas canvas)
    {
        if (!IsVisible) return;
        
        var rect = new SKRect(X, Y, X + Width, Y + Height);
        
        // 绘制轨道背景
        using var trackPaint = new SKPaint { Color = TrackColor, IsAntialias = true };
        canvas.DrawRoundRect(rect, CornerRadius, CornerRadius, trackPaint);
        
        if (IsIndeterminate)
        {
            RenderIndeterminate(canvas);
        }
        else
        {
            RenderDeterminate(canvas);
        }
    }
    
    private void RenderDeterminate(SKCanvas canvas)
    {
        double progress = (Value - Minimum) / (Maximum - Minimum);
        progress = Math.Max(0, Math.Min(1, progress));
        
        if (progress <= 0) return;
        
        float progressWidth = (float)(Width * progress);
        if (progressWidth < CornerRadius * 2) progressWidth = CornerRadius * 2;
        
        var progressRect = new SKRect(X, Y, X + progressWidth, Y + Height);
        
        using var progressPaint = new SKPaint { Color = ForegroundColor, IsAntialias = true };
        canvas.DrawRoundRect(progressRect, CornerRadius, CornerRadius, progressPaint);
    }
    
    private void RenderIndeterminate(SKCanvas canvas)
    {
        // 更新动画
        var now = DateTime.Now;
        var elapsed = (float)(now - _lastRenderTime).TotalSeconds;
        _lastRenderTime = now;
        
        _indeterminateOffset += elapsed * 200; // 每秒移动 200 像素
        if (_indeterminateOffset > Width + Width * 0.3f)
            _indeterminateOffset = -Width * 0.3f;
        
        // 绘制移动的进度块
        float blockWidth = Width * 0.3f;
        float blockX = X + _indeterminateOffset;
        
        canvas.Save();
        canvas.ClipRoundRect(new SKRoundRect(new SKRect(X, Y, X + Width, Y + Height), CornerRadius));
        
        var blockRect = new SKRect(blockX, Y, blockX + blockWidth, Y + Height);
        using var blockPaint = new SKPaint { Color = ForegroundColor, IsAntialias = true };
        canvas.DrawRoundRect(blockRect, CornerRadius, CornerRadius, blockPaint);
        
        canvas.Restore();
    }
}
