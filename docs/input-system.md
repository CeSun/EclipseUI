# 输入系统设计

EclipseUI 输入系统设计文档，借鉴 Avalonia 和 CPF 的成熟方案。

## 设计目标

### 核心目标

| 目标 | 说明 |
|------|------|
| **统一抽象** | Mouse/Touch/Pen 统一为 Pointer 概念 |
| **路由事件** | 支持 Bubble/Tunnel/Direct 三种路由策略 |
| **跨平台** | 平台无关的核心设计，适配层处理差异 |
| **可扩展** | 支持自定义手势识别器 |
| **简洁 API** | 对开发者友好的事件订阅方式 |

### 参考实现

- **Avalonia** - 统一 Pointer 抽象 + 路由事件
- **WPF/CPF** - 经典的 Tunnel/Bubble 配对
- **Flutter** - GestureRecognizer 设计

## 架构概览

```
┌─────────────────────────────────────────────────────────────┐
│                    平台适配层                                │
│                                                              │
│  Windows                    Linux                    macOS  │
│  WM_LBUTTONDOWN            XButtonPress             NSEvent │
│  WM_TOUCH                  XInput                   NSTouch │
│  WM_POINTER                libinput                 NSEvent │
│       ↓                        ↓                        ↓   │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│                    输入抽象层                                │
│                                                              │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  Pointer (统一指针抽象)                              │   │
│  │  - Mouse   鼠标                                      │   │
│  │  - Touch   触摸                                      │   │
│  │  - Pen     触控笔                                    │   │
│  └─────────────────────────────────────────────────────┘   │
│                            ↓                                │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  PointerEvent (指针事件)                             │   │
│  │  - PointerPressed     指针按下                       │   │
│  │  - PointerMoved       指针移动                       │   │
│  │  - PointerReleased    指针释放                       │   │
│  │  - PointerWheelChanged 滚轮变化                      │   │
│  │  - PointerEntered     进入元素                       │   │
│  │  - PointerExited      离开元素                       │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│                    路由事件系统                              │
│                                                              │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  RoutingStrategy (路由策略)                          │   │
│  │  - Direct    直接事件，仅源元素                       │   │
│  │  - Bubble    冒泡，从子到父                           │   │
│  │  - Tunnel    隧道，从父到子                           │   │
│  └─────────────────────────────────────────────────────┘   │
│                            ↓                                │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  RoutedEvent (路由事件)                              │   │
│  │  - 注册、触发、传播                                   │   │
│  │  - Handled 标记阻止传播                              │   │
│  │  - ClassHandler 类级别处理                           │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│                    手势系统 (可选)                           │
│                                                              │
│  内置手势: Tapped, DoubleTapped, Holding                     │
│  手势识别器: Pinch, Scroll, Swipe, Pull                      │
└─────────────────────────────────────────────────────────────┘
```

## 核心类型设计

### 1. Pointer 类型

```csharp
namespace Eclipse.Input;

/// <summary>
/// 指针类型
/// </summary>
public enum PointerType
{
    /// <summary>
    /// 鼠标或触控板
    /// </summary>
    Mouse,
    
    /// <summary>
    /// 触摸屏手指
    /// </summary>
    Touch,
    
    /// <summary>
    /// 触控笔/手写笔
    /// </summary>
    Pen
}

/// <summary>
/// 指针实例 - 代表一个输入设备
/// </summary>
public sealed class Pointer
{
    /// <summary>
    /// 指针唯一标识
    /// </summary>
    public int Id { get; }
    
    /// <summary>
    /// 指针类型
    /// </summary>
    public PointerType Type { get; }
    
    /// <summary>
    /// 是否捕获
    /// </summary>
    public bool IsCaptured { get; }
    
    /// <summary>
    /// 捕获指针 - 所有后续事件都发送到指定元素
    /// </summary>
    public void Capture(IInputElement? element);
    
    /// <summary>
    /// 获取当前捕获的元素
    /// </summary>
    public IInputElement? Captured { get; }
    
    // 静态实例缓存
    private static readonly Dictionary<int, Pointer> _pointers = new();
    
    public static Pointer GetOrCreate(int id, PointerType type)
    {
        if (!_pointers.TryGetValue(id, out var pointer))
        {
            pointer = new Pointer(id, type);
            _pointers[id] = pointer;
        }
        return pointer;
    }
}
```

### 2. PointerPoint - 指针状态

```csharp
namespace Eclipse.Input;

/// <summary>
/// 指针点信息
/// </summary>
public readonly struct PointerPoint
{
    /// <summary>
    /// 指针实例
    /// </summary>
    public Pointer Pointer { get; init; }
    
    /// <summary>
    /// 相对于指定元素的位置
    /// </summary>
    public Point Position { get; init; }
    
    /// <summary>
    /// 屏幕坐标位置
    /// </summary>
    public Point ScreenPosition { get; init; }
    
    /// <summary>
    /// 指针属性
    /// </summary>
    public PointerPointProperties Properties { get; init; }
    
    /// <summary>
    /// 时间戳
    /// </summary>
    public ulong Timestamp { get; init; }
}

/// <summary>
/// 指针属性
/// </summary>
public readonly struct PointerPointProperties
{
    // === 按键状态 ===
    
    /// <summary>
    /// 左键是否按下
    /// </summary>
    public bool IsLeftButtonPressed { get; init; }
    
    /// <summary>
    /// 右键是否按下
    /// </summary>
    public bool IsRightButtonPressed { get; init; }
    
    /// <summary>
    /// 中键是否按下
    /// </summary>
    public bool IsMiddleButtonPressed { get; init; }
    
    /// <summary>
    /// X1 按钮是否按下
    /// </summary>
    public bool IsXButton1Pressed { get; init; }
    
    /// <summary>
    /// X2 按钮是否按下
    /// </summary>
    public bool IsXButton2Pressed { get; init; }
    
    // === Pen 专用属性 ===
    
    /// <summary>
    /// 压感 (0-1)
    /// </summary>
    public float Pressure { get; init; }
    
    /// <summary>
    /// X 轴倾斜角度
    /// </summary>
    public float XTilt { get; init; }
    
    /// <summary>
    /// Y 轴倾斜角度
    /// </summary>
    public float YTilt { get; init; }
    
    /// <summary>
    /// 笔尖旋转角度
    /// </summary>
    public float Twist { get; init; }
    
    /// <summary>
    /// 是否是橡皮擦端
    /// </summary>
    public bool IsEraser { get; init; }
    
    /// <summary>
    /// 笔杆按钮是否按下
    /// </summary>
    public bool IsBarrelButtonPressed { get; init; }
    
    // === 触摸专用 ===
    
    /// <summary>
    /// 触摸接触区域 (近似)
    /// </summary>
    public Rect? ContactRect { get; init; }
    
    // === 便捷方法 ===
    
    /// <summary>
    /// 获取按下的按键
    /// </summary>
    public PointerButtons GetPressedButtons()
    {
        var buttons = PointerButtons.None;
        if (IsLeftButtonPressed) buttons |= PointerButtons.Left;
        if (IsRightButtonPressed) buttons |= PointerButtons.Right;
        if (IsMiddleButtonPressed) buttons |= PointerButtons.Middle;
        if (IsXButton1Pressed) buttons |= PointerButtons.XButton1;
        if (IsXButton2Pressed) buttons |= PointerButtons.XButton2;
        return buttons;
    }
}

/// <summary>
/// 指针按键
/// </summary>
[Flags]
public enum PointerButtons
{
    None = 0,
    Left = 1,
    Right = 2,
    Middle = 4,
    XButton1 = 8,
    XButton2 = 16
}
```

### 3. 路由事件系统

```csharp
namespace Eclipse.Input;

/// <summary>
/// 路由策略
/// </summary>
public enum RoutingStrategy
{
    /// <summary>
    /// 直接事件 - 仅在源元素触发
    /// </summary>
    Direct,
    
    /// <summary>
    /// 冒泡 - 从源元素向上传播到根
    /// </summary>
    Bubble,
    
    /// <summary>
    /// 隧道 - 从根向下传播到源元素
    /// </summary>
    Tunnel
}

/// <summary>
/// 路由事件
/// </summary>
public sealed class RoutedEvent
{
    /// <summary>
    /// 事件名称
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// 路由策略
    /// </summary>
    public RoutingStrategy RoutingStrategy { get; }
    
    /// <summary>
    /// 事件参数类型
    /// </summary>
    public Type EventArgsType { get; }
    
    /// <summary>
    /// 所属类型
    /// </summary>
    public Type OwnerType { get; }
    
    private RoutedEvent(string name, RoutingStrategy strategy, Type eventArgsType, Type ownerType)
    {
        Name = name;
        RoutingStrategy = strategy;
        EventArgsType = eventArgsType;
        OwnerType = ownerType;
    }
    
    /// <summary>
    /// 注册路由事件
    /// </summary>
    public static RoutedEvent Register<TOwner, TArgs>(
        string name, 
        RoutingStrategy routingStrategy)
        where TArgs : RoutedEventArgs, new()
    {
        return new RoutedEvent(name, routingStrategy, typeof(TArgs), typeof(TOwner));
    }
}

/// <summary>
/// 路由事件泛型版本
/// </summary>
public sealed class RoutedEvent<TArgs> where TArgs : RoutedEventArgs, new()
{
    public RoutedEvent InnerEvent { get; }
    
    public string Name => InnerEvent.Name;
    public RoutingStrategy RoutingStrategy => InnerEvent.RoutingStrategy;
    
    public RoutedEvent(RoutedEvent innerEvent)
    {
        InnerEvent = innerEvent;
    }
    
    public static RoutedEvent<TArgs> Register<TOwner>(
        string name, 
        RoutingStrategy routingStrategy)
    {
        var inner = RoutedEvent.Register<TOwner, TArgs>(name, routingStrategy);
        return new RoutedEvent<TArgs>(inner);
    }
}

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
    /// 事件源
    /// </summary>
    public object Source { get; internal set; } = null!;
    
    /// <summary>
    /// 原始源 (最开始触发事件的元素)
    /// </summary>
    public object OriginalSource { get; internal set; } = null!;
}
```

### 4. Pointer 事件参数

```csharp
namespace Eclipse.Input;

/// <summary>
/// 指针事件参数基类
/// </summary>
public class PointerEventArgs : RoutedEventArgs
{
    /// <summary>
    /// 指针实例
    /// </summary>
    public Pointer Pointer { get; }
    
    /// <summary>
    /// 获取相对于指定元素的指针点信息
    /// </summary>
    public PointerPoint GetCurrentPoint(IInputElement? relativeTo);
    
    /// <summary>
    /// 获取所有指针点 (多点触控)
    /// </summary>
    public IReadOnlyList<PointerPoint> GetIntermediatePoints(IInputElement? relativeTo);
    
    /// <summary>
    /// 键盘修饰键
    /// </summary>
    public KeyModifiers KeyModifiers { get; init; }
}

/// <summary>
/// 指针按下事件参数
/// </summary>
public class PointerPressedEventArgs : PointerEventArgs
{
    /// <summary>
    /// 点击次数 (双击检测)
    /// </summary>
    public int ClickCount { get; init; }
    
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
}

/// <summary>
/// 键盘修饰键
/// </summary>
[Flags]
public enum KeyModifiers
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Windows = 8
}
```

### 5. InputElement - 输入元素接口

```csharp
namespace Eclipse.Input;

/// <summary>
/// 可接收输入的元素
/// </summary>
public interface IInputElement
{
    /// <summary>
    /// 是否启用输入
    /// </summary>
    bool IsInputEnabled { get; }
    
    /// <summary>
    /// 是否可见
    /// </summary>
    bool IsVisible { get; }
    
    /// <summary>
    /// 是否可命中测试
    /// </summary>
    bool IsHitTestVisible { get; }
    
    /// <summary>
    /// 父元素
    /// </summary>
    IInputElement? Parent { get; }
    
    /// <summary>
    /// 子元素
    /// </summary>
    IEnumerable<IInputElement> Children { get; }
    
    /// <summary>
    /// 边界矩形
    /// </summary>
    Rect Bounds { get; }
    
    /// <summary>
    /// 命中测试
    /// </summary>
    bool HitTest(Point point);
    
    /// <summary>
    /// 添加事件处理器
    /// </summary>
    void AddHandler(RoutedEvent routedEvent, Delegate handler, RoutingStrategies routes = RoutingStrategies.Bubble | RoutingStrategies.Tunnel);
    
    /// <summary>
    /// 移除事件处理器
    /// </summary>
    void RemoveHandler(RoutedEvent routedEvent, Delegate handler);
    
    /// <summary>
    /// 触发事件
    /// </summary>
    void RaiseEvent(RoutedEventArgs e);
    
    // === 焦点 ===
    
    /// <summary>
    /// 是否可聚焦
    /// </summary>
    bool IsFocusable { get; }
    
    /// <summary>
    /// 是否聚焦
    /// </summary>
    bool IsFocused { get; }
    
    /// <summary>
    /// 聚焦
    /// </summary>
    bool Focus();
}
```

### 6. InputElement 基类实现

```csharp
namespace Eclipse.Controls;

/// <summary>
/// 可接收输入的控件基类
/// </summary>
public abstract class InputElement : ComponentBase, IInputElement
{
    // === 路由事件定义 ===
    
    // Pointer 事件 (配对: Tunnel + Bubble)
    public static readonly RoutedEvent<PointerPressedEventArgs> PointerPressedEvent =
        RoutedEvent<PointerPressedEventArgs>.Register<InputElement>(
            nameof(PointerPressed),
            RoutingStrategy.Bubble);
    
    public static readonly RoutedEvent<PointerPressedEventArgs> PreviewPointerPressedEvent =
        RoutedEvent<PointerPressedEventArgs>.Register<InputElement>(
            nameof(PreviewPointerPressed),
            RoutingStrategy.Tunnel);
    
    public static readonly RoutedEvent<PointerEventArgs> PointerMovedEvent =
        RoutedEvent<PointerEventArgs>.Register<InputElement>(
            nameof(PointerMoved),
            RoutingStrategy.Bubble);
    
    public static readonly RoutedEvent<PointerEventArgs> PreviewPointerMovedEvent =
        RoutedEvent<PointerEventArgs>.Register<InputElement>(
            nameof(PreviewPointerMoved),
            RoutingStrategy.Tunnel);
    
    public static readonly RoutedEvent<PointerReleasedEventArgs> PointerReleasedEvent =
        RoutedEvent<PointerReleasedEventArgs>.Register<InputElement>(
            nameof(PointerReleased),
            RoutingStrategy.Bubble);
    
    public static readonly RoutedEvent<PointerReleasedEventArgs> PreviewPointerReleasedEvent =
        RoutedEvent<PointerReleasedEventArgs>.Register<InputElement>(
            nameof(PreviewPointerReleased),
            RoutingStrategy.Tunnel);
    
    public static readonly RoutedEvent<PointerWheelEventArgs> PointerWheelChangedEvent =
        RoutedEvent<PointerWheelEventArgs>.Register<InputElement>(
            nameof(PointerWheelChanged),
            RoutingStrategy.Bubble);
    
    // 进入/离开
    public static readonly RoutedEvent<PointerEventArgs> PointerEnteredEvent =
        RoutedEvent<PointerEventArgs>.Register<InputElement>(
            nameof(PointerEntered),
            RoutingStrategy.Direct);
    
    public static readonly RoutedEvent<PointerEventArgs> PointerExitedEvent =
        RoutedEvent<PointerEventArgs>.Register<InputElement>(
            nameof(PointerExited),
            RoutingStrategy.Direct);
    
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
    
    public event EventHandler<PointerEventArgs> PointerReleased
    {
        add => AddHandler(PointerReleasedEvent, value);
        remove => RemoveHandler(PointerReleasedEvent, value);
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
    
    // === 属性 ===
    
    public bool IsInputEnabled { get; set; } = true;
    public bool IsHitTestVisible { get; set; } = true;
    public bool IsFocusable { get; set; }
    public bool IsFocused { get; private set; }
    
    // === 命中测试 ===
    
    public virtual bool HitTest(Point point)
    {
        if (!IsVisible || !IsHitTestVisible || !IsInputEnabled)
            return false;
        
        return Bounds.Contains(point);
    }
    
    // === 聚焦 ===
    
    public bool Focus()
    {
        if (!IsFocusable || !IsVisible)
            return false;
        
        FocusManager.SetFocus(this);
        return true;
    }
    
    protected virtual void OnGotFocus()
    {
        IsFocused = true;
    }
    
    protected virtual void OnLostFocus()
    {
        IsFocused = false;
    }
    
    // === 事件处理 ===
    
    private readonly Dictionary<RoutedEvent, List<Delegate>> _handlers = new();
    
    public void AddHandler(RoutedEvent routedEvent, Delegate handler, RoutingStrategies routes = RoutingStrategies.Bubble | RoutingStrategies.Tunnel)
    {
        if (!_handlers.TryGetValue(routedEvent, out var list))
        {
            list = new List<Delegate>();
            _handlers[routedEvent] = list;
        }
        list.Add(handler);
    }
    
    public void RemoveHandler(RoutedEvent routedEvent, Delegate handler)
    {
        if (_handlers.TryGetValue(routedEvent, out var list))
        {
            list.Remove(handler);
        }
    }
    
    public void RaiseEvent(RoutedEventArgs e)
    {
        e.Source = this;
        EventRouter.RaiseEvent(this, e);
    }
}
```

## 事件路由机制

### 1. 事件传播流程

```
用户点击 Button (在 StackPanel 内，StackPanel 在 Window 内)
    ↓
1. PlatformAdapter 接收原生事件
    ↓
2. 转换为 PointerPressedEventArgs
    ↓
3. Hit Testing 找到源元素 (Button)
    ↓
4. 构建事件路由 (从 Window 到 Button)
    ↓
┌─────────────────────────────────────────────────────────────┐
│  Tunnel 事件传播 (PreviewPointerPressed)                     │
│                                                              │
│  Window → StackPanel → Button                               │
│     ↓           ↓           ↓                               │
│  [处理?]    [处理?]      [处理?]                             │
│                                                              │
│  任一节点设置 Handled = true → 停止传播                      │
└─────────────────────────────────────────────────────────────┘
    ↓
┌─────────────────────────────────────────────────────────────┐
│  Bubble 事件传播 (PointerPressed)                            │
│                                                              │
│  Button → StackPanel → Window                               │
│     ↓           ↓           ↓                               │
│  [处理?]    [处理?]      [处理?]                             │
│                                                              │
│  任一节点设置 Handled = true → 停止传播                      │
└─────────────────────────────────────────────────────────────┘
```

### 2. EventRouter 实现

```csharp
namespace Eclipse.Input;

/// <summary>
/// 事件路由器
/// </summary>
internal static class EventRouter
{
    /// <summary>
    /// 路由事件
    /// </summary>
    public static void RaiseEvent(IInputElement source, RoutedEventArgs e)
    {
        var routedEvent = e.RoutedEvent;
        
        switch (routedEvent.RoutingStrategy)
        {
            case RoutingStrategy.Direct:
                // 直接事件 - 仅处理源元素
                InvokeHandlers(source, e);
                break;
                
            case RoutingStrategy.Bubble:
                // 冒泡 - 从源到根
                RouteAndInvoke(source, e, bubble: true);
                break;
                
            case RoutingStrategy.Tunnel:
                // 隧道 - 从根到源
                RouteAndInvoke(source, e, bubble: false);
                break;
        }
    }
    
    private static void RouteAndInvoke(IInputElement source, RoutedEventArgs e, bool bubble)
    {
        // 构建路由
        var route = BuildRoute(source);
        
        if (!bubble)
        {
            route.Reverse(); // Tunnel 从根开始
        }
        
        // 沿路由调用处理器
        foreach (var element in route)
        {
            if (e.Handled)
                break;
                
            e.Source = element;
            InvokeHandlers(element, e);
        }
    }
    
    private static List<IInputElement> BuildRoute(IInputElement source)
    {
        var route = new List<IInputElement>();
        var current = source;
        
        while (current != null)
        {
            route.Add(current);
            current = current.Parent;
        }
        
        return route;
    }
    
    private static void InvokeHandlers(IInputElement element, RoutedEventArgs e)
    {
        if (element is InputElement inputElement)
        {
            inputElement.InvokeHandlersInternal(e);
        }
    }
}
```

## 平台适配

### Windows 平台适配器

```csharp
namespace Eclipse.Windows.Input;

internal sealed class WindowsInputAdapter : IInputAdapter
{
    private readonly IntPtr _hwnd;
    private readonly InputManager _inputManager;
    
    public WindowsInputAdapter(IntPtr hwnd, InputManager inputManager)
    {
        _hwnd = hwnd;
        _inputManager = inputManager;
    }
    
    public void ProcessMessage(uint msg, IntPtr wParam, IntPtr lParam)
    {
        switch (msg)
        {
            case NativeMethods.WM_LBUTTONDOWN:
            case NativeMethods.WM_RBUTTONDOWN:
            case NativeMethods.WM_MBUTTONDOWN:
                OnPointerPressed(msg, wParam, lParam);
                break;
                
            case NativeMethods.WM_LBUTTONUP:
            case NativeMethods.WM_RBUTTONUP:
            case NativeMethods.WM_MBUTTONUP:
                OnPointerReleased(msg, wParam, lParam);
                break;
                
            case NativeMethods.WM_MOUSEMOVE:
                OnPointerMoved(wParam, lParam);
                break;
                
            case NativeMethods.WM_MOUSEWHEEL:
                OnPointerWheel(wParam, lParam);
                break;
                
            case NativeMethods.WM_TOUCH:
                OnTouch(wParam, lParam);
                break;
                
            case NativeMethods.WM_POINTERDOWN:
            case NativeMethods.WM_POINTERUP:
            case NativeMethods.WM_POINTERUPDATE:
                OnPointer(msg, wParam);
                break;
        }
    }
    
    private void OnPointerPressed(uint msg, IntPtr wParam, IntPtr lParam)
    {
        var x = NativeMethods.GET_X_LPARAM(lParam);
        var y = NativeMethods.GET_Y_LPARAM(lParam);
        var keyModifiers = GetKeyModifiers(wParam);
        var button = GetButtonFromMsg(msg);
        
        var pointer = Pointer.GetOrCreate(0, PointerType.Mouse);
        var point = new PointerPoint
        {
            Pointer = pointer,
            Position = new Point(x, y),
            Properties = new PointerPointProperties
            {
                IsLeftButtonPressed = button == PointerButtons.Left,
                IsRightButtonPressed = button == PointerButtons.Right,
                IsMiddleButtonPressed = button == PointerButtons.Middle
            }
        };
        
        _inputManager.ProcessPointerPressed(pointer, point, keyModifiers);
    }
    
    private void OnPointerMoved(IntPtr wParam, IntPtr lParam)
    {
        var x = NativeMethods.GET_X_LPARAM(lParam);
        var y = NativeMethods.GET_Y_LPARAM(lParam);
        
        var pointer = Pointer.GetOrCreate(0, PointerType.Mouse);
        var point = new PointerPoint
        {
            Pointer = pointer,
            Position = new Point(x, y),
            Properties = new PointerPointProperties
            {
                IsLeftButtonPressed = (NativeMethods.GetKeyState(NativeMethods.VK_LBUTTON) & 0x8000) != 0,
                IsRightButtonPressed = (NativeMethods.GetKeyState(NativeMethods.VK_RBUTTON) & 0x8000) != 0
            }
        };
        
        _inputManager.ProcessPointerMoved(pointer, point);
    }
    
    private void OnPointerReleased(uint msg, IntPtr wParam, IntPtr lParam)
    {
        var x = NativeMethods.GET_X_LPARAM(lParam);
        var y = NativeMethods.GET_Y_LPARAM(lParam);
        var button = GetButtonFromMsg(msg);
        
        var pointer = Pointer.GetOrCreate(0, PointerType.Mouse);
        var point = new PointerPoint
        {
            Pointer = pointer,
            Position = new Point(x, y)
        };
        
        _inputManager.ProcessPointerReleased(pointer, point, button);
    }
    
    private void OnPointerWheel(IntPtr wParam, IntPtr lParam)
    {
        var delta = NativeMethods.GET_WHEEL_DELTA_WPARAM(wParam);
        var keys = NativeMethods.GET_KEYSTATE_WPARAM(wParam);
        
        var x = NativeMethods.GET_X_LPARAM(lParam);
        var y = NativeMethods.GET_Y_LPARAM(lParam);
        
        var pointer = Pointer.GetOrCreate(0, PointerType.Mouse);
        
        _inputManager.ProcessPointerWheel(pointer, new Point(x, y), new Vector(0, delta / 120.0));
    }
    
    // Touch 和 Pen 处理...
}
```

### InputManager

```csharp
namespace Eclipse.Input;

/// <summary>
/// 输入管理器 - 核心输入处理
/// </summary>
public sealed class InputManager
{
    private readonly IHitTestService _hitTestService;
    private readonly IFocusManager _focusManager;
    
    // 当前指针状态
    private readonly Dictionary<int, PointerState> _pointerStates = new();
    
    // 鼠标悬停元素
    private IInputElement? _pointerOverElement;
    
    // 捕获状态
    private readonly Dictionary<int, IInputElement> _captures = new();
    
    public InputManager(IHitTestService hitTestService, IFocusManager focusManager)
    {
        _hitTestService = hitTestService;
        _focusManager = focusManager;
    }
    
    /// <summary>
    /// 处理指针按下
    /// </summary>
    public void ProcessPointerPressed(Pointer pointer, PointerPoint point, KeyModifiers modifiers)
    {
        var state = GetOrCreateState(pointer);
        state.PressPosition = point.Position;
        state.PressTimestamp = point.Timestamp;
        state.Point = point;
        
        // 检查捕获
        if (_captures.TryGetValue(pointer.Id, out var capturedElement))
        {
            // 有捕获，直接发送给捕获元素
            RaisePointerPressed(capturedElement, pointer, point, modifiers, 1);
        }
        else
        {
            // 命中测试
            var hitElement = _hitTestService.HitTest(point.Position);
            if (hitElement != null)
            {
                RaisePointerPressed(hitElement, pointer, point, modifiers, 1);
                
                // 自动捕获 (可选)
                if (ShouldCaptureOnPress(hitElement))
                {
                    CapturePointer(pointer, hitElement);
                }
            }
        }
    }
    
    /// <summary>
    /// 处理指针移动
    /// </summary>
    public void ProcessPointerMoved(Pointer pointer, PointerPoint point)
    {
        var state = GetOrCreateState(pointer);
        state.Point = point;
        
        // 处理捕获
        if (_captures.TryGetValue(pointer.Id, out var capturedElement))
        {
            RaisePointerMoved(capturedElement, pointer, point);
        }
        else
        {
            // 命中测试
            var hitElement = _hitTestService.HitTest(point.Position);
            
            // 悬停状态变化
            if (hitElement != _pointerOverElement)
            {
                if (_pointerOverElement != null)
                {
                    RaisePointerExited(_pointerOverElement, pointer, point);
                }
                
                _pointerOverElement = hitElement;
                
                if (hitElement != null)
                {
                    RaisePointerEntered(hitElement, pointer, point);
                }
            }
            
            // 移动事件
            if (hitElement != null)
            {
                RaisePointerMoved(hitElement, pointer, point);
            }
        }
    }
    
    /// <summary>
    /// 处理指针释放
    /// </summary>
    public void ProcessPointerReleased(Pointer pointer, PointerPoint point, PointerButtons button)
    {
        var state = GetOrCreateState(pointer);
        
        var targetElement = _captures.TryGetValue(pointer.Id, out var captured) 
            ? captured 
            : _hitTestService.HitTest(point.Position);
        
        if (targetElement != null)
        {
            RaisePointerReleased(targetElement, pointer, point, button);
            
            // 检测点击 (Tap)
            if (IsTap(state, point))
            {
                RaiseTapped(targetElement, pointer, point, 1);
            }
        }
        
        // 清除捕获
        ReleasePointerCapture(pointer);
        
        state.Reset();
    }
    
    /// <summary>
    /// 捕获指针
    /// </summary>
    public void CapturePointer(Pointer pointer, IInputElement element)
    {
        _captures[pointer.Id] = element;
        pointer.Capture(element);
    }
    
    /// <summary>
    /// 释放指针捕获
    /// </summary>
    public void ReleasePointerCapture(Pointer pointer)
    {
        if (_captures.Remove(pointer.Id, out var element))
        {
            pointer.Capture(null);
            RaisePointerCaptureLost(element, pointer);
        }
    }
    
    // === 辅助方法 ===
    
    private bool IsTap(PointerState state, PointerPoint point)
    {
        // 检查移动距离和时间
        const double maxTapDistance = 10;
        const ulong maxTapDuration = 500; // ms
        
        var distance = (point.Position - state.PressPosition).Length;
        var duration = point.Timestamp - state.PressTimestamp;
        
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
        public PointerPoint Point;
        public Point PressPosition;
        public ulong PressTimestamp;
        
        public void Reset()
        {
            PressPosition = default;
            PressTimestamp = 0;
        }
    }
}
```

## 手势系统 (可选扩展)

### 内置手势事件

```csharp
namespace Eclipse.Input;

public static class GestureEvents
{
    /// <summary>
    /// 点击
    /// </summary>
    public static readonly RoutedEvent<TappedEventArgs> TappedEvent =
        RoutedEvent<TappedEventArgs>.Register<InputElement>(nameof(Tapped), RoutingStrategy.Bubble);
    
    /// <summary>
    /// 双击
    /// </summary>
    public static readonly RoutedEvent<TappedEventArgs> DoubleTappedEvent =
        RoutedEvent<TappedEventArgs>.Register<InputElement>(nameof(DoubleTapped), RoutingStrategy.Bubble);
    
    /// <summary>
    /// 长按
    /// </summary>
    public static readonly RoutedEvent<HoldingEventArgs> HoldingEvent =
        RoutedEvent<HoldingEventArgs>.Register<InputElement>(nameof(Holding), RoutingStrategy.Bubble);
}

public class TappedEventArgs : RoutedEventArgs
{
    public PointerPoint Point { get; init; }
    public int TapCount { get; init; }
}

public class HoldingEventArgs : RoutedEventArgs
{
    public HoldingState State { get; init; }
    public PointerPoint Point { get; init; }
}

public enum HoldingState
{
    Started,   // 长按开始
    Completed, // 长按完成 (释放)
    Canceled   // 长按取消 (移动/多点)
}
```

### 手势识别器接口

```csharp
namespace Eclipse.Input.Gestures;

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
    
    protected virtual void OnPointerPressed(PointerPressedEventArgs e) { }
    protected virtual void OnPointerMoved(PointerEventArgs e) { }
    protected virtual void OnPointerReleased(PointerReleasedEventArgs e) { }
    protected virtual void OnPointerWheelChanged(PointerWheelEventArgs e) { }
    
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
}

/// <summary>
/// 双指缩放识别器
/// </summary>
public class PinchGestureRecognizer : GestureRecognizer
{
    private Pointer? _pointer1;
    private Pointer? _pointer2;
    private double _initialDistance;
    
    public event EventHandler<PinchEventArgs>? PinchStarted;
    public event EventHandler<PinchEventArgs>? PinchChanged;
    public event EventHandler<PinchEventArgs>? PinchEnded;
    
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (_pointer1 == null)
        {
            _pointer1 = e.Pointer;
        }
        else if (_pointer2 == null && e.Pointer != _pointer1)
        {
            _pointer2 = e.Pointer;
            IsActive = true;
            
            // 计算初始距离
            var p1 = e.GetCurrentPoint(Target).Position;
            var p2 = GetPointerPosition(_pointer1);
            _initialDistance = (p1 - p2).Length;
            
            CapturePointer(e.Pointer);
            
            PinchStarted?.Invoke(this, new PinchEventArgs { InitialScale = 1.0 });
        }
    }
    
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (!IsActive || _pointer1 == null || _pointer2 == null)
            return;
        
        if (e.Pointer != _pointer1 && e.Pointer != _pointer2)
            return;
        
        // 计算当前距离
        var p1 = GetPointerPosition(_pointer1);
        var p2 = GetPointerPosition(_pointer2);
        var currentDistance = (p1 - p2).Length;
        
        var scale = currentDistance / _initialDistance;
        
        PinchChanged?.Invoke(this, new PinchEventArgs { Scale = scale });
    }
    
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (e.Pointer == _pointer1 || e.Pointer == _pointer2)
        {
            IsActive = false;
            _pointer1 = null;
            _pointer2 = null;
            
            ReleasePointerCapture(e.Pointer);
            
            PinchEnded?.Invoke(this, new PinchEventArgs());
        }
    }
    
    private Point GetPointerPosition(Pointer? pointer)
    {
        // 需要从 InputManager 获取
        throw new NotImplementedException();
    }
}

public class PinchEventArgs : EventArgs
{
    public double InitialScale { get; init; }
    public double Scale { get; init; }
}
```

## 使用示例

### 1. 订阅指针事件

```csharp
public class MyButton : InputElement
{
    public MyButton()
    {
        // 订阅事件
        PointerPressed += OnPointerPressed;
        PointerReleased += OnPointerReleased;
        PointerEntered += OnPointerEntered;
        PointerExited += OnPointerExited;
    }
    
    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
        Console.WriteLine($"Pressed at {point.Position}");
        
        // 捕获指针 (拖拽时需要)
        e.Capture(this);
        
        // 标记已处理
        e.Handled = true;
    }
    
    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        // 释放捕获
        e.Pointer.Capture(null);
    }
    
    private void OnPointerEntered(object? sender, PointerEventArgs e)
    {
        // 鼠标进入
        VisualStateManager.GoToState(this, "Hover");
    }
    
    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        // 鼠标离开
        VisualStateManager.GoToState(this, "Normal");
    }
}
```

### 2. 处理 Tunnel 事件

```csharp
public class MyWindow : InputElement
{
    public MyWindow()
    {
        // 预览事件 - 在子元素处理前拦截
        PreviewPointerPressed += OnPreviewPointerPressed;
    }
    
    private void OnPreviewPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // 可以阻止子元素接收事件
        if (ShouldBlockInput())
        {
            e.Handled = true; // 子元素不会收到任何事件
        }
    }
}
```

### 3. 使用手势识别器

```csharp
public class ImageView : InputElement
{
    private readonly PinchGestureRecognizer _pinchRecognizer = new();
    private readonly ScrollGestureRecognizer _scrollRecognizer = new();
    
    public ImageView()
    {
        GestureRecognizers.Add(_pinchRecognizer);
        GestureRecognizers.Add(_scrollRecognizer);
        
        _pinchRecognizer.PinchChanged += OnPinch;
        _scrollRecognizer.ScrollChanged += OnScroll;
    }
    
    private void OnPinch(object? sender, PinchEventArgs e)
    {
        _scale *= e.Scale;
        InvalidateVisual();
    }
    
    public IList<GestureRecognizer> GestureRecognizers { get; } = new List<GestureRecognizer>();
}
```

### 4. 自定义手势识别器

```csharp
/// <summary>
/// 仅触摸的滑动手势
/// </summary>
public class TouchSwipeRecognizer : GestureRecognizer
{
    public event EventHandler<SwipeEventArgs>? Swiped;
    
    private Point _startPosition;
    private Pointer? _trackedPointer;
    
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        // 只处理触摸
        if (e.Pointer.Type != PointerType.Touch)
            return;
        
        _trackedPointer = e.Pointer;
        _startPosition = e.GetCurrentPoint(Target).Position;
        
        CapturePointer(e.Pointer);
        IsActive = true;
    }
    
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (e.Pointer != _trackedPointer)
            return;
        
        var endPosition = e.GetCurrentPoint(Target).Position;
        var delta = endPosition - _startPosition;
        
        // 检测滑动方向
        if (Math.Abs(delta.X) > 50 && Math.Abs(delta.Y) < 30)
        {
            var direction = delta.X > 0 ? SwipeDirection.Right : SwipeDirection.Left;
            Swiped?.Invoke(this, new SwipeEventArgs { Direction = direction });
        }
        
        ReleasePointerCapture(e.Pointer);
        _trackedPointer = null;
        IsActive = false;
    }
}
```

## 实现计划

### Phase 1: 核心输入 (基础)

| 任务 | 说明 | 优先级 |
|------|------|--------|
| Pointer 类型 | Pointer/PointerPoint/PointerPointProperties | P0 |
| 路由事件 | RoutedEvent/RoutedEventArgs/EventRouter | P0 |
| InputElement | 基础输入元素 | P0 |
| Hit Testing | Skia 边界检测 | P0 |
| Windows 适配 | WM_MOUSE*/WM_TOUCH | P0 |

### Phase 2: 高级输入

| 任务 | 说明 | 优先级 |
|------|------|--------|
| Pointer Capture | 指针捕获机制 | P1 |
| Focus Manager | 焦点管理 | P1 |
| Key Events | 键盘事件 | P1 |
| 内置手势 | Tapped/DoubleTapped/Holding | P1 |

### Phase 3: 扩展功能

| 任务 | 说明 | 优先级 |
|------|------|--------|
| 手势识别器 | GestureRecognizer 基础设施 | P2 |
| 内置识别器 | Pinch/Scroll/Pull | P2 |
| Pen 支持 | 压感/倾斜/橡皮擦 | P2 |
| 跨平台适配 | Linux/macOS | P2 |

## 参考

- [Avalonia Pointer Events](https://docs.avaloniaui.net/docs/input-interaction/pointer)
- [Avalonia Routed Events](https://docs.avaloniaui.net/docs/input-interaction/routed-events)
- [WPF Input Overview](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/input-overview)
- [Unicode TR#51 Emoji](https://unicode.org/reports/tr51/)