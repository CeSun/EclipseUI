using System;
using System.Collections.Generic;

namespace Eclipse.Core.Abstractions
{
    /// <summary>
    /// 渲染上下文接口 - 由渲染器实现，用于组件渲染
    /// </summary>
    public interface IRenderContext
    {
        int Depth { get; }
        IReadOnlyList<ComponentId> ComponentPath { get; }
        
        /// <summary>
        /// 开始渲染一个组件，返回 scope 和组件实例用于强类型属性设置
        /// </summary>
        IDisposable BeginComponent<T>(ComponentId id, out T component) where T : IComponent, new();
        
        /// <summary>
        /// 设置子内容
        /// </summary>
        IDisposable BeginChildContent();
    }

    /// <summary>
    /// 渲染片段委托
    /// </summary>
    public delegate void RenderFragment(IRenderContext context);
    
    /// <summary>
    /// 带参数的渲染片段委托
    /// </summary>
    public delegate void RenderFragment<T>(IRenderContext context, T value);
}
