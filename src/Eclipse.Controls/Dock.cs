using Eclipse.Core;
using Eclipse.Core.Abstractions;
using Eclipse.Input;
using Eclipse.Rendering;
using System.Collections.Generic;

namespace Eclipse.Controls;

/// <summary>
/// 停靠位置
/// </summary>
public enum Dock
{
    /// <summary>
    /// 停靠在左侧
    /// </summary>
    Left,
    
    /// <summary>
    /// 停靠在顶部
    /// </summary>
    Top,
    
    /// <summary>
    /// 停靠在右侧
    /// </summary>
    Right,
    
    /// <summary>
    /// 停靠在底部
    /// </summary>
    Bottom,
    
    /// <summary>
    /// 填充剩余空间（默认）
    /// </summary>
    Fill
}

/// <summary>
/// Dock 布局面板 - 子元素可以停靠在上、下、左、右或填充剩余空间
/// </summary>
public class DockPanel : ComponentBase
{
    /// <summary>
    /// 背景颜色
    /// </summary>
    public Color BackgroundColor { get; set; } = Color.Transparent;
    
    /// <summary>
    /// 内边距
    /// </summary>
    public double Padding { get; set; } = 0;
    
    /// <summary>
    /// 子元素间距
    /// </summary>
    public double Spacing { get; set; } = 0;
    
    /// <summary>
    /// 最后一个子元素是否填充剩余空间
    /// </summary>
    public bool LastChildFill { get; set; } = true;
    
    /// <summary>
    /// 固定高度（用于在 StackLayout 等布局中）
    /// </summary>
    public double Height { get; set; } = 200;
    
    public override bool IsVisible => true;
    
    public DockPanel()
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
    
    /// <summary>
    /// 测量布局所需尺寸
    /// </summary>
    public override Size Measure(Size availableSize, IDrawingContext context)
    {
        var paddingValue = Padding * context.Scale;
        var spacingValue = Spacing * context.Scale;
        
        // 如果有固定高度，优先使用
        var fixedHeight = Height * context.Scale;
        
        var remainingWidth = availableSize.Width - paddingValue * 2;
        var remainingHeight = fixedHeight > 0 ? fixedHeight - paddingValue * 2 : availableSize.Height - paddingValue * 2;
        
        double totalWidth = paddingValue * 2;
        double totalHeight = paddingValue * 2;
        
        // 先测量非 Fill 的子元素
        for (int i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            var dock = GetDock(child);
            var isLast = i == Children.Count - 1;
            
            // 最后一个子元素且 LastChildFill=true 时，使用剩余空间
            if (isLast && LastChildFill && dock != Dock.Fill)
            {
                continue; // 稍后处理
            }
            
            var childSize = MeasureChild(child, new Size(remainingWidth, remainingHeight), context);
            
            switch (dock)
            {
                case Dock.Left:
                case Dock.Right:
                    remainingWidth -= childSize.Width + spacingValue;
                    totalWidth += childSize.Width + spacingValue;
                    totalHeight = Math.Max(totalHeight, childSize.Height + paddingValue * 2);
                    break;
                    
                case Dock.Top:
                case Dock.Bottom:
                    remainingHeight -= childSize.Height + spacingValue;
                    totalHeight += childSize.Height + spacingValue;
                    totalWidth = Math.Max(totalWidth, childSize.Width + paddingValue * 2);
                    break;
                    
                case Dock.Fill:
                    // Fill 使用剩余空间
                    totalWidth = Math.Max(totalWidth, remainingWidth + paddingValue * 2);
                    totalHeight = Math.Max(totalHeight, remainingHeight + paddingValue * 2);
                    break;
            }
        }
        
        return new Size(totalWidth, totalHeight);
    }
    
    /// <summary>
    /// 安排子元素位置
    /// </summary>
    public override void Arrange(Rect finalBounds, IDrawingContext context)
    {
        base.Arrange(finalBounds, context);
        
        var paddingValue = Padding * context.Scale;
        var spacingValue = Spacing * context.Scale;
        
        // 计算可用区域
        var x = finalBounds.X + paddingValue;
        var y = finalBounds.Y + paddingValue;
        var width = finalBounds.Width - paddingValue * 2;
        var height = finalBounds.Height - paddingValue * 2;
        
        // 安排每个子元素
        for (int i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            var dock = GetDock(child);
            var isLast = i == Children.Count - 1;
            
            // 获取子元素尺寸
            var childSize = MeasureChild(child, new Size(width, height), context);
            
            Rect childBounds;
            
            // 最后一个子元素且 LastChildFill=true 时，填充剩余空间
            var shouldFill = (isLast && LastChildFill) || dock == Dock.Fill;
            
            if (shouldFill)
            {
                // 填充剩余空间
                childBounds = new Rect(x, y, width, height);
                
                // 安排子元素
                child.Arrange(childBounds, context);
                
                // 后续没有子元素了
                break;
            }
            
            switch (dock)
            {
                case Dock.Left:
                    childBounds = new Rect(x, y, childSize.Width, height);
                    x += childSize.Width + spacingValue;
                    width -= childSize.Width + spacingValue;
                    break;
                    
                case Dock.Right:
                    childBounds = new Rect(x + width - childSize.Width, y, childSize.Width, height);
                    width -= childSize.Width + spacingValue;
                    break;
                    
                case Dock.Top:
                    childBounds = new Rect(x, y, width, childSize.Height);
                    y += childSize.Height + spacingValue;
                    height -= childSize.Height + spacingValue;
                    break;
                    
                case Dock.Bottom:
                    childBounds = new Rect(x, y + height - childSize.Height, width, childSize.Height);
                    height -= childSize.Height + spacingValue;
                    break;
                    
                default:
                    childBounds = new Rect(x, y, width, height);
                    break;
            }
            
            // 安排子元素
            child.Arrange(childBounds, context);
        }
    }
    
    public override void Render(IDrawingContext context, Rect bounds)
    {
        UpdateBounds(bounds);
        
        // 绘制背景
        if (BackgroundColor != Color.Transparent)
        {
            context.DrawRectangle(bounds, BackgroundColor);
        }
        
        var paddingValue = Padding * context.Scale;
        var spacingValue = Spacing * context.Scale;
        
        // 计算可用区域
        var x = bounds.X + paddingValue;
        var y = bounds.Y + paddingValue;
        var width = bounds.Width - paddingValue * 2;
        var height = bounds.Height - paddingValue * 2;
        
        // 渲染每个子元素
        for (int i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            var dock = GetDock(child);
            var isLast = i == Children.Count - 1;
            
            // 获取子元素尺寸
            var childSize = MeasureChild(child, new Size(width, height), context);
            
            Rect childBounds;
            
            // 最后一个子元素且 LastChildFill=true 时，填充剩余空间
            var shouldFill = (isLast && LastChildFill) || dock == Dock.Fill;
            
            if (shouldFill)
            {
                // 填充剩余空间
                childBounds = new Rect(x, y, width, height);
                child.Render(context, childBounds);
                break;
            }
            
            switch (dock)
            {
                case Dock.Left:
                    childBounds = new Rect(x, y, childSize.Width, height);
                    x += childSize.Width + spacingValue;
                    width -= childSize.Width + spacingValue;
                    break;
                    
                case Dock.Right:
                    childBounds = new Rect(x + width - childSize.Width, y, childSize.Width, height);
                    width -= childSize.Width + spacingValue;
                    break;
                    
                case Dock.Top:
                    childBounds = new Rect(x, y, width, childSize.Height);
                    y += childSize.Height + spacingValue;
                    height -= childSize.Height + spacingValue;
                    break;
                    
                case Dock.Bottom:
                    childBounds = new Rect(x, y + height - childSize.Height, width, childSize.Height);
                    height -= childSize.Height + spacingValue;
                    break;
                    
                default:
                    childBounds = new Rect(x, y, width, height);
                    break;
            }
            
            child.Render(context, childBounds);
        }
    }
    
    /// <summary>
    /// 测量子元素
    /// </summary>
    private Size MeasureChild(IComponent child, Size availableSize, IDrawingContext context)
    {
        if (child is InteractiveControl interactiveControl)
            return interactiveControl.Measure(availableSize, context);
        if (child is StackPanel stackPanel)
            return stackPanel.Measure(availableSize, context);
        if (child is DockPanel dockPanel)
            return dockPanel.Measure(availableSize, context);
        if (child is Label label)
            return label.Measure(availableSize, context);
        if (child is ScrollView scrollView)
            return scrollView.Measure(availableSize, context);
        if (child is GridPanel gridPanel)
            return gridPanel.Measure(availableSize, context);
        if (child is Container container)
            return MeasureContainer(container, availableSize, context);
        if (child is ComponentBase componentBase)
            return MeasureComponentBaseChildren(componentBase, context);
        
        return new Size(100 * context.Scale, 100 * context.Scale);
    }
    
    /// <summary>
    /// 测量 Container
    /// </summary>
    private Size MeasureContainer(Container container, Size availableSize, IDrawingContext context)
    {
        var width = container.Width * context.Scale;
        var height = container.Height * context.Scale;
        
        // 如果有固定尺寸，直接返回
        if (container.Width > 0 && container.Height > 0)
        {
            return new Size(width, height);
        }
        
        // 否则根据子元素计算
        double maxWidth = 0;
        double maxHeight = 0;
        
        foreach (var child in container.Children)
        {
            var childSize = MeasureChild(child, availableSize, context);
            maxWidth = Math.Max(maxWidth, childSize.Width);
            maxHeight = Math.Max(maxHeight, childSize.Height);
        }
        
        // 加上 Padding
        var paddingValue = container.Padding * context.Scale;
        maxWidth += paddingValue * 2;
        maxHeight += paddingValue * 2;
        
        // 如果没有子元素，给一个默认尺寸
        if (maxWidth == 0 && maxHeight == 0)
        {
            return new Size(paddingValue * 2, paddingValue * 2);
        }
        
        // 使用固定宽高（如果指定了）
        if (container.Width > 0)
            maxWidth = width;
        if (container.Height > 0)
            maxHeight = height;
        
        return new Size(maxWidth, maxHeight);
    }
    
    /// <summary>
    /// 测量 ComponentBase 子元素
    /// </summary>
    private Size MeasureComponentBaseChildren(ComponentBase component, IDrawingContext context)
    {
        double maxWidth = 0;
        double maxHeight = 0;
        
        foreach (var child in component.Children)
        {
            var childSize = MeasureChild(child, new Size(double.PositiveInfinity, double.PositiveInfinity), context);
            maxWidth = Math.Max(maxWidth, childSize.Width);
            maxHeight += childSize.Height;
        }
        
        return new Size(maxWidth, maxHeight > 0 ? maxHeight : 100 * context.Scale);
    }
    
    // === 附加属性 ===
    
    /// <summary>
    /// Dock 附加属性
    /// </summary>
    public static readonly AttachedProperty<Dock> DockProperty = 
        new AttachedProperty<Dock>("DockPanel.Dock", Dock.Fill);
    
    /// <summary>
    /// 获取子元素的停靠位置
    /// </summary>
    public static Dock GetDock(IComponent element)
    {
        return element.Get(DockProperty);
    }
    
    /// <summary>
    /// 设置子元素的停靠位置
    /// </summary>
    public static void SetDock(IComponent element, Dock value)
    {
        element.Set(DockProperty, value);
    }
}
