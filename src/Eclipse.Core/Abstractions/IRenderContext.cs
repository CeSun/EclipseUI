using System;
using System.Collections.Generic;

namespace Eclipse.Core.Abstractions
{
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
        
        /// <summary>
        /// 设置属性（运行时/反射场景，或手写组件使用）
        /// </summary>
        void SetAttribute(string name, object? value);
        
        /// <summary>
        /// 绑定事件
        /// </summary>
        void BindEvent<THandler>(string eventName, THandler handler) where THandler : Delegate;
        
        /// <summary>
        /// 双向绑定属性
        /// </summary>
        void BindProperty<T>(string propertyName, T currentValue, Action<T> valueChanged);
        
        void SetText(string? text);
        void RenderTemplate(RenderFragment? template);
        void RenderTemplate<T>(RenderFragment<T>? template, T value);
    }

    public delegate void RenderFragment(IRenderContext context);
    public delegate void RenderFragment<T>(IRenderContext context, T value);

    public interface IBindingContext<T>
    {
        T Value { get; }
        event EventHandler<ValueChangedEventArgs<T>>? ValueChanged;
        void SetValue(T value);
    }

    public sealed class ValueChangedEventArgs<T> : EventArgs
    {
        public T OldValue { get; }
        public T NewValue { get; }
        public ValueChangedEventArgs(T oldValue, T newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}
