using Eclipse.Core;
using Eclipse.Core.Abstractions;
using Eclipse.Input;
using Eclipse.Rendering;
using System.Collections.Generic;

namespace Eclipse.Controls;

/// <summary>
/// 可交互控件基�?- 支持输入事件
/// </summary>
public abstract class InteractiveControl : InputElementBase
{
    protected Rect _bounds = new(0, 0, 100, 40);
    protected Size _desiredSize = new(100, 40);
    
    public bool IsEnabled { get; set; } = true;
    
    public override bool IsInputEnabled => IsEnabled;
    public override bool IsVisible => true;
    public override Rect Bounds => _bounds;
    
    public void UpdateBounds(Rect bounds) => _bounds = bounds;
    
    /// <summary>
    /// 测量控件所需尺寸
    /// </summary>
    public virtual Size Measure(Size availableSize, IDrawingContext context)
    {
        return _desiredSize;
    }
    
    /// <summary>
    /// 安排控件位置和尺�?
    /// </summary>
    public virtual void Arrange(Rect finalBounds, IDrawingContext context)
    {
        _bounds = finalBounds;
    }
    
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
    
    /// <summary>
    /// 子元素间�?
    /// </summary>
    public double Spacing { get; set; } = 0;
    
    /// <summary>
    /// 内边�?
    /// </summary>
    public double Padding { get; set; } = 0;
    
    public string? BackgroundColor { get; set; }
    
    private Rect _bounds;
    private Size _desiredSize = Size.Zero;
    
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
    
    /// <summary>
    /// 测量布局所需尺寸 - 实现真正�?Measure 机制
    /// </summary>
    public Size Measure(Size availableSize, IDrawingContext context)
    {
        if (Children.Count == 0)
        {
            _desiredSize = new Size(Padding * 2, Padding * 2);
            return _desiredSize;
        }
        
        var spacingValue = Spacing * context.Scale;
        var paddingValue = Padding * context.Scale;
        
        // 计算内容可用尺寸
        var contentAvailableSize = new Size(
            availableSize.Width - paddingValue * 2,
            availableSize.Height - paddingValue * 2);
        
        double totalWidth = 0;
        double totalHeight = 0;
        double maxChildWidth = 0;
        double maxChildHeight = 0;
        
        // 测量每个子元�?
        foreach (var child in Children)
        {
            Size childSize;
            
            if (child is InteractiveControl interactiveControl)
            {
                childSize = interactiveControl.Measure(contentAvailableSize, context);
            }
            else if (child is StackLayout stackLayout)
            {
                childSize = stackLayout.Measure(contentAvailableSize, context);
            }
            else if (child is Label label)
            {
                childSize = label.Measure(contentAvailableSize, context);
            }
            else if (child is TextContent textContent)
            {
                childSize = textContent.Measure(contentAvailableSize, context);
            }
            else
            {
                // 默认尺寸
                childSize = new Size(40 * context.Scale, 40 * context.Scale);
            }
            
            maxChildWidth = Math.Max(maxChildWidth, childSize.Width);
            maxChildHeight = Math.Max(maxChildHeight, childSize.Height);
            
            if (Orientation == Orientation.Vertical)
            {
                totalWidth = Math.Max(totalWidth, childSize.Width);
                totalHeight += childSize.Height;
            }
            else
            {
                totalWidth += childSize.Width;
                totalHeight = Math.Max(totalHeight, childSize.Height);
            }
        }
        
        // 添加间距
        if (Children.Count > 1)
        {
            if (Orientation == Orientation.Vertical)
            {
                totalHeight += spacingValue * (Children.Count - 1);
            }
            else
            {
                totalWidth += spacingValue * (Children.Count - 1);
            }
        }
        
        // 添加内边�?
        totalWidth += paddingValue * 2;
        totalHeight += paddingValue * 2;
        
        _desiredSize = new Size(totalWidth, totalHeight);
        return _desiredSize;
    }
    
    /// <summary>
    /// 安排子元素位�?- 实现真正�?Arrange 机制
    /// </summary>
    public void Arrange(Rect finalBounds, IDrawingContext context)
    {
        _bounds = finalBounds;
        
        var spacingValue = Spacing * context.Scale;
        var paddingValue = Padding * context.Scale;
        
        var contentBounds = new Rect(
            finalBounds.X + paddingValue,
            finalBounds.Y + paddingValue,
            finalBounds.Width - paddingValue * 2,
            finalBounds.Height - paddingValue * 2);
        
        if (Orientation == Orientation.Vertical)
        {
            double y = contentBounds.Y;
            foreach (var child in Children)
            {
                Size childSize = GetChildDesiredSize(child, context);
                var childBounds = new Rect(contentBounds.X, y, contentBounds.Width, childSize.Height);
                
                // 安排子元�?
                ArrangeChild(child, childBounds, context);
                
                y += childSize.Height + spacingValue;
            }
        }
        else
        {
            double x = contentBounds.X;
            foreach (var child in Children)
            {
                Size childSize = GetChildDesiredSize(child, context);
                var childBounds = new Rect(x, contentBounds.Y, childSize.Width, contentBounds.Height);
                
                // 安排子元�?
                ArrangeChild(child, childBounds, context);
                
                x += childSize.Width + spacingValue;
            }
        }
    }
    
    private Size GetChildDesiredSize(IComponent child, IDrawingContext context)
    {
        if (child is InteractiveControl interactiveControl)
        {
            return interactiveControl.Measure(Size.Empty, context);
        }
        else if (child is StackLayout stackLayout)
        {
            return stackLayout.Measure(Size.Empty, context);
        }
        else if (child is Label label)
        {
            return label.Measure(Size.Empty, context);
        }
        else if (child is TextContent textContent)
        {
            return textContent.Measure(Size.Empty, context);
        }
        return new Size(40 * context.Scale, 40 * context.Scale);
    }
    
    private void ArrangeChild(IComponent child, Rect bounds, IDrawingContext context)
    {
        if (child is InteractiveControl interactiveControl)
        {
            interactiveControl.Arrange(bounds, context);
        }
        else if (child is StackLayout stackLayout)
        {
            stackLayout.Arrange(bounds, context);
        }
    }
    
    public override void Render(IDrawingContext context, Rect bounds)
    {
        UpdateBounds(bounds);
        
        var spacingValue = Spacing * context.Scale;
        var paddingValue = Padding * context.Scale;
        
        if (!string.IsNullOrEmpty(BackgroundColor))
        {
            context.DrawRectangle(bounds, BackgroundColor);
        }
        
        var contentBounds = new Rect(
            bounds.X + paddingValue,
            bounds.Y + paddingValue,
            bounds.Width - paddingValue * 2,
            bounds.Height - paddingValue * 2);
        
        if (Orientation == Orientation.Vertical)
        {
            double y = contentBounds.Y;
            foreach (var child in Children)
            {
                var childHeight = MeasureChildHeight(child, context);
                var childBounds = new Rect(contentBounds.X, y, contentBounds.Width, childHeight);
                child.Render(context, childBounds);
                y += childHeight + spacingValue;
            }
        }
        else
        {
            double x = contentBounds.X;
            var childWidth = (contentBounds.Width - spacingValue * (Children.Count - 1)) / Children.Count;
            foreach (var child in Children)
            {
                var childBounds = new Rect(x, contentBounds.Y, childWidth, contentBounds.Height);
                child.Render(context, childBounds);
                x += childWidth + spacingValue;
            }
        }
    }
    
    /// <summary>
    /// 测量子元素高�?- 使用真正�?Measure 机制而非硬编�?
    /// </summary>
    private double MeasureChildHeight(IComponent component, IDrawingContext context)
    {
        if (component is InteractiveControl interactiveControl)
        {
            var size = interactiveControl.Measure(Size.Empty, context);
            return size.Height;
        }
        else if (component is StackLayout stackLayout)
        {
            var size = stackLayout.Measure(Size.Empty, context);
            return size.Height;
        }
        else if (component is Label label)
        {
            var size = label.Measure(Size.Empty, context);
            return size.Height;
        }
        else if (component is TextContent textContent)
        {
            var size = textContent.Measure(Size.Empty, context);
            return size.Height;
        }
        // 默认高度
        return 40.0 * context.Scale;
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
    
    /// <summary>
    /// 字体大小
    /// </summary>
    public double FontSize { get; set; } = 14;
    
    public string? Color { get; set; }
    public string? FontWeight { get; set; }
    public string? FontFamily { get; set; }
    public TextAlignment TextAlignment { get; set; } = TextAlignment.Left;
    
    private Size _desiredSize = Size.Zero;
    
    /// <summary>
    /// 测量文本所需尺寸
    /// </summary>
    public Size Measure(Size availableSize, IDrawingContext context)
    {
        if (string.IsNullOrEmpty(Text))
        {
            _desiredSize = Size.Zero;
            return _desiredSize;
        }
        
        var scaledFontSize = FontSize * context.Scale;
        var textWidth = context.MeasureText(Text, scaledFontSize, FontFamily);
        
        // 行高通常是字体大小的 1.2-1.5 �?
        var lineHeight = scaledFontSize * 1.3;
        
        _desiredSize = new Size(textWidth, lineHeight);
        return _desiredSize;
    }
    
    public override void Build(IBuildContext context) { }
    
    public override void Render(IDrawingContext context, Rect bounds)
    {
        if (!string.IsNullOrEmpty(Text))
        {
            var scaledFontSize = FontSize * context.Scale;
            
            // 根据 TextAlignment 计算水平位置
            double x = bounds.X;
            if (TextAlignment == TextAlignment.Center)
            {
                var textWidth = context.MeasureText(Text, scaledFontSize, FontFamily);
                x = bounds.X + (bounds.Width - textWidth) / 2;
            }
            else if (TextAlignment == TextAlignment.Right)
            {
                var textWidth = context.MeasureText(Text, scaledFontSize, FontFamily);
                x = bounds.X + bounds.Width - textWidth;
            }
            
            // y 是文本视觉中心，文本从顶部开�?
            var y = bounds.Y + scaledFontSize * 0.5;
            context.DrawText(Text, x, y, scaledFontSize, FontFamily, FontWeight, Color);
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
    private bool _isPressed = false;
    private bool _isHovered = false;
    
    // === 文本属�?===
    public string? Text { get; set; }
    public double FontSize { get; set; } = 14;
    public string? FontFamily { get; set; }
    public string? TextColor { get; set; } = "White";
    
    // === 背景颜色（各状态） ===
    /// <summary>
    /// 默认背景颜色
    /// </summary>
    public string? BackgroundColor { get; set; } = "#007AFF";
    
    /// <summary>
    /// 鼠标悬停时的背景颜色
    /// </summary>
    public string? HoverBackgroundColor { get; set; }
    
    /// <summary>
    /// 按下时的背景颜色
    /// </summary>
    public string? PressedBackgroundColor { get; set; }
    
    /// <summary>
    /// 禁用时的背景颜色
    /// </summary>
    public string? DisabledBackgroundColor { get; set; } = "#CCCCCC";
    
    // === 文本颜色（各状态） ===
    /// <summary>
    /// 禁用时的文本颜色
    /// </summary>
    public string? DisabledTextColor { get; set; } = "#888888";
    
    // === 边框属�?===
    /// <summary>
    /// 边框颜色
    /// </summary>
    public string? BorderColor { get; set; }
    
    /// <summary>
    /// 边框宽度
    /// </summary>
    public double BorderWidth { get; set; } = 0;
    
    /// <summary>
    /// 圆角半径
    /// </summary>
    public double CornerRadius { get; set; } = 4;
    
    // === 事件 ===
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
        _desiredSize = new Size(100, 44);
        
        // 指针事件
        PointerEntered += OnPointerEntered;
        PointerExited += OnPointerExited;
        PointerPressed += OnPointerPressed;
        PointerReleased += OnPointerReleased;
        
        // Tapped 事件
        Tapped += (s, e) =>
        {
            if (IsEnabled)
            {
                Click?.Invoke(this, EventArgs.Empty);
            }
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
        _isPressed = false;
        StateHasChanged();
    }
    
    /// <summary>
    /// 获取当前状态的背景颜色
    /// </summary>
    private string GetCurrentBackgroundColor()
    {
        if (!IsEnabled)
            return DisabledBackgroundColor ?? "#CCCCCC";
        
        if (_isPressed)
            return PressedBackgroundColor ?? DarkenColor(BackgroundColor ?? "#007AFF", 0.2);
        
        if (_isHovered)
            return HoverBackgroundColor ?? DarkenColor(BackgroundColor ?? "#007AFF", 0.1);
        
        return BackgroundColor ?? "#007AFF";
    }
    
    /// <summary>
    /// 获取当前状态的文本颜色
    /// </summary>
    private string GetCurrentTextColor()
    {
        if (!IsEnabled)
            return DisabledTextColor ?? "#888888";
        
        return TextColor ?? "White";
    }
    
    /// <summary>
    /// 颜色加深（用于悬停和按下状态）
    /// </summary>
    private static string DarkenColor(string hexColor, double amount)
    {
        try
        {
            // 解析十六进制颜色
            if (hexColor.StartsWith("#"))
                hexColor = hexColor.Substring(1);
            
            int r = Convert.ToInt32(hexColor.Substring(0, 2), 16);
            int g = Convert.ToInt32(hexColor.Substring(2, 2), 16);
            int b = Convert.ToInt32(hexColor.Substring(4, 2), 16);
            
            // 加深
            r = (int)(r * (1 - amount));
            g = (int)(g * (1 - amount));
            b = (int)(b * (1 - amount));
            
            return $"#{r:X2}{g:X2}{b:X2}";
        }
        catch
        {
            return hexColor;
        }
    }
    
    /// <summary>
    /// 测量按钮所需尺寸
    /// </summary>
    public override Size Measure(Size availableSize, IDrawingContext context)
    {
        if (string.IsNullOrEmpty(Text))
        {
            _desiredSize = new Size(80 * context.Scale, 44 * context.Scale);
            return _desiredSize;
        }
        
        var scaledFontSize = FontSize * context.Scale;
        var textWidth = context.MeasureText(Text, scaledFontSize, FontFamily);
        
        // 添加内边距（水平�?20px�?
        var buttonWidth = textWidth + 40 * context.Scale;
        var buttonHeight = 44 * context.Scale;
        
        _desiredSize = new Size(buttonWidth, buttonHeight);
        return _desiredSize;
    }
    
    public override void Build(IBuildContext context) { }
    
    public override void Render(IDrawingContext context, Rect bounds)
    {
        UpdateBounds(bounds);
        
        var scaledCornerRadius = CornerRadius * context.Scale;
        var bgColor = GetCurrentBackgroundColor();
        var textColor = GetCurrentTextColor();
        
        // 绘制背景
        context.DrawRoundRect(bounds, bgColor, scaledCornerRadius);
        
        // 绘制边框
        if (BorderWidth > 0 && !string.IsNullOrEmpty(BorderColor))
        {
            context.DrawRectangle(bounds, null, BorderColor, BorderWidth * context.Scale, scaledCornerRadius);
        }
        
        // 绘制聚焦边框
        if (IsFocused && IsEnabled)
        {
            var focusBounds = new Rect(
                bounds.X - 2 * context.Scale,
                bounds.Y - 2 * context.Scale,
                bounds.Width + 4 * context.Scale,
                bounds.Height + 4 * context.Scale);
            context.DrawRectangle(focusBounds, null, "#007AFF", 2 * context.Scale, scaledCornerRadius + 2);
        }
        
        // 绘制文本
        if (!string.IsNullOrEmpty(Text))
        {
            var scaledFontSize = FontSize * context.Scale;
            var textWidth = context.MeasureText(Text, scaledFontSize, FontFamily);
            var x = bounds.X + (bounds.Width - textWidth) / 2;
            var y = bounds.Y + bounds.Height / 2; // 视觉中心
            context.DrawText(Text, x, y, scaledFontSize, FontFamily, null, textColor);
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
        _desiredSize = new Size(20, 20);
        
        Tapped += (s, e) =>
        {
            if (IsEnabled)
            {
                IsChecked = !IsChecked;
            }
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
            // 包含标签文本宽度
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
        var color = IsChecked ? (CheckedColor ?? "#007AFF") : "#CCCCCC";
        context.DrawRoundRect(checkBounds, color, 4 * context.Scale);
        
        if (!string.IsNullOrEmpty(Label))
        {
            var scaledFontSize = 14 * context.Scale;
            var textY = bounds.Y + scaledFontSize * 0.5; // 视觉中心
            context.DrawText(Label, bounds.X + scaledSize + 8 * context.Scale, textY, scaledFontSize);
        }
    }
}

/// <summary>
/// 图片控件 - 支持从文件加载图片并显示
/// </summary>
public class Image : ComponentBase
{
    private string? _loadedImageKey;
    
    /// <summary>
    /// 图片源路�?
    /// </summary>
    public string? Source { get; set; }
    
    /// <summary>
    /// 宽度�?1 表示自动�?
    /// </summary>
    public double Width { get; set; } = -1;
    
    /// <summary>
    /// 高度�?1 表示自动�?
    /// </summary>
    public double Height { get; set; } = -1;
    
    /// <summary>
    /// 拉伸模式
    /// </summary>
    public Stretch Stretch { get; set; } = Stretch.Uniform;
    
    public override void Build(IBuildContext context) { }
    
    public override void Render(IDrawingContext context, Rect bounds)
    {
        // 如果没有图片源，显示占位�?
        if (string.IsNullOrEmpty(Source))
        {
            context.DrawRectangle(bounds, "#EEEEEE");
            return;
        }
        
        // 加载图片（如果尚未加载）
        if (_loadedImageKey == null || !string.Equals(_loadedImageKey, Source, StringComparison.OrdinalIgnoreCase))
        {
            _loadedImageKey = context.LoadImage(Source);
        }
        
        // 如果加载失败，显示占位符
        if (_loadedImageKey == null)
        {
            context.DrawRectangle(bounds, "#EEEEEE");
            return;
        }
        
        // 绘制图片
        context.DrawImage(_loadedImageKey, bounds, Stretch);
    }
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