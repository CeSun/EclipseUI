using System;
using System.Collections.Generic;
using Eclipse.Input;
using Eclipse.Rendering;

namespace Eclipse.Core.Abstractions
{
    /// <summary>
    /// 组件接口类型标记 - 实际功能由 ComponentBase 提供
    /// </summary>
    /// <remarks>
    /// 此接口仅用于类型引用和泛型约束，所有功能实现都在 ComponentBase 中。
    /// 这样设计是为了保持引用的灵活性，同时避免接口和基类的重复定义。
    /// </remarks>
    public interface IComponent : IDisposable
    {
        ComponentId Id { get; }
        IComponent? Parent { get; set; }
        IReadOnlyList<IComponent> Children { get; }
        void Build(IBuildContext context);
        event EventHandler? StateChanged;
        void OnInitialized();
        void OnParametersSet();
        void OnMounted();
        void OnUnmounted();
        void Render(IDrawingContext context, Rect bounds);
        void AddChild(IComponent child);
        void RemoveChild(IComponent child);
        Size Measure(Size availableSize, IDrawingContext context);
        void Arrange(Rect finalBounds, IDrawingContext context);
    }

    /// <summary>
    /// 组件ID
    /// </summary>
    public readonly struct ComponentId : IEquatable<ComponentId>
    {
        private readonly int _value;
        public ComponentId(int value) => _value = value;
        public static implicit operator int(ComponentId id) => id._value;
        public static explicit operator ComponentId(int value) => new(value);
        public bool Equals(ComponentId other) => _value == other._value;
        public override bool Equals(object? obj) => obj is ComponentId id && Equals(id);
        public override int GetHashCode() => _value;
        public override string ToString() => _value.ToString();
        public static bool operator ==(ComponentId left, ComponentId right) => left.Equals(right);
        public static bool operator !=(ComponentId left, ComponentId right) => !left.Equals(right);
    }

    /// <summary>
    /// 带属性的组件接口
    /// </summary>
    public interface IComponent<TProps> : IComponent where TProps : class
    {
        void SetProps(TProps props);
        TProps GetProps();
    }
}
