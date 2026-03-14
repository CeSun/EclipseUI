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
    
    // 缓存 Measure 阶段每个子元素的尺寸，避免 Arrange 时重新 Measure 导致不一致
    private readonly Dictionary<EclipseElement, SKSize> _childSizes = new();
    
    public override void Render(SKCanvas canvas)
    {
        if (!IsVisible) return;
        
        canvas.Save();
        
        try
        {
            // 绘制背景
            if (BackgroundColor.HasValue)
            {
                var rect = new SKRect(X, Y, X + Width, Y + Height);
                using var bgPaint = new SKPaint { Color = BackgroundColor.Value, IsAntialias = true };
                canvas.DrawRect(rect, bgPaint);
            }
            
            // 设置裁剪区域为内容区域（不包括 Padding）
            var clipRect = new SKRect(
                X + PaddingLeft,
                Y + PaddingTop,
                X + Width - PaddingRight,
                Y + Height - PaddingBottom
            );
            canvas.ClipRect(clipRect);
            
            // 渲染子元素
            RenderChildren(canvas);
        }
        finally
        {
            canvas.Restore();
        }
    }
    
    public override SKSize Measure(SKCanvas canvas, float availableWidth, float availableHeight)
    {
        _childSizes.Clear();
        
        float totalWidth = 0, totalHeight = 0, maxWidth = 0, maxHeight = 0;
        
        foreach (var child in Children)
        {
            // 对于 Vertical 方向：宽度受限，高度无限（让子元素测量实际需要的高度）
            // 对于 Horizontal 方向：宽度无限，高度受限
            float childWidth = Orientation == StackOrientation.Vertical 
                ? availableWidth - PaddingLeft - PaddingRight 
                : float.PositiveInfinity;
            float childHeight = Orientation == StackOrientation.Vertical 
                ? float.PositiveInfinity 
                : availableHeight - PaddingTop - PaddingBottom;
            
            var size = child.Measure(canvas, childWidth, childHeight);
            
            // 缓存子元素尺寸
            _childSizes[child] = size;
            
            // 加上 Margin
            float childTotalWidth = size.Width + child.MarginLeft + child.MarginRight;
            float childTotalHeight = size.Height + child.MarginTop + child.MarginBottom;
            
            if (Orientation == StackOrientation.Vertical)
            {
                totalHeight += childTotalHeight + Spacing;
                maxWidth = Math.Max(maxWidth, childTotalWidth);
            }
            else
            {
                totalWidth += childTotalWidth + Spacing;
                maxHeight = Math.Max(maxHeight, childTotalHeight);
            }
        }
        
        // 减去最后一个元素的 Spacing
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
        // 设置自身位置和尺寸，但不调用 ArrangeChildren（我们会手动排列子元素）
        X = x;
        Y = y;
        Width = width;
        Height = height;
        
        float currentX = x + PaddingLeft;
        float currentY = y + PaddingTop;
        float contentWidth = width - PaddingLeft - PaddingRight;
        float contentHeight = height - PaddingTop - PaddingBottom;
        
        foreach (var child in Children)
        {
            // 使用 Measure 阶段缓存的尺寸，不再重新 Measure
            if (!_childSizes.TryGetValue(child, out var size))
            {
                // 回退：如果没有缓存，重新测量（理论上不应该发生）
                size = child.Measure(canvas, contentWidth, contentHeight);
            }
            
            if (Orientation == StackOrientation.Vertical)
            {
                // 应用水平对齐
                float childX = currentX + child.MarginLeft;
                float childWidth = size.Width;
                float childY = currentY + child.MarginTop;
                
                // 如果子元素有 RequestedWidth 或 MaxWidth，不使用 Stretch
                bool hasRequestedWidth = child.RequestedWidth.HasValue;
                bool hasMaxWidth = child.MaxWidth.HasValue;
                
                if (child.HorizontalAlignment == HorizontalAlignment.Center)
                {
                    childX = currentX + (contentWidth - size.Width) / 2;
                }
                else if (child.HorizontalAlignment == HorizontalAlignment.Right)
                {
                    childX = currentX + contentWidth - size.Width - child.MarginRight;
                }
                else if (hasRequestedWidth || hasMaxWidth || child.HorizontalAlignment == HorizontalAlignment.Left)
                {
                    // Left 或有 RequestedWidth/MaxWidth: 使用 size.Width，位置不变（左对齐）
                    childWidth = size.Width;
                }
                else
                {
                    // Stretch 且没有 RequestedWidth/MaxWidth: 使用 contentWidth
                    childWidth = contentWidth - child.MarginLeft - child.MarginRight;
                }
                
                child.Arrange(canvas, childX, childY, childWidth, size.Height);
                currentY += size.Height + child.MarginTop + child.MarginBottom + Spacing;
            }
            else
            {
                // 应用垂直对齐
                float childY = currentY + child.MarginTop;
                float childHeight = contentHeight;
                
                if (child.VerticalAlignment == VerticalAlignment.Top)
                {
                    childHeight = size.Height;
                }
                else if (child.VerticalAlignment == VerticalAlignment.Center)
                {
                    childY = currentY + (contentHeight - size.Height) / 2;
                    childHeight = size.Height;
                }
                else if (child.VerticalAlignment == VerticalAlignment.Bottom)
                {
                    childY = currentY + contentHeight - size.Height - child.MarginBottom;
                    childHeight = size.Height;
                }
                // Stretch: 使用 contentHeight
                
                child.Arrange(canvas, currentX + child.MarginLeft, childY, size.Width, childHeight);
                currentX += size.Width + child.MarginLeft + child.MarginRight + Spacing;
            }
        }
    }
}
