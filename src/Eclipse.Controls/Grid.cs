using Eclipse.Core;
using Eclipse.Core.Abstractions;
using Eclipse.Input;
using Eclipse.Rendering;
using System.Collections.Generic;

namespace Eclipse.Controls;

/// <summary>
/// 网格布局控件
/// </summary>
public class Grid : InputElementBase
{
    private Rect _bounds;
    private Size _desiredSize = Size.Zero;
    
    /// <summary>
    /// 行定义
    /// </summary>
    private List<GridLength> _rowDefinitions = new();
    
    /// <summary>
    /// 列定义
    /// </summary>
    private List<GridLength> _columnDefinitions = new();
    
    /// <summary>
    /// 行间距
    /// </summary>
    public double RowSpacing { get; set; } = 0;
    
    /// <summary>
    /// 列间距
    /// </summary>
    public double ColumnSpacing { get; set; } = 0;
    
    /// <summary>
    /// 背景颜色
    /// </summary>
    public string? BackgroundColor { get; set; }
    
    /// <summary>
    /// 内边距
    /// </summary>
    public double Padding { get; set; } = 0;
    
    public override bool IsVisible => true;
    public override Rect Bounds => _bounds;
    
    public void UpdateBounds(Rect bounds) => _bounds = bounds;
    
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
    /// 添加行定义
    /// </summary>
    public void AddRowDefinition(GridLength height)
    {
        _rowDefinitions.Add(height);
    }
    
    /// <summary>
    /// 添加列定义
    /// </summary>
    public void AddColumnDefinition(GridLength width)
    {
        _columnDefinitions.Add(width);
    }
    
    /// <summary>
    /// 设置行定义（便捷方法）
    /// </summary>
    public void SetRowDefinitions(params GridLength[] rows)
    {
        _rowDefinitions.Clear();
        foreach (var row in rows)
        {
            _rowDefinitions.Add(row);
        }
    }
    
    /// <summary>
    /// 设置列定义（便捷方法）
    /// </summary>
    public void SetColumnDefinitions(params GridLength[] columns)
    {
        _columnDefinitions.Clear();
        foreach (var col in columns)
        {
            _columnDefinitions.Add(col);
        }
    }
    
    /// <summary>
    /// 行数
    /// </summary>
    public int RowCount => _rowDefinitions.Count > 0 ? _rowDefinitions.Count : 1;
    
    /// <summary>
    /// 列数
    /// </summary>
    public int ColumnCount => _columnDefinitions.Count > 0 ? _columnDefinitions.Count : 1;
    
    /// <summary>
    /// 测量网格所需尺寸
    /// </summary>
    public Size Measure(Size availableSize, IDrawingContext context)
    {
        var scaledPadding = Padding * context.Scale;
        var scaledRowSpacing = RowSpacing * context.Scale;
        var scaledColumnSpacing = ColumnSpacing * context.Scale;
        
        // 计算行高和列宽
        var rowHeights = CalculateRowHeights(availableSize.Height - scaledPadding * 2, scaledRowSpacing, context);
        var columnWidths = CalculateColumnWidths(availableSize.Width - scaledPadding * 2, scaledColumnSpacing, context);
        
        double totalHeight = 0;
        double totalWidth = 0;
        
        // 计算总尺寸
        foreach (var h in rowHeights)
        {
            totalHeight += h;
        }
        totalHeight += scaledRowSpacing * (rowHeights.Length - 1) + scaledPadding * 2;
        
        foreach (var w in columnWidths)
        {
            totalWidth += w;
        }
        totalWidth += scaledColumnSpacing * (columnWidths.Length - 1) + scaledPadding * 2;
        
        _desiredSize = new Size(totalWidth, totalHeight);
        return _desiredSize;
    }
    
    /// <summary>
    /// 安排子元素位置
    /// </summary>
    public void Arrange(Rect finalBounds, IDrawingContext context)
    {
        _bounds = finalBounds;
        
        var scaledPadding = Padding * context.Scale;
        var scaledRowSpacing = RowSpacing * context.Scale;
        var scaledColumnSpacing = ColumnSpacing * context.Scale;
        
        // 计算行高和列宽
        var rowHeights = CalculateRowHeights(finalBounds.Height - scaledPadding * 2, scaledRowSpacing, context);
        var columnWidths = CalculateColumnWidths(finalBounds.Width - scaledPadding * 2, scaledColumnSpacing, context);
        
        // 计算每个单元格的位置
        double y = finalBounds.Y + scaledPadding;
        for (int row = 0; row < rowHeights.Length; row++)
        {
            double x = finalBounds.X + scaledPadding;
            for (int col = 0; col < columnWidths.Length; col++)
            {
                var cellBounds = new Rect(x, y, columnWidths[col], rowHeights[row]);
                
                // 查找放置在此单元格的子元素
                var child = GetChildAt(row, col);
                if (child != null)
                {
                    ArrangeChild(child, cellBounds, context);
                }
                
                x += columnWidths[col] + scaledColumnSpacing;
            }
            y += rowHeights[row] + scaledRowSpacing;
        }
    }
    
    public override void Render(IDrawingContext context, Rect bounds)
    {
        UpdateBounds(bounds);
        
        var scaledPadding = Padding * context.Scale;
        var scaledRowSpacing = RowSpacing * context.Scale;
        var scaledColumnSpacing = ColumnSpacing * context.Scale;
        
        // 绘制背景
        if (!string.IsNullOrEmpty(BackgroundColor))
        {
            context.DrawRectangle(bounds, BackgroundColor);
        }
        
        // 计算行高和列宽
        var rowHeights = CalculateRowHeights(bounds.Height - scaledPadding * 2, scaledRowSpacing, context);
        var columnWidths = CalculateColumnWidths(bounds.Width - scaledPadding * 2, scaledColumnSpacing, context);
        
        // 绘制子元素
        double y = bounds.Y + scaledPadding;
        for (int row = 0; row < rowHeights.Length; row++)
        {
            double x = bounds.X + scaledPadding;
            for (int col = 0; col < columnWidths.Length; col++)
            {
                var cellBounds = new Rect(x, y, columnWidths[col], rowHeights[row]);
                
                var child = GetChildAt(row, col);
                if (child != null)
                {
                    child.Render(context, cellBounds);
                }
                
                x += columnWidths[col] + scaledColumnSpacing;
            }
            y += rowHeights[row] + scaledRowSpacing;
        }
    }
    
    /// <summary>
    /// 计算行高度
    /// </summary>
    private double[] CalculateRowHeights(double availableHeight, double spacing, IDrawingContext context)
    {
        var rows = RowCount;
        var heights = new double[rows];
        
        // 先计算绝对值和 Auto 行高
        double absoluteTotal = 0;
        int starCount = 0;
        
        for (int i = 0; i < rows; i++)
        {
            var rowDef = i < _rowDefinitions.Count ? _rowDefinitions[i] : GridLength.Auto;
            
            if (rowDef.IsAbsolute)
            {
                heights[i] = rowDef.Value * context.Scale;
                absoluteTotal += heights[i];
            }
            else if (rowDef.IsAuto)
            {
                // Auto 行高基于内容
                var child = GetChildAt(i, -1);
                if (child != null)
                {
                    var size = MeasureChild(child, context);
                    heights[i] = size.Height;
                    absoluteTotal += heights[i];
                }
                else
                {
                    heights[i] = 40 * context.Scale; // 默认高度
                    absoluteTotal += heights[i];
                }
            }
            else if (rowDef.IsStar)
            {
                starCount++;
                heights[i] = 0; // 暂时标记为 0
            }
        }
        
        // 计算 Star 行高
        if (starCount > 0)
        {
            var remainingHeight = availableHeight - absoluteTotal - spacing * (rows - 1);
            var starUnit = remainingHeight / starCount;
            
            for (int i = 0; i < rows; i++)
            {
                var rowDef = i < _rowDefinitions.Count ? _rowDefinitions[i] : GridLength.Auto;
                if (rowDef.IsStar)
                {
                    heights[i] = starUnit * rowDef.Value;
                }
            }
        }
        
        return heights;
    }
    
    /// <summary>
    /// 计算列宽度
    /// </summary>
    private double[] CalculateColumnWidths(double availableWidth, double spacing, IDrawingContext context)
    {
        var cols = ColumnCount;
        var widths = new double[cols];
        
        // 先计算绝对值和 Auto 列宽
        double absoluteTotal = 0;
        int starCount = 0;
        
        for (int i = 0; i < cols; i++)
        {
            var colDef = i < _columnDefinitions.Count ? _columnDefinitions[i] : GridLength.Auto;
            
            if (colDef.IsAbsolute)
            {
                widths[i] = colDef.Value * context.Scale;
                absoluteTotal += widths[i];
            }
            else if (colDef.IsAuto)
            {
                // Auto 列宽基于内容
                var child = GetChildAt(-1, i);
                if (child != null)
                {
                    var size = MeasureChild(child, context);
                    widths[i] = size.Width;
                    absoluteTotal += widths[i];
                }
                else
                {
                    widths[i] = 100 * context.Scale; // 默认宽度
                    absoluteTotal += widths[i];
                }
            }
            else if (colDef.IsStar)
            {
                starCount++;
                widths[i] = 0; // 暂时标记为 0
            }
        }
        
        // 计算 Star 列宽
        if (starCount > 0)
        {
            var remainingWidth = availableWidth - absoluteTotal - spacing * (cols - 1);
            var starUnit = remainingWidth / starCount;
            
            for (int i = 0; i < cols; i++)
            {
                var colDef = i < _columnDefinitions.Count ? _columnDefinitions[i] : GridLength.Auto;
                if (colDef.IsStar)
                {
                    widths[i] = starUnit * colDef.Value;
                }
            }
        }
        
        return widths;
    }
    
    /// <summary>
    /// 测量子元素尺寸
    /// </summary>
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
    
    /// <summary>
    /// 安排子元素
    /// </summary>
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
    
    /// <summary>
    /// 获取指定位置的子元素
    /// </summary>
    private IComponent? GetChildAt(int row, int col)
    {
        foreach (var child in Children)
        {
            if (child is GridCell gridCell)
            {
                if ((gridCell.Row == row || row < 0) && (gridCell.Column == col || col < 0))
                {
                    return gridCell.Child;
                }
            }
            // 简单模式：按顺序分配
            else if (row < 0 || col < 0)
            {
                return child;
            }
        }
        return null;
    }
}

/// <summary>
/// 网格单元格 - 用于指定子元素在网格中的位置
/// </summary>
public class GridCell : ComponentBase
{
    /// <summary>
    /// 所在行
    /// </summary>
    public int Row { get; set; } = 0;
    
    /// <summary>
    /// 所在列
    /// </summary>
    public int Column { get; set; } = 0;
    
    /// <summary>
    /// 占用行数
    /// </summary>
    public int RowSpan { get; set; } = 1;
    
    /// <summary>
    /// 占用列数
    /// </summary>
    public int ColumnSpan { get; set; } = 1;
    
    /// <summary>
    /// 子元素
    /// </summary>
    public IComponent? Child { get; set; }
    
    public override void Build(IBuildContext context) { }
    
    public override void Render(IDrawingContext context, Rect bounds)
    {
        if (Child != null)
        {
            Child.Render(context, bounds);
        }
    }
}

/// <summary>
/// 网格长度定义
/// </summary>
public struct GridLength
{
    /// <summary>
    /// 值
    /// </summary>
    public double Value { get; }
    
    /// <summary>
    /// 类型
    /// </summary>
    public GridLengthType Type { get; }
    
    /// <summary>
    /// 是否是绝对值
    /// </summary>
    public bool IsAbsolute => Type == GridLengthType.Absolute;
    
    /// <summary>
    /// 是否是自动
    /// </summary>
    public bool IsAuto => Type == GridLengthType.Auto;
    
    /// <summary>
    /// 是否是比例
    /// </summary>
    public bool IsStar => Type == GridLengthType.Star;
    
    public GridLength(double value, GridLengthType type)
    {
        Value = value;
        Type = type;
    }
    
    /// <summary>
    /// 创建绝对值
    /// </summary>
    public static GridLength Absolute(double value) => new GridLength(value, GridLengthType.Absolute);
    
    /// <summary>
    /// 创建自动值
    /// </summary>
    public static GridLength Auto => new GridLength(0, GridLengthType.Auto);
    
    /// <summary>
    /// 创建比例值
    /// </summary>
    public static GridLength Star(double value = 1) => new GridLength(value, GridLengthType.Star);
    
    /// <summary>
    /// 隐式转换数字为绝对值
    /// </summary>
    public static implicit operator GridLength(double value) => Absolute(value);
}

/// <summary>
/// 网格长度类型
/// </summary>
public enum GridLengthType
{
    /// <summary>
    /// 绝对值（像素）
    /// </summary>
    Absolute,
    
    /// <summary>
    /// 自动（基于内容）
    /// </summary>
    Auto,
    
    /// <summary>
    /// 比例（按比例分配剩余空间）
    /// </summary>
    Star
}