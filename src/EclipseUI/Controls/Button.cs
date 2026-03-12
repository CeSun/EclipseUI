using SkiaSharp;
using Microsoft.AspNetCore.Components;
using EclipseUI.Core;

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
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }
    
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
    
    void IDisposable.Dispose()
    {
        if (!_disposed)
        {
            _element = null;
            _disposed = true;
        }
    }
}
