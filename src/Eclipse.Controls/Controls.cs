using Eclipse.Core;
using Eclipse.Core.Abstractions;
using Eclipse.Input;
using Eclipse.Rendering;
using System.Collections.Generic;

namespace Eclipse.Controls;

/// <summary>
/// 可交互控件基类 - 支持输入事件
/// </summary>
public abstract class InteractiveControl : InputElementBase
{
    protected Rect _bounds = new(0, 0, 100, 40);
    
    public bool IsEnabled { get; set; } = true;
    
    public override bool IsInputEnabled => IsEnabled;
    public override bool IsVisible => true;
    public override Rect Bounds => _bounds;
    
    public void UpdateBounds(Rect bounds) => _bounds = bounds;
    
    protected override IEnumerable<IInputElement> GetInputChildren()
    {
        foreach (var child in Children)
        {
            if (child is IInputElement inputElement)
            {
                yield return inputElement;
            }
        }
    }
    
    public override void Render(IDrawingContext context, Rect bounds)
    {
        UpdateBounds(bounds);
        base.Render(context, bounds);
    }
}

/// <summary>
/// 垂直堆叠布局
/// </summary>
public class StackLayout : InputElementBase
{
    public Orientation Orientation { get; set; } = Orientation.Vertical;
    public string? Spacing { get; set; } = "0";
    public string? BackgroundColor { get; set; }
    public string? Padding { get; set; } = "0";
    
    private Rect _bounds;
    
    public double GetSpacing() => double.TryParse(Spacing, out var spacing) ? spacing : 0;
    public double GetPadding() => double.TryParse(Padding, out var padding) ? padding : 0;
    
    public override bool IsVisible => true;
    public override Rect Bounds => _bounds;
    
    public void UpdateBounds(Rect bounds) => _bounds = bounds;
    
    protected override IEnumerable<IInputElement> GetInputChildren()
    {
        foreach (var child in Children)
        {
            if (child is IInputElement inputElement)
            {
                yield return inputElement;
            }
        }
    }
    
    public override void Build(IBuildContext context) { }
    
    public override void Render(IDrawingContext context, Rect bounds)
    {
        UpdateBounds(bounds);
        
        var spacing = GetSpacing() * context.Scale;
        var padding = GetPadding() * context.Scale;
        
        if (!string.IsNullOrEmpty(BackgroundColor))
        {
            context.DrawRectangle(bounds, BackgroundColor);
        }
        
        var contentBounds = new Rect(
            bounds.X + padding,
            bounds.Y + padding,
            bounds.Width - padding * 2,
            bounds.Height - padding * 2);
        
        if (Orientation == Orientation.Vertical)
        {
            double y = contentBounds.Y;
            foreach (var child in Children)
            {
                var childHeight = EstimateHeight(child, context);
                var childBounds = new Rect(contentBounds.X, y, contentBounds.Width, childHeight);
                child.Render(context, childBounds);
                y += childHeight + spacing;
            }
        }
        else
        {
            double x = contentBounds.X;
            var childWidth = (contentBounds.Width - spacing * (Children.Count - 1)) / Children.Count;
            foreach (var child in Children)
            {
                var childBounds = new Rect(x, contentBounds.Y, childWidth, contentBounds.Height);
                child.Render(context, childBounds);
                x += childWidth + spacing;
            }
        }
    }
    
    private double EstimateHeight(IComponent component, IDrawingContext context)
    {
        return component switch
        {
            Label => 24.0 * context.Scale,
            Button => 44.0 * context.Scale,
            TextContent => 20.0 * context.Scale,
            _ => 40.0 * context.Scale
        };
    }
}

public enum Orientation
{
    Vertical,
    Horizontal
}

public class HStack : StackLayout
{
    public HStack() => Orientation = Orientation.Horizontal;
}

public class Label : ComponentBase
{
    public string? Text { get; set; }
    public string? FontSize { get; set; } = "14";
    public string? Color { get; set; }
    public string? FontWeight { get; set; }
    public string? FontFamily { get; set; }
    public TextAlignment TextAlignment { get; set; } = TextAlignment.Left;
    
    public double GetFontSize() => double.TryParse(FontSize, out var size) ? size : 14;
    
    public override void Build(IBuildContext context) { }
    
    public override void Render(IDrawingContext context, Rect bounds)
    {
        if (!string.IsNullOrEmpty(Text))
        {
            context.DrawText(Text, bounds.X, bounds.Y, GetFontSize() * context.Scale, FontFamily, FontWeight, Color);
        }
    }
}

public enum TextAlignment
{
    Left,
    Center,
    Right
}

public class Button : InteractiveControl
{
    public string? Text { get; set; }
    public string? BackgroundColor { get; set; } = "#007AFF";
    public string? TextColor { get; set; } = "White";
    public string? FontSize { get; set; } = "14";
    public string? FontFamily { get; set; }
    public string? CornerRadius { get; set; } = "4";
    
    public double GetFontSize() => double.TryParse(FontSize, out var size) ? size : 14;
    public double GetCornerRadius() => double.TryParse(CornerRadius, out var radius) ? radius : 4;
    
    public event EventHandler? Click;
    
    public event EventHandler? OnClick
    {
        add => Click += value;
        remove => Click -= value;
    }
    
    public Button()
    {
        IsFocusable = true;
        _bounds = new Rect(0, 0, 100, 40);
        
        Tapped += (s, e) =>
        {
            if (IsEnabled)
            {
                Click?.Invoke(this, EventArgs.Empty);
            }
        };
    }
    
    public override void Build(IBuildContext context) { }
    
    public override void Render(IDrawingContext context, Rect bounds)
    {
        UpdateBounds(bounds);
        
        context.DrawRoundRect(bounds, BackgroundColor ?? "#007AFF", GetCornerRadius() * context.Scale);
        
        if (!string.IsNullOrEmpty(Text))
        {
            var fontSize = GetFontSize() * context.Scale;
            var textWidth = context.MeasureText(Text, fontSize, FontFamily);
            var x = bounds.X + (bounds.Width - textWidth) / 2;
            var y = bounds.Y + bounds.Height / 2 - fontSize / 2;
            context.DrawText(Text, x, y, fontSize, FontFamily, null, TextColor);
        }
    }
}

public class TextInput : InteractiveControl
{
    public string? Text { get; set; }
    public string? Placeholder { get; set; }
    public double FontSize { get; set; } = 14;
    public string? BackgroundColor { get; set; }
    public double CornerRadius { get; set; } = 4;
    public double Padding { get; set; } = 8;
    public bool IsPassword { get; set; } = false;
    
    public event EventHandler<ValueChangedEventArgs<string?>>? TextChanged;
    
    public TextInput()
    {
        IsFocusable = true;
        _bounds = new Rect(0, 0, 200, 30);
    }
    
    public void SetText(string? newText)
    {
        if (Text != newText)
        {
            var oldText = Text;
            Text = newText;
            TextChanged?.Invoke(this, new ValueChangedEventArgs<string?>(oldText, newText));
        }
    }
    
    public override void Build(IBuildContext context) { }
    
    public override void Render(IDrawingContext context, Rect bounds)
    {
        UpdateBounds(bounds);
        
        if (!string.IsNullOrEmpty(BackgroundColor))
        {
            context.DrawRoundRect(bounds, BackgroundColor, CornerRadius * context.Scale);
        }
        
        if (!string.IsNullOrEmpty(Text))
        {
            context.DrawText(Text, bounds.X + Padding * context.Scale, bounds.Y, FontSize * context.Scale);
        }
        else if (!string.IsNullOrEmpty(Placeholder))
        {
            context.DrawText(Placeholder, bounds.X + Padding * context.Scale, bounds.Y, FontSize * context.Scale, null, null, "#888888");
        }
    }
}

public class ValueChangedEventArgs<T> : EventArgs
{
    public T OldValue { get; }
    public T NewValue { get; }
    public ValueChangedEventArgs(T oldValue, T newValue)
    {
        OldValue = oldValue;
        NewValue = newValue;
    }
}

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
            }
        }
    }
    
    public string? Label { get; set; }
    public string? CheckedColor { get; set; }
    public double Size { get; set; } = 20;
    
    public event EventHandler<ValueChangedEventArgs<bool>>? CheckedChanged;
    
    public CheckBox()
    {
        IsFocusable = true;
        _bounds = new Rect(0, 0, 20, 20);
        
        Tapped += (s, e) =>
        {
            if (IsEnabled)
            {
                IsChecked = !IsChecked;
            }
        };
    }
    
    public override void Build(IBuildContext context) { }
    
    public override void Render(IDrawingContext context, Rect bounds)
    {
        UpdateBounds(bounds);
        
        var size = Size * context.Scale;
        var checkBounds = new Rect(bounds.X, bounds.Y, size, size);
        var color = IsChecked ? (CheckedColor ?? "#007AFF") : "#CCCCCC";
        context.DrawRoundRect(checkBounds, color, 4 * context.Scale);
        
        if (!string.IsNullOrEmpty(Label))
        {
            context.DrawText(Label, bounds.X + size + 8 * context.Scale, bounds.Y, 14 * context.Scale);
        }
    }
}

public class Image : ComponentBase
{
    public string? Source { get; set; }
    public double Width { get; set; } = -1;
    public double Height { get; set; } = -1;
    public Stretch Stretch { get; set; } = Stretch.Uniform;
    
    public override void Build(IBuildContext context) { }
    
    public override void Render(IDrawingContext context, Rect bounds)
    {
        context.DrawRectangle(bounds, "#EEEEEE");
    }
}

public enum Stretch
{
    None,
    Fill,
    Uniform,
    UniformToFill
}

public class Container : ComponentBase
{
    public string? BackgroundColor { get; set; }
    public double Padding { get; set; } = 0;
    public double CornerRadius { get; set; } = 0;
    
    public override void Build(IBuildContext context) { }
    
    public override void Render(IDrawingContext context, Rect bounds)
    {
        if (!string.IsNullOrEmpty(BackgroundColor))
        {
            context.DrawRoundRect(bounds, BackgroundColor, CornerRadius * context.Scale);
        }
        
        var contentBounds = new Rect(
            bounds.X + Padding * context.Scale,
            bounds.Y + Padding * context.Scale,
            bounds.Width - Padding * 2 * context.Scale,
            bounds.Height - Padding * 2 * context.Scale);
        
        foreach (var child in Children)
        {
            child.Render(context, contentBounds);
        }
    }
}