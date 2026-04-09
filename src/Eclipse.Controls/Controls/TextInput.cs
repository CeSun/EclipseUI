using Eclipse.Core;
using Eclipse.Core.Abstractions;
using Eclipse.Input;
using Eclipse.Rendering;
using System;
using Microsoft.Extensions.DependencyInjection;

namespace Eclipse.Controls;

/// <summary>
/// 文本输入控件 - 支持光标、选择、键盘输入和 IME 输入法
/// </summary>
public class TextInput : InteractiveControl
{
    private IClipboard? _clipboard;
    
    /// <summary>
    /// 剪贴板服务（通过依赖注入获取）
    /// </summary>
    protected IClipboard Clipboard => _clipboard ??= GetClipboard();
    
    private IClipboard GetClipboard()
    {
        // 从静态应用宿主获取服务（App.Run 时设置）
        if (ComponentBase.AppHost is IAppHost appHost)
            return appHost.Services.GetRequiredService<IClipboard>();
        
        // 回退：向上遍历组件树（用于测试场景）
        IComponent? current = this;
        while (current != null)
        {
            if (current is IAppHost h)
                return h.Services.GetRequiredService<IClipboard>();
            current = current.Parent;
        }
        throw new InvalidOperationException("IClipboard service not registered: no IAppHost found");
    }
    private string? _text;
    private int _cursorPosition = 0;
    private bool _isCursorVisible = true;
    private DateTime _lastCursorToggle = DateTime.Now;
    private const double CursorBlinkIntervalMs = 500;
    
    // IME 组合状态
    private string _compositionText = string.Empty;
    private int _compositionCursor = 0;
    private bool _isComposing = false;
    
    // === 选择相关字段 ===
    private int _selectionStart = 0;
    private int _selectionEnd = 0;
    private bool _isSelecting = false;
    private int _selectionAnchor = 0; // 选择锚点（选择开始的位置）
    
    // === 鼠标选择相关字段 ===
    private Point _lastPointerPressPos;
    private DateTime _lastPointerPressTime = DateTime.MinValue;
    private int _clickCount = 0;
    private const double DoubleClickThresholdMs = 500;
    private const double DoubleClickThresholdDistance = 4;
    
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
                // 清除选择
                ClearSelectionInternal();
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
    public Color BackgroundColor { get; set; } = Color.White;
    
    /// <summary>
    /// 边框颜色（聚焦时）
    /// </summary>
    public Color FocusBorderColor { get; set; } = Color.FromArgb(0, 122, 255);
    
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
    /// 选择起始位置（较小的索引）
    /// </summary>
    public int SelectionStart => Math.Min(_selectionStart, _selectionEnd);
    
    /// <summary>
    /// 选择结束位置（较大的索引）
    /// </summary>
    public int SelectionEnd => Math.Max(_selectionStart, _selectionEnd);
    
    /// <summary>
    /// 选择长度
    /// </summary>
    public int SelectionLength => SelectionEnd - SelectionStart;
    
    /// <summary>
    /// 是否有选中文本
    /// </summary>
    public bool HasSelection => SelectionLength > 0;
    
    /// <summary>
    /// 选中的文本
    /// </summary>
    public string? SelectedText => HasSelection && _text != null 
        ? _text.Substring(SelectionStart, SelectionLength) 
        : null;
    
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
    /// 组合文本（正在输入的拼音/笔画）
    /// </summary>
    public string CompositionText => _compositionText;
    
    /// <summary>
    /// 是否正在组合输入
    /// </summary>
    public bool IsComposing => _isComposing;
    
    /// <summary>
    /// 选择背景颜色
    /// </summary>
    public Color SelectionBackgroundColor { get; set; } = Color.FromArgb(128, 0, 122, 255);
    
    /// <summary>
    /// 选择文本颜色
    /// </summary>
    public Color SelectionTextColor { get; set; } = Color.Black;
    
    /// <summary>
    /// 文本变化事件
    /// </summary>
    public event EventHandler<ValueChangedEventArgs<string?>>? TextChanged;
    
    /// <summary>
    /// 组合文本变化事件
    /// </summary>
    public event EventHandler<CompositionEventArgs>? CompositionChanged;
    
    /// <summary>
    /// 选择变化事件
    /// </summary>
    public event EventHandler<EventArgs>? SelectionChanged;
    
    public TextInput()
    {
        IsFocusable = true;
        _desiredSize = new Size(200, 30);
        
        // 订阅键盘事件
        KeyDown += OnKeyDown;
        TextInput += OnTextInput;
        
        // 订阅 IME 组合事件
        CompositionStarted += OnCompositionStarted;
        CompositionChanged += OnCompositionChanged;
        CompositionEnded += OnCompositionEnded;
        
        // 订阅指针事件（用于鼠标选择）
        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;
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
    /// 全选文本
    /// </summary>
    public void SelectAll()
    {
        var length = _text?.Length ?? 0;
        _selectionStart = 0;
        _selectionEnd = length;
        _cursorPosition = length;
        OnSelectionChanged();
        StateHasChanged();
    }
    
    /// <summary>
    /// 清除选择
    /// </summary>
    public void ClearSelection()
    {
        ClearSelectionInternal();
        StateHasChanged();
    }
    
    /// <summary>
    /// 内部清除选择（不触发 StateHasChanged）
    /// </summary>
    private void ClearSelectionInternal()
    {
        if (HasSelection)
        {
            _selectionStart = _cursorPosition;
            _selectionEnd = _cursorPosition;
            OnSelectionChanged();
        }
    }
    
    /// <summary>
    /// 选择指定范围的文本
    /// </summary>
    public void Select(int start, int length)
    {
        var maxLen = _text?.Length ?? 0;
        start = Math.Clamp(start, 0, maxLen);
        length = Math.Clamp(length, 0, maxLen - start);
        
        _selectionStart = start;
        _selectionEnd = start + length;
        _cursorPosition = _selectionEnd;
        OnSelectionChanged();
        StateHasChanged();
    }
    
    /// <summary>
    /// 删除选中的文本
    /// </summary>
    private void DeleteSelection()
    {
        if (!HasSelection || string.IsNullOrEmpty(_text))
            return;
        
        var newText = _text.Remove(SelectionStart, SelectionLength);
        _text = newText;
        _cursorPosition = SelectionStart;
        _selectionStart = _cursorPosition;
        _selectionEnd = _cursorPosition;
        
        TextChanged?.Invoke(this, new ValueChangedEventArgs<string?>(_text, _text));
        OnSelectionChanged();
    }
    
    /// <summary>
    /// 触发选择变化事件
    /// </summary>
    private void OnSelectionChanged()
    {
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }
    
    /// <summary>
    /// 处理键盘按下事件
    /// </summary>
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (!IsFocused)
            return;
        
        var hasControl = e.HasModifier(KeyModifiers.Control);
        var hasShift = e.HasModifier(KeyModifiers.Shift);
        
        switch (e.Key)
        {
            case Key.Back:
                if (HasSelection)
                {
                    DeleteSelection();
                }
                else if (_cursorPosition > 0 && !string.IsNullOrEmpty(_text))
                {
                    _text = _text.Remove(_cursorPosition - 1, 1);
                    _cursorPosition--;
                    TextChanged?.Invoke(this, new ValueChangedEventArgs<string?>(_text, _text));
                }
                e.Handled = true;
                StateHasChanged();
                break;
                
            case Key.Delete:
                if (HasSelection)
                {
                    DeleteSelection();
                }
                else if (_cursorPosition < (_text?.Length ?? 0) && !string.IsNullOrEmpty(_text))
                {
                    _text = _text.Remove(_cursorPosition, 1);
                    TextChanged?.Invoke(this, new ValueChangedEventArgs<string?>(_text, _text));
                }
                e.Handled = true;
                StateHasChanged();
                break;
                
            case Key.Left:
                if (hasShift)
                {
                    // 扩展选择向左
                    ExtendSelection(-1);
                }
                else
                {
                    // 如果有选择，移动到选择起点
                    if (HasSelection)
                    {
                        _cursorPosition = SelectionStart;
                        ClearSelectionInternal();
                    }
                    else if (_cursorPosition > 0)
                    {
                        _cursorPosition--;
                    }
                }
                e.Handled = true;
                StateHasChanged();
                break;
                
            case Key.Right:
                if (hasShift)
                {
                    // 扩展选择向右
                    ExtendSelection(1);
                }
                else
                {
                    // 如果有选择，移动到选择终点
                    if (HasSelection)
                    {
                        _cursorPosition = SelectionEnd;
                        ClearSelectionInternal();
                    }
                    else if (_cursorPosition < (_text?.Length ?? 0))
                    {
                        _cursorPosition++;
                    }
                }
                e.Handled = true;
                StateHasChanged();
                break;
                
            case Key.Home:
                if (hasShift)
                {
                    // 选择到开头
                    _selectionStart = 0;
                    _selectionEnd = _cursorPosition;
                    _cursorPosition = 0;
                    OnSelectionChanged();
                }
                else
                {
                    _cursorPosition = 0;
                    ClearSelectionInternal();
                }
                e.Handled = true;
                StateHasChanged();
                break;
                
            case Key.End:
                if (hasShift)
                {
                    // 选择到末尾
                    _selectionStart = _cursorPosition;
                    _selectionEnd = _text?.Length ?? 0;
                    _cursorPosition = _selectionEnd;
                    OnSelectionChanged();
                }
                else
                {
                    _cursorPosition = _text?.Length ?? 0;
                    ClearSelectionInternal();
                }
                e.Handled = true;
                StateHasChanged();
                break;
                
            case Key.A:
                // Ctrl+A 全选
                if (hasControl)
                {
                    SelectAll();
                    e.Handled = true;
                }
                break;
                
            case Key.C:
                // Ctrl+C 复制
                if (hasControl && HasSelection)
                {
                    CopyToClipboard();
                    e.Handled = true;
                }
                break;
                
            case Key.X:
                // Ctrl+X 剪切
                if (hasControl && HasSelection)
                {
                    CopyToClipboard();
                    DeleteSelection();
                    e.Handled = true;
                    StateHasChanged();
                }
                break;
                
            case Key.V:
                // Ctrl+V 精贴
                if (hasControl)
                {
                    var clipboardText = Clipboard.GetText();
                    if (!string.IsNullOrEmpty(clipboardText))
                    {
                        if (HasSelection)
                        {
                            DeleteSelection();
                        }
                        InsertText(clipboardText);
                    }
                    e.Handled = true;
                    StateHasChanged();
                }
                break;
                
            case Key.Enter:
                // 触发提交事件（可选）
                e.Handled = true;
                break;
        }
    }
    
    /// <summary>
    /// 扩展选择（用于 Shift+方向键）
    /// </summary>
    private void ExtendSelection(int direction)
    {
        if (!HasSelection)
        {
            _selectionStart = _cursorPosition;
            _selectionEnd = _cursorPosition;
        }
        
        var newPos = _cursorPosition + direction;
        var maxPos = _text?.Length ?? 0;
        newPos = Math.Clamp(newPos, 0, maxPos);
        
        _cursorPosition = newPos;
        _selectionEnd = newPos;
        
        OnSelectionChanged();
    }
    
    /// <summary>
    /// 在光标位置插入文本
    /// </summary>
    private void InsertText(string text)
    {
        var before = _text?.Substring(0, _cursorPosition) ?? "";
        var after = _text?.Substring(_cursorPosition) ?? "";
        _text = before + text + after;
        _cursorPosition += text.Length;
        _selectionStart = _cursorPosition;
        _selectionEnd = _cursorPosition;
        
        TextChanged?.Invoke(this, new ValueChangedEventArgs<string?>(_text, _text));
    }
    
    /// <summary>
    /// 处理文本输入事件
    /// </summary>
    private void OnTextInput(object? sender, TextInputEventArgs     e)
    {
        if (!IsFocused || string.IsNullOrEmpty(e.Text))
            return;
        
        // 如果有选择，先删除
        if (HasSelection)
        {
            DeleteSelection();
        }
        
        // 在光标位置插入文本
        InsertText(e.Text);
        StateHasChanged();
    }
    
    /// <summary>
    /// 处理 IME 组合开始
    /// </summary>
    private void OnCompositionStarted(object? sender, CompositionEventArgs e)
    {
        if (!IsFocused)
            return;
        
        _isComposing = true;
        _compositionText = string.Empty;
        _compositionCursor = 0;
        StateHasChanged();
    }
    
    /// <summary>
    /// 处理 IME 组合文本变化
    /// </summary>
    private void OnCompositionChanged(object? sender, CompositionEventArgs e)
    {
        if (!IsFocused)
            return;
        
        _compositionText = e.CompositionText;
        _compositionCursor = e.CursorPosition;
        CompositionChanged?.Invoke(this, e);
        StateHasChanged();
    }
    
    /// <summary>
    /// 处理 IME 组合结束
    /// </summary>
    private void OnCompositionEnded(object? sender, CompositionEventArgs e)
    {
        if (!IsFocused)
            return;
        
        _isComposing = false;
        _compositionText = string.Empty;
        _compositionCursor = 0;
        StateHasChanged();
    }
    
    /// <summary>
    /// 处理鼠标按下事件
    /// </summary>
    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!IsEnabled)
            return;
        
        // 获取焦点
        if (!IsFocused)
        {
            Focus();
        }
        
        var pos = e.Position;
        var clickCount = e.ClickCount;
        
        // 检测多击
        var now = DateTime.Now;
        var timeDiff = (now - _lastPointerPressTime).TotalMilliseconds;
        var distance = (pos - _lastPointerPressPos).Length;
        
        if (timeDiff < DoubleClickThresholdMs && distance < DoubleClickThresholdDistance)
        {
            _clickCount++;
        }
        else
        {
            _clickCount = 1;
        }
        
        _lastPointerPressTime = now;
        _lastPointerPressPos = pos;
        
        // 获取文本区域内的点击位置
        var charIndex = GetCharIndexAtPosition(pos);
        
        switch (_clickCount)
        {
            case 1:
                // 单击：设置光标位置并开始选择
                _cursorPosition = charIndex;
                _selectionAnchor = charIndex;
                _selectionStart = charIndex;
                _selectionEnd = charIndex;
                _isSelecting = true;
                
                // 捕获指针以便处理拖动选择
                e.Capture(this);
                break;
                
            case 2:
                // 双击：选择单词
                SelectWord(charIndex);
                _isSelecting = false;
                break;
                
            case 3:
                // 三击：选择整行
                SelectAll();
                _isSelecting = false;
                _clickCount = 0; // 重置点击计数
                break;
        }
        
        e.Handled = true;
        OnSelectionChanged();
        StateHasChanged();
    }
    
    /// <summary>
    /// 处理鼠标移动事件
    /// </summary>
    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isSelecting || !IsFocused)
            return;
        
        var charIndex = GetCharIndexAtPosition(e.Position);
        
        // 扩展选择到当前位置
        _selectionStart = Math.Min(_selectionAnchor, charIndex);
        _selectionEnd = Math.Max(_selectionAnchor, charIndex);
        _cursorPosition = charIndex;
        
        OnSelectionChanged();
        StateHasChanged();
        e.Handled = true;
    }
    
    /// <summary>
    /// 处理鼠标释放事件
    /// </summary>
    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isSelecting)
        {
            _isSelecting = false;
            ReleasePointerCapture(e.Pointer);
        }
    }
    
    /// <summary>
    /// 根据屏幕位置获取字符索引
    /// </summary>
    private int GetCharIndexAtPosition(Point position)
    {
        if (string.IsNullOrEmpty(_text))
            return 0;
        
        // 计算相对于文本区域的位置
        var scaledPadding = Padding;
        var scaledFontSize = FontSize;
        
        var textStartX = Bounds.X + scaledPadding;
        var textX = position.X - textStartX;
        
        if (textX <= 0)
            return 0;
        
        // 使用显示文本（密码模式用星号）
        var displayText = IsPassword ? new string('*', _text.Length) : _text;
        
        // 二分查找确定字符位置
        int left = 0, right = displayText.Length;
        while (left < right)
        {
            int mid = (left + right) / 2;
            var midWidth = EstimateTextWidth(displayText.Substring(0, mid), scaledFontSize);
            
            if (textX < midWidth)
            {
                right = mid;
            }
            else
            {
                left = mid + 1;
            }
        }
        
        return Math.Clamp(left, 0, _text.Length);
    }
    
    /// <summary>
    /// 估算文本宽度（简化版本，用于快速定位）
    /// 注意：精确计算需要 IDrawingContext.MeasureText
    /// </summary>
    private double EstimateTextWidth(string text, double fontSize)
    {
        // 简单估算：平均字符宽度约为字体大小的 0.5 倍
        // 对于中文和英文混合，这是一个近似值
        double width = 0;
        foreach (var c in text)
        {
            // 中文字符宽度较大
            if (c > 0x7F)
                width += fontSize * 0.9;
            else
                width += fontSize * 0.5;
        }
        return width;
    }
    
    /// <summary>
    /// 选择指定位置的单词
    /// </summary>
    private void SelectWord(int position)
    {
        if (string.IsNullOrEmpty(_text))
            return;
        
        // 查找单词边界
        int start = position;
        int end = position;
        
        // 向左查找单词边界
        while (start > 0 && !IsWordBoundary(_text[start - 1]))
        {
            start--;
        }
        
        // 向右查找单词边界
        while (end < _text.Length && !IsWordBoundary(_text[end]))
        {
            end++;
        }
        
        _selectionStart = start;
        _selectionEnd = end;
        _cursorPosition = end;
        
        OnSelectionChanged();
    }
    
    /// <summary>
    /// 判断字符是否为单词边界
    /// </summary>
    private static bool IsWordBoundary(char c)
    {
        return char.IsWhiteSpace(c) || 
               c == '.' || c == ',' || c == ';' || c == ':' ||
               c == '!' || c == '?' || c == '(' || c == ')' ||
               c == '[' || c == ']' || c == '{' || c == '}' ||
               c == '"' || c == '\'' || c == '`';
    }
    
    /// <summary>
    /// 复制选中文本到剪贴板
    /// </summary>
    private void CopyToClipboard()
    {
        if (!HasSelection || string.IsNullOrEmpty(_text))
            return;
        
        var selectedText = _text.Substring(SelectionStart, SelectionLength);
        Clipboard.SetText(selectedText);
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
        context.DrawRoundRect(bounds, BackgroundColor, scaledCornerRadius);
        
        // 绘制边框（聚焦时使用蓝色）
        var borderColor = IsFocused ? FocusBorderColor : Color.LightGray;
        context.DrawRectangle(bounds, Color.Transparent, borderColor, 1 * context.Scale, scaledCornerRadius);
        
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
        
        if (!string.IsNullOrEmpty(displayText) || _isComposing)
        {
            // 绘制选择背景
            if (HasSelection && IsFocused && !string.IsNullOrEmpty(displayText))
            {
                DrawSelection(context, textBounds, displayText, scaledFontSize);
            }
            
            // y 是文本视觉中心
            var textY = textBounds.Y + scaledFontSize * 0.5;
            
            // 绘制已确认文本
            if (!string.IsNullOrEmpty(displayText))
            {
                context.DrawText(displayText, textBounds.X, textY, scaledFontSize);
            }
            
            // 绘制组合文本（带下划线）
            if (_isComposing && !string.IsNullOrEmpty(_compositionText))
            {
                // 计算组合文本起始位置
                double compositionX = textBounds.X;
                if (!string.IsNullOrEmpty(displayText))
                {
                    compositionX += context.MeasureText(displayText, scaledFontSize, null);
                }
                
                // 绘制组合文本
                context.DrawText(_compositionText, compositionX, textY, scaledFontSize);
                
                // 绘制下划线
                var compositionWidth = context.MeasureText(_compositionText, scaledFontSize, null);
                var underlineY = textY + scaledFontSize * 0.5; // 文字下方
                context.DrawLine(
                    compositionX, underlineY,
                    compositionX + compositionWidth, underlineY,
                    Color.Black, 1 * context.Scale);
                
                // 绘制组合文本中的光标
                if (_compositionCursor > 0 && _compositionCursor <= _compositionText.Length)
                {
                    var textBeforeCursor = _compositionText.Substring(0, _compositionCursor);
                    var cursorX = compositionX + context.MeasureText(textBeforeCursor, scaledFontSize, null);
                    var cursorBounds = new Rect(cursorX, textBounds.Y, 2 * context.Scale, scaledFontSize);
                    context.DrawRectangle(cursorBounds, Color.FromArgb(0, 122, 255)); // 组合光标用蓝色
                }
            }
            
            // 绘制主光标（聚焦时且不在组合状态）
            if (IsFocused && ShouldShowCursor() && !_isComposing)
            {
                DrawCursor(context, textBounds, displayText ?? "", scaledFontSize);
            }
        }
        else if (!string.IsNullOrEmpty(Placeholder))
        {
            var textY = textBounds.Y + scaledFontSize * 0.5;
            context.DrawText(Placeholder, textBounds.X, textY, scaledFontSize, null, null, Color.Gray);
            
            // 绘制光标（聚焦时，无文本时光标在开头）
            if (IsFocused && ShouldShowCursor())
            {
                DrawCursor(context, textBounds, "", scaledFontSize);
            }
        }
    }
    
    /// <summary>
    /// 绘制选择背景
    /// </summary>
    private void DrawSelection(IDrawingContext context, Rect textBounds, string displayText, double scaledFontSize)
    {
        var selStart = SelectionStart;
        var selEnd = SelectionEnd;
        
        // 计算选择区域的起始和结束 X 坐标
        double startX = textBounds.X;
        double endX = textBounds.X;
        
        if (selStart > 0)
        {
            var textBeforeSelection = displayText.Substring(0, selStart);
            startX += context.MeasureText(textBeforeSelection, scaledFontSize, null);
        }
        
        if (selEnd > 0)
        {
            var textUpToEnd = displayText.Substring(0, selEnd);
            endX += context.MeasureText(textUpToEnd, scaledFontSize, null);
        }
        
        // 绘制选择背景
        var selectionRect = new Rect(
            startX,
            textBounds.Y,
            endX - startX,
            scaledFontSize);
        
        context.DrawRectangle(selectionRect, SelectionBackgroundColor);
    }
    
    /// <summary>
    /// 判断是否应该显示光标（闪烁效果）
    /// </summary>
    private bool ShouldShowCursor()
    {
        // 如果正在选择，始终显示光标
        if (_isSelecting)
            return true;
        
        var now = DateTime.Now;
        var elapsed = (now - _lastCursorToggle).TotalMilliseconds;
        
        if (elapsed >= CursorBlinkIntervalMs)
        {
            _isCursorVisible = !_isCursorVisible;
            _lastCursorToggle = now;
            // 触发重绘以更新光标闪烁状态
            StateHasChanged();
        }
        
        return _isCursorVisible;
    }
    
    /// <summary>
    /// 绘制光标
    /// </summary>
    private void DrawCursor(IDrawingContext context, Rect textBounds, string displayText, double scaledFontSize)
    {
        // 计算光标位置（基于 _cursorPosition）
        double cursorX = textBounds.X;
        
        if (!string.IsNullOrEmpty(displayText) && _cursorPosition > 0)
        {
            // 测量光标前文本的宽度
            var textBeforeCursor = displayText.Substring(0, _cursorPosition);
            cursorX += context.MeasureText(textBeforeCursor, scaledFontSize, null);
        }
        
        // 光标高度和位置
        var cursorHeight = scaledFontSize;
        var cursorY = textBounds.Y;
        
        // 绘制细线作为光标
        var cursorBounds = new Rect(cursorX, cursorY, 2 * context.Scale, cursorHeight);
        context.DrawRectangle(cursorBounds, Color.Black);
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
        ClearSelectionInternal();
        _clickCount = 0; // 重置点击计数
        StateHasChanged();
    }
}