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

        // 注意：先处理所有 Remove，再处理所有 Add，避免索引混乱
        // 第一步：收集所有需要移除和添加的元素
        var removes = new List<PendingEdit>();
        var adds = new List<PendingEdit>();
        
        for (var i = 0; i < _pendingEdits.Count; i++)
        {
            var edit = _pendingEdits[i];
            if (edit.Type == EditType.Remove)
                removes.Add(edit);
            else if (edit.Type == EditType.Add)
                adds.Add(edit);
        }

        // 第二步：先移除（从后往前，避免索引问题）
        for (var i = removes.Count - 1; i >= 0; i--)
        {
            var edit = removes[i];
            var parentElement = PhysicalTarget._targetElement.Element;
            var child = edit.Element._targetElement.Element;
            parentElement.RemoveChild(child);
        }

        // 第三步：再添加（从前往后，按索引插入）
        foreach (var edit in adds)
        {
            var parentElement = PhysicalTarget._targetElement.Element;
            var child = edit.Element._targetElement.Element;
            var index = edit.Index;
            
            if (index >= 0 && index < parentElement.Children.Count)
                parentElement.InsertChild(index, child);
            else
                parentElement.AddChild(child);
        }

        _pendingEdits.Clear();
    }

    private void AddPendingRemoval(EclipseComponentAdapter childToRemove, int index, HashSet<EclipseComponentAdapter> adaptersWithPendingEdits)
    {
        var targetEdits = PhysicalTarget._pendingEdits ??= new();
        adaptersWithPendingEdits.Add(PhysicalTarget);
        targetEdits.Add(new(EditType.Remove, index, childToRemove));
    }

    private void AddPendingAddition(EclipseComponentAdapter childToAdd, int index, HashSet<EclipseComponentAdapter> adaptersWithPendingEdits)
    {
        var targetEdits = PhysicalTarget._pendingEdits ??= new();
        targetEdits.Add(new(EditType.Add, index, childToAdd));
        adaptersWithPendingEdits.Add(PhysicalTarget);
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
            var index = PhysicalTarget.GetChildPhysicalIndex(childToRemove);
            PhysicalTarget.AddPendingRemoval(childToRemove, index, adaptersWithPendingEdits);
        }
        else if (childToRemove != null)
        {
            for (int i = 0; i < childToRemove.Children.Count; i++)
                childToRemove.ApplyRemoveFrame(i, adaptersWithPendingEdits);
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
