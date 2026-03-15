using SkiaSharp;
using Microsoft.AspNetCore.Components;
using EclipseUI.Core;

namespace EclipseUI.Controls;

/// <summary>
/// 开关组件
/// </summary>
public class ToggleSwitch : ComponentBase, IElementHandler, IDisposable
{
    [Parameter] public bool IsOn { get; set; }
    [Parameter] public EventCallback<bool> IsOnChanged { get; set; }
    [Parameter] public string? OnContent { get; set; }
    [Parameter] public string? OffContent { get; set; }
    [Parameter] public float FontSize { get; set; } = 12;
    [Parameter] public string? OnColor { get; set; }
    [Parameter] public string? OffColor { get; set; }
    [Parameter] public float? Width { get; set; }
    [Parameter] public float? Height { get; set; }
    
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }
    
    private ToggleSwitchElement? _element;
    private bool _disposed;
    
    [Inject] protected EclipseRenderer? Renderer { get; set; }
    
    EclipseElement IElementHandler.Element
    {
        get
        {
            if (_element == null)
            {
                _element = new ToggleSwitchElement();
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
        
        _element.IsOn = IsOn;
        _element.OnContent = OnContent ?? "";
        _element.OffContent = OffContent ?? "";
        _element.FontSize = FontSize;
        _element.OnColor = ParseColor(OnColor);
        _element.OffColor = ParseColor(OffColor);
        _element.RequestedWidth = Width;
        _element.RequestedHeight = Height;
        
        _element.OnToggled = async (isOn) =>
        {
            IsOn = isOn;
            
            if (IsOnChanged.HasDelegate)
            {
                if (Renderer != null)
                {
                    await Renderer.Dispatcher.InvokeAsync(async () =>
                    {
                        await IsOnChanged.InvokeAsync(isOn);
                    });
                }
                else
                {
                    await IsOnChanged.InvokeAsync(isOn);
                }
            }
        };
        
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
    
    private static SKColor ParseColor(string? color)
    {
        if (!string.IsNullOrEmpty(color) && color.StartsWith('#') && color.Length == 7)
            return SKColor.Parse(color);
        return SKColors.Transparent;
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
