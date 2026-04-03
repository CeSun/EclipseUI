using System;

namespace Eclipse.Core.Abstractions
{
    public interface IBindingExpression
    {
        string Path { get; }
        object? Source { get; }
        object? Value { get; }
        bool IsReadOnly { get; }
        event EventHandler? ValueChanged;
        void UpdateValue(object? value);
    }

    public interface IBindingExpression<T> : IBindingExpression
    {
        new T Value { get; }
        new event EventHandler<ValueChangedEventArgs<T>>? ValueChanged;
        void UpdateValue(T value);
    }

    public enum BindingMode { OneWay, TwoWay, OneWayToSource, OneTime }
    public enum UpdateSourceTrigger { PropertyChanged, LostFocus, Explicit }

    public sealed class BindingDescriptor
    {
        public string Path { get; set; } = string.Empty;
        public BindingMode Mode { get; set; } = BindingMode.OneWay;
        public UpdateSourceTrigger UpdateTrigger { get; set; } = UpdateSourceTrigger.PropertyChanged;
        public string? StringFormat { get; set; }
        public object? Converter { get; set; }
        public object? FallbackValue { get; set; }
        public object? TargetNullValue { get; set; }
        public int Delay { get; set; }
    }

    public interface IBindingManager
    {
        IBindingExpression CreateBinding(object source, string path, BindingDescriptor? descriptor = null);
        IBindingExpression<T> CreateBinding<T>(object source, string path, BindingDescriptor? descriptor = null);
        IBindingContext<T> Bind<T>(Func<T> getter, Action<T> setter, Action? onChanged = null);
        IBindingContext<T> Computed<T>(Func<T> compute, params IBindingExpression[] dependencies);
    }
}
