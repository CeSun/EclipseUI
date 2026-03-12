using SkiaSharp;
using EclipseUI.Core;

namespace EclipseUI.Layout;

public enum StackOrientation { Horizontal, Vertical }

/// <summary>
/// 堆叠面板元素
/// </summary>
public class StackPanelElement : EclipseElement
{
    public StackOrientation Orientation { get; set; } = StackOrientation.Vertical;
    public float Spacing { get; set; }
    
    public override SKSize Measure(SKCanvas canvas, float availableWidth, float availableHeight)
    {
        float totalWidth = 0, totalHeight = 0, maxWidth = 0, maxHeight = 0;
        
        foreach (var child in Children)
        {
            var size = child.Measure(canvas, availableWidth, availableHeight);
            if (Orientation == StackOrientation.Vertical)
            {
                totalHeight += size.Height + Spacing;
                maxWidth = Math.Max(maxWidth, size.Width);
            }
            else
            {
                totalWidth += size.Width + Spacing;
                maxHeight = Math.Max(maxHeight, size.Height);
            }
        }
        
        if (Children.Count > 0)
        {
            if (Orientation == StackOrientation.Vertical) totalHeight -= Spacing;
            else totalWidth -= Spacing;
        }
        
        return new SKSize(
            (Orientation == StackOrientation.Vertical ? maxWidth : totalWidth) + PaddingLeft + PaddingRight,
            (Orientation == StackOrientation.Vertical ? totalHeight : maxHeight) + PaddingTop + PaddingBottom
        );
    }
    
    public override void Arrange(SKCanvas canvas, float x, float y, float width, float height)
    {
        base.Arrange(canvas, x, y, width, height);
        
        float currentX = x + PaddingLeft;
        float currentY = y + PaddingTop;
        float contentWidth = width - PaddingLeft - PaddingRight;
        float contentHeight = height - PaddingTop - PaddingBottom;
        
        foreach (var child in Children)
        {
            var size = child.Measure(canvas, contentWidth, contentHeight);
            if (Orientation == StackOrientation.Vertical)
            {
                child.Arrange(canvas, currentX, currentY, contentWidth, size.Height);
                currentY += size.Height + Spacing;
            }
            else
            {
                child.Arrange(canvas, currentX, currentY, size.Width, contentHeight);
                currentX += size.Width + Spacing;
            }
        }
    }
}
