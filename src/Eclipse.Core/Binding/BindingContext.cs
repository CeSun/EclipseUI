using System;
using System.Collections.Generic;
using Eclipse.Core.Abstractions;

namespace Eclipse.Core
{
    public sealed class BindingContext<T> : IBindingContext<T>, IDisposable
    {
        private T _value;
        private readonly Action<T>? _onChanged;
        private bool _isDisposed;
        
        public BindingContext(T initialValue, Action<T>? onChanged = null)
        {
            _value = initialValue;
            _onChanged = onChanged;
        }
        
        public T Value => _value;
        public event EventHandler<ValueChangedEventArgs<T>>? ValueChanged;
        
        public void SetValue(T value)
        {
            if (EqualityComparer<T>.Default.Equals(_value, value)) return;
            var oldValue = _value;
            _value = value;
            _onChanged?.Invoke(value);
            ValueChanged?.Invoke(this, new ValueChangedEventArgs<T>(oldValue, value));
        }
        
        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            ValueChanged = null;
        }
    }

    public sealed class BindingManager : IBindingManager
    {
        public IBindingExpression CreateBinding(object source, string path, BindingDescriptor? descriptor = null)
            => new SimpleBindingExpression(source, path, descriptor ?? new BindingDescriptor());
        
        public IBindingExpression<T> CreateBinding<T>(object source, string path, BindingDescriptor? descriptor = null)
            => new SimpleBindingExpression<T>(source, path, descriptor ?? new BindingDescriptor());
        
        public IBindingContext<T> Bind<T>(Func<T> getter, Action<T> setter, Action? onChanged = null)
            => new BindingContext<T>(getter(), v => { setter(v); onChanged?.Invoke(); });
        
        public IBindingContext<T> Computed<T>(Func<T> compute, params IBindingExpression[] dependencies)
            => throw new NotImplementedException();
    }

    internal sealed class SimpleBindingExpression : IBindingExpression
    {
        public SimpleBindingExpression(object source, string path, BindingDescriptor descriptor)
        {
            Source = source; Path = path;
        }
        public string Path { get; }
        public object? Source { get; }
        public object? Value => throw new NotImplementedException();
        public bool IsReadOnly => true;
        public event EventHandler? ValueChanged;
        public void UpdateValue(object? value) => throw new NotImplementedException();
    }

    internal sealed class SimpleBindingExpression<T> : IBindingExpression<T>
    {
        public SimpleBindingExpression(object source, string path, BindingDescriptor descriptor)
        {
            Source = source; Path = path;
        }
        public string Path { get; }
        public object? Source { get; }
        object? IBindingExpression.Value => Value;
        public T Value => throw new NotImplementedException();
        public bool IsReadOnly => true;
        
        private EventHandler? _valueChanged;
        event EventHandler? IBindingExpression.ValueChanged
        {
            add => _valueChanged += value;
            remove => _valueChanged -= value;
        }
        
        public event EventHandler<ValueChangedEventArgs<T>>? ValueChanged;
        public void UpdateValue(object? value) => UpdateValue((T)value!);
        public void UpdateValue(T value) => throw new NotImplementedException();
    }
}
