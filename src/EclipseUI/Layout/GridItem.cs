using SkiaSharp;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using EclipseUI.Core;

namespace EclipseUI.Layout;

/// <summary>
/// Grid 子项组件 - 用于指定子元素的行列位置
/// </summary>
public class GridItem : ComponentBase, IElementHandler, IDisposable
{
    /// <summary>
    /// 行索引
    /// </summary>
    [Parameter] public int Row { get; set; }
    
    /// <summary>
    /// 列索引
    /// </summary>
    [Parameter] public int Column { get; set; }
    
    /// <summary>
    /// 跨行数
    /// </summary>
    [Parameter] public int RowSpan { get; set; } = 1;
    
    /// <summary>
    /// 跨列数
    /// </summary>
    [Parameter] public int ColumnSpan { get; set; } = 1;
    
    /// <summary>
    /// 背景颜色
    /// </summary>
    [Parameter] public string? Background { get; set; }
    
    /// <summary>
    /// 子内容
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }
    
    private GridItemElement? _element;
    private bool _disposed;
    
    EclipseElement IElementHandler.Element
    {
        get
        {
            if (_element == null)
            {
                _element = new GridItemElement();
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
            GridElement.SetRow(_element, Row);
            GridElement.SetColumn(_element, Column);
            GridElement.SetRowSpan(_element, RowSpan);
            GridElement.SetColumnSpan(_element, ColumnSpan);
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
