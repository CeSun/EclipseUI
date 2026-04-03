using System;
using System.Collections.Generic;

namespace Eclipse.Core.Abstractions
{
    public interface IRenderContext
    {
        int Depth { get; }
        IReadOnlyList<ComponentId> ComponentPath { get; }
        IDisposable BeginComponent<T>(ComponentId id) where T : IComponent;
        void EndComponent();
        void SetAttribute(string name, object? value);
        void SetAttributes(IReadOnlyDictionary<string, object?> attributes);
        void RemoveAttribute(string name);
        void BindEvent(string eventName, Delegate handler);
        void UnbindEvent(string eventName);
        void BindProperty<T>(string propertyName, T currentValue, Action<T> valueChanged);
        void BindProperty<T>(string propertyName, IBindingContext<T> binding);
        void SetText(string? text);
        IDisposable BeginChildContent();
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
