using System.Collections.Generic;

namespace Eclipse.Core.Abstractions
{
    public interface IRenderTreeDiff
    {
        ComponentId ComponentId { get; }
        DiffType Type { get; }
        IReadOnlyList<AttributeChange> AttributeChanges { get; }
        IReadOnlyList<ChildComponentChange> ChildChanges { get; }
    }

    public enum DiffType { Add, Update, Remove, Move }

    public readonly struct AttributeChange
    {
        public string Name { get; }
        public object? OldValue { get; }
        public object? NewValue { get; }
        public AttributeChangeType Type { get; }
        public AttributeChange(string name, object? oldValue, object? newValue, AttributeChangeType type)
        {
            Name = name; OldValue = oldValue; NewValue = newValue; Type = type;
        }
        public static AttributeChange Added(string name, object? value) => new(name, null, value, AttributeChangeType.Added);
        public static AttributeChange Updated(string name, object? oldValue, object? newValue) => new(name, oldValue, newValue, AttributeChangeType.Updated);
        public static AttributeChange Removed(string name, object? oldValue) => new(name, oldValue, null, AttributeChangeType.Removed);
    }

    public enum AttributeChangeType { Added, Updated, Removed }

    public readonly struct ChildComponentChange
    {
        public int OldIndex { get; }
        public int NewIndex { get; }
        public ComponentId ComponentId { get; }
        public DiffType Type { get; }
        public ChildComponentChange(int oldIndex, int newIndex, ComponentId componentId, DiffType type)
        {
            OldIndex = oldIndex; NewIndex = newIndex; ComponentId = componentId; Type = type;
        }
    }

    public interface IRenderTreeDiffer
    {
        IEnumerable<IRenderTreeDiff> Diff(IRenderTree oldTree, IRenderTree newTree);
        IRenderTreeDiff DiffComponent(IComponent oldComponent, IComponent newComponent);
    }

    public interface IRenderTree
    {
        IComponent? Root { get; }
        IComponent? GetComponent(ComponentId id);
        RenderFrame GetFrame(ComponentId id);
        int ComponentCount { get; }
    }

    public readonly struct RenderFrame
    {
        public ComponentId ComponentId { get; }
        public string ComponentTypeName { get; }
        public IReadOnlyDictionary<string, object?> Attributes { get; }
        public IReadOnlyList<ComponentId> Children { get; }
        public int Sequence { get; }
        public RenderFrame(ComponentId componentId, string componentTypeName, IReadOnlyDictionary<string, object?> attributes, IReadOnlyList<ComponentId> children, int sequence)
        {
            ComponentId = componentId; ComponentTypeName = componentTypeName; Attributes = attributes; Children = children; Sequence = sequence;
        }
    }
}
