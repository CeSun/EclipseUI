using SkiaSharp;
using EclipseUI.Core;
using EclipseUI.Rendering;

namespace EclipseUI.Controls;

/// <summary>
/// 文本输入框元素 - iOS 风格
/// </summary>
public class TextBoxElement : EclipseElement
{
    public string Text { get; set; } = "";
    public string Placeholder { get; set; } = "";
    public float FontSize { get; set; } = iOSTheme.FontSizeBody;
    public SKColor TextColor { get; set; } = iOSTheme.LabelPrimary;
    public SKColor? PlaceholderColor { get; set; } = iOSTheme.SystemGray2;
    public SKColor BorderColor { get; set; } = iOSTheme.SystemGray4;
    public SKColor FocusBorderColor { get; set; } = iOSTheme.SystemBlue;
    public float CornerRadius { get; set; } = iOSTheme.CornerRadiusMedium;
    public bool IsFocused { get; set; }
    public bool IsHovered { get; set; }
    
    public int CaretPosition { get; set; }
    public int SelectionStart { get; set; }
    public int SelectionEnd { get; set; }
    public bool HasSelection => SelectionStart != SelectionEnd;
    
    private float _horizontalScrollOffset = 0;
    
    public string SelectedText
    {
        get
        {
            if (!HasSelection) return "";
            var start = Math.Min(SelectionStart, SelectionEnd);
            var end = Math.Max(SelectionStart, SelectionEnd);
            return Text.Substring(start, end - start);
        }
    }
    
    private bool _caretVisible = true;
    private long _lastCaretToggleTime;
    private const int CaretBlinkInterval = 500;
    
    public Func<string, Task>? OnTextChanged { get; set; }
    public Action<bool>? OnFocus { get; set; }
    public Action<bool>? OnBlur { get; set; }
    public Func<string, Task>? OnKeyDown { get; set; }
    
    public override SKSize Measure(SKCanvas canvas, float availableWidth, float availableHeight)
    {
        // iOS 风格：更高的输入框
        float minHeight = FontSize + PaddingTop + PaddingBottom + 16;
        float contentWidth = availableWidth - PaddingLeft - PaddingRight;
        float contentHeight = Math.Max(FontSize + 16, minHeight - PaddingTop - PaddingBottom);
        
        float finalWidth = RequestedWidth ?? contentWidth;
        float finalHeight = RequestedHeight ?? Math.Max(contentHeight + PaddingTop + PaddingBottom, 44); // iOS 最小触摸目标
        
        if (MinWidth.HasValue) finalWidth = Math.Max(finalWidth, MinWidth.Value);
        if (MinHeight.HasValue) finalHeight = Math.Max(finalHeight, MinHeight.Value);
        if (MaxWidth.HasValue) finalWidth = Math.Min(finalWidth, MaxWidth.Value);
        if (MaxHeight.HasValue) finalHeight = Math.Min(finalHeight, MaxHeight.Value);
        
        finalWidth = Math.Min(finalWidth, availableWidth);
        finalHeight = Math.Min(finalHeight, availableHeight);
        
        return new SKSize(finalWidth, finalHeight);
    }
    
    public override void Render(SKCanvas canvas)
    {
        if (!IsVisible) return;
        
        canvas.Save();
        try
        {
            var rect = new SKRect(X, Y, X + Width, Y + Height);
            
            // iOS 风格背景
            var bgColor = BackgroundColor ?? iOSTheme.SystemGray6;
            using var bgPaint = new SKPaint { Color = bgColor, IsAntialias = true };
            canvas.DrawRoundRect(rect, CornerRadius, CornerRadius, bgPaint);
            
            // 边框：聚焦时蓝色，否则浅灰色
            var borderColor = IsFocused ? FocusBorderColor : iOSTheme.SystemGray4;
            var borderWidth = IsFocused ? 2f : 1f;
            using var borderPaint = new SKPaint 
            { 
                Color = borderColor, 
                IsAntialias = true,
                StrokeWidth = borderWidth,
                Style = SKPaintStyle.Stroke
            };
            var borderRect = new SKRect(X + borderWidth / 2, Y + borderWidth / 2, X + Width - borderWidth / 2, Y + Height - borderWidth / 2);
            canvas.DrawRoundRect(borderRect, CornerRadius - borderWidth / 2, CornerRadius - borderWidth / 2, borderPaint);
            
            RenderContent(canvas);
        }
        finally
        {
            canvas.Restore();
        }
    }
    
    protected override void RenderContent(SKCanvas canvas)
    {
        // 更新光标闪烁状态
        UpdateCaretBlink();
        
        // 计算内容区域（考虑 Padding）
        float contentX = X + PaddingLeft;
        float contentY = Y + PaddingTop;
        float contentWidth = Width - PaddingLeft - PaddingRight;
        float contentHeight = Height - PaddingTop - PaddingBottom;
        
        // 单行：垂直居中
        float textY = Y + Height / 2 + FontSize / 3;
        
        // 设置裁剪区域
        canvas.ClipRect(new SKRect(contentX, contentY, contentX + contentWidth, contentY + contentHeight));
        
        using var renderContext = new SkiaRenderContext(canvas);
        
        // 计算水平滚动偏移
        UpdateHorizontalScroll(contentWidth);
        float renderX = contentX - _horizontalScrollOffset;
        
        // 绘制占位符文本
        if (string.IsNullOrEmpty(Text) && !string.IsNullOrEmpty(Placeholder))
        {
            TextRenderer.DrawText(renderContext, Placeholder, renderX, textY, FontSize, 
                new Color(PlaceholderColor!.Value.Red, PlaceholderColor.Value.Green, PlaceholderColor.Value.Blue, PlaceholderColor.Value.Alpha),
                SKTextAlign.Left);
        }
        
        // 绘制选中文本背景
        if (HasSelection && IsFocused)
        {
            DrawSelectionBackground(canvas, renderX, textY - FontSize, contentWidth);
        }
        
        // 绘制文本
        if (!string.IsNullOrEmpty(Text))
        {
            TextRenderer.DrawText(renderContext, Text, renderX, textY, FontSize,
                new Color(TextColor.Red, TextColor.Green, TextColor.Blue, TextColor.Alpha),
                SKTextAlign.Left);
        }
        
        // 绘制光标
        if (IsFocused && _caretVisible)
        {
            DrawCaret(canvas, renderX, textY - FontSize);
        }
    }
    
    /// <summary>
    /// 更新水平滚动偏移，确保光标始终可见
    /// </summary>
    private void UpdateHorizontalScroll(float contentWidth)
    {
        // 测量光标位置的文本宽度
        var prefixText = Text.Substring(0, Math.Min(CaretPosition, Text.Length));
        var caretX = TextRenderer.MeasureText(prefixText, FontSize);
        
        // 如果光标超出右边界，向右滚动
        if (caretX > _horizontalScrollOffset + contentWidth - FontSize)
        {
            _horizontalScrollOffset = caretX - contentWidth + FontSize;
        }
        // 如果光标超出左边界，向左滚动
        else if (caretX < _horizontalScrollOffset)
        {
            _horizontalScrollOffset = caretX;
        }
        
        // 确保滚动偏移不为负
        if (_horizontalScrollOffset < 0)
            _horizontalScrollOffset = 0;
    }
    
    /// <summary>
    /// 绘制选中文本背景
    /// </summary>
    private void DrawSelectionBackground(SKCanvas canvas, float contentX, float contentY, float contentWidth)
    {
        var start = Math.Min(SelectionStart, SelectionEnd);
        var end = Math.Max(SelectionStart, SelectionEnd);
        
        if (start >= end || start >= Text.Length) return;
        
        // 测量选中文本的宽度
        var selectedText = Text.Substring(start, end - start);
        var prefixText = Text.Substring(0, start);
        
        var prefixWidth = TextRenderer.MeasureText(prefixText, FontSize);
        var selectionWidth = TextRenderer.MeasureText(selectedText, FontSize);
        
        var selectionRect = new SKRect(
            contentX + prefixWidth,
            contentY,
            contentX + prefixWidth + selectionWidth,
            contentY + FontSize + 4
        );
        
        using var selectionPaint = new SKPaint { Color = SKColors.LightBlue, IsAntialias = true };
        canvas.DrawRect(selectionRect, selectionPaint);
    }
    
    /// <summary>
    /// 绘制光标
    /// </summary>
    private void DrawCaret(SKCanvas canvas, float contentX, float contentY)
    {
        var prefixText = Text.Substring(0, Math.Min(CaretPosition, Text.Length));
        var caretX = contentX + TextRenderer.MeasureText(prefixText, FontSize);
        
        var caretHeight = FontSize + 2;
        var caretRect = new SKRect(caretX - 0.5f, contentY, caretX + 0.5f, contentY + caretHeight);
        
        using var caretPaint = new SKPaint { Color = SKColors.Black, IsAntialias = true };
        canvas.DrawRect(caretRect, caretPaint);
    }
    
    /// <summary>
    /// 更新光标闪烁状态
    /// </summary>
    private void UpdateCaretBlink()
    {
        var currentTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        if (currentTime - _lastCaretToggleTime >= CaretBlinkInterval)
        {
            _caretVisible = !_caretVisible;
            _lastCaretToggleTime = currentTime;
        }
    }
    
    /// <summary>
    /// 重置光标闪烁计时器
    /// </summary>
    public void ResetCaretBlink()
    {
        _caretVisible = true;
        _lastCaretToggleTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
    }
    
    /// <summary>
    /// 处理文本输入
    /// </summary>
    public async Task HandleTextInput(string text)
    {
        if (HasSelection)
        {
            // 删除选中文本
            var start = Math.Min(SelectionStart, SelectionEnd);
            var end = Math.Max(SelectionStart, SelectionEnd);
            Text = Text.Substring(0, start) + text + Text.Substring(end);
            CaretPosition = start + text.Length;
            SelectionStart = SelectionEnd = CaretPosition;
        }
        else
        {
            // 在光标位置插入文本
            Text = Text.Substring(0, CaretPosition) + text + Text.Substring(CaretPosition);
            CaretPosition += text.Length;
            SelectionStart = SelectionEnd = CaretPosition;
        }
        
        ResetCaretBlink();
        
        if (OnTextChanged != null)
        {
            await OnTextChanged(Text);
        }
    }
    
    /// <summary>
    /// 处理按键输入
    /// </summary>
    public async Task HandleKeyDown(string key)
    {
        if (OnKeyDown != null)
        {
            await OnKeyDown(key);
        }
        
        switch (key)
        {
            case "Backspace":
                await HandleBackspace();
                break;
            case "Delete":
                await HandleDelete();
                break;
            case "ArrowLeft":
                HandleArrowLeft();
                break;
            case "ArrowRight":
                HandleArrowRight();
                break;
            case "Home":
                CaretPosition = 0;
                SelectionStart = SelectionEnd = CaretPosition;
                ResetCaretBlink();
                break;
            case "End":
                CaretPosition = Text.Length;
                SelectionStart = SelectionEnd = CaretPosition;
                ResetCaretBlink();
                break;
            case "Enter":
                // 单行模式不处理 Enter
                break;
        }
    }
    
    private async Task HandleBackspace()
    {
        if (HasSelection)
        {
            var start = Math.Min(SelectionStart, SelectionEnd);
            var end = Math.Max(SelectionStart, SelectionEnd);
            Text = Text.Substring(0, start) + Text.Substring(end);
            CaretPosition = start;
            SelectionStart = SelectionEnd = CaretPosition;
        }
        else if (CaretPosition > 0)
        {
            Text = Text.Substring(0, CaretPosition - 1) + Text.Substring(CaretPosition);
            CaretPosition--;
            SelectionStart = SelectionEnd = CaretPosition;
        }
        
        ResetCaretBlink();
        
        if (OnTextChanged != null)
        {
            await OnTextChanged(Text);
        }
    }
    
    private async Task HandleDelete()
    {
        if (HasSelection)
        {
            var start = Math.Min(SelectionStart, SelectionEnd);
            var end = Math.Max(SelectionStart, SelectionEnd);
            Text = Text.Substring(0, start) + Text.Substring(end);
            CaretPosition = start;
            SelectionStart = SelectionEnd = CaretPosition;
        }
        else if (CaretPosition < Text.Length)
        {
            Text = Text.Substring(0, CaretPosition) + Text.Substring(CaretPosition + 1);
            // CaretPosition 不变
            SelectionStart = SelectionEnd = CaretPosition;
        }
        
        ResetCaretBlink();
        
        if (OnTextChanged != null)
        {
            await OnTextChanged(Text);
        }
    }
    
    private void HandleArrowLeft()
    {
        if (CaretPosition > 0)
        {
            CaretPosition--;
            SelectionStart = SelectionEnd = CaretPosition;
            ResetCaretBlink();
        }
    }
    
    private void HandleArrowRight()
    {
        if (CaretPosition < Text.Length)
        {
            CaretPosition++;
            SelectionStart = SelectionEnd = CaretPosition;
            ResetCaretBlink();
        }
    }
    
    /// <summary>
    /// 点击处理 - 设置光标位置
    /// </summary>
    public override bool HandleClick(SKPoint point)
    {
        if (!IsVisible) return false;
        
        var rect = new SKRect(X, Y, X + Width, Y + Height);
        if (!rect.Contains(point)) return false;
        
        // 获得焦点
        if (!IsFocused)
        {
            IsFocused = true;
            ResetCaretBlink();
            OnFocus?.Invoke(true);
        }
        
        // 计算点击位置对应的字符索引
        float contentX = X + PaddingLeft;
        float clickX = point.X - contentX;
        
        int newCaretPosition = 0;
        float accumulatedWidth = 0;
        
        for (int i = 0; i < Text.Length; i++)
        {
            var charWidth = TextRenderer.MeasureText(Text[i].ToString(), FontSize);
            if (clickX < accumulatedWidth + charWidth / 2)
            {
                break;
            }
            accumulatedWidth += charWidth;
            newCaretPosition = i + 1;
        }
        
        CaretPosition = newCaretPosition;
        SelectionStart = SelectionEnd = CaretPosition;
        ResetCaretBlink();
        
        return true;
    }
    
    /// <summary>
    /// 鼠标移动处理 - 悬停效果
    /// </summary>
    public override bool HandleMouseMove(float x, float y)
    {
        var rect = new SKRect(X, Y, X + Width, Y + Height);
        var point = new SKPoint(x, y);
        var wasHovered = IsHovered;
        IsHovered = rect.Contains(point);
        
        return IsHovered != wasHovered;
    }
    
    /// <summary>
    /// 鼠标离开处理
    /// </summary>
    public override void HandleMouseLeave()
    {
        IsHovered = false;
    }
    
    /// <summary>
    /// 失焦
    /// </summary>
    public void Blur()
    {
        if (IsFocused)
        {
            IsFocused = false;
            SelectionStart = SelectionEnd = 0;
            OnBlur?.Invoke(false);
        }
    }
}
