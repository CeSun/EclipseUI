using SkiaSharp;
using Microsoft.AspNetCore.Components;
using EclipseUI.Core;

namespace EclipseUI.Controls;

/// <summary>
/// 复选框组件
/// </summary>
public class CheckBox : ComponentBase, IElementHandler, IDisposable
{
    [Parameter] public bool? IsChecked { get; set; }
    [Parameter] public EventCallback<bool?> IsCheckedChanged { get; set; }
    [Parameter] public string? Content { get; set; }
    [Parameter] public bool IsThreeState { get; set; }
    [Parameter] public float FontSize { get; set; } = 14;
    [Parameter] public string? Foreground { get; set; }
    [Parameter] public float? Width { get; set; }
    [Parameter] public float? Height { get; set; }
    
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }
    
    private CheckBoxElement? _element;
    private bool _disposed;
    
    [Inject] protected EclipseRenderer? Renderer { get; set; }
    
    EclipseElement IElementHandler.Element
    {
        get
        {
            if (_element == null)
            {
                _element = new CheckBoxElement();
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
        
        _element.IsChecked = IsChecked;
        _element.Content = Content ?? "";
        _element.IsThreeState = IsThreeState;
        _element.FontSize = FontSize;
        _element.TextColor = ParseColor(Foreground);
        _element.RequestedWidth = Width;
        _element.RequestedHeight = Height;
        
        _element.OnCheckedChanged = async (isChecked) =>
        {
            IsChecked = isChecked;
            
            if (IsCheckedChanged.HasDelegate)
            {
                if (Renderer != null)
                {
                    await Renderer.Dispatcher.InvokeAsync(async () =>
                    {
                        await IsCheckedChanged.InvokeAsync(isChecked);
                    });
                }
                else
                {
                    await IsCheckedChanged.InvokeAsync(isChecked);
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
        return SKColors.Black;
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
