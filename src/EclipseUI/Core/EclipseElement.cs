using SkiaSharp;

namespace EclipseUI.Core;

/// <summary>
/// EclipseUI 元素基类 - 所�?UI 元素的抽象基�?/// </summary>
public class EclipseElement
{
    /// <summary>
    /// 父元�?    /// </summary>
    public EclipseElement? Parent { get; internal set; }
    
    /// <summary>
    /// 子元素列�?    /// </summary>
    public List<EclipseElement> Children { get; } = new();
    
    /// <summary>
    /// 元素是否可见
    /// </summary>
    public bool IsVisible { get; set; } = true;
    
    /// <summary>
    /// 元素的位置（相对于父元素�?    /// </summary>
    public float X { get; set; }
    
    /// <summary>
    /// 元素的位置（相对于父元素�?    /// </summary>
    public float Y { get; set; }
    
    /// <summary>
    /// 元素的宽�?    /// </summary>
    public float Width { get; set; }
    
    /// <summary>
    /// 元素的高�?    /// </summary>
    public float Height { get; set; }
    
    /// <summary>
    /// 左边�?    /// </summary>
    public float MarginLeft { get; set; }
    
    /// <summary>
    /// 上边�?    /// </summary>
    public float MarginTop { get; set; }
    
    /// <summary>
    /// 右边�?    /// </summary>
    public float MarginRight { get; set; }
    
    /// <summary>
    /// 下边�?    /// </summary>
    public float MarginBottom { get; set; }
    
    /// <summary>
    /// 左内边距
    /// </summary>
    public float PaddingLeft { get; set; }
    
    /// <summary>
    /// 上内边距
    /// </summary>
    public float PaddingTop { get; set; }
    
    /// <summary>
    /// 右内边距
    /// </summary>
    public float PaddingRight { get; set; }
    
    /// <summary>
    /// 下内边距
    /// </summary>
    public float PaddingBottom { get; set; }
    
    /// <summary>
    /// 背景颜色
    /// </summary>
    public SKColor? BackgroundColor { get; set; }
    
    /// <summary>
    /// 元素 ID（用于事件处理）
    /// </summary>
    public string? Id { get; set; }
    
    /// <summary>
    /// CSS 类名（用于样式）
    /// </summary>
    public string? ClassName { get; set; }
    
    /// <summary>
    /// 点击事件
    /// </summary>
    public Action<EclipseElement, SKPoint>? OnClick { get; set; }
    
    /// <summary>
    /// 获取实际绘制区域（考虑内边距）
    /// </summary>
    public SKRect ContentRect => new(
        X + PaddingLeft,
        Y + PaddingTop,
        X + Width - PaddingRight,
        Y + Height - PaddingBottom
    );
    
    /// <summary>
    /// 获取包含边距的外框区�?    /// </summary>
    public SKRect OuterRect => new(
        X - MarginLeft,
        Y - MarginTop,
        X + Width + MarginRight,
        Y + Height + MarginBottom
    );
    
    /// <summary>
    /// 测量元素所需的最小尺�?    /// </summary>
    public virtual SKSize Measure(SKCanvas canvas, float availableWidth, float availableHeight)
    {
        if (Children.Count > 0)
        {
            float maxWidth = 0;
            float maxHeight = 0;
            
            foreach (var child in Children)
            {
                var childSize = child.Measure(canvas, availableWidth, availableHeight);
                maxWidth = Math.Max(maxWidth, childSize.Width);
                maxHeight = Math.Max(maxHeight, childSize.Height);
            }
            
            return new SKSize(maxWidth + PaddingLeft + PaddingRight, maxHeight + PaddingTop + PaddingBottom);
        }
        
        return new SKSize(Width, Height);
    }
    
    /// <summary>
    /// 排列元素及其子元�?    /// </summary>
    public virtual void Arrange(SKCanvas canvas, float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
        ArrangeChildren(canvas);
    }
    
    /// <summary>
    /// 排列子元�?    /// </summary>
    protected virtual void ArrangeChildren(SKCanvas canvas)
    {
        foreach (var child in Children)
        {
            child.Arrange(canvas, X + PaddingLeft, Y + PaddingTop, 
                Width - PaddingLeft - PaddingRight, 
                Height - PaddingTop - PaddingBottom);
        }
    }
    
    /// <summary>
    /// 绘制元素及其子元�?    /// </summary>
    public virtual void Render(SKCanvas canvas)
    {
        if (!IsVisible) return;
        
        canvas.Save();
        
        try
        {
            if (BackgroundColor.HasValue)
            {
                var rect = new SKRect(X, Y, X + Width, Y + Height);
                using var paint = new SKPaint { Color = BackgroundColor.Value, IsAntialias = true };
                canvas.DrawRect(rect, paint);
            }
            
            RenderContent(canvas);
            RenderChildren(canvas);
        }
        finally
        {
            canvas.Restore();
        }
    }
    
    /// <summary>
    /// 绘制元素内容（由子类实现�?    /// </summary>
    protected virtual void RenderContent(SKCanvas canvas) { }
    
    /// <summary>
    /// 绘制子元�?    /// </summary>
    protected void RenderChildren(SKCanvas canvas)
    {
        foreach (var child in Children)
        {
            child.Render(canvas);
        }
    }
    
    /// <summary>
    /// 处理点击事件
    /// </summary>
    public virtual bool HandleClick(SKPoint point)
    {
        if (!IsVisible) return false;
        
        var rect = new SKRect(X, Y, X + Width, Y + Height);
        if (!rect.Contains(point)) return false;
        
        for (int i = Children.Count - 1; i >= 0; i--)
        {
            if (Children[i].HandleClick(point)) return true;
        }
        
        OnClick?.Invoke(this, point);
        return true;
    }
    
    /// <summary>
    /// 添加子元�?    /// </summary>
    public void AddChild(EclipseElement child)
    {
        child.Parent = this;
        Children.Add(child);
    }
    
    /// <summary>
    /// 移除子元�?    /// </summary>
    public void RemoveChild(EclipseElement child)
    {
        child.Parent = null;
        Children.Remove(child);
    }
    
    /// <summary>
    /// 清除所有子元素
    /// </summary>
    public void ClearChildren()
    {
        foreach (var child in Children) child.Parent = null;
        Children.Clear();
    }
}

/// <summary>
/// 鼠标事件参数
/// </summary>
public class MouseEventArgs
{
    public float ClientX { get; set; }
    public float ClientY { get; set; }
}
