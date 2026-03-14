using SkiaSharp;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using EclipseUI.Core;

namespace EclipseUI.Layout;

/// <summary>
/// DockPanel 面板组件 - 按停靠位置排列子元素
/// </summary>
public class DockPanel : ComponentBase, IElementHandler, IDisposable
{
    /// <summary>
    /// 最后一个子元素是否填充剩余空间
    /// </summary>
    [Parameter] public bool LastChildFill { get; set; } = true;
    
    [Parameter] public string? Background { get; set; }
    
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
    
    private DockPanelElement? _element;
    private bool _disposed;
    
    EclipseElement IElementHandler.Element
    {
        get
        {
            if (_element == null)
            {
                _element = new DockPanelElement
                {
                    LastChildFill = LastChildFill,
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
        
        _element.LastChildFill = LastChildFill;
        _element.MarginLeft = MarginLeft;
        _element.MarginTop = MarginTop;
        _element.MarginRight = MarginRight;
        _element.MarginBottom = MarginBottom;
        _element.PaddingLeft = PaddingLeft;
        _element.PaddingTop = PaddingTop;
        _element.PaddingRight = PaddingRight;
        _element.PaddingBottom = PaddingBottom;
        _element.BackgroundColor = ParseBackground(Background);
        
        _element.OnClick = OnClick.HasDelegate ? async (e, p) => await OnClick.InvokeAsync() : null;
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
