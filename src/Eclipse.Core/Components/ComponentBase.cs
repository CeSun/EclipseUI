using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Eclipse.Core.Abstractions;
using Eclipse.Input;
using Eclipse.Rendering;

namespace Eclipse.Core
{
    /// <summary>
    /// 组件基类 - 定义组件的核心接口和默认实现
    /// </summary>
    public abstract class ComponentBase : IComponent, IInputElement
    {
        private static int _nextId = 0;
        private readonly int _id;
        private IComponent? _parent;
        private readonly List<IComponent> _children = new();
        private bool _isInitialized;
        private bool _isMounted;
        private bool _isDisposed;
        private bool _isDirty = true; // 脏标记，初始为 true 以确保首次渲染
        private Rect _bounds = new Rect(0, 0, 0, 0);
        protected Size _desiredSize = new Size(100, 40); // 默认期望尺寸

        protected ComponentBase() => _id = Interlocked.Increment(ref _nextId);

        /// <summary>
        /// 组件唯一标识
        /// </summary>
        public virtual ComponentId Id => new(_id);

        /// <summary>
        /// 父组件
        /// </summary>
        public virtual IComponent? Parent
        {
            get => _parent;
            set => _parent = value;
        }

        /// <summary>
        /// 子组件列表
        /// </summary>
        public IReadOnlyList<IComponent> Children => _children;

        /// <summary>
        /// 状态变化事件
        /// </summary>
        public event EventHandler? StateChanged;

        /// <summary>
        /// 组件的边界矩形
        /// </summary>
        public virtual Rect Bounds => _bounds;

        /// <summary>
        /// 更新组件边界（在 Render 时调用）
        /// </summary>
        public void UpdateBounds(Rect bounds) => _bounds = bounds;

        /// <summary>
        /// 是否需要重建（脏标记）
        /// </summary>
        public bool IsDirty => _isDirty;

        /// <summary>
        /// 标记组件为脏，需要重建
        /// </summary>
        public void MarkDirty()
        {
            _isDirty = true;
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 清除脏标记（重建完成后调用）
        /// </summary>
        public void ClearDirty()
        {
            _isDirty = false;
        }

        /// <summary>
        /// 触发状态改变并标记为脏
        /// </summary>
        protected void StateHasChanged()
        {
            _isDirty = true;
            StateChanged?.Invoke(this, EventArgs.Empty);

            // 事件冒泡到父元素
            if (_parent is ComponentBase parentComponent)
            {
                parentComponent.OnChildStateChanged(this);
            }
        }

        /// <summary>
        /// 子元素状态变化时的回调
        /// </summary>
        protected virtual void OnChildStateChanged(IComponent child)
        {
            // 触发自己的 StateChanged 事件（让 WindowImpl 能收到）
            StateChanged?.Invoke(this, EventArgs.Empty);

            // 继续冒泡到父元素
            if (_parent is ComponentBase parentComponent)
            {
                parentComponent.OnChildStateChanged(this);
            }
        }

        /// <summary>
        /// 组件初始化
        /// </summary>
        public virtual void OnInitialized() { if (_isInitialized) return; _isInitialized = true; }

        /// <summary>
        /// 参数设置
        /// </summary>
        public virtual void OnParametersSet() { }

        /// <summary>
        /// 组件挂载
        /// </summary>
        public virtual void OnMounted() { if (_isMounted) return; _isMounted = true; }

        /// <summary>
        /// 组件卸载
        /// </summary>
        public virtual void OnUnmounted() { _isMounted = false; }

        /// <summary>
        /// 添加子组件
        /// </summary>
        public void AddChild(IComponent child) { if (child == null) return; _children.Add(child); child.Parent = this; }

        /// <summary>
        /// 移除子组件
        /// </summary>
        public void RemoveChild(IComponent child) { if (child == null) return; _children.Remove(child); child.Parent = null; }

        /// <summary>
        /// 清空子组件
        /// </summary>
        public void ClearChildren() { foreach (var child in _children) { child.Parent = null; child.Dispose(); } _children.Clear(); }

        /// <summary>
        /// 测量组件所需尺寸 - 默认实现测量所有子元素，取最大宽度和高度
        /// </summary>
        public virtual Size Measure(Size availableSize, IDrawingContext context)
        {
            double maxWidth = 0;
            double maxHeight = 0;

            foreach (var child in _children)
            {
                var childSize = child.Measure(availableSize, context);
                maxWidth = Math.Max(maxWidth, childSize.Width);
                maxHeight = Math.Max(maxHeight, childSize.Height);
            }

            if (maxWidth > 0 || maxHeight > 0)
            {
                _desiredSize = new Size(maxWidth, maxHeight);
            }

            return _desiredSize;
        }

        /// <summary>
        /// 安排组件位置和尺寸 - 默认实现安排所有子元素
        /// </summary>
        public virtual void Arrange(Rect finalBounds, IDrawingContext context)
        {
            UpdateBounds(finalBounds);

            foreach (var child in _children)
            {
                child.Arrange(finalBounds, context);
            }
        }

        /// <summary>
        /// 重建组件树 - 仅在脏标记为 true 时才执行
        /// </summary>
        public void Rebuild()
        {
            if (!_isDirty)
                return; // 不是脏的，跳过重建

            ClearChildren();
            var context = new BuildContext(this);
            Build(context);

            // 清除脏标记
            ClearDirty();
        }

        /// <summary>
        /// 强制重建组件树（忽略脏标记）
        /// </summary>
        public void ForceRebuild()
        {
            ClearChildren();
            var context = new BuildContext(this);
            Build(context);
            ClearDirty();
        }

        /// <summary>
        /// 构建组件树
        /// </summary>
        public abstract void Build(IBuildContext context);

        /// <summary>
        /// 渲染组件 - 默认实现：渲染所有子组件
        /// </summary>
        public virtual void Render(IDrawingContext context, Rect bounds)
        {
            // 默认行为：渲染所有子组件
            foreach (var child in Children)
            {
                child.Render(context, bounds);
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            OnUnmounted();
            foreach (var child in _children) child.Dispose();
            _children.Clear();
            StateChanged = null;
        }

        // === 路由事件定义 ===

        public static readonly RoutedEvent<PointerPressedEventArgs> PointerPressedEvent =
            RoutedEvent<PointerPressedEventArgs>.Register<ComponentBase>(
                nameof(PointerPressed),
                RoutingStrategy.Bubble);

        public static readonly RoutedEvent<PointerPressedEventArgs> PreviewPointerPressedEvent =
            RoutedEvent<PointerPressedEventArgs>.Register<ComponentBase>(
                nameof(PreviewPointerPressed),
                RoutingStrategy.Tunnel);

        public static readonly RoutedEvent<PointerEventArgs> PointerMovedEvent =
            RoutedEvent<PointerEventArgs>.Register<ComponentBase>(
                nameof(PointerMoved),
                RoutingStrategy.Bubble);

        public static readonly RoutedEvent<PointerEventArgs> PreviewPointerMovedEvent =
            RoutedEvent<PointerEventArgs>.Register<ComponentBase>(
                nameof(PreviewPointerMoved),
                RoutingStrategy.Tunnel);

        public static readonly RoutedEvent<PointerReleasedEventArgs> PointerReleasedEvent =
            RoutedEvent<PointerReleasedEventArgs>.Register<ComponentBase>(
                nameof(PointerReleased),
                RoutingStrategy.Bubble);

        public static readonly RoutedEvent<PointerReleasedEventArgs> PreviewPointerReleasedEvent =
            RoutedEvent<PointerReleasedEventArgs>.Register<ComponentBase>(
                nameof(PreviewPointerReleased),
                RoutingStrategy.Tunnel);

        public static readonly RoutedEvent<PointerWheelEventArgs> PointerWheelChangedEvent =
            RoutedEvent<PointerWheelEventArgs>.Register<ComponentBase>(
                nameof(PointerWheelChanged),
                RoutingStrategy.Bubble);

        public static readonly RoutedEvent<PointerEventArgs> PointerEnteredEvent =
            RoutedEvent<PointerEventArgs>.Register<ComponentBase>(
                nameof(PointerEntered),
                RoutingStrategy.Direct);

        public static readonly RoutedEvent<PointerEventArgs> PointerExitedEvent =
            RoutedEvent<PointerEventArgs>.Register<ComponentBase>(
                nameof(PointerExited),
                RoutingStrategy.Direct);

        public static readonly RoutedEvent<TappedEventArgs> TappedEvent =
            RoutedEvent<TappedEventArgs>.Register<ComponentBase>(
                nameof(Tapped),
                RoutingStrategy.Bubble);

        // === 键盘事件 ===

        public static readonly RoutedEvent<KeyEventArgs> KeyDownEvent =
            RoutedEvent<KeyEventArgs>.Register<ComponentBase>(
                nameof(KeyDown),
                RoutingStrategy.Bubble);

        public static readonly RoutedEvent<KeyEventArgs> PreviewKeyDownEvent =
            RoutedEvent<KeyEventArgs>.Register<ComponentBase>(
                nameof(PreviewKeyDown),
                RoutingStrategy.Tunnel);

        public static readonly RoutedEvent<KeyEventArgs> KeyUpEvent =
            RoutedEvent<KeyEventArgs>.Register<ComponentBase>(
                nameof(KeyUp),
                RoutingStrategy.Bubble);

        public static readonly RoutedEvent<TextInputEventArgs> TextInputEvent =
            RoutedEvent<TextInputEventArgs>.Register<ComponentBase>(
                nameof(TextInput),
                RoutingStrategy.Bubble);

        // === IME 组合事件 ===

        public static readonly RoutedEvent<CompositionEventArgs> CompositionStartedEvent =
            RoutedEvent<CompositionEventArgs>.Register<ComponentBase>(
                nameof(CompositionStarted),
                RoutingStrategy.Bubble);

        public static readonly RoutedEvent<CompositionEventArgs> CompositionChangedEvent =
            RoutedEvent<CompositionEventArgs>.Register<ComponentBase>(
                nameof(CompositionChanged),
                RoutingStrategy.Bubble);

        public static readonly RoutedEvent<CompositionEventArgs> CompositionEndedEvent =
            RoutedEvent<CompositionEventArgs>.Register<ComponentBase>(
                nameof(CompositionEnded),
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

        public event EventHandler<TappedEventArgs> Tapped
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

        // === IME 组合事件 CLR 包装 ===

        public event EventHandler<CompositionEventArgs> CompositionStarted
        {
            add => AddHandler(CompositionStartedEvent, value);
            remove => RemoveHandler(CompositionStartedEvent, value);
        }

        public event EventHandler<CompositionEventArgs> CompositionChanged
        {
            add => AddHandler(CompositionChangedEvent, value);
            remove => RemoveHandler(CompositionChangedEvent, value);
        }

        public event EventHandler<CompositionEventArgs> CompositionEnded
        {
            add => AddHandler(CompositionEndedEvent, value);
            remove => RemoveHandler(CompositionEndedEvent, value);
        }

        // === IInputElement 属性 ===

        public virtual bool IsInputEnabled { get; set; } = true;
        public virtual bool IsHitTestVisible { get; set; } = true;
        public virtual bool IsFocusable { get; set; }
        public virtual bool IsFocused { get; protected set; }
        public abstract bool IsVisible { get; }

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

        private static IAppHost? _appHost;

        /// <summary>
        /// 设置应用宿主（由 App 启动时设置）
        /// </summary>
        public static void SetAppHost(IAppHost appHost) => _appHost = appHost;

        public virtual bool Focus()
        {
            if (!IsFocusable || !IsVisible)
                return false;

            // 通过 FocusManager 设置焦点（自动清除其他元素的焦点）
            var focusManager = _appHost?.Services.GetService<Eclipse.Input.FocusManager>();
            if (focusManager != null)
            {
                return focusManager.SetFocus(this);
            }

            // 降级方案：直接设置（不应该发生，除非没有初始化）
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
            if (_handlers.TryGetValue(e.RoutedEvent, out var list) && list != null)
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

    /// <summary>
    /// 带属性的组件基类
    /// </summary>
    public abstract class ComponentBase<TProps> : ComponentBase where TProps : class
    {
        private TProps? _props;

        /// <summary>
        /// 设置属性
        /// </summary>
        public void SetProps(TProps props) { var oldProps = _props; _props = props; OnPropsChanged(oldProps, props); OnParametersSet(); StateHasChanged(); }

        /// <summary>
        /// 获取属性
        /// </summary>
        public TProps GetProps() => _props ?? throw new InvalidOperationException("Props not set");

        /// <summary>
        /// 属性变化回调
        /// </summary>
        protected virtual void OnPropsChanged(TProps? oldProps, TProps newProps) { }
    }
}
