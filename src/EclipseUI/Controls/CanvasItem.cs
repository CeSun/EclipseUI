using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using EclipseUI.Core;
using EclipseUI.Layout;

namespace EclipseUI.Controls;

/// <summary>
/// Canvas 子项包装组件，用于设置附加属性
/// </summary>
public class CanvasItem : ComponentBase, IElementHandler, IDisposable
{
    [Parameter] public float? Left { get; set; }
    [Parameter] public float? Top { get; set; }
    [Parameter] public float? Right { get; set; }
    [Parameter] public float? Bottom { get; set; }
    
    [Parameter] public RenderFragment? ChildContent { get; set; }
    
    private CanvasItemElement? _element;
    private bool _disposed;
    
    EclipseElement IElementHandler.Element
    {
        get
        {
            if (_element == null)
            {
                _element = new CanvasItemElement();
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
        
        // 设置附加属性
        if (Left.HasValue)
            _element.SetValue(Canvas.LeftProperty, Left.Value);
        if (Top.HasValue)
            _element.SetValue(Canvas.TopProperty, Top.Value);
        if (Right.HasValue)
            _element.SetValue(Canvas.RightProperty, Right.Value);
        if (Bottom.HasValue)
            _element.SetValue(Canvas.BottomProperty, Bottom.Value);
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
/// Canvas 子项元素
/// </summary>
public class CanvasItemElement : EclipseElement
{
    public override SkiaSharp.SKSize Measure(SkiaSharp.SKCanvas canvas, float availableWidth, float availableHeight)
    {
        if (Children.Count > 0)
        {
            var childSize = Children[0].Measure(canvas, availableWidth, availableHeight);
            return childSize;
        }
        return new SkiaSharp.SKSize(0, 0);
    }
    
    public override void Arrange(SkiaSharp.SKCanvas canvas, float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
        
        if (Children.Count > 0)
        {
            Children[0].Arrange(canvas, x, y, width, height);
        }
    }
    
    public override void Render(SkiaSharp.SKCanvas canvas)
    {
        if (!IsVisible) return;
        RenderChildren(canvas);
    }
}
