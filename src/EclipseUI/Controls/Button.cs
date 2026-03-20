using SkiaSharp;
using Microsoft.AspNetCore.Components;
using EclipseUI.Core;
using EclipseUI.Styling;

namespace EclipseUI.Controls;

/// <summary>
/// 按钮组件 - 纯 C# 实现
/// </summary>
public class Button : ComponentBase, IElementHandler, IDisposable
{
    [Parameter] public string? Text { get; set; }
    [Parameter] public float FontSize { get; set; } = 14;
    [Parameter] public string? Background { get; set; }
    [Parameter] public string? Foreground { get; set; }
    [Parameter] public float CornerRadius { get; set; } = 4;
    [Parameter] public float? Width { get; set; }
    [Parameter] public float? Height { get; set; }
    [Parameter] public float? MinWidth { get; set; }
    [Parameter] public float? MinHeight { get; set; }
    [Parameter] public float? MaxWidth { get; set; }
    [Parameter] public float? MaxHeight { get; set; }
    [Parameter] public HorizontalAlignment HorizontalAlignment { get; set; } = HorizontalAlignment.Stretch;
    [Parameter] public VerticalAlignment VerticalAlignment { get; set; } = VerticalAlignment.Stretch;
    
    [Parameter] public float PaddingLeft { get; set; }
    [Parameter] public float PaddingTop { get; set; }
    [Parameter] public float PaddingRight { get; set; }
    [Parameter] public float PaddingBottom { get; set; }
    
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }
    [Parameter] public string Style { get; set; } = string.Empty;
    [Parameter] public string Class { get; set; } = string.Empty;
    
    private ButtonElement? _element;
    private bool _disposed;
    
    [Inject] protected EclipseRenderer? Renderer { get; set; }
    
    EclipseElement IElementHandler.Element
    {
        get
        {
            if (_element == null)
            {
                _element = new ButtonElement();
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
        
        _element.Text = Text ?? "Button";
        _element.FontSize = FontSize;
        _element.ButtonColor = ParseBackground(Background);
        _element.TextColor = ParseColor(Foreground);
        _element.CornerRadius = CornerRadius;
                _element.RequestedWidth = Width;
        _element.RequestedHeight = Height;
        _element.MinWidth = MinWidth;
        _element.MinHeight = MinHeight;
        _element.MaxWidth = MaxWidth;
        _element.MaxHeight = MaxHeight;
        _element.HorizontalAlignment = HorizontalAlignment;
        _element.VerticalAlignment = VerticalAlignment;
        _element.RequestedHeight = Height;
        _element.PaddingLeft = PaddingLeft;
        _element.PaddingTop = PaddingTop;
        _element.PaddingRight = PaddingRight;
        _element.PaddingBottom = PaddingBottom;
        
        // 应用内联样式
        if (!string.IsNullOrEmpty(Style))
        {
            var styleProps = Styling.EclipseStyleParser.ParseInlineStyle(Style);
            foreach (var prop in styleProps)
            {
                _element.Style.ApplyProperty(prop.Key, prop.Value);
            }
        }
        
        // 应用 CSS 类样式
        if (!string.IsNullOrEmpty(Class))
        {
            var classStyle = Styling.StyleManager.GetClassStyle(Class);
            if (classStyle != null)
            {
                // 合并类样式到元素样式中
                _element.Style = MergeStyles(_element.Style, classStyle);
            }
        }
        
        _element.OnClick = OnClick.HasDelegate ? async (e, p) => 
        {
            if (Renderer != null)
            {
                await Renderer.Dispatcher.InvokeAsync(async () =>
                {
                    await OnClick.InvokeAsync(new MouseEventArgs { ClientX = p.X, ClientY = p.Y });
                });
            }
            else
            {
                await OnClick.InvokeAsync(new MouseEventArgs { ClientX = p.X, ClientY = p.Y });
            }
        } : null;
    }
    
    private static SKColor ParseBackground(string? color)
    {
        if (!string.IsNullOrEmpty(color) && color.StartsWith('#') && color.Length == 7)
            return SKColor.Parse(color);
        return SKColors.Blue;
    }
    
    private static SKColor ParseColor(string? color)
    {
        if (!string.IsNullOrEmpty(color) && color.StartsWith('#') && color.Length == 7)
            return SKColor.Parse(color);
        return SKColors.White;
    }
    
    private static Styling.Style MergeStyles(Styling.Style baseStyle, Styling.Style classStyle)
    {
        var merged = new Styling.Style();
        
        // 合并背景颜色（内联样式优先）
        merged.BackgroundColor = baseStyle.BackgroundColor ?? classStyle.BackgroundColor;
        
        // 合并文本颜色
        merged.Color = baseStyle.Color ?? classStyle.Color;
        
        // 合并字体大小（内联样式优先）
        merged.FontSize = baseStyle.FontSize != 14f ? baseStyle.FontSize : classStyle.FontSize;
        
        // 合并字体族
        merged.FontFamily = !string.IsNullOrEmpty(baseStyle.FontFamily) ? baseStyle.FontFamily : classStyle.FontFamily;
        
        // 合并字体粗细
        merged.FontBold = baseStyle.FontBold || classStyle.FontBold;
        
        // 合并字体斜体
        merged.FontItalic = baseStyle.FontItalic || classStyle.FontItalic;
        
        // 合并边框
        merged.BorderColor = baseStyle.BorderColor ?? classStyle.BorderColor;
        merged.BorderWidth = baseStyle.BorderWidth != 1f ? baseStyle.BorderWidth : classStyle.BorderWidth;
        merged.BorderRadius = baseStyle.BorderRadius != 0f ? baseStyle.BorderRadius : classStyle.BorderRadius;
        
        // 合并内边距
        merged.PaddingLeft = baseStyle.PaddingLeft != 0f ? baseStyle.PaddingLeft : classStyle.PaddingLeft;
        merged.PaddingTop = baseStyle.PaddingTop != 0f ? baseStyle.PaddingTop : classStyle.PaddingTop;
        merged.PaddingRight = baseStyle.PaddingRight != 0f ? baseStyle.PaddingRight : classStyle.PaddingRight;
        merged.PaddingBottom = baseStyle.PaddingBottom != 0f ? baseStyle.PaddingBottom : classStyle.PaddingBottom;
        
        // 合并外边距
        merged.MarginLeft = baseStyle.MarginLeft != 0f ? baseStyle.MarginLeft : classStyle.MarginLeft;
        merged.MarginTop = baseStyle.MarginTop != 0f ? baseStyle.MarginTop : classStyle.MarginTop;
        merged.MarginRight = baseStyle.MarginRight != 0f ? baseStyle.MarginRight : classStyle.MarginRight;
        merged.MarginBottom = baseStyle.MarginBottom != 0f ? baseStyle.MarginBottom : classStyle.MarginBottom;
        
        // 合并尺寸
        merged.Width = baseStyle.Width != -1f ? baseStyle.Width : classStyle.Width;
        merged.Height = baseStyle.Height != -1f ? baseStyle.Height : classStyle.Height;
        
        // 合并透明度
        merged.Opacity = baseStyle.Opacity != 1f ? baseStyle.Opacity : classStyle.Opacity;
        
        // 合并阴影
        merged.BoxShadow = !string.IsNullOrEmpty(baseStyle.BoxShadow) ? baseStyle.BoxShadow : classStyle.BoxShadow;
        
        return merged;
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
