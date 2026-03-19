using SkiaSharp;
using EclipseUI.Core;

namespace EclipseUI.Layout;

/// <summary>
/// 流式布局面板元素
/// </summary>
public class WrapPanelElement : EclipseElement
{
    public StackOrientation Orientation { get; set; } = StackOrientation.Horizontal;
    public float ItemSpacing { get; set; } = 8;
    public float LineSpacing { get; set; } = 8;
    
    private readonly Dictionary<EclipseElement, SKSize> _childSizes = new();
    
    public override void Render(SKCanvas canvas)
    {
        if (!IsVisible) return;
        
        canvas.Save();
        try
        {
            if (BackgroundColor.HasValue)
            {
                var rect = new SKRect(X, Y, X + Width, Y + Height);
                using var bgPaint = new SKPaint { Color = BackgroundColor.Value, IsAntialias = true };
                canvas.DrawRect(rect, bgPaint);
            }
            
            var clipRect = new SKRect(
                X + PaddingLeft,
                Y + PaddingTop,
                X + Width - PaddingRight,
                Y + Height - PaddingBottom
            );
            canvas.ClipRect(clipRect);
            
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
        
        float contentWidth = availableWidth - PaddingLeft - PaddingRight;
        float contentHeight = availableHeight - PaddingTop - PaddingBottom;
        
        if (Orientation == StackOrientation.Horizontal)
        {
            return MeasureHorizontal(canvas, contentWidth, contentHeight);
        }
        else
        {
            return MeasureVertical(canvas, contentWidth, contentHeight);
        }
    }
    
    private SKSize MeasureHorizontal(SKCanvas canvas, float contentWidth, float contentHeight)
    {
        float currentX = 0;
        float currentLineHeight = 0;
        float totalHeight = 0;
        float maxLineWidth = 0;
        
        foreach (var child in Children)
        {
            var size = child.Measure(canvas, float.PositiveInfinity, float.PositiveInfinity);
            _childSizes[child] = size;
            
            float childTotalWidth = size.Width + child.MarginLeft + child.MarginRight;
            float childTotalHeight = size.Height + child.MarginTop + child.MarginBottom;
            
            // 换行判断
            if (currentX > 0 && currentX + childTotalWidth > contentWidth)
            {
                maxLineWidth = Math.Max(maxLineWidth, currentX - ItemSpacing);
                totalHeight += currentLineHeight + LineSpacing;
                currentX = 0;
                currentLineHeight = 0;
            }
            
            currentX += childTotalWidth + ItemSpacing;
            currentLineHeight = Math.Max(currentLineHeight, childTotalHeight);
        }
        
        // 最后一行
        if (currentX > 0)
        {
            maxLineWidth = Math.Max(maxLineWidth, currentX - ItemSpacing);
            totalHeight += currentLineHeight;
        }
        
        return new SKSize(
            maxLineWidth + PaddingLeft + PaddingRight,
            totalHeight + PaddingTop + PaddingBottom
        );
    }
    
    private SKSize MeasureVertical(SKCanvas canvas, float contentWidth, float contentHeight)
    {
        float currentY = 0;
        float currentColumnWidth = 0;
        float totalWidth = 0;
        float maxColumnHeight = 0;
        
        foreach (var child in Children)
        {
            var size = child.Measure(canvas, float.PositiveInfinity, float.PositiveInfinity);
            _childSizes[child] = size;
            
            float childTotalWidth = size.Width + child.MarginLeft + child.MarginRight;
            float childTotalHeight = size.Height + child.MarginTop + child.MarginBottom;
            
            // 换列判断
            if (currentY > 0 && currentY + childTotalHeight > contentHeight)
            {
                maxColumnHeight = Math.Max(maxColumnHeight, currentY - ItemSpacing);
                totalWidth += currentColumnWidth + LineSpacing;
                currentY = 0;
                currentColumnWidth = 0;
            }
            
            currentY += childTotalHeight + ItemSpacing;
            currentColumnWidth = Math.Max(currentColumnWidth, childTotalWidth);
        }
        
        // 最后一列
        if (currentY > 0)
        {
            maxColumnHeight = Math.Max(maxColumnHeight, currentY - ItemSpacing);
            totalWidth += currentColumnWidth;
        }
        
        return new SKSize(
            totalWidth + PaddingLeft + PaddingRight,
            maxColumnHeight + PaddingTop + PaddingBottom
        );
    }
    
    public override void Arrange(SKCanvas canvas, float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
        
        float contentWidth = width - PaddingLeft - PaddingRight;
        float contentHeight = height - PaddingTop - PaddingBottom;
        
        if (Orientation == StackOrientation.Horizontal)
        {
            ArrangeHorizontal(canvas, contentWidth, contentHeight);
        }
        else
        {
            ArrangeVertical(canvas, contentWidth, contentHeight);
        }
    }
    
    private void ArrangeHorizontal(SKCanvas canvas, float contentWidth, float contentHeight)
    {
        float startX = X + PaddingLeft;
        float startY = Y + PaddingTop;
        float currentX = 0;
        float currentY = 0;
        float currentLineHeight = 0;
        
        foreach (var child in Children)
        {
            if (!_childSizes.TryGetValue(child, out var size))
            {
                size = child.Measure(canvas, float.PositiveInfinity, float.PositiveInfinity);
            }
            
            float childTotalWidth = size.Width + child.MarginLeft + child.MarginRight;
            float childTotalHeight = size.Height + child.MarginTop + child.MarginBottom;
            
            // 换行判断
            if (currentX > 0 && currentX + childTotalWidth > contentWidth)
            {
                currentY += currentLineHeight + LineSpacing;
                currentX = 0;
                currentLineHeight = 0;
            }
            
            child.Arrange(canvas,
                startX + currentX + child.MarginLeft,
                startY + currentY + child.MarginTop,
                size.Width,
                size.Height);
            
            currentX += childTotalWidth + ItemSpacing;
            currentLineHeight = Math.Max(currentLineHeight, childTotalHeight);
        }
    }
    
    private void ArrangeVertical(SKCanvas canvas, float contentWidth, float contentHeight)
    {
        float startX = X + PaddingLeft;
        float startY = Y + PaddingTop;
        float currentX = 0;
        float currentY = 0;
        float currentColumnWidth = 0;
        
        foreach (var child in Children)
        {
            if (!_childSizes.TryGetValue(child, out var size))
            {
                size = child.Measure(canvas, float.PositiveInfinity, float.PositiveInfinity);
            }
            
            float childTotalWidth = size.Width + child.MarginLeft + child.MarginRight;
            float childTotalHeight = size.Height + child.MarginTop + child.MarginBottom;
            
            // 换列判断
            if (currentY > 0 && currentY + childTotalHeight > contentHeight)
            {
                currentX += currentColumnWidth + LineSpacing;
                currentY = 0;
                currentColumnWidth = 0;
            }
            
            child.Arrange(canvas,
                startX + currentX + child.MarginLeft,
                startY + currentY + child.MarginTop,
                size.Width,
                size.Height);
            
            currentY += childTotalHeight + ItemSpacing;
            currentColumnWidth = Math.Max(currentColumnWidth, childTotalWidth);
        }
    }
}
