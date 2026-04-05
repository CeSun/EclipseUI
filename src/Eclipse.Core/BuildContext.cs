using System;
using System.Collections.Generic;
using Eclipse.Core.Abstractions;

namespace Eclipse.Core;

/// <summary>
/// 构建上下文实现
/// </summary>
public sealed class BuildContext : IBuildContext
{
    private readonly Stack<IComponent> _componentStack = new();
    private readonly List<ComponentId> _path = new();
    private IComponent? _rootComponent;
    
    public int Depth => _componentStack.Count;
    public IReadOnlyList<ComponentId> ComponentPath => _path;
    
    /// <summary>
    /// 根组件
    /// </summary>
    public IComponent? RootComponent => _rootComponent;
    
    public IDisposable BeginComponent<T>(ComponentId id, out T component) where T : IComponent, new()
    {
        component = new T();
        
        // 第一个组件是根组件
        if (_rootComponent == null)
        {
            _rootComponent = component;
        }
        
        // 设置父组件
        if (_componentStack.TryPeek(out var parent))
        {
            parent.AddChild(component);
        }
        
        _componentStack.Push(component);
        _path.Add(id);
        
        return new ComponentScope(this);
    }
    
    public IDisposable BeginChildContent()
    {
        return new ChildContentScope(this);
    }
    
    private void EndComponent()
    {
        if (_componentStack.Count > 0)
        {
            _componentStack.Pop();
            if (_path.Count > 0)
            {
                _path.RemoveAt(_path.Count - 1);
            }
        }
    }
    
    private sealed class ComponentScope : IDisposable
    {
        private readonly BuildContext _context;
        private bool _disposed;
        
        public ComponentScope(BuildContext context)
        {
            _context = context;
        }
        
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _context.EndComponent();
        }
    }
    
    private sealed class ChildContentScope : IDisposable
    {
        public ChildContentScope(BuildContext context) { }
        public void Dispose() { }
    }
}