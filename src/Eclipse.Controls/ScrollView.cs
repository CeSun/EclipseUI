using Eclipse.Core;
using Eclipse.Core.Abstractions;
using Eclipse.Input;
using Eclipse.Rendering;
using System.Collections.Generic;

namespace Eclipse.Controls;

/// <summary>
/// 滚动视图 - 可滚动容器
/// </summary>
public class ScrollView : InputElementBase
{
    private Rect _bounds;
    private Size _contentSize = Size.Zero;
    private double _scrollX = 0;
    private double _scrollY = 0;
    private double _maxScrollX = 0;
    private double _maxScrollY = 0;
    
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
    public double ScrollX => _scrollX;
    
    /// <summary>
    /// 垂直滚动偏移
    /// </summary>
    public double ScrollY => _scrollY;
    
    /// <summary>
    /// 滚动条宽度
    /// </summary>
    public double ScrollBarWidth { get; set; } = 10;
    
    /// <summary>
    /// 滚动条颜色
    /// </summary>
    public string? ScrollBarColor { get; set; } = "#CCCCCC";
    
    /// <summary>
    /// 背景颜色
    /// </summary>
    public string? BackgroundColor { get; set; }
    
    /// <summary>
    /// 内容内边距
    /// </summary>
    public double Padding { get; set; } = 0;
    
    public override bool IsVisible => true;
    public override Rect Bounds => _bounds;
    
    public void UpdateBounds(Rect bounds) => _bounds = bounds;
    
    protected override IEnumerable<IInputElement> GetInputChildren()
    {
        // 滚动视图的子元素通常不直接接收输入
        return Array.Empty<IInputElement>();
    }
    
    public override void Build(IBuildContext context) { }
    
    /// <summary>
    /// 测量内容所需尺寸
    /// </summary>
    public Size Measure(Size availableSize, IDrawingContext context)
    {
        // 测量所有子元素以确定内容大小
        double maxWidth = 0;
        double maxHeight = 0;
        
        foreach (var child in Children)
        {
            Size childSize;
            if (child is InteractiveControl interactiveControl)
            {
                childSize = interactiveControl.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity), context);
            }
            else if (child is StackLayout stackLayout)
            {
                childSize = stackLayout.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity), context);
            }
            else if (child is Label label)
            {
                childSize = label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity), context);
            }
            else
            {
                childSize = new Size(100 * context.Scale, 100 * context.Scale);
            }
            
            maxWidth = Math.Max(maxWidth, childSize.Width);
            maxHeight = Math.Max(maxHeight, childSize.Height);
        }
        
        _contentSize = new Size(maxWidth + Padding * 2 * context.Scale, maxHeight + Padding * 2 * context.Scale);
        return availableSize;
    }
    
    /// <summary>
    /// 安排子元素位置
    /// </summary>
    public void Arrange(Rect finalBounds, IDrawingContext context)
    {
        _bounds = finalBounds;
        
        // 计算最大滚动范围
        _maxScrollX = Math.Max(0, _contentSize.Width - finalBounds.Width);
        _maxScrollY = Math.Max(0, _contentSize.Height - finalBounds.Height);
        
        // 限制滚动范围
        _scrollX = Math.Clamp(_scrollX, 0, _maxScrollX);
        _scrollY = Math.Clamp(_scrollY, 0, _maxScrollY);
    }
    
    public override void Render(IDrawingContext context, Rect bounds)
    {
        UpdateBounds(bounds);
        
        var scaledPadding = Padding * context.Scale;
        var scaledScrollBarWidth = ScrollBarWidth * context.Scale;
        
        // 绘制背景
        if (!string.IsNullOrEmpty(BackgroundColor))
        {
            context.DrawRectangle(bounds, BackgroundColor);
        }
        
        // 计算内容可见区域
        var contentBounds = new Rect(
            bounds.X + scaledPadding - _scrollX,
            bounds.Y + scaledPadding - _scrollY,
            _contentSize.Width,
            _contentSize.Height);
        
        // 绘制子元素（应用滚动偏移）
        foreach (var child in Children)
        {
            child.Render(context, contentBounds);
        }
        
        // 绘制滚动条
        if (VerticalScrollBarVisible && _maxScrollY > 0)
        {
            DrawVerticalScrollBar(context, bounds, scaledScrollBarWidth);
        }
        
        if (HorizontalScrollBarVisible && _maxScrollX > 0)
        {
            DrawHorizontalScrollBar(context, bounds, scaledScrollBarWidth);
        }
    }
    
    private void DrawVerticalScrollBar(IDrawingContext context, Rect bounds, double scrollBarWidth)
    {
        // 滚动条区域
        var scrollBarBounds = new Rect(
            bounds.X + bounds.Width - scrollBarWidth,
            bounds.Y,
            scrollBarWidth,
            bounds.Height);
        
        // 滚动滑块高度
        var thumbHeight = bounds.Height * (bounds.Height / _contentSize.Height);
        var thumbY = bounds.Y + (bounds.Height - thumbHeight) * (_scrollY / _maxScrollY);
        
        // 绘制滚动条背景
        context.DrawRectangle(scrollBarBounds, "#EEEEEE");
        
        // 绘制滚动滑块
        var thumbBounds = new Rect(
            scrollBarBounds.X,
            thumbY,
            scrollBarWidth,
            thumbHeight);
        
        context.DrawRectangle(thumbBounds, ScrollBarColor ?? "#CCCCCC", null, 0, scrollBarWidth / 2);
    }
    
    private void DrawHorizontalScrollBar(IDrawingContext context, Rect bounds, double scrollBarWidth)
    {
        // 滚动条区域
        var scrollBarBounds = new Rect(
            bounds.X,
            bounds.Y + bounds.Height - scrollBarWidth,
            bounds.Width,
            scrollBarWidth);
        
        // 滚动滑块宽度
        var thumbWidth = bounds.Width * (bounds.Width / _contentSize.Width);
        var thumbX = bounds.X + (bounds.Width - thumbWidth) * (_scrollX / _maxScrollX);
        
        // 绘制滚动条背景
        context.DrawRectangle(scrollBarBounds, "#EEEEEE");
        
        // 绘制滚动滑块
        var thumbBounds = new Rect(
            thumbX,
            scrollBarBounds.Y,
            thumbWidth,
            scrollBarWidth);
        
        context.DrawRectangle(thumbBounds, ScrollBarColor ?? "#CCCCCC", null, 0, scrollBarWidth / 2);
    }
    
    /// <summary>
    /// 滚动到指定位置
    /// </summary>
    public void ScrollTo(double x, double y)
    {
        _scrollX = Math.Clamp(x, 0, _maxScrollX);
        _scrollY = Math.Clamp(y, 0, _maxScrollY);
        StateHasChanged();
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
    /// 处理滚轮事件
    /// </summary>
    public override bool HitTest(Point point)
    {
        if (!IsVisible || !IsHitTestVisible)
            return false;
        
        return Bounds.Contains(point);
    }
    
    /// <summary>
    /// 处理指针滚轮
    /// </summary>
    public void OnPointerWheel(PointerWheelEventArgs e)
    {
        // 滚动内容
        _scrollY -= e.Delta.Y * 50; // 滚动步长
        _scrollX -= e.Delta.X * 50;
        
        // 限制范围
        _scrollY = Math.Clamp(_scrollY, 0, _maxScrollY);
        _scrollX = Math.Clamp(_scrollX, 0, _maxScrollX);
        
        e.Handled = true;
        StateHasChanged();
    }
}