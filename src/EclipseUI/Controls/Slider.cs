using SkiaSharp;
using Microsoft.AspNetCore.Components;
using EclipseUI.Core;

namespace EclipseUI.Controls;

/// <summary>
/// 滑块组件
/// </summary>
public class Slider : ComponentBase, IElementHandler, IDisposable
{
    [Parameter] public double Value { get; set; }
    [Parameter] public EventCallback<double> ValueChanged { get; set; }
    [Parameter] public double Minimum { get; set; } = 0;
    [Parameter] public double Maximum { get; set; } = 100;
    [Parameter] public double TickFrequency { get; set; } = 1;
    [Parameter] public bool ShowTicks { get; set; }
    [Parameter] public bool IsSnapToTick { get; set; }
    [Parameter] public Orientation Orientation { get; set; } = Orientation.Horizontal;
    [Parameter] public float FontSize { get; set; } = 12;
    [Parameter] public string? Foreground { get; set; }
    [Parameter] public string? TrackColor { get; set; }
    [Parameter] public string? ThumbColor { get; set; }
    [Parameter] public float? Width { get; set; }
    [Parameter] public float? Height { get; set; }
    
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }
    
    private SliderElement? _element;
    private bool _disposed;
    
    [Inject] protected EclipseRenderer? Renderer { get; set; }
    
    EclipseElement IElementHandler.Element
    {
        get
        {
            if (_element == null)
            {
                _element = new SliderElement();
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
        
        _element.Value = Value;
        _element.Minimum = Minimum;
        _element.Maximum = Maximum;
        _element.TickFrequency = TickFrequency;
        _element.ShowTicks = ShowTicks;
        _element.IsSnapToTick = IsSnapToTick;
        _element.Orientation = Orientation;
        _element.FontSize = FontSize;
        _element.TextColor = ParseColor(Foreground);
        _element.TrackColor = ParseColor(TrackColor);
        _element.ThumbColor = ParseColor(ThumbColor);
        _element.RequestedWidth = Width;
        _element.RequestedHeight = Height;
        
        _element.OnValueChanged = async (value) =>
        {
            Value = value;
            
            if (ValueChanged.HasDelegate)
            {
                if (Renderer != null)
                {
                    await Renderer.Dispatcher.InvokeAsync(async () =>
                    {
                        await ValueChanged.InvokeAsync(value);
                    });
                }
                else
                {
                    await ValueChanged.InvokeAsync(value);
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

/// <summary>
/// 方向枚举
/// </summary>
public enum Orientation
{
    Horizontal,
    Vertical
}
