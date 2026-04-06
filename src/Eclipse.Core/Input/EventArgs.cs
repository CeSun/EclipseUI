using System;

namespace Eclipse.Input;

/// <summary>
/// 路由事件参数基类
/// </summary>
public class RoutedEventArgs : EventArgs
{
    /// <summary>
    /// 事件是否已处理
    /// </summary>
    public bool Handled { get; set; }
    
    /// <summary>
    /// 路由事件
    /// </summary>
    public RoutedEvent RoutedEvent { get; internal set; } = null!;
    
    /// <summary>
    /// 事件源 (当前处理事件的元素)
    /// </summary>
    public object Source { get; internal set; } = null!;
    
    /// <summary>
    /// 原始源 (最开始触发事件的元素)
    /// </summary>
    public object OriginalSource { get; internal set; } = null!;
}

/// <summary>
/// 指针事件参数基类
/// </summary>
public class PointerEventArgs : RoutedEventArgs
{
    /// <summary>
    /// 指针实例
    /// </summary>
    public Pointer Pointer { get; private set; }
    
    /// <summary>
    /// 相对于源元素的位置
    /// </summary>
    public Point Position { get; private set; }
    
    /// <summary>
    /// 键盘修饰键
    /// </summary>
    public KeyModifiers KeyModifiers { get; init; }
    
    /// <summary>
    /// 指针属性
    /// </summary>
    public PointerPointProperties Properties { get; init; }
    
    public PointerEventArgs() : this(Pointer.Mouse, Point.Zero) { }
    
    public PointerEventArgs(Pointer pointer, Point position)
    {
        Pointer = pointer;
        Position = position;
    }
    
    /// <summary>
    /// 获取相对于指定元素的位置
    /// </summary>
    public Point GetPosition(IInputElement? relativeTo)
    {
        return Position;
    }
}

/// <summary>
/// 指针按下事件参数
/// </summary>
public class PointerPressedEventArgs : PointerEventArgs
{
    /// <summary>
    /// 点击次数 (双击检测)
    /// </summary>
    public int ClickCount { get; init; } = 1;
    
    public PointerPressedEventArgs() : base() { }
    
    public PointerPressedEventArgs(Pointer pointer, Point position) 
        : base(pointer, position)
    {
    }
    
    /// <summary>
    /// 捕获指针
    /// </summary>
    public void Capture(IInputElement element)
    {
        Pointer.Capture(element);
    }
}

/// <summary>
/// 指针释放事件参数
/// </summary>
public class PointerReleasedEventArgs : PointerEventArgs
{
    /// <summary>
    /// 初始按下位置
    /// </summary>
    public Point InitialPressPosition { get; init; }
    
    public PointerReleasedEventArgs() : base() { }
    
    public PointerReleasedEventArgs(Pointer pointer, Point position) 
        : base(pointer, position)
    {
    }
}

/// <summary>
/// 指针滚轮事件参数
/// </summary>
public class PointerWheelEventArgs : PointerEventArgs
{
    /// <summary>
    /// 滚轮增量 (正值向上/右，负值向下/左)
    /// </summary>
    public Vector Delta { get; init; }
    
    /// <summary>
    /// 是否是精确滚动 (触控板)
    /// </summary>
    public bool IsPrecise { get; init; }
    
    public PointerWheelEventArgs() : base() { }
    
    public PointerWheelEventArgs(Pointer pointer, Point position, Vector delta) 
        : base(pointer, position)
    {
        Delta = delta;
    }
}

/// <summary>
/// 指针捕获丢失事件参数
/// </summary>
public class PointerCaptureLostEventArgs : RoutedEventArgs
{
    public Pointer Pointer { get; }
    
    public PointerCaptureLostEventArgs() => Pointer = Pointer.Mouse;
    
    public PointerCaptureLostEventArgs(Pointer pointer)
    {
        Pointer = pointer;
    }
}

/// <summary>
/// IME 组合事件参数
/// </summary>
public class CompositionEventArgs : RoutedEventArgs
{
    /// <summary>
    /// 组合文本（正在输入的拼音/笔画）
    /// </summary>
    public string CompositionText { get; } = string.Empty;
    
    /// <summary>
    /// 组合文本中的光标位置
    /// </summary>
    public int CursorPosition { get; } = 0;
    
    public CompositionEventArgs() { }
    
    public CompositionEventArgs(string compositionText, int cursorPosition = 0)
    {
        CompositionText = compositionText;
        CursorPosition = cursorPosition;
    }
}