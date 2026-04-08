using Eclipse.Core;
using Eclipse.Core.Abstractions;
using Eclipse.Input;
using Eclipse.Rendering;
using System;

namespace Eclipse.Controls;

/// <summary>
/// 按钮控件
/// </summary>
public class Button : InteractiveControl
{
    private bool _isPressed = false;
    private bool _isHovered = false;
    
    public string? Text { get; set; }
    public double FontSize { get; set; } = 14;
    public string? FontFamily { get; set; }
    public Color TextColor { get; set; } = Color.White;
    public Color BackgroundColor { get; set; } = Color.SystemBlue;
    public Color? HoverBackgroundColor { get; set; }
    public Color? PressedBackgroundColor { get; set; }
    public Color DisabledBackgroundColor { get; set; } = Color.LightGray;
    public Color DisabledTextColor { get; set; } = Color.Gray;
    public Color? BorderColor { get; set; }
    public double BorderWidth { get; set; } = 0;
    public double CornerRadius { get; set; } = 4;
    
    public event EventHandler? Click;
    
    public event EventHandler? OnClick
    {
        add => Click += value;
        remove => Click -= value;
    }
    
    public Button()
    {
        IsFocusable = true;
        _desiredSize = new Size(100, 44);
        
        PointerEntered += OnPointerEntered;
        PointerExited += OnPointerExited;
        PointerPressed += OnPointerPressed;
        PointerReleased += OnPointerReleased;
        
        Tapped += (s, e) =>
        {
            if (IsEnabled)
                Click?.Invoke(this, EventArgs.Empty);
        };
    }
    
    private void OnPointerEntered(object? sender, PointerEventArgs e)
    {
        if (!IsEnabled) return;
        _isHovered = true;
        StateHasChanged();
    }
    
    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        _isHovered = false;
        _isPressed = false;
        StateHasChanged();
    }
    
    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!IsEnabled) return;
        _isPressed = true;
        StateHasChanged();
    }
    
    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!IsEnabled) return;
        _isPressed = false;
        StateHasChanged();
    }
    
    private Color GetCurrentBackgroundColor()
    {
        if (!IsEnabled)
            return DisabledBackgroundColor;
        
        if (_isPressed)
            return PressedBackgroundColor ?? BackgroundColor.Darken(0.2);
        
        if (_isHovered)
            return HoverBackgroundColor ?? BackgroundColor.Darken(0.1);
        
        return BackgroundColor;
    }
    
    private Color GetCurrentTextColor()
    {
        return IsEnabled ? TextColor : DisabledTextColor;
    }
    
    public override Size Measure(Size availableSize, IDrawingContext context)
    {
        if (string.IsNullOrEmpty(Text))
        {
            _desiredSize = new Size(80 * context.Scale, 44 * context.Scale);
            return _desiredSize;
        }
        
        var scaledFontSize = FontSize * context.Scale;
        var textWidth = context.MeasureText(Text, scaledFontSize, FontFamily);
        
        _desiredSize = new Size(textWidth + 40 * context.Scale, 44 * context.Scale);
        return _desiredSize;
    }
    
    public override void Build(IBuildContext context) { }
    
    public override void Render(IDrawingContext context, Rect bounds)
    {
        UpdateBounds(bounds);
        
        var scaledCornerRadius = CornerRadius * context.Scale;
        var bgColor = GetCurrentBackgroundColor();
        var textColor = GetCurrentTextColor();
        
        context.DrawRoundRect(bounds, bgColor, scaledCornerRadius);
        
        if (BorderWidth > 0 && BorderColor.HasValue)
            context.DrawRectangle(bounds, Color.Transparent, BorderColor, BorderWidth * context.Scale, scaledCornerRadius);
        
        if (IsFocused && IsEnabled)
        {
            var focusBounds = new Rect(
                bounds.X - 2 * context.Scale,
                bounds.Y - 2 * context.Scale,
                bounds.Width + 4 * context.Scale,
                bounds.Height + 4 * context.Scale);
            context.DrawRectangle(focusBounds, Color.Transparent, Color.SystemBlue, 2 * context.Scale, scaledCornerRadius + 2);
        }
        
        if (!string.IsNullOrEmpty(Text))
        {
            var scaledFontSize = FontSize * context.Scale;
            var textWidth = context.MeasureText(Text, scaledFontSize, FontFamily);
            var x = bounds.X + (bounds.Width - textWidth) / 2;
            var y = bounds.Y + bounds.Height / 2;
            context.DrawText(Text, x, y, scaledFontSize, FontFamily, null, textColor);
        }
    }
}
