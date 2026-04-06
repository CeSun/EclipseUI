using System;
using System.Collections.Generic;
using Eclipse.Core.Abstractions;

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
    private IInputElement? _rootElement;
    
    /// <summary>
    /// 根元素（用于 Hit Testing）
    /// </summary>
    public IInputElement? RootElement 
    { 
        get => _rootElement;
        set => _rootElement = value;
    }
    
    /// <summary>
    /// 设置根元素（供渲染器在 Rebuild 后调用）
    /// </summary>
    public void SetRootElementForRender(IInputElement root)
    {
        _rootElement = root;
    }
    
    // 事件
    public event EventHandler<PointerPressedEventArgs>? PointerPressed;
    public event EventHandler<PointerEventArgs>? PointerMoved;
    public event EventHandler<PointerReleasedEventArgs>? PointerReleased;
    public event EventHandler<PointerWheelEventArgs>? PointerWheel;
    
    // 键盘事件
    public event EventHandler<KeyEventArgs>? KeyDown;
    public event EventHandler<KeyEventArgs>? KeyUp;
    public event EventHandler<TextInputEventArgs>? TextInput;
    
    // IME 组合事件
    public event EventHandler<CompositionEventArgs>? CompositionStarted;
    public event EventHandler<CompositionEventArgs>? CompositionChanged;
    public event EventHandler<CompositionEventArgs>? CompositionEnded;
    
    // IME 位置更新委托
    private Action<double, double>? _compositionPositionHandler;
    
    /// <summary>
    /// 注册组合位置更新处理器
    /// </summary>
    public void RegisterCompositionPositionHandler(Action<double, double> handler)
    {
        _compositionPositionHandler = handler;
    }
    
    /// <summary>
    /// 更新组合窗口位置
    /// </summary>
    public void UpdateCompositionPosition(double x, double y)
    {
        _compositionPositionHandler?.Invoke(x, y);
    }
    
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
    
    /// <summary>
    /// 处理 IME 组合开始
    /// </summary>
    public void ProcessCompositionStarted()
    {
        var focusedElement = FocusManager.FocusedElement ?? RootElement;
        
        if (focusedElement != null)
        {
            RaiseCompositionStarted(focusedElement);
        }
    }
    
    /// <summary>
    /// 处理 IME 组合文本变化
    /// </summary>
    public void ProcessCompositionChanged(string compositionText, int cursorPosition)
    {
        var focusedElement = FocusManager.FocusedElement ?? RootElement;
        
        if (focusedElement != null)
        {
            RaiseCompositionChanged(focusedElement, compositionText, cursorPosition);
        }
    }
    
    /// <summary>
    /// 处理 IME 组合结束
    /// </summary>
    public void ProcessCompositionEnded()
    {
        var focusedElement = FocusManager.FocusedElement ?? RootElement;
        
        if (focusedElement != null)
        {
            RaiseCompositionEnded(focusedElement);
        }
    }
    
    // === 命中测试 ===
    
    private IInputElement? HitTest(Point point)
    {
        if (RootElement == null)
            return null;
        
        return HitTestRecursiveWithContainers((IComponent)RootElement, point);
    }
    
    private IInputElement? HitTestRecursive(IInputElement element, Point point)
    {
        // 不可见或禁用时跳过整个元素（包括子元素）
        if (!element.IsVisible || !element.IsInputEnabled)
            return null;
        
        // 先检查子元素 (后渲染的在上面) - 即使 IsHitTestVisible=false 也要检查子元素
        // 注意：element.Children 返回 IInputElement，但实际子元素可能包含非 IInputElement 的容器
        // 所以需要用 component.Children 来获取所有子元素
        if (element is IComponent component)
        {
            foreach (var child in component.Children)
            {
                if (child is IInputElement childInputElement)
                {
                    var result = HitTestRecursive(childInputElement, point);
                    if (result != null)
                        return result;
                }
                else
                {
                    // 非 IInputElement 容器，递归检查
                    var result = HitTestRecursiveWithContainers(child, point);
                    if (result != null)
                        return result;
                }
            }
        }
        else
        {
            // 不是 IComponent，使用原有的 Children 属性
            foreach (var child in element.Children)
            {
                var result = HitTestRecursive(child, point);
                if (result != null)
                    return result;
            }
        }
        
        // 再检查自己（受 IsHitTestVisible 控制）
        if (element.IsHitTestVisible && element.HitTest(point))
            return element;
        
        return null;
    }
    
    /// <summary>
    /// 递归命中测试（支持 ComponentBase 容器）
    /// </summary>
    private IInputElement? HitTestRecursiveWithContainers(IComponent component, Point point)
    {
        // 如果是 IInputElement，检查可见性和启用状态
        if (component is IInputElement inputElement)
        {
            // 不可见或禁用时跳过整个元素（包括子元素）
            if (!inputElement.IsVisible || !inputElement.IsInputEnabled)
            {
                return null;
            }
        }
        
        // 先检查子元素 (后渲染的在上面) - 即使 IsHitTestVisible=false 也要检查子元素
        foreach (var child in component.Children)
        {
            // 如果子元素是 IInputElement，用 HitTestRecursive
            if (child is IInputElement childInputElement)
            {
                var result = HitTestRecursive(childInputElement, point);
                if (result != null)
                    return result;
            }
            else
            {
                // 否则继续递归（可能是 ComponentBase 容器）
                var result = HitTestRecursiveWithContainers(child, point);
                if (result != null)
                    return result;
            }
        }
        
        // 如果自己也是 IInputElement，检查自己（但受 IsHitTestVisible 控制）
        if (component is IInputElement selfElement)
        {
            if (selfElement.IsHitTestVisible && selfElement.HitTest(point))
            {
                return selfElement;
            }
        }
        
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
        var args = new TappedEventArgs(pointer, position)
        {
            TapCount = tapCount
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
    
    // === IME 组合事件触发 ===
    
    private void RaiseCompositionStarted(IInputElement target)
    {
        var args = new CompositionEventArgs();
        args.RoutedEvent = InputElementBase.CompositionStartedEvent;
        target.RaiseEvent(args);
        
        CompositionStarted?.Invoke(this, args);
    }
    
    private void RaiseCompositionChanged(IInputElement target, string compositionText, int cursorPosition)
    {
        var args = new CompositionEventArgs(compositionText, cursorPosition);
        args.RoutedEvent = InputElementBase.CompositionChangedEvent;
        target.RaiseEvent(args);
        
        CompositionChanged?.Invoke(this, args);
    }
    
    private void RaiseCompositionEnded(IInputElement target)
    {
        var args = new CompositionEventArgs();
        args.RoutedEvent = InputElementBase.CompositionEndedEvent;
        target.RaiseEvent(args);
        
        CompositionEnded?.Invoke(this, args);
    }
    
    // === 辅助方法 ===
    
    private bool IsTap(PointerState state, Point releasePosition)
    {
        const double maxTapDistance = 50;
        const ulong maxTapDuration = 1000;
        
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