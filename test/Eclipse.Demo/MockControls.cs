using System;
using Eclipse.Core;
using Eclipse.Core.Abstractions;

namespace Eclipse.Demo.Controls;

/// <summary>
/// 垂直堆叠布局
/// </summary>
public class StackLayout : ComponentBase
{
    public Orientation Orientation { get; set; } = Orientation.Vertical;
    public double Spacing { get; set; } = 0;
    public string? BackgroundColor { get; set; }
    public double Padding { get; set; } = 0;

    // Build 方法留空 - 属性由生成代码通过 BeginComponent(out var component) 设置
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
    public double FontSize { get; set; } = 14;
    public string? Color { get; set; }
    public string? FontWeight { get; set; }
    public TextAlignment TextAlignment { get; set; } = TextAlignment.Left;

    public override void Build(IBuildContext context)
    {
        // Build 空实�?- 属性由生成代码通过 BeginComponent(out var component) 设置
    }
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
    public string? BackgroundColor { get; set; }
    public string? TextColor { get; set; }
    public double FontSize { get; set; } = 14;
    public bool IsEnabled { get; set; } = true;
    public double CornerRadius { get; set; } = 4;
    public double Padding { get; set; } = 8;

    public event EventHandler? OnClick;

    public override void Build(IBuildContext context)
    {
        // Build 空实�?- 属性由生成代码通过 BeginComponent(out var component) 设置
    }
}

/// <summary>
/// 文本输入�?/// </summary>
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
public class CheckBox : ComponentBase
{
    public bool IsChecked { get; set; }
    public string? Label { get; set; }
    public string? CheckedColor { get; set; }
    public double Size { get; set; } = 20;
    public bool IsEnabled { get; set; } = true;

    public event EventHandler<ValueChangedEventArgs<bool>>? OnCheckedChanged;

    public override void Build(IBuildContext context) { }
}

/// <summary>
/// 开�?/// </summary>
public class Switch : ComponentBase
{
    public bool IsToggled { get; set; }
    public string? OnColor { get; set; }
    public string? OffColor { get; set; }
    public double Scale { get; set; } = 1.0;
    public bool IsEnabled { get; set; } = true;

    public event EventHandler<ValueChangedEventArgs<bool>>? IsToggledChanged;

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
    public Aspect Aspect { get; set; } = Aspect.AspectFit;
    public string? BackgroundColor { get; set; }

    public override void Build(IBuildContext context) { }
}

public enum Aspect
{
    Fill,
    AspectFit,
    AspectFill
}

/// <summary>
/// 边框容器
/// </summary>
public class Border : ComponentBase
{
    public double Padding { get; set; } = 0;
    public string? BackgroundColor { get; set; }
    public string? BorderColor { get; set; }
    public double BorderWidth { get; set; } = 0;
    public double CornerRadius { get; set; } = 0;

    public override void Build(IBuildContext context) { }
}

/// <summary>
/// 滚动视图
/// </summary>
public class ScrollView : ComponentBase
{
    public ScrollOrientation Orientation { get; set; } = ScrollOrientation.Vertical;
    public string? BackgroundColor { get; set; }
    public double Padding { get; set; } = 0;

    public override void Build(IBuildContext context) { }
}

public enum ScrollOrientation
{
    Vertical,
    Horizontal,
    Both
}

/// <summary>
/// 空白占位
/// </summary>
public class Spacer : ComponentBase
{
    public double Size { get; set; } = 10;

    public override void Build(IBuildContext context) { }
}

/// <summary>
/// 分隔�?/// </summary>
public class Divider : ComponentBase
{
    public string? Color { get; set; }
    public double Thickness { get; set; } = 1;
    public double Margin { get; set; } = 0;

    public override void Build(IBuildContext context) { }
}

/// <summary>
/// 卡片容器
/// </summary>
public class Card : ComponentBase
{
    public string? BackgroundColor { get; set; }
    public double Padding { get; set; } = 16;
    public double CornerRadius { get; set; } = 8;
    public double Elevation { get; set; } = 2;
    public string? BorderColor { get; set; }

    public override void Build(IBuildContext context) { }
}
