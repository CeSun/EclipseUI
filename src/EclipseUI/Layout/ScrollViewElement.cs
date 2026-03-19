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
        // 计算内容区域尺寸
        float contentWidth = availableWidth - PaddingLeft - PaddingRight;
        float contentHeight = availableHeight - PaddingTop - PaddingBottom;
        
        // 应用 MaxWidth/MaxHeight 限制
        if (MaxWidth.HasValue)
            contentWidth = Math.Min(contentWidth, MaxWidth.Value - PaddingLeft - PaddingRight);
        if (MaxHeight.HasValue)
            contentHeight = Math.Min(contentHeight, MaxHeight.Value - PaddingTop - PaddingBottom);
        
        // 测量子元素（传递可用宽度，无限高度让内容决定实际高度）
        float measuredContentHeight = 0;
        float measuredContentWidth = 0;
        
        // 如果宽度是无限的，先用一个合理的默认值测量
        float measureWidth = float.IsPositiveInfinity(contentWidth) ? 800 : contentWidth;
        
        if (Children.Count > 0)
        {
            foreach (var child in Children)
            {
                var childSize = child.Measure(canvas, measureWidth, float.PositiveInfinity);
                measuredContentWidth = Math.Max(measuredContentWidth, childSize.Width);
                measuredContentHeight += childSize.Height;
            }
        }
        
        // 应用用户设置的尺寸
        float finalWidth, finalHeight;
        
        if (RequestedWidth.HasValue)
        {
            finalWidth = RequestedWidth.Value;
        }
        else if (float.IsPositiveInfinity(availableWidth))
        {
            // 宽度无限时，使用测量的内容宽度
            finalWidth = measuredContentWidth + PaddingLeft + PaddingRight;
        }
        else
        {
            // 宽度有限时，使用可用宽度（Stretch 行为）
            finalWidth = availableWidth;
        }
        
        finalHeight = RequestedHeight ?? contentHeight;
        
        // 应用 Min/Max 限制
        if (MinWidth.HasValue) finalWidth = Math.Max(finalWidth, MinWidth.Value);
        if (MinHeight.HasValue) finalHeight = Math.Max(finalHeight, MinHeight.Value);
        if (MaxWidth.HasValue) finalWidth = Math.Min(finalWidth, MaxWidth.Value);
        if (MaxHeight.HasValue) finalHeight = Math.Min(finalHeight, MaxHeight.Value);
        
        // 保存内容总高度用于滚动（不包含 Padding，Padding 是 ScrollView 自身的装饰）
        ContentHeight = measuredContentHeight;
        
        return new SKSize(finalWidth, finalHeight);
    }
    
    public override void Arrange(SKCanvas canvas, float x, float y, float width, float height)
    {
        base.Arrange(canvas, x, y, width, height);
        
        // 限制滚动偏移量（使用内容区域高度）
        float contentAreaHeight = height - PaddingTop - PaddingBottom;
        float maxScroll = Math.Max(0, ContentHeight - contentAreaHeight);
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
        }
        finally
        {
            // 恢复裁剪状态，确保滚动条不受裁剪影响
            canvas.Restore();
        }
        
        // 在裁剪区域外绘制滚动条（这样滚动条才会显示在内容上方）
        float contentAreaHeight = Height - PaddingTop - PaddingBottom;
        if (ShowScrollbar && ContentHeight > contentAreaHeight)
        {
            RenderScrollbar(canvas);
        }
    }
    
    private void RenderScrollbar(SKCanvas canvas)
    {
        float scrollbarWidth = 6;
        float paddingRight = 4;
        
        // 内容区域高度（不包括 Padding）
        float contentAreaHeight = Height - PaddingTop - PaddingBottom;
        
        // 计算滚动条位置和大小
        float contentVisibleRatio = contentAreaHeight / ContentHeight;
        float scrollRatio = ScrollOffset / (ContentHeight - contentAreaHeight);
        
        float scrollbarHeight = Math.Max(30, contentAreaHeight * contentVisibleRatio);
        float scrollbarY = Y + PaddingTop + scrollRatio * (contentAreaHeight - scrollbarHeight);
        
        // 获取画布的实际裁剪边界，确保滚动条不会画到窗口外
        var clipBounds = canvas.LocalClipBounds;
        float effectiveRight = Math.Min(X + Width, clipBounds.Right);
        float scrollbarX = effectiveRight - PaddingRight - scrollbarWidth - paddingRight;
        
        // 绘制滚动条背景（轨道）
        var bgRect = new SKRect(scrollbarX, Y + PaddingTop, scrollbarX + scrollbarWidth, Y + PaddingTop + contentAreaHeight);
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
    public override bool HandleMouseWheel(float deltaY)
    {
        // 首先尝试让子元素处理
        for (int i = Children.Count - 1; i >= 0; i--)
        {
            if (Children[i].HandleMouseWheel(deltaY))
                return true;
        }
        
        // 子元素没有处理，自己处理滚动
        float contentAreaHeight = Height - PaddingTop - PaddingBottom;
        if (ContentHeight <= contentAreaHeight)
            return false;
        
        float scrollAmount = -deltaY * 20; // 滚动速度（反向，符合直觉）
        ScrollOffset += scrollAmount;
        ScrollOffset = Math.Clamp(ScrollOffset, 0, ContentHeight - contentAreaHeight);
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
    public override bool HandleMouseDown(float x, float y)
    {
        // 首先检查子元素（从后往前，优先处理上层的元素）
        for (int i = Children.Count - 1; i >= 0; i--)
        {
            if (Children[i].HandleMouseDown(x, y))
                return true;
        }
        
        // 然后检查滚动条
        if (ContentHeight <= Height || !ShowScrollbar)
            return false;
        
        // 内容区域高度
        float contentAreaHeight = Height - PaddingTop - PaddingBottom;
        
        // 检查是否点击在滚动条区域
        float scrollbarWidth = 6;
        float paddingRight = 4;
        float scrollbarX = X + Width - PaddingRight - scrollbarWidth - paddingRight;
        
        if (x >= scrollbarX && x <= scrollbarX + scrollbarWidth &&
            y >= Y + PaddingTop && y <= Y + PaddingTop + contentAreaHeight)
        {
            // 检查是否点击在滑块上
            float scrollbarHeight = GetScrollbarHeight();
            float contentVisibleRatio = contentAreaHeight / ContentHeight;
            float scrollRatio = ScrollOffset / (ContentHeight - contentAreaHeight);
            float scrollbarY = Y + PaddingTop + scrollRatio * (contentAreaHeight - scrollbarHeight);
            
            if (y >= scrollbarY && y <= scrollbarY + scrollbarHeight)
            {
                // 点击在滑块上，开始拖动
                _isDraggingScrollbar = true;
                _dragStartY = y;
                _dragStartOffset = ScrollOffset;
                return true;
            }
            else
            {
                // 点击在空白区域，滚动一页
                float pageScrollAmount = contentAreaHeight * 0.8f; // 滚动 80% 的可视区域
                if (y < scrollbarY)
                {
                    // 点击在滑块上方，向上滚动
                    ScrollOffset = Math.Max(0, ScrollOffset - pageScrollAmount);
                }
                else
                {
                    // 点击在滑块下方，向下滚动
                    ScrollOffset = Math.Min(ContentHeight - contentAreaHeight, ScrollOffset + pageScrollAmount);
                }
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 处理鼠标移动事件
    /// </summary>
    public override bool HandleMouseMove(float x, float y)
    {
        // 首先处理子元素
        bool childHandled = false;
        for (int i = Children.Count - 1; i >= 0; i--)
        {
            if (Children[i].HandleMouseMove(x, y))
                childHandled = true;
        }
        
        // 内容区域高度
        float contentAreaHeight = Height - PaddingTop - PaddingBottom;
        
        // 处理拖动
        if (_isDraggingScrollbar)
        {
            float deltaY = y - _dragStartY;
            float scrollRatio = deltaY / (contentAreaHeight - GetScrollbarHeight());
            float newOffset = _dragStartOffset + scrollRatio * (ContentHeight - contentAreaHeight);
            
            ScrollOffset = Math.Clamp(newOffset, 0, ContentHeight - contentAreaHeight);
            return true;
        }
        
        // 检测是否悬停在滚动条上
        if (ContentHeight <= contentAreaHeight || !ShowScrollbar)
            return childHandled;
        
        float scrollbarWidth = 6;
        float paddingRight = 4;
        float scrollbarX = X + Width - PaddingRight - scrollbarWidth - paddingRight;
        
        bool wasHovering = _isHoveringScrollbar;
        _isHoveringScrollbar = (x >= scrollbarX && x <= scrollbarX + scrollbarWidth &&
                                y >= Y + PaddingTop && y <= Y + PaddingTop + contentAreaHeight);
        
        return childHandled || _isHoveringScrollbar != wasHovering;
    }
    
    /// <summary>
    /// 处理鼠标释放事件
    /// </summary>
    public override void HandleMouseUp()
    {
        base.HandleMouseUp();
        _isDraggingScrollbar = false;
    }
    
    /// <summary>
    /// 处理鼠标离开事件
    /// </summary>
    public override void HandleMouseLeave()
    {
        base.HandleMouseLeave();
        _isHoveringScrollbar = false;
    }
    
    private float GetScrollbarHeight()
    {
        float contentAreaHeight = Height - PaddingTop - PaddingBottom;
        float contentVisibleRatio = contentAreaHeight / ContentHeight;
        return Math.Max(30, contentAreaHeight * contentVisibleRatio);
    }
}