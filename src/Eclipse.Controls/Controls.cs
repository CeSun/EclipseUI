using Eclipse.Core;
using Eclipse.Core.Abstractions;

namespace Eclipse.Controls;

/// <summary>
/// 垂直堆叠布局
/// </summary>
public class StackLayout : ComponentBase
{
    public Orientation Orientation { get; set; } = Orientation.Vertical;
    public string? Spacing { get; set; } = "0";
    public string? BackgroundColor { get; set; }
    public string? Padding { get; set; } = "0";
    
    /// <summary>
    /// 获取解析后的间距
    /// </summary>
    public double GetSpacing() => double.TryParse(Spacing, out var spacing) ? spacing : 0;
    
    /// <summary>
    /// 获取解析后的内边距
    /// </summary>
    public double GetPadding() => double.TryParse(Padding, out var padding) ? padding : 0;
    
    public override void Render(IBuildContext context) { }
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
    public TextAlignment TextAlignment { get; set; } = TextAlignment.Left;
    
    /// <summary>
    /// 获取解析后的字体大小
    /// </summary>
    public double GetFontSize() => double.TryParse(FontSize, out var size) ? size : 14;
    
    public override void Render(IBuildContext context) { }
}

public enum TextAlignment
{
    Left,
    Center,
    Right
}

/// <summary>
/// 按钮
/// </summary>
public class Button : ComponentBase
{
    public string? Text { get; set; }
    public string? BackgroundColor { get; set; } = "#007AFF";
    public string? TextColor { get; set; } = "White";
    public string? FontSize { get; set; } = "14";
    public bool IsEnabled { get; set; } = true;
    public string? CornerRadius { get; set; } = "4";
    public string? Padding { get; set; } = "8";
    
    /// <summary>
    /// 获取解析后的字体大小
    /// </summary>
    public double GetFontSize() => double.TryParse(FontSize, out var size) ? size : 14;
    
    /// <summary>
    /// 获取解析后的圆角半径
    /// </summary>
    public double GetCornerRadius() => double.TryParse(CornerRadius, out var radius) ? radius : 4;
    
    public event EventHandler? OnClick;
    
    public override void Render(IBuildContext context) { }
}

/// <summary>
/// 文本输入框
/// </summary>
public class TextInput : ComponentBase
{
    public string? Text { get; set; }
    public string? Placeholder { get; set; }
    public double FontSize { get; set; } = 14;
    public string? BackgroundColor { get; set; }
    public string? BorderColor { get; set; }
    public double CornerRadius { get; set; } = 4;
    public double Padding { get; set; } = 8;
    public bool IsEnabled { get; set; } = true;
    public bool IsPassword { get; set; } = false;
    
    public event EventHandler<ValueChangedEventArgs<string?>>? OnTextChanged;
    
    public override void Render(IBuildContext context) { }
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
public class CheckBox : ComponentBase
{
    public bool IsChecked { get; set; }
    public string? Label { get; set; }
    public string? CheckedColor { get; set; }
    public double Size { get; set; } = 20;
    public bool IsEnabled { get; set; } = true;
    
    public event EventHandler<ValueChangedEventArgs<bool>>? OnCheckedChanged;
    
    public override void Render(IBuildContext context) { }
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
    
    public override void Render(IBuildContext context) { }
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
    
    public override void Render(IBuildContext context) { }
}