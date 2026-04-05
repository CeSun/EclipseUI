using System;
using System.Collections.Generic;
using Eclipse.Core.Abstractions;

namespace Eclipse.Input;

/// <summary>
/// 可接收输入的元素基类
/// </summary>
public abstract class InputElementBase : Core.ComponentBase, IInputElement
{
    // === 路由事件定义 ===
    
    public static readonly RoutedEvent<PointerPressedEventArgs> PointerPressedEvent =
        RoutedEvent<PointerPressedEventArgs>.Register<InputElementBase>(
            nameof(PointerPressed),
            RoutingStrategy.Bubble);
    
    public static readonly RoutedEvent<PointerPressedEventArgs> PreviewPointerPressedEvent =
        RoutedEvent<PointerPressedEventArgs>.Register<InputElementBase>(
            nameof(PreviewPointerPressed),
            RoutingStrategy.Tunnel);
    
    public static readonly RoutedEvent<PointerEventArgs> PointerMovedEvent =
        RoutedEvent<PointerEventArgs>.Register<InputElementBase>(
            nameof(PointerMoved),
            RoutingStrategy.Bubble);
    
    public static readonly RoutedEvent<PointerEventArgs> PreviewPointerMovedEvent =
        RoutedEvent<PointerEventArgs>.Register<InputElementBase>(
            nameof(PreviewPointerMoved),
            RoutingStrategy.Tunnel);
    
    public static readonly RoutedEvent<PointerReleasedEventArgs> PointerReleasedEvent =
        RoutedEvent<PointerReleasedEventArgs>.Register<InputElementBase>(
            nameof(PointerReleased),
            RoutingStrategy.Bubble);
    
    public static readonly RoutedEvent<PointerReleasedEventArgs> PreviewPointerReleasedEvent =
        RoutedEvent<PointerReleasedEventArgs>.Register<InputElementBase>(
            nameof(PreviewPointerReleased),
            RoutingStrategy.Tunnel);
    
    public static readonly RoutedEvent<PointerWheelEventArgs> PointerWheelChangedEvent =
        RoutedEvent<PointerWheelEventArgs>.Register<InputElementBase>(
            nameof(PointerWheelChanged),
            RoutingStrategy.Bubble);
    
    public static readonly RoutedEvent<PointerEventArgs> PointerEnteredEvent =
        RoutedEvent<PointerEventArgs>.Register<InputElementBase>(
            nameof(PointerEntered),
            RoutingStrategy.Direct);
    
    public static readonly RoutedEvent<PointerEventArgs> PointerExitedEvent =
        RoutedEvent<PointerEventArgs>.Register<InputElementBase>(
            nameof(PointerExited),
            RoutingStrategy.Direct);
    
    public static readonly RoutedEvent<PointerPressedEventArgs> TappedEvent =
        RoutedEvent<PointerPressedEventArgs>.Register<InputElementBase>(
            nameof(Tapped),
            RoutingStrategy.Bubble);
    
    // === 键盘事件 ===
    
    public static readonly RoutedEvent<KeyEventArgs> KeyDownEvent =
        RoutedEvent<KeyEventArgs>.Register<InputElementBase>(
            nameof(KeyDown),
            RoutingStrategy.Bubble);
    
    public static readonly RoutedEvent<KeyEventArgs> PreviewKeyDownEvent =
        RoutedEvent<KeyEventArgs>.Register<InputElementBase>(
            nameof(PreviewKeyDown),
            RoutingStrategy.Tunnel);
    
    public static readonly RoutedEvent<KeyEventArgs> KeyUpEvent =
        RoutedEvent<KeyEventArgs>.Register<InputElementBase>(
            nameof(KeyUp),
            RoutingStrategy.Bubble);
    
    public static readonly RoutedEvent<TextInputEventArgs> TextInputEvent =
        RoutedEvent<TextInputEventArgs>.Register<InputElementBase>(
            nameof(TextInput),
            RoutingStrategy.Bubble);
    
    // === CLR 事件包装 ===
    
    public event EventHandler<PointerPressedEventArgs> PointerPressed
    {
        add => AddHandler(PointerPressedEvent, value);
        remove => RemoveHandler(PointerPressedEvent, value);
    }
    
    public event EventHandler<PointerPressedEventArgs> PreviewPointerPressed
    {
        add => AddHandler(PreviewPointerPressedEvent, value);
        remove => RemoveHandler(PreviewPointerPressedEvent, value);
    }
    
    public event EventHandler<PointerEventArgs> PointerMoved
    {
        add => AddHandler(PointerMovedEvent, value);
        remove => RemoveHandler(PointerMovedEvent, value);
    }
    
    public event EventHandler<PointerEventArgs> PreviewPointerMoved
    {
        add => AddHandler(PreviewPointerMovedEvent, value);
        remove => RemoveHandler(PreviewPointerMovedEvent, value);
    }
    
    public event EventHandler<PointerReleasedEventArgs> PointerReleased
    {
        add => AddHandler(PointerReleasedEvent, value);
        remove => RemoveHandler(PointerReleasedEvent, value);
    }
    
    public event EventHandler<PointerReleasedEventArgs> PreviewPointerReleased
    {
        add => AddHandler(PreviewPointerReleasedEvent, value);
        remove => RemoveHandler(PreviewPointerReleasedEvent, value);
    }
    
    public event EventHandler<PointerWheelEventArgs> PointerWheelChanged
    {
        add => AddHandler(PointerWheelChangedEvent, value);
        remove => RemoveHandler(PointerWheelChangedEvent, value);
    }
    
    public event EventHandler<PointerEventArgs> PointerEntered
    {
        add => AddHandler(PointerEnteredEvent, value);
        remove => RemoveHandler(PointerEnteredEvent, value);
    }
    
    public event EventHandler<PointerEventArgs> PointerExited
    {
        add => AddHandler(PointerExitedEvent, value);
        remove => RemoveHandler(PointerExitedEvent, value);
    }
    
    public event EventHandler<PointerPressedEventArgs> Tapped
    {
        add => AddHandler(TappedEvent, value);
        remove => RemoveHandler(TappedEvent, value);
    }
    
    // === 键盘事件 CLR 包装 ===
    
    public event EventHandler<KeyEventArgs> KeyDown
    {
        add => AddHandler(KeyDownEvent, value);
        remove => RemoveHandler(KeyDownEvent, value);
    }
    
    public event EventHandler<KeyEventArgs> PreviewKeyDown
    {
        add => AddHandler(PreviewKeyDownEvent, value);
        remove => RemoveHandler(PreviewKeyDownEvent, value);
    }
    
    public event EventHandler<KeyEventArgs> KeyUp
    {
        add => AddHandler(KeyUpEvent, value);
        remove => RemoveHandler(KeyUpEvent, value);
    }
    
    public event EventHandler<TextInputEventArgs> TextInput
    {
        add => AddHandler(TextInputEvent, value);
        remove => RemoveHandler(TextInputEvent, value);
    }
    
    // === 属性 ===
    
    public virtual bool IsInputEnabled { get; set; } = true;
    public virtual bool IsHitTestVisible { get; set; } = true;
    public virtual bool IsFocusable { get; set; }
    public virtual bool IsFocused { get; protected set; }
    public abstract bool IsVisible { get; }
    public abstract Rect Bounds { get; }
    
    // IInputElement.Parent 显式实现
    IInputElement? IInputElement.Parent => Parent as IInputElement;
    IEnumerable<IInputElement> IInputElement.Children => GetInputChildren();
    
    protected virtual IEnumerable<IInputElement> GetInputChildren() => Array.Empty<IInputElement>();
    
    // === 命中测试 ===
    
    public virtual bool HitTest(Point point)
    {
        if (!IsVisible || !IsHitTestVisible || !IsInputEnabled)
            return false;
        
        return Bounds.Contains(point);
    }
    
    // === 聚焦 ===
    
    public virtual bool Focus()
    {
        if (!IsFocusable || !IsVisible)
            return false;
        
        IsFocused = true;
        return true;
    }
    
    protected virtual void OnGotFocus() => IsFocused = true;
    protected virtual void OnLostFocus() => IsFocused = false;
    
    /// <summary>
    /// 设置聚焦状态 (由 FocusManager 调用)
    /// </summary>
    internal void SetIsFocused(bool focused)
    {
        if (IsFocused == focused)
            return;
        
        IsFocused = focused;
        
        if (focused)
            OnGotFocus();
        else
            OnLostFocus();
    }
    
    // === 指针捕获 ===
    
    public virtual void CapturePointer(Pointer pointer)
    {
        pointer.Capture(this);
    }
    
    public virtual void ReleasePointerCapture(Pointer pointer)
    {
        if (pointer.Captured == this)
        {
            pointer.Capture(null);
        }
    }
    
    // === 事件处理 ===
    
    private readonly Dictionary<RoutedEvent, List<Delegate>> _handlers = new();
    
    public virtual void AddHandler(RoutedEvent routedEvent, Delegate handler)
    {
        if (!_handlers.TryGetValue(routedEvent, out var list))
        {
            list = new List<Delegate>();
            _handlers[routedEvent] = list;
        }
        list.Add(handler);
    }
    
    public virtual void RemoveHandler(RoutedEvent routedEvent, Delegate handler)
    {
        if (_handlers.TryGetValue(routedEvent, out var list))
        {
            list.Remove(handler);
        }
    }
    
    public virtual void RaiseEvent(RoutedEventArgs e)
    {
        e.Source = this;
        EventRouter.RaiseEvent(this, e);
    }
    
    internal void InvokeHandlersInternal(RoutedEventArgs e)
    {
        if (_handlers.TryGetValue(e.RoutedEvent, out var list))
        {
            foreach (var handler in list)
            {
                if (e.Handled)
                    break;
                    
                handler.DynamicInvoke(this, e);
            }
        }
    }
}