using System;

namespace Eclipse.Input;

/// <summary>
/// 点击事件参数 - 专用于 Tapped 事件
/// </summary>
public class TappedEventArgs : RoutedEventArgs
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
    /// 点击次数 (1 = 单击, 2 = 双击)
    /// </summary>
    public int TapCount { get; init; } = 1;
    
    /// <summary>
    /// 键盘修饰键
    /// </summary>
    public KeyModifiers KeyModifiers { get; init; }
    
    public TappedEventArgs() : this(Pointer.Mouse, Point.Zero) { }
    
    public TappedEventArgs(Pointer pointer, Point position)
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
    
    /// <summary>
    /// 捕获指针
    /// </summary>
    public void Capture(IInputElement element)
    {
        Pointer.Capture(element);
    }
}