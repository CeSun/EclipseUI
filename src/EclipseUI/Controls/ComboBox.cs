using SkiaSharp;
using Microsoft.AspNetCore.Components;
using EclipseUI.Core;
using EclipseUI.Layout;

namespace EclipseUI.Controls;

/// <summary>
/// 下拉选择框组件
/// </summary>
public class ComboBox : ComponentBase, IElementHandler, IDisposable
{
    [Parameter] public IList<string>? ItemsSource { get; set; }
    [Parameter] public string? SelectedItem { get; set; }
    [Parameter] public EventCallback<string?> SelectedItemChanged { get; set; }
    [Parameter] public int SelectedIndex { get; set; } = -1;
    [Parameter] public EventCallback<int> SelectedIndexChanged { get; set; }
    [Parameter] public string? Placeholder { get; set; }
    [Parameter] public float FontSize { get; set; } = 14;
    [Parameter] public string? Foreground { get; set; }
    [Parameter] public string? Background { get; set; }
    [Parameter] public float? Width { get; set; }
    [Parameter] public float? Height { get; set; }
    [Parameter] public float? MinWidth { get; set; } = 120;
    [Parameter] public float? MinHeight { get; set; } = 36;
    
    [Parameter] public float PaddingLeft { get; set; } = 12;
    [Parameter] public float PaddingTop { get; set; } = 8;
    [Parameter] public float PaddingRight { get; set; } = 36;
    [Parameter] public float PaddingBottom { get; set; } = 8;
    
    [Parameter] public EventCallback<FocusEventArgs> OnFocus { get; set; }
    [Parameter] public EventCallback<FocusEventArgs> OnBlur { get; set; }
    
    private ComboBoxElement? _element;
    private bool _disposed;
    
    [Inject] protected EclipseRenderer? Renderer { get; set; }
    
    EclipseElement IElementHandler.Element
    {
        get
        {
            if (_element == null)
            {
                _element = new ComboBoxElement();
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
        
        _element.ItemsSource = ItemsSource ?? new List<string>();
        _element.SelectedIndex = SelectedIndex;
        _element.SelectedItem = SelectedItem;
        _element.Placeholder = Placeholder ?? "请选择...";
        _element.FontSize = FontSize;
        _element.TextColor = ParseColor(Foreground);
        _element.BackgroundColor = ParseBackground(Background);
        _element.RequestedWidth = Width;
        _element.RequestedHeight = Height;
        _element.MinWidth = MinWidth;
        _element.MinHeight = MinHeight;
        _element.PaddingLeft = PaddingLeft;
        _element.PaddingTop = PaddingTop;
        _element.PaddingRight = PaddingRight;
        _element.PaddingBottom = PaddingBottom;
        
        _element.OnItemSelected = async (index, item) =>
        {
            SelectedIndex = index;
            SelectedItem = item;
            
            if (SelectedItemChanged.HasDelegate)
            {
                if (Renderer != null)
                {
                    await Renderer.Dispatcher.InvokeAsync(async () =>
                    {
                        await SelectedItemChanged.InvokeAsync(item);
                    });
                }
                else
                {
                    await SelectedItemChanged.InvokeAsync(item);
                }
            }
            
            if (SelectedIndexChanged.HasDelegate)
            {
                if (Renderer != null)
                {
                    await Renderer.Dispatcher.InvokeAsync(async () =>
                    {
                        await SelectedIndexChanged.InvokeAsync(index);
                    });
                }
                else
                {
                    await SelectedIndexChanged.InvokeAsync(index);
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
