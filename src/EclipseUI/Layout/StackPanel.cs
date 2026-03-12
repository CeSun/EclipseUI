using SkiaSharp;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
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
        
        // 修复：Vertical 模式用 maxWidth，Horizontal 模式用 totalWidth
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

/// <summary>
/// Razor 组件
/// </summary>
public class StackPanel : EclipseComponentBase
{
    [Parameter] public StackOrientation Orientation { get; set; } = StackOrientation.Vertical;
    [Parameter] public float Spacing { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public EventCallback OnClick { get; set; }
    
    // Margin 属性
    [Parameter] public float MarginLeft { get; set; }
    [Parameter] public float MarginTop { get; set; }
    [Parameter] public float MarginRight { get; set; }
    [Parameter] public float MarginBottom { get; set; }
    
    // Padding 属性
    [Parameter] public float PaddingLeft { get; set; }
    [Parameter] public float PaddingTop { get; set; }
    [Parameter] public float PaddingRight { get; set; }
    [Parameter] public float PaddingBottom { get; set; }
    
    protected override EclipseElement CreateElement()
    {
        var element = new StackPanelElement
        {
            Orientation = Orientation,
            Spacing = Spacing,
            MarginLeft = MarginLeft,
            MarginTop = MarginTop,
            MarginRight = MarginRight,
            MarginBottom = MarginBottom,
            PaddingLeft = PaddingLeft,
            PaddingTop = PaddingTop,
            PaddingRight = PaddingRight,
            PaddingBottom = PaddingBottom
        };
        
        // 绑定点击事件
        if (OnClick.HasDelegate)
        {
            element.OnClick = async (e, p) => await OnClick.InvokeAsync();
        }
        
        return element;
    }
    
    protected override void UpdateElementFromParameters()
    {
        if (_element is StackPanelElement sp)
        {
            sp.Orientation = Orientation;
            sp.Spacing = Spacing;
            sp.MarginLeft = MarginLeft;
            sp.MarginTop = MarginTop;
            sp.MarginRight = MarginRight;
            sp.MarginBottom = MarginBottom;
            sp.PaddingLeft = PaddingLeft;
            sp.PaddingTop = PaddingTop;
            sp.PaddingRight = PaddingRight;
            sp.PaddingBottom = PaddingBottom;
            
            // 更新点击事件
            sp.OnClick = OnClick.HasDelegate ? async (e, p) => await OnClick.InvokeAsync() : null;
        }
    }
    
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        // 使用 CascadingValue 传递父元素给子组件
        builder.OpenComponent<CascadingValue<EclipseElement>>(0);
        builder.AddAttribute(1, "Value", Element);
        builder.AddAttribute(2, "IsFixed", true);
        
        // 使用 ChildContent 参数或派生类的直接内容
        if (ChildContent != null)
        {
            builder.AddAttribute(3, "ChildContent", ChildContent);
        }
        
        builder.CloseComponent();
    }
}
