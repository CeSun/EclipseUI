using Eclipse.Core;
using Eclipse.Core.Abstractions;
using Eclipse.Input;
using Eclipse.Rendering;
using System.Collections.Generic;

namespace Eclipse.Controls;

/// <summary>
/// Canvas 附加属性
/// </summary>
public static class Canvas
{
    /// <summary>
    /// 左边距
    /// </summary>
    public static readonly AttachedProperty<double> LeftProperty = new("Canvas.Left", 0);
    
    /// <summary>
    /// 上边距
    /// </summary>
    public static readonly AttachedProperty<double> TopProperty = new("Canvas.Top", 0);
    
    /// <summary>
    /// 右边距
    /// </summary>
    public static readonly AttachedProperty<double> RightProperty = new("Canvas.Right", 0);
    
    /// <summary>
    /// 下边距
    /// </summary>
    public static readonly AttachedProperty<double> BottomProperty = new("Canvas.Bottom", 0);
    
    /// <summary>
    /// Z 顺序
    /// </summary>
    public static readonly AttachedProperty<int> ZIndexProperty = new("Canvas.ZIndex", 0);
    
    // 便捷访问器
    public static double GetLeft(IComponent element) => element.Get(LeftProperty);
    public static void SetLeft(IComponent element, double value) => element.Set(LeftProperty, value);
    
    public static double GetTop(IComponent element) => element.Get(TopProperty);
    public static void SetTop(IComponent element, double value) => element.Set(TopProperty, value);
    
    public static double GetRight(IComponent element) => element.Get(RightProperty);
    public static void SetRight(IComponent element, double value) => element.Set(RightProperty, value);
    
    public static double GetBottom(IComponent element) => element.Get(BottomProperty);
    public static void SetBottom(IComponent element, double value) => element.Set(BottomProperty, value);
    
    public static int GetZIndex(IComponent element) => element.Get(ZIndexProperty);
    public static void SetZIndex(IComponent element, int value) => element.Set(ZIndexProperty, value);
}

/// <summary>
/// 绝对定位布局面板
/// </summary>
public class CanvasPanel : ComponentBase
{
    public Color BackgroundColor { get; set; } = Color.Transparent;
    
    public override bool IsVisible => true;
    
    protected override IEnumerable<IInputElement> GetInputChildren()
    {
        foreach (var child in Children)
        {
            if (child is IInputElement inputElement)
            {
                yield return inputElement;
            }
        }
    }
    
    public override void Build(IBuildContext context) { }
    
    /// <summary>
    /// 测量 Canvas 所需尺寸 - 基于子元素的最大边界
    /// </summary>
    public override Size Measure(Size availableSize, IDrawingContext context)
    {
        double maxWidth = 0;
        double maxHeight = 0;
        
        foreach (var child in Children)
        {
            var childSize = child.Measure(availableSize, context);
            var left = child.Get(Canvas.LeftProperty);
            var top = child.Get(Canvas.TopProperty);
            
            maxWidth = Math.Max(maxWidth, left + childSize.Width);
            maxHeight = Math.Max(maxHeight, top + childSize.Height);
        }
        
        return new Size(maxWidth, maxHeight);
    }
    
    /// <summary>
    /// 安排子元素位置（绝对定位）
    /// </summary>
    public override void Arrange(Rect finalBounds, IDrawingContext context)
    {
        base.Arrange(finalBounds, context);
        
        foreach (var child in Children)
        {
            var left = child.Get(Canvas.LeftProperty);
            var top = child.Get(Canvas.TopProperty);
            var right = child.Get(Canvas.RightProperty);
            var bottom = child.Get(Canvas.BottomProperty);
            
            // 获取子元素尺寸
            var childSize = child.Measure(Size.Empty, context);
            
            double childX, childY;
            double childWidth = childSize.Width;
            double childHeight = childSize.Height;
            
            // 计算位置
            if (left >= 0)
            {
                childX = finalBounds.X + left;
            }
            else if (right >= 0)
            {
                childX = finalBounds.X + finalBounds.Width - right - childWidth;
            }
            else
            {
                childX = finalBounds.X;
            }
            
            if (top >= 0)
            {
                childY = finalBounds.Y + top;
            }
            else if (bottom >= 0)
            {
                childY = finalBounds.Y + finalBounds.Height - bottom - childHeight;
            }
            else
            {
                childY = finalBounds.Y;
            }
            
            var childBounds = new Rect(childX, childY, childWidth, childHeight);
            child.Arrange(childBounds, context);
        }
    }
    
    public override void Render(IDrawingContext context, Rect bounds)
    {
        UpdateBounds(bounds);
        
        if (BackgroundColor != Color.Transparent)
        {
            context.DrawRectangle(bounds, BackgroundColor);
        }
        
        foreach (var child in Children)
        {
            var left = child.Get(Canvas.LeftProperty);
            var top = child.Get(Canvas.TopProperty);
            var right = child.Get(Canvas.RightProperty);
            var bottom = child.Get(Canvas.BottomProperty);
            
            var childSize = child.Measure(Size.Empty, context);
            
            double childX, childY;
            double childWidth = childSize.Width;
            double childHeight = childSize.Height;
            
            if (left >= 0)
            {
                childX = bounds.X + left;
            }
            else if (right >= 0)
            {
                childX = bounds.X + bounds.Width - right - childWidth;
            }
            else
            {
                childX = bounds.X;
            }
            
            if (top >= 0)
            {
                childY = bounds.Y + top;
            }
            else if (bottom >= 0)
            {
                childY = bounds.Y + bounds.Height - bottom - childHeight;
            }
            else
            {
                childY = bounds.Y;
            }
            
            var childBounds = new Rect(childX, childY, childWidth, childHeight);
            child.Arrange(childBounds, context);
            child.Render(context, childBounds);
        }
    }
}
