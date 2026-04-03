using System;
using System.Collections.Generic;
using Eclipse.Core.Abstractions;

namespace Eclipse.Core
{
    /// <summary>
    /// 构建上下文实现 - 用于构建组件树
    /// </summary>
    public class BuildContext : IBuildContext
    {
        private readonly Stack<BuildFrame> _frames = new();
        private readonly List<IComponent> _rootComponents = new();
        
        public int Depth => _frames.Count;
        
        public IReadOnlyList<ComponentId> ComponentPath => 
            _frames.Count > 0 ? _frames.Peek().Path : Array.Empty<ComponentId>();
        
        /// <summary>
        /// 获取构建完成后的根组件列表
        /// </summary>
        public IReadOnlyList<IComponent> RootComponents => _rootComponents;
        
        /// <summary>
        /// 获取单个根组件（如果有且只有一个）
        /// </summary>
        public IComponent? RootComponent => 
            _rootComponents.Count == 1 ? _rootComponents[0] : null;
        
        public IDisposable BeginComponent<T>(ComponentId id, out T component) where T : IComponent, new()
        {
            // 创建组件实例
            component = new T();
            component.OnInitialized();
            
            // 构建当前帧的路径
            var path = BuildPath(id);
            
            // 如果有父组件，建立父子关系
            if (_frames.Count > 0)
            {
                var parentFrame = _frames.Peek();
                if (parentFrame.IsInChildContent)
                {
                    parentFrame.Component.AddChild(component);
                }
            }
            else
            {
                // 没有父组件，是根组件
                _rootComponents.Add(component);
            }
            
            // 创建新帧并 push
            var frame = new BuildFrame(id, component, path);
            _frames.Push(frame);
            
            // 返回 scope，Dispose 时 pop 并完成构建
            return new ComponentScope(this, component);
        }
        
        public IDisposable BeginChildContent()
        {
            if (_frames.Count == 0)
                throw new InvalidOperationException("BeginChildContent must be called within a component scope");
            
            var frame = _frames.Peek();
            frame.BeginChildContent();
            
            return new ChildContentScope(frame);
        }
        
        private List<ComponentId> BuildPath(ComponentId newId)
        {
            var path = new List<ComponentId>();
            if (_frames.Count > 0)
            {
                path.AddRange(_frames.Peek().Path);
            }
            path.Add(newId);
            return path;
        }
        
        private void EndComponent(IComponent component)
        {
            if (_frames.Count == 0)
                throw new InvalidOperationException("No component to end");
            
            var frame = _frames.Pop();
            if (frame.Component != component)
                throw new InvalidOperationException("Mismatched component end");
            
            // 组件构建完成，触发生命周期
            // 注意：不调用 Render，因为生成代码已经构建好了组件树
            component.OnParametersSet();
            component.OnMounted();
        }
        
        /// <summary>
        /// 构建帧 - 跟踪当前构建状态
        /// </summary>
        private class BuildFrame
        {
            public ComponentId Id { get; }
            public IComponent Component { get; }
            public List<ComponentId> Path { get; }
            public bool IsInChildContent { get; private set; }
            
            public BuildFrame(ComponentId id, IComponent component, List<ComponentId> path)
            {
                Id = id;
                Component = component;
                Path = path;
                IsInChildContent = false;
            }
            
            public void BeginChildContent()
            {
                IsInChildContent = true;
            }
            
            public void EndChildContent()
            {
                IsInChildContent = false;
            }
        }
        
        /// <summary>
        /// 组件 scope - Dispose 时结束组件构建
        /// </summary>
        private class ComponentScope : IDisposable
        {
            private readonly BuildContext _context;
            private readonly IComponent _component;
            private bool _disposed;
            
            public ComponentScope(BuildContext context, IComponent component)
            {
                _context = context;
                _component = component;
            }
            
            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _context.EndComponent(_component);
            }
        }
        
        /// <summary>
        /// 子内容 scope - Dispose 时结束子内容区域
        /// </summary>
        private class ChildContentScope : IDisposable
        {
            private readonly BuildFrame _frame;
            private bool _disposed;
            
            public ChildContentScope(BuildFrame frame)
            {
                _frame = frame;
            }
            
            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _frame.EndChildContent();
            }
        }
    }
}