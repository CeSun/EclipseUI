using SkiaSharp;

namespace EclipseUI.Core;

/// <summary>
/// 根元素 - 始终填充整个可用窗口空间
/// </summary>
internal class RootElement : EclipseElement
{
    public override SKSize Measure(SKCanvas canvas, float availableWidth, float availableHeight)
    {
        // 先让子元素用整个可用空间测量自己
        foreach (var child in Children)
        {
            child.Measure(canvas, availableWidth, availableHeight);
        }
        
        // 根元素始终返回整个可用窗口大小，确保填充整个窗口
        return new SKSize(availableWidth, availableHeight);
    }
    
    public override void Arrange(SKCanvas canvas, float x, float y, float width, float height)
    {
        // 根元素占据整个窗口空间
        X = x;
        Y = y;
        Width = width;
        Height = height;
        
        // 子元素也填充整个根元素空间（减去内边距）
        foreach (var child in Children)
        {
            child.Arrange(canvas, 
                X + PaddingLeft, 
                Y + PaddingTop, 
                Width - PaddingLeft - PaddingRight, 
                Height - PaddingTop - PaddingBottom);
        }
    }
}

/// <summary>
/// 根元素处理器 - 实现 IElementHandler 用于根元素
/// </summary>
internal class RootElementHandler : IElementHandler
{
    public EclipseElement Element { get; } = new RootElement();
}
