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
        
        protected ComponentBase() => _id = Interlocked.Increment(ref _nextId);
        
        public ComponentId Id => new(_id);
        public IComponent? Parent => _parent;
        public IReadOnlyList<IComponent> Children => _children;
        public event EventHandler? StateChanged;
        
        IComponent? IComponent.Parent { get => _parent; set => _parent = value; }
        
        protected void StateHasChanged() => StateChanged?.Invoke(this, EventArgs.Empty);
        
        public virtual void OnInitialized() { if (_isInitialized) return; _isInitialized = true; }
        public virtual void OnParametersSet() { }
        public virtual void OnMounted() { if (_isMounted) return; _isMounted = true; }
        public virtual void OnUnmounted() { _isMounted = false; }
        
        public void AddChild(IComponent child) { if (child == null) return; _children.Add(child); child.Parent = this; }
        public void RemoveChild(IComponent child) { if (child == null) return; _children.Remove(child); child.Parent = null; }
        public void ClearChildren() { foreach (var child in _children) { child.Parent = null; child.Dispose(); } _children.Clear(); }
        
        /// <summary>
        /// 重建组件树 - 清除旧的子组件并重新调用 Build
        /// </summary>
        public void Rebuild()
        {
            ClearChildren();
            var context = new BuildContext(this);
            Build(context);
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
