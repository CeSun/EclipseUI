using Eclipse.Core;
using Eclipse.Core.Abstractions;
using Eclipse.Input;
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
    /// 更新边界（供渲染器使用）
    /// </summary>
    public void UpdateBounds(Rect bounds) => _bounds = bounds;
    
    protected override IEnumerable<IInputElement> GetInputChildren()
    {
        // 返回所有 IInputElement 类型的子组件
        foreach (var child in Children)
        {
            if (child is IInputElement inputElement)
            {
                yield return inputElement;
            }
        }
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
    
    /// <summary>
    /// 更新边界（供渲染器使用）
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
    
    public override void Build(IBuildContext context) { }
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
    
    /// <summary>
    /// 点击事件 (新 API)
    /// </summary>
    public event EventHandler? Click;
    
    /// <summary>
    /// 点击事件 (兼容旧 API，由 Source Generator 使用)
    /// </summary>
    public event EventHandler? OnClick
    {
        add => Click += value;
        remove => Click -= value;
    }
    
    public Button()
    {
        IsFocusable = true;
        _bounds = new Rect(0, 0, 100, 40); // 默认按钮大小
        
        Tapped += (s, e) =>
        {
            Console.WriteLine($"[Button.Tapped] Button clicked! IsEnabled={IsEnabled}, Click handlers={Click?.GetInvocationList()?.Length ?? 0}");
            if (IsEnabled)
            {
                Click?.Invoke(this, EventArgs.Empty);
            }
        };
    }
    
    public override void Build(IBuildContext context) { }
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
}