using SkiaSharp;
using EclipseUI.Core;

namespace EclipseUI.Layout;

/// <summary>
/// 绝对定位画布元素
/// </summary>
public class CanvasElement : EclipseElement
{
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
            
            var clipRect = new SKRect(X, Y, X + Width, Y + Height);
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
        // Canvas 测量所有子元素，但不影响自身尺寸
        foreach (var child in Children)
        {
            child.Measure(canvas, float.PositiveInfinity, float.PositiveInfinity);
        }
        
        // 如果有 RequestedWidth/Height，使用它们；否则使用可用空间
        float width = RequestedWidth ?? availableWidth;
        float height = RequestedHeight ?? availableHeight;
        
        // 应用 Min/Max 约束
        if (MinWidth.HasValue) width = Math.Max(width, MinWidth.Value);
        if (MaxWidth.HasValue) width = Math.Min(width, MaxWidth.Value);
        if (MinHeight.HasValue) height = Math.Max(height, MinHeight.Value);
        if (MaxHeight.HasValue) height = Math.Min(height, MaxHeight.Value);
        
        return new SKSize(width, height);
    }
    
    public override void Arrange(SKCanvas canvas, float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
        
        foreach (var child in Children)
        {
            // 获取附加属性
            float? left = GetAttachedValue(child, Canvas.LeftProperty);
            float? top = GetAttachedValue(child, Canvas.TopProperty);
            float? right = GetAttachedValue(child, Canvas.RightProperty);
            float? bottom = GetAttachedValue(child, Canvas.BottomProperty);
            
            // 测量子元素
            var size = child.Measure(canvas, float.PositiveInfinity, float.PositiveInfinity);
            
            // 计算位置
            float childX = x;
            float childY = y;
            float childWidth = size.Width;
            float childHeight = size.Height;
            
            if (left.HasValue)
            {
                childX = x + left.Value;
            }
            else if (right.HasValue)
            {
                childX = x + width - right.Value - childWidth;
            }
            
            if (top.HasValue)
            {
                childY = y + top.Value;
            }
            else if (bottom.HasValue)
            {
                childY = y + height - bottom.Value - childHeight;
            }
            
            // 如果同时设置了 left 和 right，拉伸宽度
            if (left.HasValue && right.HasValue)
            {
                childX = x + left.Value;
                childWidth = width - left.Value - right.Value;
            }
            
            // 如果同时设置了 top 和 bottom，拉伸高度
            if (top.HasValue && bottom.HasValue)
            {
                childY = y + top.Value;
                childHeight = height - top.Value - bottom.Value;
            }
            
            child.Arrange(canvas, childX, childY, childWidth, childHeight);
        }
    }
    
    private static float? GetAttachedValue(EclipseElement element, int propertyKey)
    {
        var value = element.GetValue<float?>(propertyKey, null);
        return value;
    }
}
