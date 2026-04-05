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
    /// 安排控件位置和尺寸
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
    /// 子元素间距
    /// </summary>
    public double Spacing { get; set; } = 0;
    
    /// <summary>
    /// 内边距
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
    /// 测量布局所需尺寸 - 实现真正的 Measure 机制
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
        
        // 测量每个子元素
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
        
        // 添加内边距
        totalWidth += paddingValue * 2;
        totalHeight += paddingValue * 2;
        
        _desiredSize = new Size(totalWidth, totalHeight);
        return _desiredSize;
    }
    
    /// <summary>
    /// 安排子元素位置 - 实现真正的 Arrange 机制
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
                
                // 安排子元素
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
                
                // 安排子元素
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
    /// 测量子元素高度 - 使用真正的 Measure 机制而非硬编码
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
        
        // 行高通常是字体大小的 1.2-1.5 倍
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
            
            context.DrawText(Text, x, bounds.Y, scaledFontSize, FontFamily, FontWeight, Color);
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
    
    /// <summary>
    /// 字体大小
    /// </summary>
    public double FontSize { get; set; } = 14;
    
    /// <summary>
    /// 圆角半径
    /// </summary>
    public double CornerRadius { get; set; } = 4;
    
    public string? FontFamily { get; set; }
    
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
        
        Tapped += (s, e) =>
        {
            if (IsEnabled)
            {
                Click?.Invoke(this, EventArgs.Empty);
            }
        };
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
        
        // 添加内边距（水平各 20px）
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
        context.DrawRoundRect(bounds, BackgroundColor ?? "#007AFF", scaledCornerRadius);
        
        if (!string.IsNullOrEmpty(Text))
        {
            var scaledFontSize = FontSize * context.Scale;
            var textWidth = context.MeasureText(Text, scaledFontSize, FontFamily);
            var x = bounds.X + (bounds.Width - textWidth) / 2;
            var y = bounds.Y + bounds.Height / 2 - scaledFontSize / 2;
            context.DrawText(Text, x, y, scaledFontSize, FontFamily, null, TextColor);
        }
    }
}

/// <summary>
/// 文本输入控件 - 支持光标和键盘输入
/// </summary>
public class TextInput : InteractiveControl
{
    private string? _text;
    private int _cursorPosition = 0;
    private bool _isCursorVisible = true;
    private DateTime _lastCursorToggle = DateTime.Now;
    private const double CursorBlinkIntervalMs = 500;
    
    /// <summary>
    /// 输入文本
    /// </summary>
    public string? Text
    {
        get => _text;
        set
        {
            if (_text != value)
            {
                var oldText = _text;
                _text = value;
                // 确保光标位置在有效范围内
                _cursorPosition = Math.Min(_cursorPosition, _text?.Length ?? 0);
                TextChanged?.Invoke(this, new ValueChangedEventArgs<string?>(oldText, value));
                StateHasChanged();
            }
        }
    }
    
    /// <summary>
    /// 占位符文本
    /// </summary>
    public string? Placeholder { get; set; }
    
    /// <summary>
    /// 字体大小
    /// </summary>
    public double FontSize { get; set; } = 14;
    
    /// <summary>
    /// 背景颜色
    /// </summary>
    public string? BackgroundColor { get; set; } = "#FFFFFF";
    
    /// <summary>
    /// 边框颜色（聚焦时）
    /// </summary>
    public string? FocusBorderColor { get; set; } = "#007AFF";
    
    /// <summary>
    /// 圆角半径
    /// </summary>
    public double CornerRadius { get; set; } = 4;
    
    /// <summary>
    /// 内边距
    /// </summary>
    public double Padding { get; set; } = 8;
    
    /// <summary>
    /// 是否为密码输入
    /// </summary>
    public bool IsPassword { get; set; } = false;
    
    /// <summary>
    /// 光标位置（字符索引）
    /// </summary>
    public int CursorPosition
    {
        get => _cursorPosition;
        set
        {
            var maxPos = _text?.Length ?? 0;
            _cursorPosition = Math.Clamp(value, 0, maxPos);
        }
    }
    
    /// <summary>
    /// 文本变化事件
    /// </summary>
    public event EventHandler<ValueChangedEventArgs<string?>>? TextChanged;
    
    public TextInput()
    {
        IsFocusable = true;
        _bounds = new Rect(0, 0, 200, 30);
        _desiredSize = new Size(200, 30);
        
        // 订阅键盘事件
        KeyDown += OnKeyDown;
        TextInput += OnTextInput;
    }
    
    /// <summary>
    /// 设置文本（保留光标位置）
    /// </summary>
    public void SetText(string? newText)
    {
        if (_text != newText)
        {
            var oldText = _text;
            _text = newText;
            TextChanged?.Invoke(this, new ValueChangedEventArgs<string?>(oldText, newText));
            StateHasChanged();
        }
    }
    
    /// <summary>
    /// 处理键盘按下事件
    /// </summary>
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (!IsFocused)
            return;
        
        switch (e.Key)
        {
            case Key.Back:
                // 删除光标前的字符
                if (_cursorPosition > 0 && !string.IsNullOrEmpty(_text))
                {
                    _text = _text.Remove(_cursorPosition - 1, 1);
                    _cursorPosition--;
                    TextChanged?.Invoke(this, new ValueChangedEventArgs<string?>(_text, _text));
                    e.Handled = true;
                    StateHasChanged();
                }
                break;
                
            case Key.Delete:
                // 删除光标后的字符
                if (_cursorPosition < (_text?.Length ?? 0) && !string.IsNullOrEmpty(_text))
                {
                    _text = _text.Remove(_cursorPosition, 1);
                    TextChanged?.Invoke(this, new ValueChangedEventArgs<string?>(_text, _text));
                    e.Handled = true;
                    StateHasChanged();
                }
                break;
                
            case Key.Left:
                // 移动光标向左
                if (_cursorPosition > 0)
                {
                    _cursorPosition--;
                    e.Handled = true;
                    StateHasChanged();
                }
                break;
                
            case Key.Right:
                // 移动光标向右
                if (_cursorPosition < (_text?.Length ?? 0))
                {
                    _cursorPosition++;
                    e.Handled = true;
                    StateHasChanged();
                }
                break;
                
            case Key.Home:
                // 移动光标到开头
                _cursorPosition = 0;
                e.Handled = true;
                StateHasChanged();
                break;
                
            case Key.End:
                // 移动光标到末尾
                _cursorPosition = _text?.Length ?? 0;
                e.Handled = true;
                StateHasChanged();
                break;
                
            case Key.Enter:
                // 触发提交事件（可选）
                e.Handled = true;
                break;
        }
    }
    
    /// <summary>
    /// 处理文本输入事件
    /// </summary>
    private void OnTextInput(object? sender, TextInputEventArgs e)
    {
        if (!IsFocused || string.IsNullOrEmpty(e.Text))
            return;
        
        // 在光标位置插入文本
        var before = _text?.Substring(0, _cursorPosition) ?? "";
        var after = _text?.Substring(_cursorPosition) ?? "";
        var newText = before + e.Text + after;
        
        _text = newText;
        _cursorPosition += e.Text.Length;
        
        TextChanged?.Invoke(this, new ValueChangedEventArgs<string?>(null, newText));
        StateHasChanged();
    }
    
    /// <summary>
    /// 测量输入框所需尺寸
    /// </summary>
    public override Size Measure(Size availableSize, IDrawingContext context)
    {
        var scaledFontSize = FontSize * context.Scale;
        var scaledPadding = Padding * context.Scale;
        
        // 默认高度基于字体大小和内边距
        var height = scaledFontSize * 1.5 + scaledPadding * 2;
        
        // 宽度根据文本内容或可用空间
        double width;
        if (!string.IsNullOrEmpty(_text))
        {
            var textWidth = context.MeasureText(_text, scaledFontSize, null);
            width = textWidth + scaledPadding * 2 + 20 * context.Scale; // 额外空间用于光标
        }
        else
        {
            width = 200 * context.Scale; // 默认宽度
        }
        
        // 限制最大宽度
        if (!availableSize.IsEmpty && availableSize.Width < width)
        {
            width = availableSize.Width;
        }
        
        _desiredSize = new Size(width, height);
        return _desiredSize;
    }
    
    public override void Build(IBuildContext context) { }
    
    public override void Render(IDrawingContext context, Rect bounds)
    {
        UpdateBounds(bounds);
        
        var scaledFontSize = FontSize * context.Scale;
        var scaledCornerRadius = CornerRadius * context.Scale;
        var scaledPadding = Padding * context.Scale;
        
        // 绘制背景
        var bgColor = BackgroundColor ?? "#FFFFFF";
        context.DrawRoundRect(bounds, bgColor, scaledCornerRadius);
        
        // 绘制边框（聚焦时使用蓝色）
        var borderColor = IsFocused ? (FocusBorderColor ?? "#007AFF") : "#CCCCCC";
        context.DrawRectangle(bounds, null, borderColor, 1 * context.Scale, scaledCornerRadius);
        
        // 计算文本绘制区域
        var textBounds = new Rect(
            bounds.X + scaledPadding,
            bounds.Y + scaledPadding,
            bounds.Width - scaledPadding * 2,
            bounds.Height - scaledPadding * 2);
        
        // 绘制文本或占位符
        var displayText = _text;
        if (IsPassword && !string.IsNullOrEmpty(_text))
        {
            // 密码模式显示为圆点
            displayText = new string('*', _text.Length);
        }
        
        if (!string.IsNullOrEmpty(displayText))
        {
            context.DrawText(displayText, textBounds.X, textBounds.Y, scaledFontSize);
            
            // 绘制光标（聚焦时）
            if (IsFocused && ShouldShowCursor())
            {
                DrawCursor(context, textBounds, scaledFontSize);
            }
        }
        else if (!string.IsNullOrEmpty(Placeholder))
        {
            context.DrawText(Placeholder, textBounds.X, textBounds.Y, scaledFontSize, null, null, "#888888");
            
            // 绘制光标（聚焦时，无文本时光标在开头）
            if (IsFocused && ShouldShowCursor())
            {
                DrawCursor(context, textBounds, scaledFontSize);
            }
        }
    }
    
    /// <summary>
    /// 判断是否应该显示光标（闪烁效果）
    /// </summary>
    private bool ShouldShowCursor()
    {
        var now = DateTime.Now;
        var elapsed = (now - _lastCursorToggle).TotalMilliseconds;
        
        if (elapsed >= CursorBlinkIntervalMs)
        {
            _isCursorVisible = !_isCursorVisible;
            _lastCursorToggle = now;
        }
        
        return _isCursorVisible;
    }
    
    /// <summary>
    /// 绘制光标
    /// </summary>
    private void DrawCursor(IDrawingContext context, Rect textBounds, double scaledFontSize)
    {
        // 计算光标位置（基于 _cursorPosition）
        double cursorX = textBounds.X;
        
        if (!string.IsNullOrEmpty(_text) && _cursorPosition > 0)
        {
            // 测量光标前文本的宽度
            var textBeforeCursor = _text.Substring(0, _cursorPosition);
            if (IsPassword)
            {
                textBeforeCursor = new string('*', _cursorPosition);
            }
            cursorX += context.MeasureText(textBeforeCursor, scaledFontSize, null);
        }
        
        // 光标高度和位置
        var cursorHeight = scaledFontSize;
        var cursorY = textBounds.Y;
        
        // 绘制细线作为光标
        var cursorBounds = new Rect(cursorX, cursorY, 2 * context.Scale, cursorHeight);
        context.DrawRectangle(cursorBounds, "#000000");
    }
    
    protected override void OnGotFocus()
    {
        base.OnGotFocus();
        _isCursorVisible = true;
        _lastCursorToggle = DateTime.Now;
        StateHasChanged();
    }
    
    protected override void OnLostFocus()
    {
        base.OnLostFocus();
        _isCursorVisible = false;
        StateHasChanged();
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
            context.DrawText(Label, bounds.X + scaledSize + 8 * context.Scale, bounds.Y, 14 * context.Scale);
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
    /// 图片源路径
    /// </summary>
    public string? Source { get; set; }
    
    /// <summary>
    /// 宽度（-1 表示自动）
    /// </summary>
    public double Width { get; set; } = -1;
    
    /// <summary>
    /// 高度（-1 表示自动）
    /// </summary>
    public double Height { get; set; } = -1;
    
    /// <summary>
    /// 拉伸模式
    /// </summary>
    public Stretch Stretch { get; set; } = Stretch.Uniform;
    
    public override void Build(IBuildContext context) { }
    
    public override void Render(IDrawingContext context, Rect bounds)
    {
        // 如果没有图片源，显示占位符
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