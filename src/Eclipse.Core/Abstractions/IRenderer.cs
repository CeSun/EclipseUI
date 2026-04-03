using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Eclipse.Core.Abstractions
{
    public interface IRenderer
    {
        IComponent? Root { get; }
        IDispatcher Dispatcher { get; }
        void Render(IComponent rootComponent);
        void RenderComponent(IComponent component);
        void RenderBatch(IEnumerable<IComponent> components);
        void Clear();
        event EventHandler<ComponentRenderedEventArgs>? ComponentRendered;
        event EventHandler<ComponentErrorEventArgs>? RenderError;
    }

    public sealed class ComponentRenderedEventArgs : EventArgs
    {
        public ComponentId ComponentId { get; }
        public TimeSpan RenderDuration { get; }
        public RenderType Type { get; }
        public ComponentRenderedEventArgs(ComponentId componentId, TimeSpan renderDuration, RenderType type)
        {
            ComponentId = componentId;
            RenderDuration = renderDuration;
            Type = type;
        }
    }

    public enum RenderType { Initial, Update, Remove }

    public sealed class ComponentErrorEventArgs : EventArgs
    {
        public ComponentId ComponentId { get; }
        public Exception Exception { get; }
        public ComponentErrorEventArgs(ComponentId componentId, Exception exception)
        {
            ComponentId = componentId;
            Exception = exception;
        }
    }

    public interface IDispatcher
    {
        bool IsDispatchRequired { get; }
        void Invoke(Action action);
        Task InvokeAsync(Action action);
        Task<T> InvokeAsync<T>(Func<T> func);
        Task Delay(TimeSpan delay);
    }
}
