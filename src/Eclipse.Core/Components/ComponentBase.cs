using System;
using System.Collections.Generic;
using System.Threading;
using Eclipse.Core.Abstractions;

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
        
        protected void AddChild(IComponent child) { if (child == null) return; _children.Add(child); child.Parent = this; }
        protected void RemoveChild(IComponent child) { if (child == null) return; _children.Remove(child); child.Parent = null; }
        protected void ClearChildren() { foreach (var child in _children) { child.Parent = null; child.Dispose(); } _children.Clear(); }
        
        public abstract void Render(IBuildContext context);
        
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
