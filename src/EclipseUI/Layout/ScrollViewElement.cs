using SkiaSharp;
using EclipseUI.Core;

namespace EclipseUI.Layout;

/// <summary>
/// 滚动方向
/// </summary>
public enum ScrollOrientation
{
    Vertical,
    Horizontal
}

/// <summary>
/// 滚动条可见性模式
/// </summary>
public enum ScrollbarVisibility
{
    /// <summary>
    /// 始终显示
    /// </summary>
    Visible,
    /// <summary>
    /// 始终隐藏
    /// </summary>
    Hidden,
    /// <summary>
    /// 自动隐藏（滚动或悬停时显示）
    /// </summary>
    Auto
}

/// <summary>
/// 滚动视图元素
/// </summary>
public class ScrollViewElement : EclipseElement
{
    /// <summary>
    /// 滚动方向
    /// </summary>
    public ScrollOrientation Orientation { get; set; } = ScrollOrientation.Vertical;
    
    /// <summary>
    /// 滚动偏移量
    /// </summary>
    public float ScrollOffset { get; set; }
    
    /// <summary>
    /// 内容总尺寸（根据方向为高度或宽度）
    /// </summary>
    public float ContentSize { get; private set; }
    
    /// <summary>
    /// 是否显示滚动条（兼容旧属性）
    /// </summary>
    public bool ShowScrollbar { get; set; } = true;
    
    /// <summary>
    /// 滚动条可见性模式
    /// </summary>
    public ScrollbarVisibility ScrollbarVisibility { get; set; } = ScrollbarVisibility.Visible;
    
    // 自动隐藏相关
    private long _lastScrollTime;
    private const int AutoHideDelayMs = 1500; // 1.5秒后自动隐藏
    
    // 滚动条常量
    private const float ScrollbarWidth = 8;
    private const float ScrollbarMargin = 2;
    
    public override SKSize Measure(SKCanvas canvas, float availableWidth, float availableHeight)
    {
        float contentWidth = availableWidth - PaddingLeft - PaddingRight;
        float contentHeight = availableHeight - PaddingTop - PaddingBottom;
        
        if (MaxWidth.HasValue)
            contentWidth = Math.Min(contentWidth, MaxWidth.Value - PaddingLeft - PaddingRight);
        if (MaxHeight.HasValue)
            contentHeight = Math.Min(contentHeight, MaxHeight.Value - PaddingTop - PaddingBottom);
        
        float measuredContentHeight = 0;
        float measuredContentWidth = 0;
        
        if (Orientation == ScrollOrientation.Vertical)
        {
            // 垂直滚动：宽度固定，高度无限
            float measureWidth = float.IsPositiveInfinity(contentWidth) ? 800 : contentWidth;
            foreach (var child in Children)
            {
                var childSize = child.Measure(canvas, measureWidth, float.PositiveInfinity);
                measuredContentWidth = Math.Max(measuredContentWidth, childSize.Width);
                measuredContentHeight += childSize.Height;
            }
            ContentSize = measuredContentHeight;
        }
        else
        {
            // 水平滚动：高度固定，宽度无限
            float measureHeight = float.IsPositiveInfinity(contentHeight) ? 600 : contentHeight;
            foreach (var child in Children)
            {
                var childSize = child.Measure(canvas, float.PositiveInfinity, measureHeight);
                measuredContentWidth += childSize.Width;
                measuredContentHeight = Math.Max(measuredContentHeight, childSize.Height);
            }
            ContentSize = measuredContentWidth;
        }
        
        float finalWidth, finalHeight;
        
        if (RequestedWidth.HasValue)
            finalWidth = RequestedWidth.Value;
        else if (float.IsPositiveInfinity(availableWidth))
            finalWidth = measuredContentWidth + PaddingLeft + PaddingRight;
        else
            finalWidth = availableWidth;
        
        if (RequestedHeight.HasValue)
            finalHeight = RequestedHeight.Value;
        else if (float.IsPositiveInfinity(availableHeight))
            finalHeight = measuredContentHeight + PaddingTop + PaddingBottom;
        else if (Orientation == ScrollOrientation.Horizontal)
            // 水平滚动时，高度应该是内容高度，而不是可用高度
            finalHeight = measuredContentHeight + PaddingTop + PaddingBottom;
        else
            finalHeight = availableHeight;
        
        if (MinWidth.HasValue) finalWidth = Math.Max(finalWidth, MinWidth.Value);
        if (MinHeight.HasValue) finalHeight = Math.Max(finalHeight, MinHeight.Value);
        if (MaxWidth.HasValue) finalWidth = Math.Min(finalWidth, MaxWidth.Value);
        if (MaxHeight.HasValue) finalHeight = Math.Min(finalHeight, MaxHeight.Value);
        
        return new SKSize(finalWidth, finalHeight);
    }
    
    public override void Arrange(SKCanvas canvas, float x, float y, float width, float height)
    {
        base.Arrange(canvas, x, y, width, height);
        
        float viewportSize = Orientation == ScrollOrientation.Vertical
            ? height - PaddingTop - PaddingBottom
            : width - PaddingLeft - PaddingRight;
        
        float maxScroll = Math.Max(0, ContentSize - viewportSize);
        ScrollOffset = Math.Clamp(ScrollOffset, 0, maxScroll);
        
        float contentWidth = width - PaddingLeft - PaddingRight;
        float contentHeight = height - PaddingTop - PaddingBottom;
        
        if (Orientation == ScrollOrientation.Vertical)
        {
            float currentY = y + PaddingTop - ScrollOffset;
            foreach (var child in Children)
            {
                var childSize = child.Measure(canvas, contentWidth, float.PositiveInfinity);
                child.Arrange(canvas, x + PaddingLeft, currentY, contentWidth, childSize.Height);
                currentY += childSize.Height;
            }
        }
        else
        {
            float currentX = x + PaddingLeft - ScrollOffset;
            foreach (var child in Children)
            {
                var childSize = child.Measure(canvas, float.PositiveInfinity, contentHeight);
                child.Arrange(canvas, currentX, y + PaddingTop, childSize.Width, contentHeight);
                currentX += childSize.Width;
            }
        }
    }
    
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
            
            var clipRect = new SKRect(
                X + PaddingLeft,
                Y + PaddingTop,
                X + Width - PaddingRight,
                Y + Height - PaddingBottom
            );
            canvas.ClipRect(clipRect);
            
            RenderChildren(canvas);
        }
        finally
        {
            canvas.Restore();
        }
        
        float viewportSize = Orientation == ScrollOrientation.Vertical
            ? Height - PaddingTop - PaddingBottom
            : Width - PaddingLeft - PaddingRight;
        
        // 判断是否显示滚动条
        bool shouldShowScrollbar = false;
        if (ContentSize > viewportSize)
        {
            switch (ScrollbarVisibility)
            {
                case ScrollbarVisibility.Visible:
                    shouldShowScrollbar = ShowScrollbar;
                    break;
                case ScrollbarVisibility.Hidden:
                    shouldShowScrollbar = false;
                    break;
                case ScrollbarVisibility.Auto:
                    // 自动模式：拖动中、悬停中、或最近滚动过
                    var currentTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    shouldShowScrollbar = _isDraggingScrollbar || _isHoveringScrollbar || 
                                         (currentTime - _lastScrollTime < AutoHideDelayMs);
                    break;
            }
        }
        
        if (shouldShowScrollbar)
        {
            RenderScrollbar(canvas);
        }
    }
    
    private void RenderScrollbar(SKCanvas canvas)
    {
        float viewportSize = Orientation == ScrollOrientation.Vertical
            ? Height - PaddingTop - PaddingBottom
            : Width - PaddingLeft - PaddingRight;
        
        float contentVisibleRatio = viewportSize / ContentSize;
        float scrollRatio = ScrollOffset / (ContentSize - viewportSize);
        float scrollbarLength = Math.Max(30, viewportSize * contentVisibleRatio);
        
        float scrollbarX, scrollbarY, scrollbarW, scrollbarH;
        
        var clipBounds = canvas.LocalClipBounds;
        
        if (Orientation == ScrollOrientation.Vertical)
        {
            float effectiveRight = Math.Min(X + Width, clipBounds.Right);
            scrollbarX = effectiveRight - ScrollbarWidth - ScrollbarMargin;
            scrollbarY = Y + PaddingTop + scrollRatio * (viewportSize - scrollbarLength);
            scrollbarW = ScrollbarWidth;
            scrollbarH = scrollbarLength;
            _renderedScrollbarX = scrollbarX;
            _renderedScrollbarY = Y + PaddingTop;
        }
        else
        {
            // 水平滚动条在 ScrollView 最底部边缘（Padding 外部）
            scrollbarX = X + PaddingLeft + scrollRatio * (viewportSize - scrollbarLength);
            scrollbarY = Y + Height - ScrollbarWidth - ScrollbarMargin;
            scrollbarW = scrollbarLength;
            scrollbarH = ScrollbarWidth;
            _renderedScrollbarX = X + PaddingLeft;
            _renderedScrollbarY = scrollbarY;
        }
        
        // 绘制轨道
        SKRect bgRect;
        if (Orientation == ScrollOrientation.Vertical)
            bgRect = new SKRect(scrollbarX, Y + PaddingTop, scrollbarX + ScrollbarWidth, Y + PaddingTop + viewportSize);
        else
            bgRect = new SKRect(X + PaddingLeft, scrollbarY, X + PaddingLeft + viewportSize, scrollbarY + ScrollbarWidth);
        
        using var bgPaint = new SKPaint { Color = new SKColor(0, 0, 0, 20), IsAntialias = true };
        canvas.DrawRoundRect(bgRect, ScrollbarWidth / 2, ScrollbarWidth / 2, bgPaint);
        
        // 绘制滑块
        byte alpha = _isDraggingScrollbar ? (byte)150 : (_isHoveringScrollbar ? (byte)120 : (byte)80);
        var thumbRect = new SKRect(scrollbarX, scrollbarY, scrollbarX + scrollbarW, scrollbarY + scrollbarH);
        using var thumbPaint = new SKPaint { Color = new SKColor(0, 0, 0, alpha), IsAntialias = true };
        canvas.DrawRoundRect(thumbRect, ScrollbarWidth / 2, ScrollbarWidth / 2, thumbPaint);
    }
    
    public override bool HandleMouseWheel(float x, float y, float deltaY)
    {
        // 只有鼠标在 ScrollView 区域内才处理滚轮
        var rect = new SKRect(X, Y, X + Width, Y + Height);
        if (!rect.Contains(x, y)) return false;
        
        // 先让子元素处理
        for (int i = Children.Count - 1; i >= 0; i--)
        {
            if (Children[i].HandleMouseWheel(x, y, deltaY))
                return true;
        }
        
        float viewportSize = Orientation == ScrollOrientation.Vertical
            ? Height - PaddingTop - PaddingBottom
            : Width - PaddingLeft - PaddingRight;
        
        if (ContentSize <= viewportSize)
            return false;
        
        float scrollAmount = -deltaY * 20;
        ScrollOffset += scrollAmount;
        ScrollOffset = Math.Clamp(ScrollOffset, 0, ContentSize - viewportSize);
        
        // 更新最后滚动时间（用于自动隐藏）
        _lastScrollTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        
        // 触发重绘
        RequestRedraw();
        
        return true;
    }
    
    private bool _isDraggingScrollbar;
    private float _dragStartPos;
    private float _dragStartOffset;
    private bool _isHoveringScrollbar;
    private float _renderedScrollbarX;
    private float _renderedScrollbarY;
    
    public override bool HandleClick(SKPoint point)
    {
        if (!IsVisible) return false;
        
        // 检查点击是否在 ScrollView 区域内
        var rect = new SKRect(X, Y, X + Width, Y + Height);
        if (!rect.Contains(point)) return false;
        
        // 传递给子元素处理
        for (int i = Children.Count - 1; i >= 0; i--)
        {
            if (Children[i].HandleClick(point)) return true;
        }
        
        return false;
    }
    
    public override bool HandleMouseDown(float x, float y)
    {
        for (int i = Children.Count - 1; i >= 0; i--)
        {
            if (Children[i].HandleMouseDown(x, y))
                return true;
        }
        
        float viewportSize = Orientation == ScrollOrientation.Vertical
            ? Height - PaddingTop - PaddingBottom
            : Width - PaddingLeft - PaddingRight;
        
        if (ContentSize <= viewportSize || !ShowScrollbar)
            return false;
        
        // 检查是否点击在滚动条区域
        bool inScrollbarArea;
        if (Orientation == ScrollOrientation.Vertical)
        {
            inScrollbarArea = x >= _renderedScrollbarX && x <= _renderedScrollbarX + ScrollbarWidth &&
                              y >= Y + PaddingTop && y <= Y + PaddingTop + viewportSize;
        }
        else
        {
            inScrollbarArea = x >= X + PaddingLeft && x <= X + PaddingLeft + viewportSize &&
                              y >= _renderedScrollbarY && y <= _renderedScrollbarY + ScrollbarWidth;
        }
        
        if (inScrollbarArea)
        {
            float scrollbarLength = Math.Max(30, viewportSize * (viewportSize / ContentSize));
            float scrollRatio = ScrollOffset / (ContentSize - viewportSize);
            float scrollbarPos = Orientation == ScrollOrientation.Vertical
                ? Y + PaddingTop + scrollRatio * (viewportSize - scrollbarLength)
                : X + PaddingLeft + scrollRatio * (viewportSize - scrollbarLength);
            
            float clickPos = Orientation == ScrollOrientation.Vertical ? y : x;
            
            if (clickPos >= scrollbarPos && clickPos <= scrollbarPos + scrollbarLength)
            {
                _isDraggingScrollbar = true;
                _dragStartPos = clickPos;
                _dragStartOffset = ScrollOffset;
                return true;
            }
            else
            {
                float pageScrollAmount = viewportSize * 0.8f;
                if (clickPos < scrollbarPos)
                    ScrollOffset = Math.Max(0, ScrollOffset - pageScrollAmount);
                else
                    ScrollOffset = Math.Min(ContentSize - viewportSize, ScrollOffset + pageScrollAmount);
                return true;
            }
        }
        
        return false;
    }
    
    public override bool HandleMouseMove(float x, float y)
    {
        bool childHandled = false;
        for (int i = Children.Count - 1; i >= 0; i--)
        {
            if (Children[i].HandleMouseMove(x, y))
                childHandled = true;
        }
        
        float viewportSize = Orientation == ScrollOrientation.Vertical
            ? Height - PaddingTop - PaddingBottom
            : Width - PaddingLeft - PaddingRight;
        
        if (_isDraggingScrollbar)
        {
            float currentPos = Orientation == ScrollOrientation.Vertical ? y : x;
            float deltaPos = currentPos - _dragStartPos;
            float scrollbarLength = Math.Max(30, viewportSize * (viewportSize / ContentSize));
            float scrollRatio = deltaPos / (viewportSize - scrollbarLength);
            float newOffset = _dragStartOffset + scrollRatio * (ContentSize - viewportSize);
            
            ScrollOffset = Math.Clamp(newOffset, 0, ContentSize - viewportSize);
            
            // 触发重绘
            RequestRedraw();
            
            return true;
        }
        
        if (ContentSize <= viewportSize || !ShowScrollbar)
            return childHandled;
        
        bool wasHovering = _isHoveringScrollbar;
        if (Orientation == ScrollOrientation.Vertical)
        {
            _isHoveringScrollbar = x >= _renderedScrollbarX && x <= _renderedScrollbarX + ScrollbarWidth &&
                                   y >= Y + PaddingTop && y <= Y + PaddingTop + viewportSize;
        }
        else
        {
            _isHoveringScrollbar = x >= X + PaddingLeft && x <= X + PaddingLeft + viewportSize &&
                                   y >= _renderedScrollbarY && y <= _renderedScrollbarY + ScrollbarWidth;
        }
        
        return childHandled || _isHoveringScrollbar != wasHovering;
    }
    
    public override void HandleMouseUp()
    {
        base.HandleMouseUp();
        _isDraggingScrollbar = false;
    }
    
    public override void HandleMouseLeave()
    {
        base.HandleMouseLeave();
        _isHoveringScrollbar = false;
    }
}
