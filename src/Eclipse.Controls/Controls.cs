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
    protected Rect _bounds = new(0, 0, 100, 40); // 默认大小
    
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    
    public override bool IsInputEnabled => IsEnabled;
    public override bool IsVisible => true;
    public override Rect Bounds => _bounds;
    
    /// <summary>
    /// 更新边界（供渲染使用）
    /// </summary>
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
    
    public override void Render(DrawingContext context, Rect bounds)
    {
        UpdateBounds(bounds);
        base.Render(context, bounds);
    }
}

/// <summary>
/// 垂直堆叠布局 - 也支持输入事件（作为容器）
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
    
    public override void Render(DrawingContext context, Rect bounds)
    {
        UpdateBounds(bounds);
        
        var spacing = GetSpacing() * context.Scale;
        var padding = GetPadding() * context.Scale;
        
        // 绘制背景
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
                context.DrawChild(child, childBounds);
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
                context.DrawChild(child, childBounds);
                x += childWidth + spacing;
            }
        }
    }
    
    private double EstimateHeight(IComponent component, DrawingContext context)
    {
        return component switch
        {
            Label label => 24.0 * context.Scale,
            Button button => 44.0 * context.Scale,
            TextContent text => 20.0 * context.Scale,
            _ => 40.0 * context.Scale
        };
    }
}

public enum Orientation
{
    Vertical,
    Horizontal
}

/// <summary>
/// 水平堆叠布局
/// </summary>
public class HStack : StackLayout
{
    public HStack() => Orientation = Orientation.Horizontal;
}

/// <summary>
/// 文本标签
/// </summary>
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
    
    public override void Render(DrawingContext context, Rect bounds)
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

/// <summary>
/// 按钮 - 支持输入事件
/// </summary>
public class Button : InteractiveControl
{
    public string? Text { get; set; }
    public string? BackgroundColor { get; set; } = "#007AFF";
    public string? TextColor { get; set; } = "White";
    public string? FontSize { get; set; } = "14";
    public string? FontFamily { get; set; }
    public string? CornerRadius { get; set; } = "4";
    public string? Padding { get; set; } = "8";
    
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
    
    public override void Render(DrawingContext context, Rect bounds)
    {
        UpdateBounds(bounds);
        
        // 绘制圆角矩形背景
        context.DrawRoundRect(bounds, BackgroundColor ?? "#007AFF", GetCornerRadius() * context.Scale);
        
        // 绘制文本（居中）
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

/// <summary>
/// 文本输入框
/// </summary>
public class TextInput : InteractiveControl
{
    public string? Text { get; set; }
    public string? Placeholder { get; set; }
    public double FontSize { get; set; } = 14;
    public string? BackgroundColor { get; set; }
    public string? BorderColor { get; set; }
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
    
    public override void Render(DrawingContext context, Rect bounds)
    {
        UpdateBounds(bounds);
        
        // 绘制背景和边框
        if (!string.IsNullOrEmpty(BackgroundColor))
        {
            context.DrawRoundRect(bounds, BackgroundColor, (float)CornerRadius * context.Scale);
        }
        
        // 绘制文本
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

/// <summary>
/// 复选框
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
    
    public override void Render(DrawingContext context, Rect bounds)
    {
        UpdateBounds(bounds);
        
        // 绘制复选框
        var size = Size * context.Scale;
        var checkBounds = new Rect(bounds.X, bounds.Y, size, size);
        var color = IsChecked ? (CheckedColor ?? "#007AFF") : "#CCCCCC";
        context.DrawRoundRect(checkBounds, color, 4 * context.Scale);
        
        // 绘制标签
        if (!string.IsNullOrEmpty(Label))
        {
            context.DrawText(Label, bounds.X + size + 8 * context.Scale, bounds.Y, 14 * context.Scale);
        }
    }
}

/// <summary>
/// 图片
/// </summary>
public class Image : ComponentBase
{
    public string? Source { get; set; }
    public double Width { get; set; } = -1;
    public double Height { get; set; } = -1;
    public Stretch Stretch { get; set; } = Stretch.Uniform;
    
    public override void Build(IBuildContext context) { }
}

public enum Stretch
{
    None,
    Fill,
    Uniform,
    UniformToFill
}

/// <summary>
/// 容器
/// </summary>
public class Container : ComponentBase
{
    public string? BackgroundColor { get; set; }
    public double Padding { get; set; } = 0;
    public double CornerRadius { get; set; } = 0;
    
    public override void Build(IBuildContext context) { }
    
    public override void Render(DrawingContext context, Rect bounds)
    {
        if (!string.IsNullOrEmpty(BackgroundColor))
        {
            context.DrawRoundRect(bounds, BackgroundColor, (float)CornerRadius * context.Scale);
        }
        
        // 渲染子组件
        var contentBounds = new Rect(
            bounds.X + Padding * context.Scale,
            bounds.Y + Padding * context.Scale,
            bounds.Width - Padding * 2 * context.Scale,
            bounds.Height - Padding * 2 * context.Scale);
        
        foreach (var child in Children)
        {
            context.DrawChild(child, contentBounds);
        }
    }
}