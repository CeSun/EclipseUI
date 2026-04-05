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
    private readonly IComponent _root;
    
    public int Depth => _componentStack.Count;
    public IReadOnlyList<ComponentId> ComponentPath => _path;
    
    /// <summary>
    /// 根组件
    /// </summary>
    public IComponent? RootComponent => _root;
    
    /// <summary>
    /// 创建构建上下文
    /// </summary>
    /// <param name="root">调用 Build 的根组件</param>
    public BuildContext(IComponent? root = null)
    {
        _root = root!;
    }
    
    public IDisposable BeginComponent<T>(ComponentId id, out T component) where T : IComponent, new()
    {
        component = new T();
        
        // 如果栈是空的，添加到根组件
        if (_componentStack.Count == 0 && _root != null)
        {
            _root.AddChild(component);
        }
        // 否则添加到栈顶组件
        else if (_componentStack.TryPeek(out var parent))
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