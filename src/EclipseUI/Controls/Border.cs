using SkiaSharp;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using EclipseUI.Core;

namespace EclipseUI.Controls;

/// <summary>
/// 边框容器组件
/// </summary>
public class Border : ComponentBase, IElementHandler, IDisposable
{
    [Parameter] public string? Background { get; set; }
    [Parameter] public string? BorderBrush { get; set; }
    [Parameter] public float BorderThickness { get; set; } = 0;
    [Parameter] public float CornerRadius { get; set; } = 0;
    
    [Parameter] public float? Width { get; set; }
    [Parameter] public float? Height { get; set; }
    [Parameter] public float? MinWidth { get; set; }
    [Parameter] public float? MinHeight { get; set; }
    [Parameter] public float? MaxWidth { get; set; }
    [Parameter] public float? MaxHeight { get; set; }
    
    [Parameter] public float PaddingLeft { get; set; }
    [Parameter] public float PaddingTop { get; set; }
    [Parameter] public float PaddingRight { get; set; }
    [Parameter] public float PaddingBottom { get; set; }
    
    [Parameter] public float MarginLeft { get; set; }
    [Parameter] public float MarginTop { get; set; }
    [Parameter] public float MarginRight { get; set; }
    [Parameter] public float MarginBottom { get; set; }
    
    [Parameter] public HorizontalAlignment HorizontalAlignment { get; set; } = HorizontalAlignment.Stretch;
    [Parameter] public VerticalAlignment VerticalAlignment { get; set; } = VerticalAlignment.Stretch;
    
    [Parameter] public RenderFragment? ChildContent { get; set; }
    
    private BorderElement? _element;
    private bool _disposed;
    
    EclipseElement IElementHandler.Element
    {
        get
        {
            if (_element == null)
            {
                _element = new BorderElement();
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
        
        _element.BackgroundColor = ParseColor(Background);
        _element.BorderColor = ParseColor(BorderBrush) ?? SKColors.Transparent;
        _element.BorderThickness = BorderThickness;
        _element.CornerRadius = CornerRadius;
        _element.RequestedWidth = Width;
        _element.RequestedHeight = Height;
        _element.MinWidth = MinWidth;
        _element.MinHeight = MinHeight;
        _element.MaxWidth = MaxWidth;
        _element.MaxHeight = MaxHeight;
        _element.PaddingLeft = PaddingLeft;
        _element.PaddingTop = PaddingTop;
        _element.PaddingRight = PaddingRight;
        _element.PaddingBottom = PaddingBottom;
        _element.MarginLeft = MarginLeft;
        _element.MarginTop = MarginTop;
        _element.MarginRight = MarginRight;
        _element.MarginBottom = MarginBottom;
        _element.HorizontalAlignment = HorizontalAlignment;
        _element.VerticalAlignment = VerticalAlignment;
    }
    
    private static SKColor? ParseColor(string? color)
    {
        if (!string.IsNullOrEmpty(color) && color.StartsWith('#'))
        {
            if (color.Length == 7) return SKColor.Parse(color);
            if (color.Length == 9) return SKColor.Parse(color);
        }
        return null;
    }
    
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.AddContent(0, ChildContent);
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
/// 边框容器元素
/// </summary>
public class BorderElement : EclipseElement
{
    public SKColor BorderColor { get; set; } = SKColors.Transparent;
    public float BorderThickness { get; set; } = 0;
    public float CornerRadius { get; set; } = 0;
    
    public override SKSize Measure(SKCanvas canvas, float availableWidth, float availableHeight)
    {
        float contentWidth = availableWidth - PaddingLeft - PaddingRight - BorderThickness * 2;
        float contentHeight = availableHeight - PaddingTop - PaddingBottom - BorderThickness * 2;
        
        float childWidth = 0, childHeight = 0;
        
        if (Children.Count > 0)
        {
            var childSize = Children[0].Measure(canvas, contentWidth, contentHeight);
            childWidth = childSize.Width;
            childHeight = childSize.Height;
        }
        
        float finalWidth = RequestedWidth ?? (childWidth + PaddingLeft + PaddingRight + BorderThickness * 2);
        float finalHeight = RequestedHeight ?? (childHeight + PaddingTop + PaddingBottom + BorderThickness * 2);
        
        if (MinWidth.HasValue) finalWidth = Math.Max(finalWidth, MinWidth.Value);
        if (MinHeight.HasValue) finalHeight = Math.Max(finalHeight, MinHeight.Value);
        if (MaxWidth.HasValue) finalWidth = Math.Min(finalWidth, MaxWidth.Value);
        if (MaxHeight.HasValue) finalHeight = Math.Min(finalHeight, MaxHeight.Value);
        
        return new SKSize(finalWidth, finalHeight);
    }
    
    public override void Arrange(SKCanvas canvas, float x, float y, float width, float height)
    {
        base.Arrange(canvas, x, y, width, height);
        
        if (Children.Count > 0)
        {
            float childX = x + PaddingLeft + BorderThickness;
            float childY = y + PaddingTop + BorderThickness;
            float childWidth = width - PaddingLeft - PaddingRight - BorderThickness * 2;
            float childHeight = height - PaddingTop - PaddingBottom - BorderThickness * 2;
            
            var childSize = Children[0].Measure(canvas, childWidth, childHeight);
            Children[0].Arrange(canvas, childX, childY, childSize.Width, childSize.Height);
        }
    }
    
    public override void Render(SKCanvas canvas)
    {
        if (!IsVisible) return;
        
        var rect = new SKRect(X, Y, X + Width, Y + Height);
        
        // 绘制背景
        if (BackgroundColor.HasValue)
        {
            using var bgPaint = new SKPaint { Color = BackgroundColor.Value, IsAntialias = true };
            if (CornerRadius > 0)
                canvas.DrawRoundRect(rect, CornerRadius, CornerRadius, bgPaint);
            else
                canvas.DrawRect(rect, bgPaint);
        }
        
        // 绘制边框
        if (BorderThickness > 0 && BorderColor != SKColors.Transparent)
        {
            using var borderPaint = new SKPaint
            {
                Color = BorderColor,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = BorderThickness
            };
            
            var borderRect = new SKRect(
                X + BorderThickness / 2,
                Y + BorderThickness / 2,
                X + Width - BorderThickness / 2,
                Y + Height - BorderThickness / 2
            );
            
            if (CornerRadius > 0)
                canvas.DrawRoundRect(borderRect, CornerRadius, CornerRadius, borderPaint);
            else
                canvas.DrawRect(borderRect, borderPaint);
        }
        
        // 渲染子元素
        RenderChildren(canvas);
    }
}
