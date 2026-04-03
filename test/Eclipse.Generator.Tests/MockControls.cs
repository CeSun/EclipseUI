using System;
using Eclipse.Core;
using Eclipse.Core.Abstractions;

namespace Eclipse.Generator.Tests.Controls;

/// <summary>
/// 垂直堆叠布局
/// </summary>
public class StackLayout : ComponentBase
{
    public Orientation Orientation { get; set; } = Orientation.Vertical;
    public double Spacing { get; set; } = 0;
    public string? BackgroundColor { get; set; }
    public double Padding { get; set; } = 0;

    public override void Render(IRenderContext context)
    {
        context.SetAttribute(nameof(Orientation), Orientation);
        context.SetAttribute(nameof(Spacing), Spacing);
        if (BackgroundColor != null)
            context.SetAttribute(nameof(BackgroundColor), BackgroundColor);
        context.SetAttribute(nameof(Padding), Padding);
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
/// 文本标签（显示组件，不支持双向绑定）
/// </summary>
public class Label : ComponentBase
{
    public string? Text { get; set; }
    public double FontSize { get; set; } = 14;
    public string? Color { get; set; }
    public string? FontWeight { get; set; }
    public TextAlignment TextAlignment { get; set; } = TextAlignment.Left;

    public override void Render(IRenderContext context)
    {
        if (Text != null)
            context.SetText(Text);
        context.SetAttribute(nameof(FontSize), FontSize);
        if (Color != null)
            context.SetAttribute(nameof(Color), Color);
        if (FontWeight != null)
            context.SetAttribute(nameof(FontWeight), FontWeight);
        context.SetAttribute(nameof(TextAlignment), TextAlignment);
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
    private string? _text;
    public string? Text 
    { 
        get => _text;
        set
        {
            if (_text != value)
            {
                var old = _text;
                _text = value;
                TextChanged?.Invoke(this, new ValueChangedEventArgs<string?>(old, value));
            }
        }
    }
    
    public string? BackgroundColor { get; set; }
    public string? TextColor { get; set; }
    public double FontSize { get; set; } = 14;
    public bool IsEnabled { get; set; } = true;
    public double CornerRadius { get; set; } = 4;
    public double Padding { get; set; } = 8;

    public event EventHandler<ValueChangedEventArgs<string?>>? TextChanged;
    public event EventHandler? OnClick;

    public override void Render(IRenderContext context)
    {
        if (Text != null)
            context.SetText(Text);
        if (BackgroundColor != null)
            context.SetAttribute(nameof(BackgroundColor), BackgroundColor);
        if (TextColor != null)
            context.SetAttribute(nameof(TextColor), TextColor);
        context.SetAttribute(nameof(FontSize), FontSize);
        context.SetAttribute(nameof(IsEnabled), IsEnabled);
        context.SetAttribute(nameof(CornerRadius), CornerRadius);
        context.SetAttribute(nameof(Padding), Padding);
    }
}

/// <summary>
/// 文本输入框
/// </summary>
public class TextInput : ComponentBase
{
    private string? _text;
    public string? Text 
    { 
        get => _text;
        set
        {
            if (_text != value)
            {
                var old = _text;
                _text = value;
                TextChanged?.Invoke(this, new ValueChangedEventArgs<string?>(old, value));
            }
        }
    }
    
    public string? Placeholder { get; set; }
    public double FontSize { get; set; } = 14;
    public string? BackgroundColor { get; set; }
    public string? BorderColor { get; set; }
    public double CornerRadius { get; set; } = 4;
    public double Padding { get; set; } = 8;
    public bool IsEnabled { get; set; } = true;
    public bool IsPassword { get; set; } = false;

    public event EventHandler<ValueChangedEventArgs<string?>>? TextChanged;
    public event EventHandler<ValueChangedEventArgs<string?>>? OnTextChanged;

    public override void Render(IRenderContext context)
    {
        if (Text != null)
            context.SetAttribute(nameof(Text), Text);
        if (Placeholder != null)
            context.SetAttribute(nameof(Placeholder), Placeholder);
        context.SetAttribute(nameof(FontSize), FontSize);
        if (BackgroundColor != null)
            context.SetAttribute(nameof(BackgroundColor), BackgroundColor);
        if (BorderColor != null)
            context.SetAttribute(nameof(BorderColor), BorderColor);
        context.SetAttribute(nameof(CornerRadius), CornerRadius);
        context.SetAttribute(nameof(Padding), Padding);
        context.SetAttribute(nameof(IsEnabled), IsEnabled);
        context.SetAttribute(nameof(IsPassword), IsPassword);
    }
}

/// <summary>
/// 复选框
/// </summary>
public class CheckBox : ComponentBase
{
    private bool _isChecked;
    public bool IsChecked 
    { 
        get => _isChecked;
        set
        {
            if (_isChecked != value)
            {
                var old = _isChecked;
                _isChecked = value;
                IsCheckedChanged?.Invoke(this, new ValueChangedEventArgs<bool>(old, value));
            }
        }
    }
    
    public string? Label { get; set; }
    public string? CheckedColor { get; set; }
    public double Size { get; set; } = 20;
    public bool IsEnabled { get; set; } = true;

    public event EventHandler<ValueChangedEventArgs<bool>>? IsCheckedChanged;
    public event EventHandler<ValueChangedEventArgs<bool>>? OnCheckedChanged;

    public override void Render(IRenderContext context)
    {
        context.SetAttribute(nameof(IsChecked), IsChecked);
        if (Label != null)
            context.SetAttribute(nameof(Label), Label);
        if (CheckedColor != null)
            context.SetAttribute(nameof(CheckedColor), CheckedColor);
        context.SetAttribute(nameof(Size), Size);
        context.SetAttribute(nameof(IsEnabled), IsEnabled);
    }
}

/// <summary>
/// 开关
/// </summary>
public class Switch : ComponentBase
{
    private bool _isToggled;
    public bool IsToggled 
    { 
        get => _isToggled;
        set
        {
            if (_isToggled != value)
            {
                var old = _isToggled;
                _isToggled = value;
                IsToggledChanged?.Invoke(this, new ValueChangedEventArgs<bool>(old, value));
            }
        }
    }
    
    public string? OnColor { get; set; }
    public string? OffColor { get; set; }
    public double Scale { get; set; } = 1.0;
    public bool IsEnabled { get; set; } = true;

    public event EventHandler<ValueChangedEventArgs<bool>>? IsToggledChanged;

    public override void Render(IRenderContext context)
    {
        context.SetAttribute(nameof(IsToggled), IsToggled);
        if (OnColor != null)
            context.SetAttribute(nameof(OnColor), OnColor);
        if (OffColor != null)
            context.SetAttribute(nameof(OffColor), OffColor);
        context.SetAttribute(nameof(Scale), Scale);
        context.SetAttribute(nameof(IsEnabled), IsEnabled);
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
    public Aspect Aspect { get; set; } = Aspect.AspectFit;
    public string? BackgroundColor { get; set; }

    public override void Render(IRenderContext context)
    {
        if (Source != null)
            context.SetAttribute(nameof(Source), Source);
        if (Width > 0)
            context.SetAttribute(nameof(Width), Width);
        if (Height > 0)
            context.SetAttribute(nameof(Height), Height);
        context.SetAttribute(nameof(Aspect), Aspect);
        if (BackgroundColor != null)
            context.SetAttribute(nameof(BackgroundColor), BackgroundColor);
    }
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

    public override void Render(IRenderContext context)
    {
        context.SetAttribute(nameof(Padding), Padding);
        if (BackgroundColor != null)
            context.SetAttribute(nameof(BackgroundColor), BackgroundColor);
        if (BorderColor != null)
            context.SetAttribute(nameof(BorderColor), BorderColor);
        context.SetAttribute(nameof(BorderWidth), BorderWidth);
        context.SetAttribute(nameof(CornerRadius), CornerRadius);
    }
}

/// <summary>
/// 滚动视图
/// </summary>
public class ScrollView : ComponentBase
{
    public ScrollOrientation Orientation { get; set; } = ScrollOrientation.Vertical;
    public string? BackgroundColor { get; set; }
    public double Padding { get; set; } = 0;

    public override void Render(IRenderContext context)
    {
        context.SetAttribute(nameof(Orientation), Orientation);
        if (BackgroundColor != null)
            context.SetAttribute(nameof(BackgroundColor), BackgroundColor);
        context.SetAttribute(nameof(Padding), Padding);
    }
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

    public override void Render(IRenderContext context)
    {
        context.SetAttribute(nameof(Size), Size);
    }
}

/// <summary>
/// 分隔线
/// </summary>
public class Divider : ComponentBase
{
    public string? Color { get; set; }
    public double Thickness { get; set; } = 1;
    public double Margin { get; set; } = 0;

    public override void Render(IRenderContext context)
    {
        if (Color != null)
            context.SetAttribute(nameof(Color), Color);
        context.SetAttribute(nameof(Thickness), Thickness);
        context.SetAttribute(nameof(Margin), Margin);
    }
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

    public override void Render(IRenderContext context)
    {
        if (BackgroundColor != null)
            context.SetAttribute(nameof(BackgroundColor), BackgroundColor);
        context.SetAttribute(nameof(Padding), Padding);
        context.SetAttribute(nameof(CornerRadius), CornerRadius);
        context.SetAttribute(nameof(Elevation), Elevation);
        if (BorderColor != null)
            context.SetAttribute(nameof(BorderColor), BorderColor);
    }
}