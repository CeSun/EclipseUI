using System;
using System.Collections.Generic;

namespace Eclipse.Core.Abstractions
{
    /// <summary>
    /// 构建上下文接口 - 用于构建组件树
    /// </summary>
    public interface IBuildContext
    {
        int Depth { get; }
        IReadOnlyList<ComponentId> ComponentPath { get; }
        
        /// <summary>
        /// 开始构建一个组件，返回 scope 和组件实例用于强类型属性设置
        /// </summary>
        IDisposable BeginComponent<T>(ComponentId id, out T component) where T : IComponent, new();
        
        /// <summary>
        /// 开始子内容区域
        /// </summary>
        IDisposable BeginChildContent();
    }

    /// <summary>
    /// 渲染片段委托
    /// </summary>
    public delegate void RenderFragment(IBuildContext context);
    
    /// <summary>
    /// 带参数的渲染片段委托
    /// </summary>
    public delegate void RenderFragment<T>(IBuildContext context, T value);
}