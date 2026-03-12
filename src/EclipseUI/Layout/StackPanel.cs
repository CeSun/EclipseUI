using SkiaSharp;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using EclipseUI.Core;

namespace EclipseUI.Layout;

/// <summary>
/// 堆叠面板组件 - 纯 C# 实现
/// </summary>
public class StackPanel : ComponentBase, IElementHandler, IDisposable
{
    [Parameter] public StackOrientation Orientation { get; set; } = StackOrientation.Vertical;
    [Parameter] public float Spacing { get; set; }
    [Parameter] public EventCallback OnClick { get; set; }
    
    [Parameter] public float MarginLeft { get; set; }
    [Parameter] public float MarginTop { get; set; }
    [Parameter] public float MarginRight { get; set; }
    [Parameter] public float MarginBottom { get; set; }
    
    [Parameter] public float PaddingLeft { get; set; }
    [Parameter] public float PaddingTop { get; set; }
    [Parameter] public float PaddingRight { get; set; }
    [Parameter] public float PaddingBottom { get; set; }
    
    [Parameter] public RenderFragment? ChildContent { get; set; }
    
    private StackPanelElement? _element;
    private bool _disposed;
    
    EclipseElement IElementHandler.Element
    {
        get
        {
            if (_element == null)
            {
                _element = new StackPanelElement
                {
                    Orientation = Orientation,
                    Spacing = Spacing,
                    MarginLeft = MarginLeft,
                    MarginTop = MarginTop,
                    MarginRight = MarginRight,
                    MarginBottom = MarginBottom,
                    PaddingLeft = PaddingLeft,
                    PaddingTop = PaddingTop,
                    PaddingRight = PaddingRight,
                    PaddingBottom = PaddingBottom
                };
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
        _element.Spacing = Spacing;
        _element.MarginLeft = MarginLeft;
        _element.MarginTop = MarginTop;
        _element.MarginRight = MarginRight;
        _element.MarginBottom = MarginBottom;
        _element.PaddingLeft = PaddingLeft;
        _element.PaddingTop = PaddingTop;
        _element.PaddingRight = PaddingRight;
        _element.PaddingBottom = PaddingBottom;
        
        _element.OnClick = OnClick.HasDelegate ? async (e, p) => await OnClick.InvokeAsync() : null;
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
