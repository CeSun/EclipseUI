using SkiaSharp;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using EclipseUI.Core;

namespace EclipseUI.Layout;

/// <summary>
/// 流式布局面板组件
/// </summary>
public class WrapPanel : ComponentBase, IElementHandler, IDisposable
{
    [Parameter] public StackOrientation Orientation { get; set; } = StackOrientation.Horizontal;
    [Parameter] public float ItemSpacing { get; set; } = 8;
    [Parameter] public float LineSpacing { get; set; } = 8;
    [Parameter] public string? Background { get; set; }
    
    [Parameter] public float MarginLeft { get; set; }
    [Parameter] public float MarginTop { get; set; }
    [Parameter] public float MarginRight { get; set; }
    [Parameter] public float MarginBottom { get; set; }
    
    [Parameter] public float PaddingLeft { get; set; }
    [Parameter] public float PaddingTop { get; set; }
    [Parameter] public float PaddingRight { get; set; }
    [Parameter] public float PaddingBottom { get; set; }
    
    [Parameter] public float? Width { get; set; }
    [Parameter] public float? Height { get; set; }
    [Parameter] public float? MinWidth { get; set; }
    [Parameter] public float? MinHeight { get; set; }
    [Parameter] public float? MaxWidth { get; set; }
    [Parameter] public float? MaxHeight { get; set; }
    [Parameter] public HorizontalAlignment HorizontalAlignment { get; set; } = HorizontalAlignment.Stretch;
    [Parameter] public VerticalAlignment VerticalAlignment { get; set; } = VerticalAlignment.Stretch;
    
    [Parameter] public RenderFragment? ChildContent { get; set; }
    
    private WrapPanelElement? _element;
    private bool _disposed;
    
    EclipseElement IElementHandler.Element
    {
        get
        {
            if (_element == null)
            {
                _element = new WrapPanelElement();
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
        
        _element.Orientation = Orientation;
        _element.ItemSpacing = ItemSpacing;
        _element.LineSpacing = LineSpacing;
        _element.MarginLeft = MarginLeft;
        _element.MarginTop = MarginTop;
        _element.MarginRight = MarginRight;
        _element.MarginBottom = MarginBottom;
        _element.PaddingLeft = PaddingLeft;
        _element.PaddingTop = PaddingTop;
        _element.PaddingRight = PaddingRight;
        _element.PaddingBottom = PaddingBottom;
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
}
