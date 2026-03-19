using Microsoft.AspNetCore.Components.RenderTree;
using SkiaSharp;

namespace EclipseUI.Core;

/// <summary>
/// 通用组件适配器 - 处理所有组件类型
/// </summary>
internal sealed class EclipseComponentAdapter
{
    public EclipseComponentAdapter(EclipseRenderer renderer, EclipseComponentAdapter closestPhysicalParent, IElementHandler knownTargetElement = null)
    {
        Renderer = renderer;
        _closestPhysicalParent = closestPhysicalParent;
        _targetElement = knownTargetElement;
    }

    public string Name { get; set; } = "";
    public int DeepLevel { get; init; }
    public EclipseComponentAdapter Parent { get; private set; }
    public List<EclipseComponentAdapter> Children { get; } = new();

    private readonly EclipseComponentAdapter _closestPhysicalParent;
    private IElementHandler _targetElement;

    private EclipseComponentAdapter PhysicalTarget => _targetElement != null ? this : _closestPhysicalParent;

    public EclipseRenderer Renderer { get; }

    private List<PendingEdit> _pendingEdits;

    public void ApplyEdits(
        int componentId,
        ArrayBuilderSegment<RenderTreeEdit> edits,
        RenderBatch batch,
        HashSet<EclipseComponentAdapter> adaptersWithPendingEdits)
    {
        var referenceFrames = batch.ReferenceFrames.Array;

        foreach (var edit in edits)
        {
            switch (edit.Type)
            {
                case RenderTreeEditType.PrependFrame:
                    ApplyPrependFrame(batch, componentId, edit.SiblingIndex, referenceFrames, edit.ReferenceFrameIndex, adaptersWithPendingEdits);
                    break;
                case RenderTreeEditType.RemoveFrame:
                    ApplyRemoveFrame(edit.SiblingIndex, adaptersWithPendingEdits);
                    break;
                case RenderTreeEditType.UpdateText:
                    var textFrame = referenceFrames[edit.ReferenceFrameIndex];
                    if (!string.IsNullOrWhiteSpace(textFrame.TextContent))
                    {
                        // 文本内容处理（由支持文本的元素处理）
                    }
                    break;
                case RenderTreeEditType.UpdateMarkup:
                    var markupFrame = referenceFrames[edit.ReferenceFrameIndex];
                    if (!string.IsNullOrWhiteSpace(markupFrame.MarkupContent))
                    {
                        throw new NotImplementedException("Markup content not supported yet.");
                    }
                    break;
            }
        }
    }

    public void ApplyPendingEdits()
    {
        if (_pendingEdits == null)
            return;

        for (var i = 0; i < _pendingEdits.Count; i++)
        {
            var edit = _pendingEdits[i];
            var nextEdit = _pendingEdits.ElementAtOrDefault(i + 1);

            // 如果有连续的 Remove -> Add 操作且索引相同，优化为替换
            if (nextEdit.Index == edit.Index
                && edit.Type == EditType.Remove
                && nextEdit.Type == EditType.Add
                && edit.Element?._targetElement != null
                && nextEdit.Element?._targetElement != null)
            {
                var newChild = nextEdit.Element._targetElement.Element;
                var index = edit.Index;
                var parentChildren = _targetElement.Element.Children;
                
                // 按索引移除旧元素
                if (index >= 0 && index < parentChildren.Count)
                {
                    var removed = parentChildren[index];
                    removed.Parent = null;
                    parentChildren.RemoveAt(index);
                }
                // 在同位置插入新元素
                if (newChild != null)
                {
                    if (index >= 0 && index <= parentChildren.Count)
                        _targetElement.Element.InsertChild(index, newChild);
                    else
                        _targetElement.Element.AddChild(newChild);
                }
                
                i++; // 跳过下一个 Add 操作
            }
            else if (edit.Type == EditType.Remove)
            {
                var index = edit.Index;
                var parentChildren = _targetElement.Element.Children;
                if (index >= 0 && index < parentChildren.Count)
                {
                    var removed = parentChildren[index];
                    removed.Parent = null;
                    parentChildren.RemoveAt(index);
                }
            }
            else if (edit.Type == EditType.Add)
            {
                var child = edit.Element?._targetElement?.Element;
                if (child != null)
                {
                    var index = edit.Index;
                    if (index >= 0 && index <= _targetElement.Element.Children.Count)
                        _targetElement.Element.InsertChild(index, child);
                    else
                        _targetElement.Element.AddChild(child);
                }
            }
        }

        _pendingEdits.Clear();
    }

    private void AddPendingRemoval(EclipseComponentAdapter childToRemove, int index, HashSet<EclipseComponentAdapter> adaptersWithPendingEdits)
    {
        var targetEdits = PhysicalTarget._pendingEdits ??= new();
        adaptersWithPendingEdits.Add(PhysicalTarget);

        if (targetEdits.Count == 0)
        {
            targetEdits.Add(new(EditType.Remove, index, childToRemove));
            return;
        }

        // 参考 Blazonia：如果有 Add 和 Remove 操作，把 Remove 放在对应 Add 之前以便优化为替换
        int i;
        for (i = targetEdits.Count; i > 0; i--)
        {
            var previousEdit = targetEdits[i - 1];

            if (previousEdit.Type == EditType.Remove)
                break;

            if (previousEdit.Index < index - 1)
                break;

            if (i >= 2
                && previousEdit.Type == EditType.Add
                && targetEdits[i - 2] is { Type: EditType.Remove } previousRemoval
                && previousRemoval.Index == previousEdit.Index)
            {
                break;
            }

            if (previousEdit.Index <= index)
                index--;

            if (previousEdit.Index > index)
                targetEdits[i - 1] = previousEdit with { Index = previousEdit.Index - 1 };
        }

        targetEdits.Insert(i, new(EditType.Remove, index, childToRemove));
    }

    private void AddPendingAddition(EclipseComponentAdapter childToAdd, int index, HashSet<EclipseComponentAdapter> adaptersWithPendingEdits)
    {
        var targetEdits = PhysicalTarget._pendingEdits ??= new();
        targetEdits.Add(new(EditType.Add, index, childToAdd));
        adaptersWithPendingEdits.Add(PhysicalTarget);
    }

    private int GetChildPhysicalIndex(EclipseComponentAdapter childAdapter)
    {
        var index = 0;
        return FindChildPhysicalIndexRecursive(this, childAdapter, ref index) ? index : -1;

        static bool FindChildPhysicalIndexRecursive(EclipseComponentAdapter parent, EclipseComponentAdapter targetChild, ref int index)
        {
            foreach (var child in parent.Children)
            {
                if (child is null)
                    continue;

                if (child == targetChild)
                    return true;

                if (child._targetElement != null)
                {
                    index++;
                }

                if (child._targetElement == null)
                {
                    if (FindChildPhysicalIndexRecursive(child, targetChild, ref index))
                        return true;
                }
            }

            return false;
        }
    }

    private void ApplyRemoveFrame(int siblingIndex, HashSet<EclipseComponentAdapter> adaptersWithPendingEdits)
    {
        var childToRemove = Children[siblingIndex];
        RemoveChildElementAndDescendants(childToRemove, adaptersWithPendingEdits);
        Children.RemoveAt(siblingIndex);
    }

    private void RemoveChildElementAndDescendants(EclipseComponentAdapter childToRemove, HashSet<EclipseComponentAdapter> adaptersWithPendingEdits)
    {
        if (childToRemove?._targetElement != null)
        {
            // 计算物理索引并添加到 pending edits
            var index = PhysicalTarget.GetChildPhysicalIndex(childToRemove);
            PhysicalTarget.AddPendingRemoval(childToRemove, index, adaptersWithPendingEdits);
        }
        else if (childToRemove != null)
        {
            // 当前组件没有元素，递归处理子组件
            for (int i = 0; i < childToRemove.Children.Count; i++)
            {
                childToRemove.ApplyRemoveFrame(i, adaptersWithPendingEdits);
            }
        }
    }

    private int ApplyPrependFrame(
        RenderBatch batch,
        int componentId,
        int siblingIndex,
        RenderTreeFrame[] frames,
        int frameIndex,
        HashSet<EclipseComponentAdapter> adaptersWithPendingEdits)
    {
        ref var frame = ref frames[frameIndex];
        switch (frame.FrameType)
        {
            case RenderTreeFrameType.Component:
                {
                    var childAdapter = AddChildAdapter(siblingIndex, frame);
                    if (childAdapter._targetElement is not null)
                        AddElementAsChildElement(childAdapter, adaptersWithPendingEdits);
                    return 1;
                }
            case RenderTreeFrameType.Region:
                {
                    return InsertFrameRange(batch, componentId, siblingIndex, frames, frameIndex + 1, frameIndex + frame.RegionSubtreeLength, adaptersWithPendingEdits);
                }
            case RenderTreeFrameType.Text:
                {
                    Children.Insert(siblingIndex, null);
                    return 1;
                }
            case RenderTreeFrameType.Markup:
                {
                    Children.Insert(siblingIndex, null);
                    return 1;
                }
            default:
                throw new NotImplementedException($"Not supported frame type: {frame.FrameType}");
        }
    }

    private void AddElementAsChildElement(EclipseComponentAdapter childAdapter, HashSet<EclipseComponentAdapter> adaptersWithPendingEdits)
    {
        if (childAdapter is null)
            return;

        var elementIndex = PhysicalTarget.GetChildPhysicalIndex(childAdapter);
        AddPendingAddition(childAdapter, elementIndex, adaptersWithPendingEdits);
    }

    private int InsertFrameRange(
        RenderBatch batch,
        int componentId,
        int childIndex,
        RenderTreeFrame[] frames,
        int startIndex,
        int endIndexExcl,
        HashSet<EclipseComponentAdapter> adaptersWithPendingEdits)
    {
        var origChildIndex = childIndex;
        for (var frameIndex = startIndex; frameIndex < endIndexExcl; frameIndex++)
        {
            ref var frame = ref batch.ReferenceFrames.Array[frameIndex];
            var numChildrenInserted = ApplyPrependFrame(batch, componentId, childIndex, frames, frameIndex, adaptersWithPendingEdits);
            childIndex += numChildrenInserted;
            frameIndex += CountDescendantFrames(frame);
        }

        return (childIndex - origChildIndex);
    }

    private static int CountDescendantFrames(RenderTreeFrame frame)
    {
        return frame.FrameType switch
        {
            RenderTreeFrameType.Component => frame.ComponentSubtreeLength - 1,
            RenderTreeFrameType.Region => frame.RegionSubtreeLength - 1,
            _ => 0,
        };
    }

    private EclipseComponentAdapter AddChildAdapter(int siblingIndex, RenderTreeFrame frame)
    {
        var name = frame.FrameType is RenderTreeFrameType.Component
            ? $"For: '{frame.Component.GetType().FullName}'"
            : $"{frame.FrameType}, sib#={siblingIndex}";

        var childAdapter = new EclipseComponentAdapter(Renderer, PhysicalTarget)
        {
            Parent = this,
            Name = name,
            DeepLevel = DeepLevel + 1
        };

        if (frame.FrameType is RenderTreeFrameType.Component)
        {
            Renderer.RegisterComponentAdapter(childAdapter, frame.ComponentId);

            if (frame.Component is IElementHandler targetHandler)
            {
                childAdapter._targetElement = targetHandler;
            }
        }

        Children.Insert(siblingIndex, childAdapter);
        return childAdapter;
    }

    public void Dispose()
    {
        if (_targetElement is IDisposable disposableTargetElement)
        {
            disposableTargetElement.Dispose();
        }
    }

    record struct PendingEdit(EditType Type, int Index, EclipseComponentAdapter Element);
    enum EditType { Add, Remove }
}
