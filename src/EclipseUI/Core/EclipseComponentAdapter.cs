using Microsoft.AspNetCore.Components.RenderTree;
using SkiaSharp;

namespace EclipseUI.Core;

/// <summary>
/// Skia 组件适配�?- 连接 Blazor 渲染树和 Skia 元素�?/// </summary>
internal sealed class EclipseComponentAdapter
{
    public EclipseComponentAdapter(EclipseRenderer renderer)
    {
        Renderer = renderer;
    }
    
    /// <summary>
    /// 调试名称
    /// </summary>
    public string Name { get; set; } = "";
    
    /// <summary>
    /// 深度（用于排序）
    /// </summary>
    public int Depth { get; init; }
    
    /// <summary>
    /// 父适配�?    /// </summary>
    public EclipseComponentAdapter? Parent { get; private set; }
    
    /// <summary>
    /// 子适配器列�?    /// </summary>
    public List<EclipseComponentAdapter?> Children { get; } = new();
    
    /// <summary>
    /// 对应�?Skia 元素
    /// </summary>
    public EclipseElement? Element { get; set; }
    
    /// <summary>
    /// 渲染器引�?    /// </summary>
    public EclipseRenderer Renderer { get; }
    
    /// <summary>
    /// 待处理的编辑
    /// </summary>
    private List<PendingEdit>? _pendingEdits;
    
    /// <summary>
    /// 应用编辑
    /// </summary>
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
    
    /// <summary>
    /// 应用待处理的编辑
    /// </summary>
    public void ApplyPendingEdits()
    {
        if (_pendingEdits == null || _pendingEdits.Count == 0) return;
        
        foreach (var edit in _pendingEdits)
        {
            if (edit.Type == EditType.Add && edit.Element != null)
            {
                AddElement(edit.Element, edit.Index);
            }
            else if (edit.Type == EditType.Remove && edit.Element != null)
            {
                RemoveElement(edit.Element);
            }
        }
        
        _pendingEdits.Clear();
    }
    
    private void AddElement(EclipseElement element, int index)
    {
        if (Element != null)
        {
            if (index >= 0 && index < Element.Children.Count)
            {
                Element.Children.Insert(index, element);
            }
            else
            {
                Element.Children.Add(element);
            }
            element.Parent = Element;
        }
    }
    
    private void RemoveElement(EclipseElement element)
    {
        Element?.RemoveChild(element);
    }
    
    private void ApplyPrependFrame(
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
                var childAdapter = AddChildAdapter(siblingIndex, frame);
                
                if (childAdapter.Element != null)
                {
                    AddPendingAddition(childAdapter.Element, siblingIndex, adaptersWithPendingEdits);
                }
                break;
                
            case RenderTreeFrameType.Region:
                InsertFrameRange(batch, componentId, siblingIndex, frames, frameIndex + 1, frameIndex + frame.RegionSubtreeLength, adaptersWithPendingEdits);
                break;
                
            case RenderTreeFrameType.Text:
                // 文本�?- 暂时忽略
                Children.Insert(siblingIndex, null);
                break;
                
            case RenderTreeFrameType.Markup:
                // 标记�?- 暂时忽略
                Children.Insert(siblingIndex, null);
                break;
        }
    }
    
    private void ApplyRemoveFrame(int siblingIndex, HashSet<EclipseComponentAdapter> adaptersWithPendingEdits)
    {
        var childToRemove = Children[siblingIndex];
        if (childToRemove?.Element != null)
        {
            AddPendingRemoval(childToRemove.Element, siblingIndex, adaptersWithPendingEdits);
        }
        Children.RemoveAt(siblingIndex);
    }
    
    private void AddPendingAddition(EclipseElement element, int index, HashSet<EclipseComponentAdapter> adaptersWithPendingEdits)
    {
        _pendingEdits ??= new();
        _pendingEdits.Add(new PendingEdit(EditType.Add, index, element));
        adaptersWithPendingEdits.Add(this);
    }
    
    private void AddPendingRemoval(EclipseElement element, int index, HashSet<EclipseComponentAdapter> adaptersWithPendingEdits)
    {
        _pendingEdits ??= new();
        _pendingEdits.Add(new PendingEdit(EditType.Remove, index, element));
        adaptersWithPendingEdits.Add(this);
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
            ref var frame = ref frames[frameIndex];
            ApplyPrependFrame(batch, componentId, childIndex, frames, frameIndex, adaptersWithPendingEdits);
            childIndex++;
            frameIndex += CountDescendantFrames(frame);
        }
        return childIndex - origChildIndex;
    }
    
    private static int CountDescendantFrames(RenderTreeFrame frame)
    {
        return frame.FrameType switch
        {
            RenderTreeFrameType.Component => frame.ComponentSubtreeLength - 1,
            RenderTreeFrameType.Region => frame.RegionSubtreeLength - 1,
            _ => 0
        };
    }
    
    private EclipseComponentAdapter AddChildAdapter(int siblingIndex, RenderTreeFrame frame)
    {
        var childAdapter = new EclipseComponentAdapter(Renderer)
        {
            Parent = this,
            Name = frame.Component?.GetType().FullName ?? "Unknown",
            Depth = Depth + 1
        };
        
        Renderer.RegisterComponentAdapter(childAdapter, frame.ComponentId);
        
        if (frame.Component is IElementHandler handler)
        {
            var childElement = handler.Element;
            childAdapter.Element = childElement;
            
            // 将子元素添加到父元素的 Children
            if (Element != null && childElement != null)
            {
                Element.AddChild(childElement);
            }
        }
        
        Children.Insert(siblingIndex, childAdapter);
        return childAdapter;
    }
    
    public void Dispose()
    {
        Element?.ClearChildren();
    }
    
    record struct PendingEdit(EditType Type, int Index, EclipseElement Element);
    enum EditType { Add, Remove }
}
