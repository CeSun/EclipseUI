using Eclipse.Core;
using Eclipse.Core.Abstractions;
using Eclipse.Input;
using Eclipse.Rendering;
using System;
using System.Collections.Generic;

namespace Eclipse.Controls;

/// <summary>
/// 可交互控件基类 - 支持输入事件
/// </summary>
public abstract class InteractiveControl : InputElementBase
{
    protected Size _desiredSize = new(100, 40);
    
    public bool IsEnabled { get; set; } = true;
    
    public override bool IsInputEnabled => IsEnabled;
    public override bool IsVisible => true;
    
    public virtual Size Measure(Size availableSize, IDrawingContext context)
    {
        return _desiredSize;
    }
    
    public virtual void Arrange(Rect finalBounds, IDrawingContext context)
    {
        UpdateBounds(finalBounds);
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
    public double Spacing { get; set; } = 0;
    public double Padding { get; set; } = 0;
    public Color BackgroundColor { get; set; } = Color.Transparent;
    
    /// <summary>
    /// 固定宽度（-1 表示自动）
    /// </summary>
    public double Width { get; set; } = -1;
    
    /// <summary>
    /// 固定高度（-1 表示自动）
    /// </summary>
    public double Height { get; set; } = -1;
    
    private Size _desiredSize = Size.Zero;
    
    public override bool IsVisible => true;
    
    public StackLayout()
    {
        IsHitTestVisible = false;
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
    
    public override void Build(IBuildContext context) { }
    
    public Size Measure(Size availableSize, IDrawingContext context)
    {
        if (Children.Count == 0)
        {
            _desiredSize = new Size(Padding * 2, Padding * 2);
            return _desiredSize;
        }
        
        var spacingValue = Spacing * context.Scale;
        var paddingValue = Padding * context.Scale;
        
        var contentAvailableSize = new Size(
            availableSize.Width - paddingValue * 2,
            availableSize.Height - paddingValue * 2);
        
        double totalWidth = 0;
        double totalHeight = 0;
        
        foreach (var child in Children)
        {
            Size childSize = GetChildSize(child, contentAvailableSize, context);
            
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
        
        if (Children.Count > 1)
        {
            if (Orientation == Orientation.Vertical)
                totalHeight += spacingValue * (Children.Count - 1);
            else
                totalWidth += spacingValue * (Children.Count - 1);
        }
        
        totalWidth += paddingValue * 2;
        totalHeight += paddingValue * 2;
        
        _desiredSize = new Size(totalWidth, totalHeight);
        return _desiredSize;
    }
    
    public void Arrange(Rect finalBounds, IDrawingContext context)
    {
        UpdateBounds(finalBounds);
        
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
                Size childSize = GetChildSize(child, new Size(contentBounds.Width, contentBounds.Height), context);
                var childBounds = new Rect(contentBounds.X, y, contentBounds.Width, childSize.Height);
                ArrangeChild(child, childBounds, context);
                y += childSize.Height + spacingValue;
            }
        }
        else
        {
            double x = contentBounds.X;
            foreach (var child in Children)
            {
                Size childSize = GetChildSize(child, new Size(contentBounds.Width, contentBounds.Height), context);
                var childBounds = new Rect(x, contentBounds.Y, childSize.Width, contentBounds.Height);
                ArrangeChild(child, childBounds, context);
                x += childSize.Width + spacingValue;
            }
        }
    }
    
    private Size GetChildSize(IComponent child, Size availableSize, IDrawingContext context)
    {
        if (child is InteractiveControl interactiveControl)
            return interactiveControl.Measure(availableSize, context);
        if (child is StackLayout stackLayout)
            return stackLayout.Measure(availableSize, context);
        if (child is DockPanel dockPanel)
            return dockPanel.Measure(availableSize, context);
        if (child is Label label)
            return label.Measure(availableSize, context);
        if (child is TextContent textContent)
            return textContent.Measure(availableSize, context);
        if (child is ScrollView scrollView)
            return scrollView.Measure(availableSize, context);
        if (child is ComponentBase componentBase)
        {
            double height = 0;
            foreach (var c in componentBase.Children)
                height += GetChildSize(c, availableSize, context).Height;
            return new Size(100 * context.Scale, height > 0 ? height : 40 * context.Scale);
        }
        return new Size(40 * context.Scale, 40 * context.Scale);
    }
    
    private void ArrangeChild(IComponent child, Rect bounds, IDrawingContext context)
    {
        if (child is InteractiveControl interactiveControl)
            interactiveControl.Arrange(bounds, context);
        else if (child is StackLayout stackLayout)
            stackLayout.Arrange(bounds, context);
    }
    
    public override void Render(IDrawingContext context, Rect bounds)
    {
        UpdateBounds(bounds);
        
        var spacingValue = Spacing * context.Scale;
        var paddingValue = Padding * context.Scale;
        
        if (BackgroundColor != Color.Transparent)
            context.DrawRectangle(bounds, BackgroundColor);
        
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
                var childHeight = GetChildSize(child, new Size(contentBounds.Width, contentBounds.Height), context).Height;
                var childBounds = new Rect(contentBounds.X, y, contentBounds.Width, childHeight);
                
                // 对 ScrollView 和 DockPanel 调用 Measure 和 Arrange
                if (child is ScrollView scrollView)
                {
                    scrollView.Measure(new Size(contentBounds.Width, contentBounds.Height), context);
                    scrollView.Arrange(childBounds, context);
                }
                else if (child is DockPanel dockPanel)
                {
                    dockPanel.Measure(new Size(contentBounds.Width, contentBounds.Height), context);
                    dockPanel.Arrange(childBounds, context);
                }
                
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
                
                // 对 ScrollView 和 DockPanel 调用 Measure 和 Arrange
                if (child is ScrollView scrollViewH)
                {
                    scrollViewH.Measure(new Size(childWidth, contentBounds.Height), context);
                    scrollViewH.Arrange(childBounds, context);
                }
                else if (child is DockPanel dockPanelH)
                {
                    dockPanelH.Measure(new Size(childWidth, contentBounds.Height), context);
                    dockPanelH.Arrange(childBounds, context);
                }
                
                child.Render(context, childBounds);
                x += childWidth + spacingValue;
            }
        }
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
    public double FontSize { get; set; } = 14;
    public Color Color { get; set; } = Color.Black;
    public string? FontWeight { get; set; }
    public string? FontFamily { get; set; }
    public TextAlignment TextAlignment { get; set; } = TextAlignment.Left;
    public Color BackgroundColor { get; set; } = Color.Transparent;
    public double Padding { get; set; } = 0;
    
    private Size _desiredSize = Size.Zero;
    
    public Size Measure(Size availableSize, IDrawingContext context)
    {
        if (string.IsNullOrEmpty(Text))
        {
            _desiredSize = Size.Zero;
            return _desiredSize;
        }
        
        var scaledFontSize = FontSize * context.Scale;
        var scaledPadding = Padding * context.Scale;
        var textWidth = context.MeasureText(Text, scaledFontSize, FontFamily);
        var lineHeight = scaledFontSize * 1.3;
        
        _desiredSize = new Size(textWidth + scaledPadding * 2, lineHeight + scaledPadding * 2);
        return _desiredSize;
    }
    
    public override void Build(IBuildContext context) { }
    
    public override void Render(IDrawingContext context, Rect bounds)
    {
        UpdateBounds(bounds);
        
        var scaledPadding = Padding * context.Scale;
        
        if (BackgroundColor != Color.Transparent)
            context.DrawRectangle(bounds, BackgroundColor);
        
        if (!string.IsNullOrEmpty(Text))
        {
            var scaledFontSize = FontSize * context.Scale;
            
            var textBounds = new Rect(
                bounds.X + scaledPadding,
                bounds.Y + scaledPadding,
                bounds.Width - scaledPadding * 2,
                bounds.Height - scaledPadding * 2);
            
            double x = textBounds.X;
            if (TextAlignment == TextAlignment.Center)
            {
                var textWidth = context.MeasureText(Text, scaledFontSize, FontFamily);
                x = textBounds.X + (textBounds.Width - textWidth) / 2;
            }
            else if (TextAlignment == TextAlignment.Right)
            {
                var textWidth = context.MeasureText(Text, scaledFontSize, FontFamily);
                x = textBounds.X + textBounds.Width - textWidth;
            }
            
            var y = textBounds.Y + scaledFontSize * 0.5;
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

public class Image : ComponentBase
{
    private string? _loadedImageKey;
    
    public string? Source { get; set; }
    public double Width { get; set; } = -1;
    public double Height { get; set; } = -1;
    public Stretch Stretch { get; set; } = Stretch.Uniform;
    
    public override void Build(IBuildContext context) { }
    
    public override void Render(IDrawingContext context, Rect bounds)
    {
        if (string.IsNullOrEmpty(Source))
        {
            context.DrawRectangle(bounds, Color.LightGray);
            return;
        }
        
        if (_loadedImageKey == null || !string.Equals(_loadedImageKey, Source, StringComparison.OrdinalIgnoreCase))
            _loadedImageKey = context.LoadImage(Source);
        
        if (_loadedImageKey == null)
        {
            context.DrawRectangle(bounds, Color.LightGray);
            return;
        }
        
        context.DrawImage(_loadedImageKey, bounds, Stretch);
    }
}

public class Container : ComponentBase
{
    public Color BackgroundColor { get; set; } = Color.Transparent;
    public double Padding { get; set; } = 0;
    public double CornerRadius { get; set; } = 0;
    
    /// <summary>
    /// 固定宽度（-1 表示自动）
    /// </summary>
    public double Width { get; set; } = -1;
    
    /// <summary>
    /// 固定高度（-1 表示自动）
    /// </summary>
    public double Height { get; set; } = -1;
    
    public override void Build(IBuildContext context) { }
    
    public override void Render(IDrawingContext context, Rect bounds)
    {
        if (BackgroundColor != Color.Transparent)
            context.DrawRoundRect(bounds, BackgroundColor, CornerRadius * context.Scale);
        
        var contentBounds = new Rect(
            bounds.X + Padding * context.Scale,
            bounds.Y + Padding * context.Scale,
            bounds.Width - Padding * 2 * context.Scale,
            bounds.Height - Padding * 2 * context.Scale);
        
        foreach (var child in Children)
            child.Render(context, contentBounds);
    }
}