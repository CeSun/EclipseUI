using SkiaSharp;
using EclipseUI.Core;

namespace EclipseUI.Layout;

/// <summary>
/// DockPanel 布局元素 - 按停靠位置排列子元素
/// </summary>
public class DockPanelElement : EclipseElement
{
    /// <summary>
    /// 最后一个元素是否填充剩余空间
    /// </summary>
    public bool LastChildFill { get; set; } = true;
    
    public override SKSize Measure(SKCanvas canvas, float availableWidth, float availableHeight)
    {
        float usedLeft = 0, usedTop = 0, usedRight = 0, usedBottom = 0;
        float maxWidth = 0, maxHeight = 0;
        
        var dockChildren = GetDockChildren();
        int lastDockIndex = dockChildren.Count - 1;
        
        // 检查是否在无限空间中
        bool isWidthInfinite = float.IsPositiveInfinity(availableWidth);
        bool isHeightInfinite = float.IsPositiveInfinity(availableHeight);
        
        // 第一次遍历：测量所有子元素
        foreach (var child in dockChildren)
        {
            var childDock = child.Dock;
            int childIndex = dockChildren.IndexOf(child);
            
            // 最后一个元素且 LastChildFill=true 时，视为 Fill
            if (childIndex == lastDockIndex && LastChildFill)
            {
                childDock = Dock.Fill;
            }
            
            // 计算可用空间
            float childAvailWidth = isWidthInfinite ? float.PositiveInfinity : availableWidth - usedLeft - usedRight;
            float childAvailHeight = isHeightInfinite ? float.PositiveInfinity : availableHeight - usedTop - usedBottom;
            
            var size = child.Measure(canvas, childAvailWidth, childAvailHeight);
            
            // 累加占用的空间
            switch (childDock)
            {
                case Dock.Top:
                    usedTop += size.Height;
                    maxWidth = Math.Max(maxWidth, size.Width);
                    break;
                case Dock.Bottom:
                    usedBottom += size.Height;
                    maxWidth = Math.Max(maxWidth, size.Width);
                    break;
                case Dock.Left:
                    usedLeft += size.Width;
                    maxHeight = Math.Max(maxHeight, size.Height);
                    break;
                case Dock.Right:
                    usedRight += size.Width;
                    maxHeight = Math.Max(maxHeight, size.Height);
                    break;
                case Dock.Fill:
                    // Fill 元素：使用测量出的实际尺寸，而不是可用空间
                    maxWidth = Math.Max(maxWidth, size.Width);
                    maxHeight = Math.Max(maxHeight, size.Height);
                    break;
            }
        }
        
        // 计算总尺寸
        float totalWidth = usedLeft + usedRight + maxWidth;
        float totalHeight = usedTop + usedBottom + maxHeight;
        
        // 应用 RequestedWidth/Height
        if (RequestedWidth.HasValue)
            totalWidth = RequestedWidth.Value - PaddingLeft - PaddingRight;
        if (RequestedHeight.HasValue)
            totalHeight = RequestedHeight.Value - PaddingTop - PaddingBottom;
        
        return new SKSize(
            totalWidth + PaddingLeft + PaddingRight,
            totalHeight + PaddingTop + PaddingBottom
        );
    }
    
    public override void Arrange(SKCanvas canvas, float x, float y, float width, float height)
    {
        base.Arrange(canvas, x, y, width, height);
        
        float currentLeft = x + PaddingLeft;
        float currentTop = y + PaddingTop;
        float currentRight = x + width - PaddingRight;
        float currentBottom = y + height - PaddingBottom;
        
        var dockChildren = GetDockChildren();
        int lastDockIndex = dockChildren.Count - 1;
        
        foreach (var child in dockChildren)
        {
            var childDock = child.Dock;
            int childIndex = dockChildren.IndexOf(child);
            
            // 最后一个元素且 LastChildFill=true 时，视为 Fill
            if (childIndex == lastDockIndex && LastChildFill)
            {
                childDock = Dock.Fill;
            }
            
            float childWidth = currentRight - currentLeft;
            float childHeight = currentBottom - currentTop;
            
            switch (childDock)
            {
                case Dock.Top:
                    {
                        var size = child.Measure(canvas, childWidth, childHeight);
                        child.Arrange(canvas, currentLeft, currentTop, childWidth, size.Height);
                        currentTop += size.Height;
                        break;
                    }
                case Dock.Bottom:
                    {
                        var size = child.Measure(canvas, childWidth, childHeight);
                        child.Arrange(canvas, currentLeft, currentBottom - size.Height, childWidth, size.Height);
                        currentBottom -= size.Height;
                        break;
                    }
                case Dock.Left:
                    {
                        var size = child.Measure(canvas, childWidth, childHeight);
                        child.Arrange(canvas, currentLeft, currentTop, size.Width, childHeight);
                        currentLeft += size.Width;
                        break;
                    }
                case Dock.Right:
                    {
                        var size = child.Measure(canvas, childWidth, childHeight);
                        child.Arrange(canvas, currentRight - size.Width, currentTop, size.Width, childHeight);
                        currentRight -= size.Width;
                        break;
                    }
                case Dock.Fill:
                    {
                        child.Arrange(canvas, currentLeft, currentTop, childWidth, childHeight);
                        break;
                    }
            }
        }
    }
    
    /// <summary>
    /// 获取所有 DockPanelItem 子元素
    /// </summary>
    private List<DockPanelItemElement> GetDockChildren()
    {
        var result = new List<DockPanelItemElement>();
        foreach (var child in Children)
        {
            if (child is DockPanelItemElement dockItem)
            {
                result.Add(dockItem);
            }
        }
        return result;
    }
}
