using SkiaSharp;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using EclipseUI.Core;

namespace EclipseUI.Layout;

/// <summary>
/// 网格布局面板 - 按行列网格排列子元素
/// </summary>
public class Grid : ComponentBase, IElementHandler, IDisposable
{
    [Parameter] public EventCallback OnClick { get; set; }
    
    [Parameter] public float MarginLeft { get; set; }
    [Parameter] public float MarginTop { get; set; }
    [Parameter] public float MarginRight { get; set; }
    [Parameter] public float MarginBottom { get; set; }
    
    [Parameter] public float PaddingLeft { get; set; }
    [Parameter] public float PaddingTop { get; set; }
    [Parameter] public float PaddingRight { get; set; }
    [Parameter] public float PaddingBottom { get; set; }
    
    /// <summary>
    /// 行定义，逗号分隔，支持：Auto, 像素值，* 比例
    /// 示例："Auto, *, 2*" 或 "100, *, Auto"
    /// </summary>
    [Parameter] public string? RowDefinitions { get; set; }
    
    /// <summary>
    /// 列定义，逗号分隔，支持：Auto, 像素值，* 比例
    /// 示例："100, *, 2*" 或 "Auto, *, *"
    /// </summary>
    [Parameter] public string? ColumnDefinitions { get; set; }
    
    [Parameter] public float Spacing { get; set; }
    
    [Parameter] public RenderFragment? ChildContent { get; set; }
    
    [Parameter] public float? Width { get; set; }
    [Parameter] public float? Height { get; set; }
    [Parameter] public float? MinWidth { get; set; }
    [Parameter] public float? MinHeight { get; set; }
    [Parameter] public float? MaxWidth { get; set; }
    [Parameter] public float? MaxHeight { get; set; }
    [Parameter] public HorizontalAlignment HorizontalAlignment { get; set; } = HorizontalAlignment.Stretch;
    [Parameter] public VerticalAlignment VerticalAlignment { get; set; } = VerticalAlignment.Stretch;
    
    [Parameter] public string? Background { get; set; }
    
    private GridElement? _element;
    private bool _disposed;
    
    EclipseElement IElementHandler.Element
    {
        get
        {
            if (_element == null)
            {
                _element = new GridElement
                {
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
        
        _element.MarginLeft = MarginLeft;
        _element.MarginTop = MarginTop;
        _element.MarginRight = MarginRight;
        _element.MarginBottom = MarginBottom;
        _element.PaddingLeft = PaddingLeft;
        _element.PaddingTop = PaddingTop;
        _element.PaddingRight = PaddingRight;
        _element.PaddingBottom = PaddingBottom;
        _element.Spacing = Spacing;
        _element.RequestedWidth = Width;
        _element.RequestedHeight = Height;
        _element.MinWidth = MinWidth;
        _element.MinHeight = MinHeight;
        _element.MaxWidth = MaxWidth;
        _element.MaxHeight = MaxHeight;
        _element.HorizontalAlignment = HorizontalAlignment;
        _element.VerticalAlignment = VerticalAlignment;
        _element.BackgroundColor = ParseBackground(Background);
        
        // 解析行定义
        _element.RowDefinitions.Clear();
        if (!string.IsNullOrEmpty(RowDefinitions))
        {
            foreach (var rowDef in RowDefinitions.Split(','))
            {
                _element.RowDefinitions.Add(new RowDefinitionInternal { Height = ParseGridLength(rowDef.Trim()) });
            }
        }
        
        // 解析列定义
        _element.ColumnDefinitions.Clear();
        if (!string.IsNullOrEmpty(ColumnDefinitions))
        {
            foreach (var colDef in ColumnDefinitions.Split(','))
            {
                _element.ColumnDefinitions.Add(new ColumnDefinitionInternal { Width = ParseGridLength(colDef.Trim()) });
            }
        }
        
        _element.OnClick = OnClick.HasDelegate ? async (e, p) => await OnClick.InvokeAsync() : null;
    }
    
    private static GridLength ParseGridLength(string value)
    {
        value = value.Trim();
        
        if (value.Equals("Auto", StringComparison.OrdinalIgnoreCase))
            return GridLength.Auto;
        
        if (value.EndsWith("*", StringComparison.OrdinalIgnoreCase))
        {
            if (value == "*")
                return GridLength.Star;
            
            if (double.TryParse(value.TrimEnd('*'), out var starValue))
                return new GridLength(starValue, GridUnitType.Star);
        }
        
        if (double.TryParse(value, out var pixelValue))
            return GridLength.Pixel(pixelValue);
        
        return GridLength.Star;
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