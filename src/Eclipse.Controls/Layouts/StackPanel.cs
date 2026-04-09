using Eclipse.Core;
using Eclipse.Core.Abstractions;
using Eclipse.Input;
using Eclipse.Rendering;
using System;
using System.Collections.Generic;

namespace Eclipse.Controls;

/// <summary>
/// 堆叠布局面板 - 子元素按垂直或水平方向堆叠排列
/// </summary>
public class StackPanel : ComponentBase
{
    public Orientation Orientation { get; set; } = Orientation.Vertical;
    public double Spacing { get; set; } = 0;
    public double Padding { get; set; } = 0;
    public Color BackgroundColor { get; set; } = Color.Transparent;
    
    /// <summary>
    /// 固定宽度（-1 表示自动）
    /// </summary>
    public double Width { get; set; } = -1;
    
    /// <summary>
    /// 固定高度（-1 表示自动）
    /// </summary>
    public double Height { get; set; } = -1;
    
    public override bool IsVisible => true;
    
    public StackPanel()
    {
        IsHitTestVisible = false;
    }
    
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
    
    public override Size Measure(Size availableSize, IDrawingContext context)
    {
        if (Children.Count == 0)
        {
            _desiredSize = new Size(Padding * 2, Padding * 2);
            return _desiredSize;
        }
        
        var spacingValue = Spacing * context.Scale;
        var paddingValue = Padding * context.Scale;
        
        var contentAvailableSize = new Size(
            availableSize.Width - paddingValue * 2,
            availableSize.Height - paddingValue * 2);
        
        double totalWidth = 0;
        double totalHeight = 0;
        
        foreach (var child in Children)
        {
            Size childSize = child.Measure(contentAvailableSize, context);
            
            if (Orientation == Orientation.Vertical)
            {
                totalWidth = Math.Max(totalWidth, childSize.Width);
                totalHeight += childSize.Height;
            }
            else
            {
                totalWidth += childSize.Width;
                totalHeight = Math.Max(totalHeight, childSize.Height);
            }
        }
        
        if (Children.Count > 1)
        {
            if (Orientation == Orientation.Vertical)
                totalHeight += spacingValue * (Children.Count - 1);
            else
                totalWidth += spacingValue * (Children.Count - 1);
        }
        
        totalWidth += paddingValue * 2;
        totalHeight += paddingValue * 2;
        
        _desiredSize = new Size(totalWidth, totalHeight);
        return _desiredSize;
    }
    
    public override void Arrange(Rect finalBounds, IDrawingContext context)
    {
        // 不调用 base.Arrange()：它会把所有子元素 Arrange 为 finalBounds，
        // 而布局面板应该给每个子元素各自的 bounds
        UpdateBounds(finalBounds);
        
        var spacingValue = Spacing * context.Scale;
        var paddingValue = Padding * context.Scale;
        
        var contentBounds = new Rect(
            finalBounds.X + paddingValue,
            finalBounds.Y + paddingValue,
            Math.Max(0, finalBounds.Width - paddingValue * 2),
            Math.Max(0, finalBounds.Height - paddingValue * 2));
        
        if (Orientation == Orientation.Vertical)
        {
            double y = contentBounds.Y;
            double remainingHeight = contentBounds.Height;
            foreach (var child in Children)
            {
                Size childSize = child.Measure(new Size(contentBounds.Width, Math.Max(0, remainingHeight)), context);
                var childBounds = new Rect(contentBounds.X, y, contentBounds.Width, childSize.Height);
                child.Arrange(childBounds, context);
                y += childSize.Height + spacingValue;
                remainingHeight -= childSize.Height + spacingValue;
            }
        }
        else
        {
            double x = contentBounds.X;
            double remainingWidth = contentBounds.Width;
            foreach (var child in Children)
            {
                Size childSize = child.Measure(new Size(Math.Max(0, remainingWidth), contentBounds.Height), context);
                var childBounds = new Rect(x, contentBounds.Y, childSize.Width, contentBounds.Height);
                child.Arrange(childBounds, context);
                x += childSize.Width + spacingValue;
                remainingWidth -= childSize.Width + spacingValue;
            }
        }
    }
    
    public override void Render(IDrawingContext context, Rect bounds)
    {
        UpdateBounds(bounds);
        
        var spacingValue = Spacing * context.Scale;
        var paddingValue = Padding * context.Scale;
        
        if (BackgroundColor != Color.Transparent)
            context.DrawRectangle(bounds, BackgroundColor);
        
        var contentBounds = new Rect(
            bounds.X + paddingValue,
            bounds.Y + paddingValue,
            bounds.Width - paddingValue * 2,
            bounds.Height - paddingValue * 2);
        
        if (Orientation == Orientation.Vertical)
        {
            double y = contentBounds.Y;
            double remainingHeight = contentBounds.Height;
            foreach (var child in Children)
            {
                var childSize = child.Measure(new Size(contentBounds.Width, Math.Max(0, remainingHeight)), context);
                var childBounds = new Rect(contentBounds.X, y, contentBounds.Width, childSize.Height);
                child.Arrange(childBounds, context);
                child.Render(context, childBounds);
                y += childSize.Height + spacingValue;
                remainingHeight -= childSize.Height + spacingValue;
            }
        }
        else
        {
            double x = contentBounds.X;
            double remainingWidth = contentBounds.Width;
            foreach (var child in Children)
            {
                var childSize = child.Measure(new Size(Math.Max(0, remainingWidth), contentBounds.Height), context);
                var childBounds = new Rect(x, contentBounds.Y, childSize.Width, contentBounds.Height);
                child.Arrange(childBounds, context);
                child.Render(context, childBounds);
                x += childSize.Width + spacingValue;
                remainingWidth -= childSize.Width + spacingValue;
            }
        }
    }
}
