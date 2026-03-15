using SkiaSharp;
using Microsoft.AspNetCore.Components;
using EclipseUI.Core;

namespace EclipseUI.Controls;

/// <summary>
/// 文本输入框组件
/// </summary>
public class TextBox : ComponentBase, IElementHandler, IDisposable
{
    [Parameter] public string? Text { get; set; }
    [Parameter] public EventCallback<string> TextChanged { get; set; }
    [Parameter] public string? Placeholder { get; set; }
    [Parameter] public float FontSize { get; set; } = 14;
    [Parameter] public string? Foreground { get; set; }
    [Parameter] public string? Background { get; set; }
    [Parameter] public float? Width { get; set; }
    [Parameter] public float? Height { get; set; }
    [Parameter] public float? MinWidth { get; set; }
    [Parameter] public float? MinHeight { get; set; }
    [Parameter] public float? MaxWidth { get; set; }
    [Parameter] public float? MaxHeight { get; set; }
    [Parameter] public HorizontalAlignment HorizontalAlignment { get; set; } = HorizontalAlignment.Stretch;
    [Parameter] public VerticalAlignment VerticalAlignment { get; set; } = VerticalAlignment.Stretch;
    
    [Parameter] public float PaddingLeft { get; set; } = 8;
    [Parameter] public float PaddingTop { get; set; } = 6;
    [Parameter] public float PaddingRight { get; set; } = 8;
    [Parameter] public float PaddingBottom { get; set; } = 6;
    
    [Parameter] public EventCallback<FocusEventArgs> OnFocus { get; set; }
    [Parameter] public EventCallback<FocusEventArgs> OnBlur { get; set; }
    [Parameter] public EventCallback<KeyEventArgs> OnKeyDown { get; set; }
    [Parameter] public EventCallback<KeyEventArgs> OnKeyUp { get; set; }
    
    private TextBoxElement? _element;
    private bool _disposed;
    
    [Inject] protected EclipseRenderer? Renderer { get; set; }
    
    EclipseElement IElementHandler.Element
    {
        get
        {
            if (_element == null)
            {
                _element = new TextBoxElement();
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
        
        _element.Text = Text ?? "";
        _element.Placeholder = Placeholder ?? "";
        _element.FontSize = FontSize;
        _element.TextColor = ParseColor(Foreground);
        _element.BackgroundColor = ParseBackground(Background);
        _element.RequestedWidth = Width;
        _element.RequestedHeight = Height;
        _element.MinWidth = MinWidth;
        _element.MinHeight = MinHeight;
        _element.MaxWidth = MaxWidth;
        _element.MaxHeight = MaxHeight;
        _element.HorizontalAlignment = HorizontalAlignment;
        _element.VerticalAlignment = VerticalAlignment;
        _element.PaddingLeft = PaddingLeft;
        _element.PaddingTop = PaddingTop;
        _element.PaddingRight = PaddingRight;
        _element.PaddingBottom = PaddingBottom;
        
        _element.OnFocus = OnFocus.HasDelegate ? async (e) =>
        {
            if (Renderer != null)
            {
                await Renderer.Dispatcher.InvokeAsync(async () =>
                {
                    await OnFocus.InvokeAsync(new FocusEventArgs { IsFocused = true });
                });
            }
            else
            {
                await OnFocus.InvokeAsync(new FocusEventArgs { IsFocused = true });
            }
        } : null;
        
        _element.OnBlur = OnBlur.HasDelegate ? async (e) =>
        {
            if (Renderer != null)
            {
                await Renderer.Dispatcher.InvokeAsync(async () =>
                {
                    await OnBlur.InvokeAsync(new FocusEventArgs { IsFocused = false });
                });
            }
            else
            {
                await OnBlur.InvokeAsync(new FocusEventArgs { IsFocused = false });
            }
        } : null;
        
        _element.OnKeyDown = OnKeyDown.HasDelegate ? async (key) =>
        {
            if (Renderer != null)
            {
                await Renderer.Dispatcher.InvokeAsync(async () =>
                {
                    await OnKeyDown.InvokeAsync(new KeyEventArgs { Key = key });
                });
            }
            else
            {
                await OnKeyDown.InvokeAsync(new KeyEventArgs { Key = key });
            }
        } : null;
        
        _element.OnTextChanged = async (newText) =>
        {
            Text = newText;
            if (TextChanged.HasDelegate)
            {
                if (Renderer != null)
                {
                    await Renderer.Dispatcher.InvokeAsync(async () =>
                    {
                        await TextChanged.InvokeAsync(newText);
                    });
                }
                else
                {
                    await TextChanged.InvokeAsync(newText);
                }
            }
        };
    }
    
    private static SKColor? ParseBackground(string? color)
    {
        if (!string.IsNullOrEmpty(color) && color.StartsWith('#') && color.Length == 7)
            return SKColor.Parse(color);
        return SKColors.White;
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

/// <summary>
/// 焦点事件参数
/// </summary>
public class FocusEventArgs
{
    public bool IsFocused { get; set; }
}

/// <summary>
/// 键盘事件参数
/// </summary>
public class KeyEventArgs
{
    public string Key { get; set; } = "";
}
