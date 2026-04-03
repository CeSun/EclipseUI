using System;
using System.Collections.Generic;

namespace Eclipse.Core.Abstractions
{
    /// <summary>
    /// 组件基类接口
    /// </summary>
    public interface IComponent : IDisposable
    {
        ComponentId Id { get; }
        IComponent? Parent { get; set; }
        IReadOnlyList<IComponent> Children { get; }
        void Render(IRenderContext context);
        event EventHandler? StateChanged;
        void OnInitialized();
        void OnParametersSet();
        void OnMounted();
        void OnUnmounted();
    }

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

    public interface IComponent<TProps> : IComponent where TProps : class
    {
        void SetProps(TProps props);
        TProps GetProps();
    }

    }
