using Eclipse.Core;
using Eclipse.Core.Abstractions;
using Eclipse.Input;
using Eclipse.Rendering;
using System;

namespace Eclipse.Controls;

/// <summary>
/// 复选框控件
/// </summary>
public class CheckBox : InteractiveControl
{
    private bool _isChecked;
    
    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (_isChecked != value)
            {
                var oldValue = _isChecked;
                _isChecked = value;
                CheckedChanged?.Invoke(this, new ValueChangedEventArgs<bool>(oldValue, value));
                StateHasChanged();
            }
        }
    }
    
    public string? Label { get; set; }
    public Color CheckedColor { get; set; } = Color.SystemBlue;
    public double Size { get; set; } = 20;
    
    public event EventHandler<ValueChangedEventArgs<bool>>? CheckedChanged;
    
    public CheckBox()
    {
        IsFocusable = true;
        _desiredSize = new Size(20, 20);
        
        Tapped += (s, e) =>
        {
            if (IsEnabled)
                IsChecked = !IsChecked;
        };
    }
    
    public override Size Measure(Size availableSize, IDrawingContext context)
    {
        var scaledSize = Size * context.Scale;
        
        if (string.IsNullOrEmpty(Label))
        {
            _desiredSize = new Size(scaledSize, scaledSize);
        }
        else
        {
            var textWidth = context.MeasureText(Label, 14 * context.Scale, null);
            _desiredSize = new Size(scaledSize + 8 * context.Scale + textWidth, scaledSize);
        }
        
        return _desiredSize;
    }
    
    public override void Build(IBuildContext context) { }
    
    public override void Render(IDrawingContext context, Rect bounds)
    {
        UpdateBounds(bounds);
        
        var scaledSize = Size * context.Scale;
        var checkBounds = new Rect(bounds.X, bounds.Y, scaledSize, scaledSize);
        var color = IsChecked ? CheckedColor : Color.LightGray;
        context.DrawRoundRect(checkBounds, color, 4 * context.Scale);
        
        if (!string.IsNullOrEmpty(Label))
        {
            var scaledFontSize = 14 * context.Scale;
            var textY = bounds.Y + scaledFontSize * 0.5;
            context.DrawText(Label, bounds.X + scaledSize + 8 * context.Scale, textY, scaledFontSize);
        }
    }
}
