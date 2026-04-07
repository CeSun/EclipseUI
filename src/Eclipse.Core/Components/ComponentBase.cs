using System;
using System.Collections.Generic;
using System.Threading;
using Eclipse.Core.Abstractions;
using Eclipse.Input;
using Eclipse.Rendering;

namespace Eclipse.Core
{
    public abstract class ComponentBase : IComponent
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
        
        protected ComponentBase() => _id = Interlocked.Increment(ref _nextId);
        
        public ComponentId Id => new(_id);
        public IComponent? Parent => _parent;
        public IReadOnlyList<IComponent> Children => _children;
        public event EventHandler? StateChanged;
        
        /// <summary>
        /// 组件的边界矩形
        /// </summary>
        public Rect Bounds => _bounds;
        
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
        
        IComponent? IComponent.Parent { get => _parent; set => _parent = value; }
        
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
        
        public virtual void OnInitialized() { if (_isInitialized) return; _isInitialized = true; }
        public virtual void OnParametersSet() { }
        public virtual void OnMounted() { if (_isMounted) return; _isMounted = true; }
        public virtual void OnUnmounted() { _isMounted = false; }
        
        public void AddChild(IComponent child) { if (child == null) return; _children.Add(child); child.Parent = this; }
        public void RemoveChild(IComponent child) { if (child == null) return; _children.Remove(child); child.Parent = null; }
        public void ClearChildren() { foreach (var child in _children) { child.Parent = null; child.Dispose(); } _children.Clear(); }
        
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
        
        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            OnUnmounted();
            foreach (var child in _children) child.Dispose();
            _children.Clear();
            StateChanged = null;
        }
    }

    public abstract class ComponentBase<TProps> : ComponentBase, IComponent<TProps> where TProps : class
    {
        private TProps? _props;
        public void SetProps(TProps props) { var oldProps = _props; _props = props; OnPropsChanged(oldProps, props); OnParametersSet(); StateHasChanged(); }
        public TProps GetProps() => _props ?? throw new InvalidOperationException("Props not set");
        protected virtual void OnPropsChanged(TProps? oldProps, TProps newProps) { }
    }

    }