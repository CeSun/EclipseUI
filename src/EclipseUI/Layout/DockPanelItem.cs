using SkiaSharp;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using EclipseUI.Core;

namespace EclipseUI.Layout;

/// <summary>
/// DockPanel 子项组件 - 用于指定子元素的停靠位置
/// </summary>
public class DockPanelItem : ComponentBase, IElementHandler, IDisposable
{
    /// <summary>
    /// 停靠位置
    /// </summary>
    [Parameter] public Dock Dock { get; set; } = Dock.Fill;
    
    /// <summary>
    /// 背景颜色
    /// </summary>
    [Parameter] public string? Background { get; set; }
    
    /// <summary>
    /// 子内容
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }
    
    private DockPanelItemElement? _element;
    private bool _disposed;
    
    EclipseElement IElementHandler.Element
    {
        get
        {
            if (_element == null)
            {
                _element = new DockPanelItemElement
                {
                    Dock = Dock
                };
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
        
        if (_element != null)
        {
            _element.Dock = Dock;
            _element.BackgroundColor = ParseBackground(Background);
        }
    }
    
    private static SKColor? ParseBackground(string? color)
    {
        if (!string.IsNullOrEmpty(color) && color.StartsWith('#') && color.Length == 7)
            return SKColor.Parse(color);
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
/// DockPanel 子项元素
/// </summary>
public class DockPanelItemElement : EclipseElement
{
    /// <summary>
    /// 停靠位置
    /// </summary>
    public Dock Dock { get; set; } = Dock.Fill;
    
    public override SKSize Measure(SKCanvas canvas, float availableWidth, float availableHeight)
    {
        if (Children.Count == 0)
        {
            return new SKSize(0, 0);
        }
        
        // 测量子元素
        var child = Children[0];
        return child.Measure(canvas, availableWidth, availableHeight);
    }
    
    public override void Arrange(SKCanvas canvas, float x, float y, float width, float height)
    {
        base.Arrange(canvas, x, y, width, height);
        
        // 将子元素排列到相同区域
        if (Children.Count > 0)
        {
            Children[0].Arrange(canvas, x, y, width, height);
        }
    }
    
    public override void Render(SKCanvas canvas)
    {
        if (!IsVisible) return;
        
        canvas.Save();
        
        try
        {
            // 绘制背景
            if (BackgroundColor.HasValue)
            {
                var rect = new SKRect(X, Y, X + Width, Y + Height);
                using var bgPaint = new SKPaint { Color = BackgroundColor.Value, IsAntialias = true };
                canvas.DrawRect(rect, bgPaint);
            }
            
            // 设置裁剪区域，防止子元素超出边界
            var clipRect = new SKRect(X, Y, X + Width, Y + Height);
            canvas.ClipRect(clipRect);
            
            // 渲染子元素
            if (Children.Count > 0)
            {
                Children[0].Render(canvas);
            }
        }
        finally
        {
            canvas.Restore();
        }
    }
}
