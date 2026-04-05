using System;
using System.Collections.Generic;

namespace Eclipse.Input;

/// <summary>
/// 输入管理器 - 核心输入处理
/// </summary>
public sealed class InputManager
{
    // 焦点管理器
    public FocusManager FocusManager { get; } = new();
    
    // 当前指针状态
    private readonly Dictionary<int, PointerState> _pointerStates = new();
    
    // 鼠标悬停元素
    private IInputElement? _pointerOverElement;
    
    // 根元素
    public IInputElement? RootElement { get; set; }
    
    // 事件
    public event EventHandler<PointerPressedEventArgs>? PointerPressed;
    public event EventHandler<PointerEventArgs>? PointerMoved;
    public event EventHandler<PointerReleasedEventArgs>? PointerReleased;
    public event EventHandler<PointerWheelEventArgs>? PointerWheel;
    
    // 键盘事件
    public event EventHandler<KeyEventArgs>? KeyDown;
    public event EventHandler<KeyEventArgs>? KeyUp;
    public event EventHandler<TextInputEventArgs>? TextInput;
    
    /// <summary>
    /// 处理指针按下
    /// </summary>
    public void ProcessPointerPressed(
        Pointer pointer, 
        Point position, 
        PointerPointProperties properties,
        KeyModifiers modifiers = KeyModifiers.None,
        int clickCount = 1)
    {
        var state = GetOrCreateState(pointer);
        state.PressPosition = position;
        state.PressTimestamp = (ulong)Environment.TickCount64;
        state.Properties = properties;
        
        // 检查捕获
        if (pointer.Captured != null)
        {
            RaisePointerPressed(pointer.Captured, pointer, position, properties, modifiers, clickCount);
        }
        else
        {
            // 命中测试
            var hitElement = HitTest(position);
            if (hitElement != null)
            {
                RaisePointerPressed(hitElement, pointer, position, properties, modifiers, clickCount);
            }
        }
    }
    
    /// <summary>
    /// 处理指针移动
    /// </summary>
    public void ProcessPointerMoved(
        Pointer pointer, 
        Point position,
        PointerPointProperties properties = default,
        KeyModifiers modifiers = KeyModifiers.None)
    {
        var state = GetOrCreateState(pointer);
        state.Position = position;
        state.Properties = properties;
        
        // 处理捕获
        if (pointer.Captured != null)
        {
            RaisePointerMoved(pointer.Captured, pointer, position, properties, modifiers);
        }
        else
        {
            // 命中测试
            var hitElement = HitTest(position);
            
            // 悬停状态变化
            if (hitElement != _pointerOverElement)
            {
                if (_pointerOverElement != null)
                {
                    RaisePointerExited(_pointerOverElement, pointer, position);
                }
                
                _pointerOverElement = hitElement;
                
                if (hitElement != null)
                {
                    RaisePointerEntered(hitElement, pointer, position);
                }
            }
            
            // 移动事件
            if (hitElement != null)
            {
                RaisePointerMoved(hitElement, pointer, position, properties, modifiers);
            }
        }
    }
    
    /// <summary>
    /// 处理指针释放
    /// </summary>
    public void ProcessPointerReleased(
        Pointer pointer, 
        Point position,
        PointerButtons releasedButton,
        KeyModifiers modifiers = KeyModifiers.None)
    {
        var state = GetOrCreateState(pointer);
        
        var targetElement = pointer.Captured ?? HitTest(position);
        
        if (targetElement != null)
        {
            RaisePointerReleased(targetElement, pointer, position, state.Properties, modifiers);
            
            // 检测点击 (Tap)
            if (IsTap(state, position))
            {
                RaiseTapped(targetElement, pointer, position, 1);
            }
        }
        
        // 清除状态
        state.Reset();
        
        // 如果是触摸，移除指针
        if (pointer.Type == PointerType.Touch)
        {
            Pointer.Remove(pointer.Id);
        }
    }
    
    /// <summary>
    /// 处理指针滚轮
    /// </summary>
    public void ProcessPointerWheel(
        Pointer pointer,
        Point position,
        Vector delta,
        KeyModifiers modifiers = KeyModifiers.None)
    {
        var targetElement = pointer.Captured ?? HitTest(position);
        
        if (targetElement != null)
        {
            RaisePointerWheel(targetElement, pointer, position, delta, modifiers);
        }
    }
    
    // === 键盘处理 ===
    
    /// <summary>
    /// 处理按键按下
    /// </summary>
    public void ProcessKeyDown(Key key, int keyCode, KeyModifiers modifiers = KeyModifiers.None, bool isRepeat = false)
    {
        var focusedElement = FocusManager.FocusedElement ?? RootElement;
        
        if (focusedElement != null)
        {
            RaiseKeyDown(focusedElement, key, keyCode, modifiers, isRepeat);
        }
    }
    
    /// <summary>
    /// 处理按键释放
    /// </summary>
    public void ProcessKeyUp(Key key, int keyCode, KeyModifiers modifiers = KeyModifiers.None)
    {
        var focusedElement = FocusManager.FocusedElement ?? RootElement;
        
        if (focusedElement != null)
        {
            RaiseKeyUp(focusedElement, key, keyCode, modifiers);
        }
    }
    
    /// <summary>
    /// 处理文本输入
    /// </summary>
    public void ProcessTextInput(string text)
    {
        var focusedElement = FocusManager.FocusedElement;
        
        if (focusedElement != null)
        {
            RaiseTextInput(focusedElement, text);
        }
    }
    
    // === 命中测试 ===
    
    private IInputElement? HitTest(Point point)
    {
        if (RootElement == null)
        {
            Console.WriteLine("[InputManager] HitTest: RootElement is null");
            return null;
        }
        
        var result = HitTestRecursive(RootElement, point);
        Console.WriteLine($"[InputManager] HitTest at {point}: {(result?.GetType().Name ?? "null")}");
        return result;
    }
    
    private IInputElement? HitTestRecursive(IInputElement element, Point point)
    {
        if (!element.IsVisible || !element.IsHitTestVisible || !element.IsInputEnabled)
            return null;
        
        var bounds = element.Bounds;
        Console.WriteLine($"[InputManager] HitTestRecursive: {element.GetType().Name} bounds={bounds}");
        
        // 先检查子元素 (后渲染的在上面)
        foreach (var child in element.Children)
        {
            var result = HitTestRecursive(child, point);
            if (result != null)
                return result;
        }
        
        // 再检查自己
        if (element.HitTest(point))
            return element;
        
        return null;
    }
    
    // === 事件触发 ===
    
    private void RaisePointerPressed(
        IInputElement target,
        Pointer pointer,
        Point position,
        PointerPointProperties properties,
        KeyModifiers modifiers,
        int clickCount)
    {
        // 先触发 Tunnel 事件
        var previewArgs = new PointerPressedEventArgs(pointer, position)
        {
            Properties = properties,
            KeyModifiers = modifiers,
            ClickCount = clickCount
        };
        
        var previewEvent = InputElementBase.PreviewPointerPressedEvent;
        previewArgs.RoutedEvent = previewEvent;
        target.RaiseEvent(previewArgs);
        
        if (previewArgs.Handled)
            return;
        
        // 再触发 Bubble 事件
        var args = new PointerPressedEventArgs(pointer, position)
        {
            Properties = properties,
            KeyModifiers = modifiers,
            ClickCount = clickCount
        };
        
        args.RoutedEvent = InputElementBase.PointerPressedEvent;
        target.RaiseEvent(args);
        
        PointerPressed?.Invoke(this, args);
    }
    
    private void RaisePointerMoved(
        IInputElement target,
        Pointer pointer,
        Point position,
        PointerPointProperties properties,
        KeyModifiers modifiers)
    {
        // 先触发 Tunnel 事件
        var previewArgs = new PointerEventArgs(pointer, position)
        {
            Properties = properties,
            KeyModifiers = modifiers
        };
        
        previewArgs.RoutedEvent = InputElementBase.PreviewPointerMovedEvent;
        target.RaiseEvent(previewArgs);
        
        if (previewArgs.Handled)
            return;
        
        // 再触发 Bubble 事件
        var args = new PointerEventArgs(pointer, position)
        {
            Properties = properties,
            KeyModifiers = modifiers
        };
        
        args.RoutedEvent = InputElementBase.PointerMovedEvent;
        target.RaiseEvent(args);
        
        PointerMoved?.Invoke(this, args);
    }
    
    private void RaisePointerReleased(
        IInputElement target,
        Pointer pointer,
        Point position,
        PointerPointProperties properties,
        KeyModifiers modifiers)
    {
        // 先触发 Tunnel 事件
        var previewArgs = new PointerReleasedEventArgs(pointer, position)
        {
            Properties = properties,
            KeyModifiers = modifiers
        };
        
        previewArgs.RoutedEvent = InputElementBase.PreviewPointerReleasedEvent;
        target.RaiseEvent(previewArgs);
        
        if (previewArgs.Handled)
            return;
        
        // 再触发 Bubble 事件
        var args = new PointerReleasedEventArgs(pointer, position)
        {
            Properties = properties,
            KeyModifiers = modifiers,
            InitialPressPosition = GetOrCreateState(pointer).PressPosition
        };
        
        args.RoutedEvent = InputElementBase.PointerReleasedEvent;
        target.RaiseEvent(args);
        
        PointerReleased?.Invoke(this, args);
    }
    
    private void RaisePointerEntered(IInputElement target, Pointer pointer, Point position)
    {
        var args = new PointerEventArgs(pointer, position)
        {
            RoutedEvent = InputElementBase.PointerEnteredEvent
        };
        target.RaiseEvent(args);
    }
    
    private void RaisePointerExited(IInputElement target, Pointer pointer, Point position)
    {
        var args = new PointerEventArgs(pointer, position)
        {
            RoutedEvent = InputElementBase.PointerExitedEvent
        };
        target.RaiseEvent(args);
    }
    
    private void RaisePointerWheel(
        IInputElement target,
        Pointer pointer,
        Point position,
        Vector delta,
        KeyModifiers modifiers)
    {
        var args = new PointerWheelEventArgs(pointer, position, delta)
        {
            KeyModifiers = modifiers
        };
        
        args.RoutedEvent = InputElementBase.PointerWheelChangedEvent;
        target.RaiseEvent(args);
        
        PointerWheel?.Invoke(this, args);
    }
    
    private void RaiseTapped(IInputElement target, Pointer pointer, Point position, int tapCount)
    {
        var args = new PointerPressedEventArgs(pointer, position)
        {
            ClickCount = tapCount
        };
        
        args.RoutedEvent = InputElementBase.TappedEvent;
        target.RaiseEvent(args);
    }
    
    // === 键盘事件触发 ===
    
    private void RaiseKeyDown(IInputElement target, Key key, int keyCode, KeyModifiers modifiers, bool isRepeat)
    {
        // Tunnel
        var previewArgs = new KeyEventArgs(key, keyCode, modifiers) { IsRepeat = isRepeat };
        previewArgs.RoutedEvent = InputElementBase.PreviewKeyDownEvent;
        target.RaiseEvent(previewArgs);
        
        if (previewArgs.Handled)
            return;
        
        // Bubble
        var args = new KeyEventArgs(key, keyCode, modifiers) { IsRepeat = isRepeat };
        args.RoutedEvent = InputElementBase.KeyDownEvent;
        target.RaiseEvent(args);
        
        KeyDown?.Invoke(this, args);
    }
    
    private void RaiseKeyUp(IInputElement target, Key key, int keyCode, KeyModifiers modifiers)
    {
        var args = new KeyEventArgs(key, keyCode, modifiers);
        args.RoutedEvent = InputElementBase.KeyUpEvent;
        target.RaiseEvent(args);
        
        KeyUp?.Invoke(this, args);
    }
    
    private void RaiseTextInput(IInputElement target, string text)
    {
        var args = new TextInputEventArgs(text);
        args.RoutedEvent = InputElementBase.TextInputEvent;
        target.RaiseEvent(args);
        
        TextInput?.Invoke(this, args);
    }
    
    // === 辅助方法 ===
    
    private bool IsTap(PointerState state, Point releasePosition)
    {
        const double maxTapDistance = 10;
        const ulong maxTapDuration = 500;
        
        var distance = (releasePosition - state.PressPosition).Length;
        var now = (ulong)Environment.TickCount64;
        var duration = now - state.PressTimestamp;
        
        return distance < maxTapDistance && duration < maxTapDuration;
    }
    
    private PointerState GetOrCreateState(Pointer pointer)
    {
        if (!_pointerStates.TryGetValue(pointer.Id, out var state))
        {
            state = new PointerState();
            _pointerStates[pointer.Id] = state;
        }
        return state;
    }
    
    private sealed class PointerState
    {
        public Point Position;
        public Point PressPosition;
        public ulong PressTimestamp;
        public PointerPointProperties Properties;
        
        public void Reset()
        {
            PressPosition = default;
            PressTimestamp = 0;
        }
    }
}