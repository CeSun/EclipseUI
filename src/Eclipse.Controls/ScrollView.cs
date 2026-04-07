using Eclipse.Core;
using Eclipse.Core.Abstractions;
using Eclipse.Input;
using Eclipse.Rendering;
using System.Collections.Generic;

namespace Eclipse.Controls;

/// <summary>
/// 滚动视图 - 可滚动容器，支持水平和垂直滚动
/// </summary>
public class ScrollView : ComponentBase
{
    private Rect _bounds;
    private Size _contentSize = Size.Zero;
    private double _scrollX = 0;
    private double _scrollY = 0;
    private double _maxScrollX = 0;
    private double _maxScrollY = 0;
    
    // 滚动条状态
    private bool _verticalScrollBarHovered = false;
    private bool _horizontalScrollBarHovered = false;
    private bool _verticalThumbDragging = false;
    private bool _horizontalThumbDragging = false;
    private double _dragStartOffset = 0;
    private double _dragStartThumbPosition = 0;
    
    // 滚动条样式
    private double _scrollBarOpacity = 0.0; // 用于淡入淡出动画
    private DateTime _lastScrollTime = DateTime.MinValue;
    private const double ScrollBarFadeDelayMs = 1500; // 滚动后多久开始淡出
    private const double ScrollBarFadeDurationMs = 300; // 淡出动画时长
    
    #region 属性
    
    /// <summary>
    /// 是否显示水平滚动条
    /// </summary>
    public bool HorizontalScrollBarVisible { get; set; } = false;
    
    /// <summary>
    /// 是否显示垂直滚动条
    /// </summary>
    public bool VerticalScrollBarVisible { get; set; } = true;
    
    /// <summary>
    /// 水平滚动偏移
    /// </summary>
    public double ScrollX
    {
        get => _scrollX;
        set
        {
            var newValue = Math.Clamp(value, 0, _maxScrollX);
            if (Math.Abs(_scrollX - newValue) > 0.001)
            {
                _scrollX = newValue;
                OnScrollChanged();
            }
        }
    }
    
    /// <summary>
    /// 垂直滚动偏移
    /// </summary>
    public double ScrollY
    {
        get => _scrollY;
        set
        {
            var newValue = Math.Clamp(value, 0, _maxScrollY);
            if (Math.Abs(_scrollY - newValue) > 0.001)
            {
                _scrollY = newValue;
                OnScrollChanged();
            }
        }
    }
    
    /// <summary>
    /// 最大水平滚动偏移
    /// </summary>
    public double MaxScrollX => _maxScrollX;
    
    /// <summary>
    /// 最大垂直滚动偏移
    /// </summary>
    public double MaxScrollY => _maxScrollY;
    
    /// <summary>
    /// 内容尺寸
    /// </summary>
    public Size ContentSize => _contentSize;
    
    /// <summary>
    /// 视口宽度
    /// </summary>
    public double ViewportWidth => _bounds.Width;
    
    /// <summary>
    /// 视口高度
    /// </summary>
    public double ViewportHeight => _bounds.Height;
    
    /// <summary>
    /// 滚动条宽度（像素）
    /// </summary>
    public double ScrollBarWidth { get; set; } = 10;
    
    /// <summary>
    /// 滚动条颜色
    /// </summary>
    public Color ScrollBarColor { get; set; } = Color.Gray;
    
    /// <summary>
    /// 滚动条悬停颜色
    /// </summary>
    public Color ScrollBarHoverColor { get; set; } = Color.DarkGray;
    
    /// <summary>
    /// 滚动条背景颜色
    /// </summary>
    public Color ScrollBarTrackColor { get; set; } = Color.LightGray;
    
    /// <summary>
    /// 滚动条圆角半径
    /// </summary>
    public double ScrollBarCornerRadius { get; set; } = 5;
    
    /// <summary>
    /// 背景颜色
    /// </summary>
    public Color BackgroundColor { get; set; } = Color.Transparent;
    
    /// <summary>
    /// 内容内边距
    /// </summary>
    public double Padding { get; set; } = 0;
    
    /// <summary>
    /// 鼠标滚轮滚动步长（像素）
    /// </summary>
    public double ScrollStep { get; set; } = 50;
    
    /// <summary>
    /// 是否启用惯性滚动
    /// </summary>
    public bool EnableInertia { get; set; } = false;
    
    /// <summary>
    /// 是否显示滚动条（自动隐藏）
    /// </summary>
    public bool AutoHideScrollBar { get; set; } = true;
    
    /// <summary>
    /// 滚动位置改变事件
    /// </summary>
    public event EventHandler<ScrollChangedEventArgs>? ScrollChanged;
    
    #endregion
    
    #region 输入元素接口实现
    
    public override bool IsVisible => true;
    
    protected override IEnumerable<IInputElement> GetInputChildren()
    {
        // 滚动视图的子元素可能也需要接收输入
        foreach (var child in Children)
        {
            if (child is IInputElement inputElement)
            {
                yield return inputElement;
            }
        }
    }
    
    public override void Build(IBuildContext context) { }
    
    #endregion
    
    #region 测量和布局
    
    /// <summary>
    /// 测量内容所需尺寸
    /// ScrollView 应该使用 availableSize 作为视口大小，而不是内容大小
    /// </summary>
    public override Size Measure(Size availableSize, IDrawingContext context)
    {
        // 测量所有子元素以确定内容大小（使用无限空间测量内容实际尺寸）
        double maxWidth = 0;
        double maxHeight = 0;

        foreach (var child in Children)
        {
            var childSize = child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity), context);
            maxWidth = Math.Max(maxWidth, childSize.Width);
            maxHeight = Math.Max(maxHeight, childSize.Height);
        }

        _contentSize = new Size(maxWidth + Padding * 2 * context.Scale, maxHeight + Padding * 2 * context.Scale);

        // ScrollView 返回父布局提供的可用空间作为视口大小
        // 只有当父布局没有提供可用空间时，才使用内容大小
        double viewportWidth = availableSize.IsEmpty || availableSize.Width <= 0
            ? _contentSize.Width
            : availableSize.Width;
        double viewportHeight = availableSize.IsEmpty || availableSize.Height <= 0
            ? _contentSize.Height
            : availableSize.Height;

        // 不要用内容大小限制视口，视口可以小于内容（需要滚动）
        // 但视口不应小于合理的最小值
        viewportWidth = Math.Max(viewportWidth, 50 * context.Scale);
        viewportHeight = Math.Max(viewportHeight, 50 * context.Scale);

        return new Size(viewportWidth, viewportHeight);
    }
    
    /// <summary>
    /// 安排子元素位置
    /// </summary>
    public override void Arrange(Rect finalBounds, IDrawingContext context)
    {
        _bounds = finalBounds;
        base.Arrange(finalBounds, context);
        
        // 计算最大滚动范围
        _maxScrollX = Math.Max(0, _contentSize.Width - finalBounds.Width);
        _maxScrollY = Math.Max(0, _contentSize.Height - finalBounds.Height);
        
        // 限制滚动范围
        _scrollX = Math.Clamp(_scrollX, 0, _maxScrollX);
        _scrollY = Math.Clamp(_scrollY, 0, _maxScrollY);
        
        // 安排子元素 - 子元素应该占据内容区域，而不是视口
        var scaledPadding = Padding * context.Scale;
        var contentBounds = new Rect(
            finalBounds.X + scaledPadding,
            finalBounds.Y + scaledPadding,
            _contentSize.Width - scaledPadding * 2,
            _contentSize.Height - scaledPadding * 2);
        
        foreach (var child in Children)
        {
            child.Arrange(contentBounds, context);
        }
    }
    
    #endregion
    
    #region 渲染
    
    public override void Render(IDrawingContext context, Rect bounds)
    {
        UpdateBounds(bounds);

        var scaledPadding = Padding * context.Scale;
        var scaledScrollBarWidth = ScrollBarWidth * context.Scale;
        var scaledCornerRadius = ScrollBarCornerRadius * context.Scale;

        // 绘制背景
        if (BackgroundColor != Color.Transparent)
        {
            context.DrawRectangle(bounds, BackgroundColor);
        }

        // 视口区域（子元素可见区域）
        var viewportBounds = new Rect(
            bounds.X + scaledPadding,
            bounds.Y + scaledPadding,
            bounds.Width - scaledPadding * 2,
            bounds.Height - scaledPadding * 2);

        // 推入裁剪区域，只在视口内渲染
        context.PushClip(viewportBounds);

        try
        {
            // 渲染子元素（应用滚动偏移）
            // 每个子元素根据其在内容中的位置和滚动偏移来渲染
            double childY = 0;
            foreach (var child in Children)
            {
                // 获取子元素尺寸
                var childSize = GetChildRenderSize(child);

                // 计算子元素在视口中的位置（考虑滚动偏移）
                var childX = viewportBounds.X;
                var renderY = viewportBounds.Y + childY - _scrollY;
                var renderHeight = childSize.Height;

                // 只有当子元素在视口内时才渲染
                if (renderY + renderHeight >= viewportBounds.Y && renderY <= viewportBounds.Y + viewportBounds.Height)
                {
                    var childBounds = new Rect(childX, renderY, viewportBounds.Width, renderHeight);
                    child.Render(context, childBounds);
                }

                childY += childSize.Height;
            }
        }
        finally
        {
            // 弹出裁剪区域
            context.PopClip();
        }
        
        // 更新滚动条可见性状态
        UpdateScrollBarVisibility();
        
        // 计算滚动条透明度
        var opacity = CalculateScrollBarOpacity();
        
        // 绘制滚动条
        if (VerticalScrollBarVisible && _maxScrollY > 0)
        {
            DrawVerticalScrollBar(context, bounds, scaledScrollBarWidth, scaledCornerRadius, opacity);
        }
        
        if (HorizontalScrollBarVisible && _maxScrollX > 0)
        {
            DrawHorizontalScrollBar(context, bounds, scaledScrollBarWidth, scaledCornerRadius, opacity);
        }
    }
    
    /// <summary>
    /// 获取子元素渲染尺寸
    /// 使用 Arrange 后的 Bounds，避免重复测量
    /// </summary>
    private Size GetChildRenderSize(IComponent child)
    {
        // 使用 Arrange 后的 Bounds（Bounds 在 ComponentBase 中定义）
        if (child is ComponentBase component && !component.Bounds.IsEmpty)
        {
            return new Size(component.Bounds.Width, component.Bounds.Height);
        }
        return Size.Zero;
    }
    
    private void UpdateScrollBarVisibility()
    {
        if (!AutoHideScrollBar)
        {
            _scrollBarOpacity = 1.0;
            return;
        }
        
        var now = DateTime.Now;
        var elapsed = (now - _lastScrollTime).TotalMilliseconds;
        
        if (elapsed < ScrollBarFadeDelayMs)
        {
            // 滚动后保持显示
            _scrollBarOpacity = 1.0;
        }
        else if (elapsed < ScrollBarFadeDelayMs + ScrollBarFadeDurationMs)
        {
            // 淡出中
            var fadeProgress = (elapsed - ScrollBarFadeDelayMs) / ScrollBarFadeDurationMs;
            _scrollBarOpacity = Math.Max(0, 1.0 - fadeProgress);
        }
        else
        {
            _scrollBarOpacity = 0;
        }
        
        // 悬停时保持显示
        if (_verticalScrollBarHovered || _horizontalScrollBarHovered || 
            _verticalThumbDragging || _horizontalThumbDragging)
        {
            _scrollBarOpacity = 1.0;
        }
    }
    
    private double CalculateScrollBarOpacity()
    {
        return _scrollBarOpacity;
    }
    
    private void DrawVerticalScrollBar(IDrawingContext context, Rect bounds, double scrollBarWidth, double cornerRadius, double opacity)
    {
        if (opacity <= 0) return;
        
        var scrollBarBounds = new Rect(
            bounds.X + bounds.Width - scrollBarWidth,
            bounds.Y,
            scrollBarWidth,
            bounds.Height);
        
        var thumbHeight = Math.Max(20 * context.Scale, bounds.Height * (bounds.Height / _contentSize.Height));
        var thumbY = bounds.Y + (bounds.Height - thumbHeight) * (_scrollY / _maxScrollY);
        
        _verticalThumbBounds = new Rect(scrollBarBounds.X, thumbY, scrollBarWidth, thumbHeight);
        
        var trackColor = Color.FromArgb((int)(opacity * 0.5 * 255), ScrollBarTrackColor);
        context.DrawRectangle(scrollBarBounds, trackColor, null, 0, cornerRadius);
        
        var thumbColor = _verticalThumbDragging 
            ? ScrollBarHoverColor
            : (_verticalScrollBarHovered ? ScrollBarHoverColor : ScrollBarColor);
        
        thumbColor = Color.FromArgb((int)(opacity * 255), thumbColor);
        
        var thumbBounds = new Rect(
            scrollBarBounds.X,
            thumbY,
            scrollBarWidth,
            thumbHeight);
        
        context.DrawRectangle(thumbBounds, thumbColor, null, 0, cornerRadius);
    }
    
    private void DrawHorizontalScrollBar(IDrawingContext context, Rect bounds, double scrollBarWidth, double cornerRadius, double opacity)
    {
        if (opacity <= 0) return;
        
        var scrollBarBounds = new Rect(
            bounds.X,
            bounds.Y + bounds.Height - scrollBarWidth,
            bounds.Width,
            scrollBarWidth);
        
        var thumbWidth = Math.Max(20 * context.Scale, bounds.Width * (bounds.Width / _contentSize.Width));
        var thumbX = bounds.X + (bounds.Width - thumbWidth) * (_scrollX / _maxScrollX);
        
        _horizontalThumbBounds = new Rect(thumbX, scrollBarBounds.Y, thumbWidth, scrollBarWidth);
        
        var trackColor = Color.FromArgb((int)(opacity * 0.5 * 255), ScrollBarTrackColor);
        context.DrawRectangle(scrollBarBounds, trackColor, null, 0, cornerRadius);
        
        var thumbColor = _horizontalThumbDragging 
            ? ScrollBarHoverColor
            : (_horizontalScrollBarHovered ? ScrollBarHoverColor : ScrollBarColor);
        
        thumbColor = Color.FromArgb((int)(opacity * 255), thumbColor);
        
        var thumbBounds = new Rect(
            thumbX,
            scrollBarBounds.Y,
            thumbWidth,
            scrollBarWidth);
        
        context.DrawRectangle(thumbBounds, thumbColor, null, 0, cornerRadius);
    }
    
    private static Color ApplyOpacity(Color color, double opacity)
    {
        return Color.FromArgb((int)(opacity * 255), color);
    }
    
    #endregion
    
    #region 滚动控制
    
    /// <summary>
    /// 滚动到指定位置
    /// </summary>
    public void ScrollTo(double x, double y)
    {
        var oldScrollX = _scrollX;
        var oldScrollY = _scrollY;
        
        _scrollX = Math.Clamp(x, 0, _maxScrollX);
        _scrollY = Math.Clamp(y, 0, _maxScrollY);
        
        if (Math.Abs(oldScrollX - _scrollX) > 0.001 || Math.Abs(oldScrollY - _scrollY) > 0.001)
        {
            OnScrollChanged();
        }
    }
    
    /// <summary>
    /// 滚动指定的增量
    /// </summary>
    public void ScrollBy(double deltaX, double deltaY)
    {
        ScrollTo(_scrollX + deltaX, _scrollY + deltaY);
    }
    
    /// <summary>
    /// 滚动到顶部
    /// </summary>
    public void ScrollToTop()
    {
        ScrollTo(_scrollX, 0);
    }
    
    /// <summary>
    /// 滚动到底部
    /// </summary>
    public void ScrollToBottom()
    {
        ScrollTo(_scrollX, _maxScrollY);
    }
    
    /// <summary>
    /// 滚动到左侧
    /// </summary>
    public void ScrollToLeft()
    {
        ScrollTo(0, _scrollY);
    }
    
    /// <summary>
    /// 滚动到右侧
    /// </summary>
    public void ScrollToRight()
    {
        ScrollTo(_maxScrollX, _scrollY);
    }
    
    /// <summary>
    /// 滚动使指定元素可见
    /// </summary>
    public void ScrollIntoView(Rect elementBounds)
    {
        // 垂直滚动
        if (elementBounds.Y < _scrollY)
        {
            _scrollY = elementBounds.Y;
        }
        else if (elementBounds.Bottom > _scrollY + _bounds.Height)
        {
            _scrollY = elementBounds.Bottom - _bounds.Height;
        }
        
        // 水平滚动
        if (elementBounds.X < _scrollX)
        {
            _scrollX = elementBounds.X;
        }
        else if (elementBounds.Right > _scrollX + _bounds.Width)
        {
            _scrollX = elementBounds.Right - _bounds.Width;
        }
        
        _scrollX = Math.Clamp(_scrollX, 0, _maxScrollX);
        _scrollY = Math.Clamp(_scrollY, 0, _maxScrollY);
        OnScrollChanged();
    }
    
    private void OnScrollChanged()
    {
        _lastScrollTime = DateTime.Now;
        _scrollBarOpacity = 1.0;
        ScrollChanged?.Invoke(this, new ScrollChangedEventArgs(_scrollX, _scrollY, _maxScrollX, _maxScrollY));
        StateHasChanged();
    }
    
    #endregion
    
    #region 输入处理
    
    // 用于命中测试的滑块区域缓存
    private Rect _verticalThumbBounds;
    private Rect _horizontalThumbBounds;
    
    public override bool HitTest(Point point)
    {
        if (!IsVisible || !IsHitTestVisible)
            return false;
        
        return Bounds.Contains(point);
    }
    
    /// <summary>
    /// 初始化输入事件处理
    /// </summary>
    public ScrollView()
    {
        // 订阅指针事件
        PointerWheelChanged += OnPointerWheelChanged;
        PointerPressed += OnPointerPressedHandler;
        PointerMoved += OnPointerMovedHandler;
        PointerReleased += OnPointerReleasedHandler;
        PointerEntered += OnPointerEnteredHandler;
        PointerExited += OnPointerExitedHandler;
    }
    
    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (!IsInputEnabled) return;
        
        // 计算滚动增量
        var deltaY = -e.Delta.Y * ScrollStep;
        var deltaX = -e.Delta.X * ScrollStep;
        
        // 优先处理垂直滚动
        if (_maxScrollY > 0 && Math.Abs(deltaY) > 0.001)
        {
            ScrollBy(0, deltaY);
            e.Handled = true;
        }
        
        // 水平滚动（如果有水平滚动条或按住 Shift）
        if (_maxScrollX > 0 && Math.Abs(deltaX) > 0.001)
        {
            ScrollBy(deltaX, 0);
            e.Handled = true;
        }
    }
    
    private void OnPointerPressedHandler(object? sender, PointerPressedEventArgs e)
    {
        if (!IsInputEnabled) return;
        
        var point = e.Position;
        var scaledScrollBarWidth = ScrollBarWidth;
        
        // 检查是否点击垂直滚动条
        if (VerticalScrollBarVisible && _maxScrollY > 0)
        {
            var vScrollBarBounds = new Rect(
                _bounds.X + _bounds.Width - scaledScrollBarWidth,
                _bounds.Y,
                scaledScrollBarWidth,
                _bounds.Height);
            
            if (vScrollBarBounds.Contains(point))
            {
                // 检查是否点击滑块
                if (_verticalThumbBounds.Contains(point))
                {
                    // 开始拖动滑块
                    _verticalThumbDragging = true;
                    _dragStartOffset = point.Y;
                    _dragStartThumbPosition = _scrollY;
                    e.Capture(this);
                }
                else
                {
                    // 点击轨道，跳转到对应位置
                    var thumbHeight = Math.Max(20, _bounds.Height * (_bounds.Height / _contentSize.Height));
                    var clickY = point.Y - _bounds.Y;
                    var newScrollY = (clickY / _bounds.Height) * _maxScrollY;
                    
                    // 使滑块中心移动到点击位置
                    var thumbCenterOffset = thumbHeight / 2 / _bounds.Height * _maxScrollY;
                    ScrollTo(_scrollX, newScrollY - thumbCenterOffset);
                }
                
                e.Handled = true;
                return;
            }
        }
        
        // 检查是否点击水平滚动条
        if (HorizontalScrollBarVisible && _maxScrollX > 0)
        {
            var hScrollBarBounds = new Rect(
                _bounds.X,
                _bounds.Y + _bounds.Height - scaledScrollBarWidth,
                _bounds.Width,
                scaledScrollBarWidth);
            
            if (hScrollBarBounds.Contains(point))
            {
                // 检查是否点击滑块
                if (_horizontalThumbBounds.Contains(point))
                {
                    // 开始拖动滑块
                    _horizontalThumbDragging = true;
                    _dragStartOffset = point.X;
                    _dragStartThumbPosition = _scrollX;
                    e.Capture(this);
                }
                else
                {
                    // 点击轨道，跳转到对应位置
                    var thumbWidth = Math.Max(20, _bounds.Width * (_bounds.Width / _contentSize.Width));
                    var clickX = point.X - _bounds.X;
                    var newScrollX = (clickX / _bounds.Width) * _maxScrollX;
                    
                    // 使滑块中心移动到点击位置
                    var thumbCenterOffset = thumbWidth / 2 / _bounds.Width * _maxScrollX;
                    ScrollTo(newScrollX - thumbCenterOffset, _scrollY);
                }
                
                e.Handled = true;
                return;
            }
        }
    }
    
    private void OnPointerMovedHandler(object? sender, PointerEventArgs e)
    {
        if (!IsInputEnabled) return;
        
        var point = e.Position;
        var scaledScrollBarWidth = ScrollBarWidth;
        
        // 处理拖动
        if (_verticalThumbDragging)
        {
            // 计算拖动距离对应的滚动距离
            var deltaY = point.Y - _dragStartOffset;
            var scrollRatio = _maxScrollY / (_bounds.Height - GetVerticalThumbHeight());
            ScrollTo(_scrollX, _dragStartThumbPosition + deltaY * scrollRatio);
            e.Handled = true;
            return;
        }
        
        if (_horizontalThumbDragging)
        {
            var deltaX = point.X - _dragStartOffset;
            var scrollRatio = _maxScrollX / (_bounds.Width - GetHorizontalThumbWidth());
            ScrollTo(_dragStartThumbPosition + deltaX * scrollRatio, _scrollY);
            e.Handled = true;
            return;
        }
        
        // 更新悬停状态
        bool vHovered = false;
        bool hHovered = false;
        
        if (VerticalScrollBarVisible && _maxScrollY > 0)
        {
            var vScrollBarBounds = new Rect(
                _bounds.X + _bounds.Width - scaledScrollBarWidth,
                _bounds.Y,
                scaledScrollBarWidth,
                _bounds.Height);
            
            vHovered = vScrollBarBounds.Contains(point);
        }
        
        if (HorizontalScrollBarVisible && _maxScrollX > 0)
        {
            var hScrollBarBounds = new Rect(
                _bounds.X,
                _bounds.Y + _bounds.Height - scaledScrollBarWidth,
                _bounds.Width,
                scaledScrollBarWidth);
            
            hHovered = hScrollBarBounds.Contains(point);
        }
        
        if (_verticalScrollBarHovered != vHovered || _horizontalScrollBarHovered != hHovered)
        {
            _verticalScrollBarHovered = vHovered;
            _horizontalScrollBarHovered = hHovered;
            StateHasChanged();
        }
    }
    
    private void OnPointerReleasedHandler(object? sender, PointerReleasedEventArgs e)
    {
        if (_verticalThumbDragging || _horizontalThumbDragging)
        {
            _verticalThumbDragging = false;
            _horizontalThumbDragging = false;
            e.Handled = true;
            StateHasChanged();
        }
    }
    
    private void OnPointerEnteredHandler(object? sender, PointerEventArgs e)
    {
        // 鼠标进入时显示滚动条
        _scrollBarOpacity = 1.0;
        StateHasChanged();
    }
    
    private void OnPointerExitedHandler(object? sender, PointerEventArgs e)
    {
        _verticalScrollBarHovered = false;
        _horizontalScrollBarHovered = false;
        
        if (!_verticalThumbDragging && !_horizontalThumbDragging)
        {
            StateHasChanged();
        }
    }
    
    private double GetVerticalThumbHeight()
    {
        if (_contentSize.Height <= 0) return 0;
        return Math.Max(20, _bounds.Height * (_bounds.Height / _contentSize.Height));
    }
    
    private double GetHorizontalThumbWidth()
    {
        if (_contentSize.Width <= 0) return 0;
        return Math.Max(20, _bounds.Width * (_bounds.Width / _contentSize.Width));
    }
    
    #endregion
    
    #region 兼容旧 API
    
    /// <summary>
    /// 处理滚轮事件（兼容旧 API）
    /// </summary>
    [Obsolete("Use PointerWheelChanged event instead")]
    public void OnPointerWheel(PointerWheelEventArgs e)
    {
        OnPointerWheelChanged(this, e);
    }
    
    #endregion
}

/// <summary>
/// 滚动位置改变事件参数
/// </summary>
public class ScrollChangedEventArgs : EventArgs
{
    /// <summary>
    /// 当前水平滚动位置
    /// </summary>
    public double ScrollX { get; }
    
    /// <summary>
    /// 当前垂直滚动位置
    /// </summary>
    public double ScrollY { get; }
    
    /// <summary>
    /// 最大水平滚动位置
    /// </summary>
    public double MaxScrollX { get; }
    
    /// <summary>
    /// 最大垂直滚动位置
    /// </summary>
    public double MaxScrollY { get; }
    
    /// <summary>
    /// 是否已滚动到顶部
    /// </summary>
    public bool IsAtTop => ScrollY <= 0;
    
    /// <summary>
    /// 是否已滚动到底部
    /// </summary>
    public bool IsAtBottom => ScrollY >= MaxScrollY;
    
    /// <summary>
    /// 是否已滚动到左侧
    /// </summary>
    public bool IsAtLeft => ScrollX <= 0;
    
    /// <summary>
    /// 是否已滚动到右侧
    /// </summary>
    public bool IsAtRight => ScrollX >= MaxScrollX;
    
    public ScrollChangedEventArgs(double scrollX, double scrollY, double maxScrollX, double maxScrollY)
    {
        ScrollX = scrollX;
        ScrollY = scrollY;
        MaxScrollX = maxScrollX;
        MaxScrollY = maxScrollY;
    }
}