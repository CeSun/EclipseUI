using Eclipse.Core;
using Eclipse.Core.Abstractions;
using Eclipse.Input;
using Eclipse.Rendering;

namespace Eclipse.Controls;

/// <summary>
/// Canvas 附加属性
/// </summary>
public static class Canvas
{
    /// <summary>
    /// 左边距
    /// </summary>
    public static readonly AttachedProperty<double> Left = new("Canvas.Left", 0);
    
    /// <summary>
    /// 上边距
    /// </summary>
    public static readonly AttachedProperty<double> Top = new("Canvas.Top", 0);
    
    /// <summary>
    /// 右边距
    /// </summary>
    public static readonly AttachedProperty<double> Right = new("Canvas.Right", 0);
    
    /// <summary>
    /// 下边距
    /// </summary>
    public static readonly AttachedProperty<double> Bottom = new("Canvas.Bottom", 0);
    
    /// <summary>
    /// Z 顺序
    /// </summary>
    public static readonly AttachedProperty<int> ZIndex = new("Canvas.ZIndex", 0);
}

/// <summary>
/// 绝对定位布局控件
/// </summary>
public class CanvasLayout : InputElementBase
{
    public Color? BackgroundColor { get; set; }
    
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
    /// 安排子元素位置（绝对定位）
    /// </summary>
    public void Arrange(Rect finalBounds, IDrawingContext context)
    {
        UpdateBounds(finalBounds);
        
        foreach (var child in Children)
        {
            var left = child.Get(Canvas.Left);
            var top = child.Get(Canvas.Top);
            var right = child.Get(Canvas.Right);
            var bottom = child.Get(Canvas.Bottom);
            
            // 获取子元素尺寸
            var childSize = MeasureChild(child, context);
            
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
            ArrangeChild(child, childBounds, context);
        }
    }
    
    public override void Render(IDrawingContext context, Rect bounds)
    {
        UpdateBounds(bounds);
        
        if (BackgroundColor.HasValue)
        {
            context.DrawRectangle(bounds, BackgroundColor);
        }
        
        foreach (var child in Children)
        {
            var left = child.Get(Canvas.Left);
            var top = child.Get(Canvas.Top);
            var right = child.Get(Canvas.Right);
            var bottom = child.Get(Canvas.Bottom);
            
            var childSize = MeasureChild(child, context);
            
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
            child.Render(context, childBounds);
        }
    }
    
    private Size MeasureChild(IComponent child, IDrawingContext context)
    {
        if (child is InteractiveControl interactiveControl)
        {
            return interactiveControl.Measure(Size.Empty, context);
        }
        else if (child is StackLayout stackLayout)
        {
            return stackLayout.Measure(Size.Empty, context);
        }
        else if (child is Label label)
        {
            return label.Measure(Size.Empty, context);
        }
        return new Size(100 * context.Scale, 40 * context.Scale);
    }
    
    private void ArrangeChild(IComponent child, Rect bounds, IDrawingContext context)
    {
        if (child is InteractiveControl interactiveControl)
        {
            interactiveControl.Arrange(bounds, context);
        }
        else if (child is StackLayout stackLayout)
        {
            stackLayout.Arrange(bounds, context);
        }
    }
}