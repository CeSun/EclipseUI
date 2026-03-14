using SkiaSharp;
using EclipseUI.Core;

namespace EclipseUI.Layout;

/// <summary>
/// 滚动视图元素
/// </summary>
public class ScrollViewElement : EclipseElement
{
    /// <summary>
    /// 滚动偏移量（垂直方向）
    /// </summary>
    public float ScrollOffset { get; set; }
    
    /// <summary>
    /// 内容总高度
    /// </summary>
    public float ContentHeight { get; private set; }
    
    /// <summary>
    /// 是否显示滚动条
    /// </summary>
    public bool ShowScrollbar { get; set; } = true;
    
    public override SKSize Measure(SKCanvas canvas, float availableWidth, float availableHeight)
    {
        // 计算内容尺寸
        float contentWidth = availableWidth - PaddingLeft - PaddingRight;
        float contentHeight = availableHeight - PaddingTop - PaddingBottom;
        
        // 应用 MaxWidth/MaxHeight 限制
        if (MaxWidth.HasValue)
            contentWidth = Math.Min(contentWidth, MaxWidth.Value - PaddingLeft - PaddingRight);
        if (MaxHeight.HasValue)
            contentHeight = Math.Min(contentHeight, MaxHeight.Value - PaddingTop - PaddingBottom);
        
        // 测量子元素（传递无限高度，让内容决定实际高度）
        float measuredContentHeight = 0;
        float measuredContentWidth = 0;
        
        if (Children.Count > 0)
        {
            foreach (var child in Children)
            {
                var childSize = child.Measure(canvas, contentWidth, float.PositiveInfinity);
                measuredContentWidth = Math.Max(measuredContentWidth, childSize.Width);
                measuredContentHeight += childSize.Height;
            }
        }
        
        // 应用用户设置的尺寸
        float finalWidth = RequestedWidth ?? (measuredContentWidth + PaddingLeft + PaddingRight);
        float finalHeight = RequestedHeight ?? contentHeight;
        
        // 应用 Min/Max 限制
        if (MinWidth.HasValue) finalWidth = Math.Max(finalWidth, MinWidth.Value);
        if (MinHeight.HasValue) finalHeight = Math.Max(finalHeight, MinHeight.Value);
        if (MaxWidth.HasValue) finalWidth = Math.Min(finalWidth, MaxWidth.Value);
        if (MaxHeight.HasValue) finalHeight = Math.Min(finalHeight, MaxHeight.Value);
        
        // 保存内容总高度用于滚动
        ContentHeight = measuredContentHeight + PaddingTop + PaddingBottom;
        
        return new SKSize(finalWidth, finalHeight);
    }
    
    public override void Arrange(SKCanvas canvas, float x, float y, float width, float height)
    {
        base.Arrange(canvas, x, y, width, height);
        
        // 限制滚动偏移量
        float maxScroll = Math.Max(0, ContentHeight - height);
        ScrollOffset = Math.Clamp(ScrollOffset, 0, maxScroll);
        
        // 排列子元素（应用滚动偏移）
        float currentY = y + PaddingTop - ScrollOffset;
        float contentWidth = width - PaddingLeft - PaddingRight;
        
        foreach (var child in Children)
        {
            var childSize = child.Measure(canvas, contentWidth, float.PositiveInfinity);
            child.Arrange(canvas, x + PaddingLeft, currentY, contentWidth, childSize.Height);
            currentY += childSize.Height;
        }
    }
    
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
            
            // 设置裁剪区域
            var clipRect = new SKRect(
                X + PaddingLeft,
                Y + PaddingTop,
                X + Width - PaddingRight,
                Y + Height - PaddingBottom
            );
            canvas.ClipRect(clipRect);
            
            // 渲染子元素
            RenderChildren(canvas);
            
            // 绘制滚动条
            if (ShowScrollbar && ContentHeight > Height)
            {
                RenderScrollbar(canvas);
            }
        }
        finally
        {
            canvas.Restore();
        }
    }
    
    private void RenderScrollbar(SKCanvas canvas)
    {
        float scrollbarWidth = 6;
        float paddingRight = 4;
        
        // 计算滚动条位置和大小
        float contentVisibleRatio = Height / ContentHeight;
        float scrollRatio = ScrollOffset / (ContentHeight - Height);
        
        float scrollbarHeight = Math.Max(30, Height * contentVisibleRatio);
        float scrollbarY = Y + PaddingTop + scrollRatio * (Height - scrollbarHeight);
        float scrollbarX = X + Width - PaddingRight - scrollbarWidth - paddingRight;
        
        // 绘制滚动条背景
        var bgRect = new SKRect(scrollbarX, Y + PaddingTop, scrollbarX + scrollbarWidth, Y + Height - PaddingBottom);
        using var bgPaint = new SKPaint 
        { 
            Color = new SKColor(0, 0, 0, 30), 
            IsAntialias = true 
        };
        canvas.DrawRect(bgRect, bgPaint);
        
        // 根据状态设置滚动条颜色
        byte alpha;
        if (_isDraggingScrollbar)
            alpha = 150;  // 拖动时最深
        else if (_isHoveringScrollbar)
            alpha = 120;  // 悬停时较深
        else
            alpha = 80;   // 默认
        
        // 绘制滚动条
        var thumbRect = new SKRect(scrollbarX, scrollbarY, scrollbarX + scrollbarWidth, scrollbarY + scrollbarHeight);
        using var thumbPaint = new SKPaint 
        { 
            Color = new SKColor(0, 0, 0, alpha), 
            IsAntialias = true 
        };
        canvas.DrawRoundRect(thumbRect, 3, 3, thumbPaint);
    }
    
    /// <summary>
    /// 处理鼠标滚轮事件
    /// </summary>
    public bool HandleMouseWheel(float deltaY)
    {
        if (ContentHeight <= Height)
            return false;
        
        float scrollAmount = -deltaY * 20; // 滚动速度（反向，符合直觉）
        ScrollOffset += scrollAmount;
        ScrollOffset = Math.Clamp(ScrollOffset, 0, ContentHeight - Height);
        return true;
    }
    
    /// <summary>
    /// 是否正在拖动滚动条
    /// </summary>
    private bool _isDraggingScrollbar;
    private float _dragStartY;
    private float _dragStartOffset;
    
    /// <summary>
    /// 鼠标是否悬停在滚动条上
    /// </summary>
    private bool _isHoveringScrollbar;
    
    /// <summary>
    /// 处理鼠标按下事件
    /// </summary>
    public bool HandleMouseDown(float x, float y)
    {
        if (ContentHeight <= Height || !ShowScrollbar)
            return false;
        
        // 检查是否点击在滚动条区域
        float scrollbarWidth = 6;
        float paddingRight = 4;
        float scrollbarX = X + Width - PaddingRight - scrollbarWidth - paddingRight;
        
        if (x >= scrollbarX && x <= scrollbarX + scrollbarWidth &&
            y >= Y + PaddingTop && y <= Y + Height - PaddingBottom)
        {
            // 检查是否点击在滑块上
            float scrollbarHeight = GetScrollbarHeight();
            float contentVisibleRatio = Height / ContentHeight;
            float scrollRatio = ScrollOffset / (ContentHeight - Height);
            float scrollbarY = Y + PaddingTop + scrollRatio * (Height - scrollbarHeight);
            
            if (y >= scrollbarY && y <= scrollbarY + scrollbarHeight)
            {
                // 点击在滑块上，开始拖动
                _isDraggingScrollbar = true;
                _dragStartY = y;
                _dragStartOffset = ScrollOffset;
            }
            else
            {
                // 点击在空白区域，滚动一页
                float pageScrollAmount = Height * 0.8f; // 滚动 80% 的可视区域
                if (y < scrollbarY)
                {
                    // 点击在滑块上方，向上滚动
                    ScrollOffset = Math.Max(0, ScrollOffset - pageScrollAmount);
                }
                else
                {
                    // 点击在滑块下方，向下滚动
                    ScrollOffset = Math.Min(ContentHeight - Height, ScrollOffset + pageScrollAmount);
                }
            }
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 处理鼠标移动事件
    /// </summary>
    public bool HandleMouseMove(float x, float y)
    {
        if (_isDraggingScrollbar)
        {
            float deltaY = y - _dragStartY;
            float scrollRatio = deltaY / (Height - GetScrollbarHeight());
            float newOffset = _dragStartOffset + scrollRatio * (ContentHeight - Height);
            
            ScrollOffset = Math.Clamp(newOffset, 0, ContentHeight - Height);
            return true;
        }
        
        // 检测是否悬停在滚动条上
        if (ContentHeight <= Height || !ShowScrollbar)
            return false;
        
        float scrollbarWidth = 6;
        float paddingRight = 4;
        float scrollbarX = X + Width - PaddingRight - scrollbarWidth - paddingRight;
        
        bool wasHovering = _isHoveringScrollbar;
        _isHoveringScrollbar = (x >= scrollbarX && x <= scrollbarX + scrollbarWidth &&
                                y >= Y + PaddingTop && y <= Y + Height - PaddingBottom);
        
        return _isHoveringScrollbar != wasHovering;
    }
    
    /// <summary>
    /// 处理鼠标释放事件
    /// </summary>
    public void HandleMouseUp()
    {
        _isDraggingScrollbar = false;
    }
    
    /// <summary>
    /// 处理鼠标离开事件
    /// </summary>
    public void HandleMouseLeave()
    {
        _isHoveringScrollbar = false;
    }
    
    private float GetScrollbarHeight()
    {
        float contentVisibleRatio = Height / ContentHeight;
        return Math.Max(30, Height * contentVisibleRatio);
    }
}
