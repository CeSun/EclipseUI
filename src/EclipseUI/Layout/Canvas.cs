using SkiaSharp;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using EclipseUI.Core;

namespace EclipseUI.Layout;

/// <summary>
/// 绝对定位画布组件
/// </summary>
public class Canvas : ComponentBase, IElementHandler, IDisposable
{
    [Parameter] public string? Background { get; set; }
    
    [Parameter] public float MarginLeft { get; set; }
    [Parameter] public float MarginTop { get; set; }
    [Parameter] public float MarginRight { get; set; }
    [Parameter] public float MarginBottom { get; set; }
    
    [Parameter] public float? Width { get; set; }
    [Parameter] public float? Height { get; set; }
    [Parameter] public float? MinWidth { get; set; }
    [Parameter] public float? MinHeight { get; set; }
    [Parameter] public float? MaxWidth { get; set; }
    [Parameter] public float? MaxHeight { get; set; }
    [Parameter] public HorizontalAlignment HorizontalAlignment { get; set; } = HorizontalAlignment.Stretch;
    [Parameter] public VerticalAlignment VerticalAlignment { get; set; } = VerticalAlignment.Stretch;
    
    [Parameter] public RenderFragment? ChildContent { get; set; }
    
    private CanvasElement? _element;
    private bool _disposed;
    
    EclipseElement IElementHandler.Element
    {
        get
        {
            if (_element == null)
            {
                _element = new CanvasElement();
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
        
        _element.MarginLeft = MarginLeft;
        _element.MarginTop = MarginTop;
        _element.MarginRight = MarginRight;
        _element.MarginBottom = MarginBottom;
        _element.BackgroundColor = ParseBackground(Background);
        _element.RequestedWidth = Width;
        _element.RequestedHeight = Height;
        _element.MinWidth = MinWidth;
        _element.MinHeight = MinHeight;
        _element.MaxWidth = MaxWidth;
        _element.MaxHeight = MaxHeight;
        _element.HorizontalAlignment = HorizontalAlignment;
        _element.VerticalAlignment = VerticalAlignment;
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
    
    // 附加属性 Key
    public static readonly int LeftProperty = 1001;
    public static readonly int TopProperty = 1002;
    public static readonly int RightProperty = 1003;
    public static readonly int BottomProperty = 1004;
}
