using System;

namespace Eclipse.Input.Gestures;

/// <summary>
/// 长按状态
/// </summary>
public enum HoldingState
{
    Started,
    Completed,
    Canceled
}

/// <summary>
/// 长按事件参数
/// </summary>
public class HoldingEventArgs : EventArgs
{
    public HoldingState State { get; init; }
    public PointerPoint Point { get; init; }
}

/// <summary>
/// 手势识别器基类
/// </summary>
public abstract class GestureRecognizer
{
    /// <summary>
    /// 关联的元素
    /// </summary>
    public IInputElement? Target { get; internal set; }
    
    /// <summary>
    /// 是否正在识别手势
    /// </summary>
    public bool IsActive { get; protected set; }
    
    /// <summary>
    /// 处理指针按下
    /// </summary>
    protected virtual void OnPointerPressed(PointerPressedEventArgs e) { }
    
    /// <summary>
    /// 处理指针移动
    /// </summary>
    protected virtual void OnPointerMoved(PointerEventArgs e) { }
    
    /// <summary>
    /// 处理指针释放
    /// </summary>
    protected virtual void OnPointerReleased(PointerReleasedEventArgs e) { }
    
    /// <summary>
    /// 处理指针取消
    /// </summary>
    protected virtual void OnPointerCanceled(PointerEventArgs e) { }
    
    /// <summary>
    /// 捕获指针
    /// </summary>
    protected void CapturePointer(Pointer pointer)
    {
        Target?.CapturePointer(pointer);
    }
    
    /// <summary>
    /// 释放指针捕获
    /// </summary>
    protected void ReleasePointerCapture(Pointer pointer)
    {
        Target?.ReleasePointerCapture(pointer);
    }
    
    internal void ProcessPointerPressed(PointerPressedEventArgs e) => OnPointerPressed(e);
    internal void ProcessPointerMoved(PointerEventArgs e) => OnPointerMoved(e);
    internal void ProcessPointerReleased(PointerReleasedEventArgs e) => OnPointerReleased(e);
    internal void ProcessPointerCanceled(PointerEventArgs e) => OnPointerCanceled(e);
}

/// <summary>
/// 内置手势事件 - 添加到 InputElementBase
/// </summary>
public static class GestureEvents
{
    /// <summary>
    /// 长按事件
    /// </summary>
    public static readonly RoutedEvent<HoldingRoutedEventArgs> HoldingEvent =
        RoutedEvent<HoldingRoutedEventArgs>.Register<InputElementBase>(
            "Holding",
            RoutingStrategy.Bubble);
    
    /// <summary>
    /// 双击事件
    /// </summary>
    public static readonly RoutedEvent<RoutedEventArgs> DoubleTappedEvent =
        RoutedEvent<RoutedEventArgs>.Register<InputElementBase>(
            "DoubleTapped",
            RoutingStrategy.Bubble);
}

/// <summary>
/// 长按路由事件参数
/// </summary>
public class HoldingRoutedEventArgs : RoutedEventArgs
{
    public HoldingState State { get; init; }
    public Pointer Pointer { get; init; } = Pointer.Mouse;
    public Point Position { get; init; }
    
    public HoldingRoutedEventArgs() { }
    
    public HoldingRoutedEventArgs(HoldingState state, Pointer pointer, Point position)
    {
        State = state;
        Pointer = pointer;
        Position = position;
    }
}