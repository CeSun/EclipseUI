using SkiaSharp;
using EclipseUI.Core;

namespace EclipseUI.Layout;

/// <summary>
/// 行定义内部类
/// </summary>
public class RowDefinitionInternal
{
    public GridLength Height { get; set; } = GridLength.Star;
}

/// <summary>
/// 列定义内部类
/// </summary>
public class ColumnDefinitionInternal
{
    public GridLength Width { get; set; } = GridLength.Star;
}

/// <summary>
/// Grid 长度类型
/// </summary>
public enum GridUnitType
{
    /// <summary>
    /// 像素值
    /// </summary>
    Pixel,
    
    /// <summary>
    /// 自动
    /// </summary>
    Auto,
    
    /// <summary>
    /// 比例
    /// </summary>
    Star
}

/// <summary>
/// Grid 长度
/// </summary>
public class GridLength
{
    public double Value { get; set; }
    public GridUnitType GridUnitType { get; set; }
    
    public GridLength() : this(1, GridUnitType.Star) { }
    
    public GridLength(double value, GridUnitType type)
    {
        Value = value;
        GridUnitType = type;
    }
    
    public static GridLength Auto => new GridLength(0, GridUnitType.Auto);
    
    public static GridLength Star => new GridLength(1, GridUnitType.Star);
    
    public static GridLength Pixel(double pixels) => new GridLength(pixels, GridUnitType.Pixel);
}

/// <summary>
/// 网格布局元素
/// </summary>
public class GridElement : EclipseElement
{
    public List<RowDefinitionInternal> RowDefinitions { get; } = new();
    public List<ColumnDefinitionInternal> ColumnDefinitions { get; } = new();
    
    public override SKSize Measure(SKCanvas canvas, float availableWidth, float availableHeight)
    {
        var gridChildren = GetGridChildren();
        
        // 初始化行列定义
        EnsureRowColumnDefinitions(gridChildren);
        
        int rowCount = RowDefinitions.Count;
        int colCount = ColumnDefinitions.Count;
        
        // 计算每行的实际高度和每列的实际宽度
        var rowHeights = new float[rowCount];
        var colWidths = new float[colCount];
        
        // 第一次遍历：测量 Auto 和 Pixel 的行列
        MeasureAutoAndPixel(canvas, availableWidth, availableHeight, gridChildren, rowHeights, colWidths);
        
        // 计算剩余空间用于 Star 分配
        float usedWidth = 0, usedHeight = 0;
        for (int i = 0; i < colCount; i++) usedWidth += colWidths[i];
        for (int i = 0; i < rowCount; i++) usedHeight += rowHeights[i];
        
        float remainingWidth = Math.Max(0, availableWidth - PaddingLeft - PaddingRight - usedWidth);
        float remainingHeight = Math.Max(0, availableHeight - PaddingTop - PaddingBottom - usedHeight);
        
        // 分配 Star 行列
        DistributeStar(remainingWidth, remainingHeight, rowHeights, colWidths);
        
        // 第二次遍历：测量子元素并更新行列尺寸（可能会修改 Auto 类型的高度）
        MeasureChildren(canvas, gridChildren, rowHeights, colWidths);
        
        // 重新计算 Star 类型的高度（因为 Auto 类型可能改变了）
        float usedHeightAfter = 0;
        for (int i = 0; i < rowCount; i++)
        {
            if (RowDefinitions[i].Height.GridUnitType != GridUnitType.Star)
                usedHeightAfter += rowHeights[i];
        }
        float remainingHeightAfter = Math.Max(0, availableHeight - PaddingTop - PaddingBottom - usedHeightAfter);
        DistributeStar(remainingWidth, remainingHeightAfter, rowHeights, colWidths);
        
        // 计算总尺寸
        float totalWidth = 0, totalHeight = 0;
        for (int i = 0; i < colCount; i++) totalWidth += colWidths[i];
        for (int i = 0; i < rowCount; i++) totalHeight += rowHeights[i];
        
        return new SKSize(
            totalWidth + PaddingLeft + PaddingRight,
            totalHeight + PaddingTop + PaddingBottom
        );
    }
    
    public override void Arrange(SKCanvas canvas, float x, float y, float width, float height)
    {
        base.Arrange(canvas, x, y, width, height);
        
        var gridChildren = GetGridChildren();
        EnsureRowColumnDefinitions(gridChildren);
        
        int rowCount = RowDefinitions.Count;
        int colCount = ColumnDefinitions.Count;
        
        // 计算每行每列的尺寸和起始位置
        var rowHeights = new float[rowCount];
        var colWidths = new float[colCount];
        var rowOffsets = new float[rowCount];
        var colOffsets = new float[colCount];
        
        // 测量获取行列尺寸
        Measure(canvas, width, height);
        
        // 重新计算行列尺寸（基于实际可用空间）
        float contentWidth = width - PaddingLeft - PaddingRight;
        float contentHeight = height - PaddingTop - PaddingBottom;
        
        // 分配列宽
        float totalStarColumns = 0;
        float usedWidth = 0;
        foreach (var col in ColumnDefinitions)
        {
            if (col.Width.GridUnitType == GridUnitType.Pixel)
                usedWidth += (float)col.Width.Value;
            else if (col.Width.GridUnitType == GridUnitType.Auto)
                usedWidth += 0; // Auto 会在子元素测量时确定
            else
                totalStarColumns += (float)col.Width.Value;
        }
        
        float remainingWidth = Math.Max(0, contentWidth - usedWidth);
        float starUnitWidth = totalStarColumns > 0 ? remainingWidth / totalStarColumns : 0;
        
        // 第一次：计算 Pixel 和 Star 类型的列宽
        float totalStarColumns2 = 0;
        float usedWidth2 = 0;
        for (int i = 0; i < colCount; i++)
        {
            var col = ColumnDefinitions[i];
            if (col.Width.GridUnitType == GridUnitType.Pixel)
            {
                colWidths[i] = (float)col.Width.Value;
                usedWidth2 += colWidths[i];
            }
            else if (col.Width.GridUnitType == GridUnitType.Star)
            {
                totalStarColumns2 += (float)col.Width.Value;
            }
        }
        
        // 测量 Auto 类型的列宽
        foreach (var child in gridChildren)
        {
            int col = GetColumn(child);
            if (ColumnDefinitions[col].Width.GridUnitType == GridUnitType.Auto)
            {
                var childSize = child.Measure(canvas, contentWidth, contentHeight);
                colWidths[col] = Math.Max(colWidths[col], childSize.Width);
            }
        }
        
        // 重新计算 usedWidth2
        usedWidth2 = 0;
        for (int i = 0; i < colCount; i++)
        {
            if (ColumnDefinitions[i].Width.GridUnitType == GridUnitType.Auto || 
                ColumnDefinitions[i].Width.GridUnitType == GridUnitType.Pixel)
            {
                usedWidth2 += colWidths[i];
            }
        }
        
        // 计算 Star 列宽
        float remainingWidth2 = Math.Max(0, contentWidth - usedWidth2);
        float starUnitWidth2 = totalStarColumns2 > 0 ? remainingWidth2 / totalStarColumns2 : 0;
        
        float currentX = 0;
        for (int i = 0; i < colCount; i++)
        {
            colOffsets[i] = currentX;
            var col = ColumnDefinitions[i];
            if (col.Width.GridUnitType == GridUnitType.Star)
            {
                colWidths[i] = (float)col.Width.Value * starUnitWidth2;
            }
            currentX += colWidths[i];
        }
        
        // 分配行高
        float totalStarRows2 = 0;
        float usedHeight2 = 0;
        for (int i = 0; i < rowCount; i++)
        {
            var row = RowDefinitions[i];
            if (row.Height.GridUnitType == GridUnitType.Pixel)
            {
                rowHeights[i] = (float)row.Height.Value;
                usedHeight2 += rowHeights[i];
            }
            else if (row.Height.GridUnitType == GridUnitType.Star)
            {
                totalStarRows2 += (float)row.Height.Value;
            }
        }
        
        // 测量 Auto 类型的行高
        foreach (var child in gridChildren)
        {
            int row = GetRow(child);
            
            if (RowDefinitions[row].Height.GridUnitType == GridUnitType.Auto)
            {
                var childSize = child.Measure(canvas, colWidths[GetColumn(child)], contentHeight);
                rowHeights[row] = Math.Max(rowHeights[row], childSize.Height);
            }
        }
        
        // 重新计算 usedHeight2（累加所有 Auto 和 Pixel 行高）
        usedHeight2 = 0;
        for (int i = 0; i < rowCount; i++)
        {
            if (RowDefinitions[i].Height.GridUnitType == GridUnitType.Auto || 
                RowDefinitions[i].Height.GridUnitType == GridUnitType.Pixel)
            {
                usedHeight2 += rowHeights[i];
            }
        }
        
        // 计算 Star 高度
        float remainingHeight2 = Math.Max(0, contentHeight - usedHeight2);
        float starUnitHeight2 = totalStarRows2 > 0 ? remainingHeight2 / totalStarRows2 : 0;
        
        float currentY = 0;
        for (int i = 0; i < rowCount; i++)
        {
            rowOffsets[i] = currentY;
            var row = RowDefinitions[i];
            if (row.Height.GridUnitType == GridUnitType.Star)
            {
                rowHeights[i] = (float)row.Height.Value * starUnitHeight2;
            }
            // Auto 和 Pixel 类型的 rowHeights[i] 已经在前面计算过
            currentY += rowHeights[i];
        }
        
        // 排列子元素
        foreach (var child in gridChildren)
        {
            int row = GetRow(child);
            int col = GetColumn(child);
            int rowSpan = GetRowSpan(child);
            int colSpan = GetColumnSpan(child);
            
            // 计算子元素的位置和尺寸
            float childX = x + PaddingLeft + colOffsets[col];
            float childY = y + PaddingTop + rowOffsets[row];
            
            float childWidth = 0;
            for (int i = col; i < Math.Min(col + colSpan, colCount); i++)
                childWidth += colWidths[i];
            
            float childHeight = 0;
            for (int i = row; i < Math.Min(row + rowSpan, rowCount); i++)
                childHeight += rowHeights[i];
            
            child.Arrange(canvas, childX, childY, childWidth, childHeight);
        }
    }
    
    /// <summary>
    /// 测量 Auto 和 Pixel 类型的行列
    /// </summary>
    private void MeasureAutoAndPixel(SKCanvas canvas, float availableWidth, float availableHeight, 
        List<GridItemElement> children, float[] rowHeights, float[] colWidths)
    {
        int rowCount = RowDefinitions.Count;
        int colCount = ColumnDefinitions.Count;
        
        // 处理 Pixel 类型
        for (int i = 0; i < colCount; i++)
        {
            if (ColumnDefinitions[i].Width.GridUnitType == GridUnitType.Pixel)
                colWidths[i] = (float)ColumnDefinitions[i].Width.Value;
        }
        
        for (int i = 0; i < rowCount; i++)
        {
            if (RowDefinitions[i].Height.GridUnitType == GridUnitType.Pixel)
                rowHeights[i] = (float)RowDefinitions[i].Height.Value;
        }
        
        // 处理 Auto 类型 - 测量该行列中所有子元素
        foreach (var child in children)
        {
            int row = GetRow(child);
            int col = GetColumn(child);
            
            // 测量子元素
            var childSize = child.Measure(canvas, availableWidth, availableHeight);
            
            // 如果是 Auto 类型，更新行列尺寸
            if (ColumnDefinitions[col].Width.GridUnitType == GridUnitType.Auto)
                colWidths[col] = Math.Max(colWidths[col], childSize.Width);
            
            if (RowDefinitions[row].Height.GridUnitType == GridUnitType.Auto)
                rowHeights[row] = Math.Max(rowHeights[row], childSize.Height);
        }
    }
    
    /// <summary>
    /// 分配 Star 类型的行列
    /// </summary>
    private void DistributeStar(float remainingWidth, float remainingHeight, float[] rowHeights, float[] colWidths)
    {
        // 计算 Star 列的总权重
        float totalStarColumns = 0;
        for (int i = 0; i < ColumnDefinitions.Count; i++)
        {
            if (ColumnDefinitions[i].Width.GridUnitType == GridUnitType.Star)
                totalStarColumns += (float)ColumnDefinitions[i].Width.Value;
        }
        
        if (totalStarColumns > 0)
        {
            float starUnitWidth = remainingWidth / totalStarColumns;
            for (int i = 0; i < ColumnDefinitions.Count; i++)
            {
                if (ColumnDefinitions[i].Width.GridUnitType == GridUnitType.Star)
                    colWidths[i] = (float)ColumnDefinitions[i].Width.Value * starUnitWidth;
            }
        }
        
        // 计算 Star 行的总权重
        float totalStarRows = 0;
        for (int i = 0; i < RowDefinitions.Count; i++)
        {
            if (RowDefinitions[i].Height.GridUnitType == GridUnitType.Star)
                totalStarRows += (float)RowDefinitions[i].Height.Value;
        }
        
        if (totalStarRows > 0)
        {
            float starUnitHeight = remainingHeight / totalStarRows;
            for (int i = 0; i < RowDefinitions.Count; i++)
            {
                if (RowDefinitions[i].Height.GridUnitType == GridUnitType.Star)
                    rowHeights[i] = (float)RowDefinitions[i].Height.Value * starUnitHeight;
            }
        }
    }
    
    /// <summary>
    /// 测量子元素并更新行列尺寸
    /// </summary>
    private void MeasureChildren(SKCanvas canvas, List<GridItemElement> children, float[] rowHeights, float[] colWidths)
    {
        int rowCount = RowDefinitions.Count;
        int colCount = ColumnDefinitions.Count;
        
        // 计算每行每列的总 Star 权重
        float totalStarColumns = 0, totalStarRows = 0;
        for (int i = 0; i < colCount; i++)
            if (ColumnDefinitions[i].Width.GridUnitType == GridUnitType.Star)
                totalStarColumns += (float)ColumnDefinitions[i].Width.Value;
        for (int i = 0; i < rowCount; i++)
            if (RowDefinitions[i].Height.GridUnitType == GridUnitType.Star)
                totalStarRows += (float)RowDefinitions[i].Height.Value;
        
        foreach (var child in children)
        {
            int row = GetRow(child);
            int col = GetColumn(child);
            int rowSpan = GetRowSpan(child);
            int colSpan = GetColumnSpan(child);
            
            // 计算子元素可用的空间
            float availableWidth = 0;
            for (int i = col; i < Math.Min(col + colSpan, colCount); i++)
            {
                if (ColumnDefinitions[i].Width.GridUnitType == GridUnitType.Star)
                    availableWidth += 1000; // 临时值，后面会重新计算
                else
                    availableWidth += colWidths[i];
            }
            
            float availableHeight = 0;
            for (int i = row; i < Math.Min(row + rowSpan, rowCount); i++)
            {
                if (RowDefinitions[i].Height.GridUnitType == GridUnitType.Star)
                    availableHeight += 1000;
                else
                    availableHeight += rowHeights[i];
            }
            
            // 测量子元素
            var childSize = child.Measure(canvas, availableWidth, availableHeight);
            
            // 如果是 Auto 类型，更新行列尺寸
            for (int i = col; i < Math.Min(col + colSpan, colCount); i++)
            {
                if (ColumnDefinitions[i].Width.GridUnitType == GridUnitType.Auto)
                    colWidths[i] = Math.Max(colWidths[i], childSize.Width / colSpan);
            }
            
            for (int i = row; i < Math.Min(row + rowSpan, rowCount); i++)
            {
                if (RowDefinitions[i].Height.GridUnitType == GridUnitType.Auto)
                    rowHeights[i] = Math.Max(rowHeights[i], childSize.Height / rowSpan);
            }
        }
    }
    
    /// <summary>
    /// 获取所有 GridItemElement 子元素
    /// </summary>
    private List<GridItemElement> GetGridChildren()
    {
        var result = new List<GridItemElement>();
        foreach (var child in Children)
        {
            if (child is GridItemElement gridItem)
                result.Add(gridItem);
        }
        return result;
    }
    
    /// <summary>
    /// 确保行列定义存在
    /// </summary>
    private void EnsureRowColumnDefinitions(List<GridItemElement> children)
    {
        if (children.Count == 0)
        {
            if (RowDefinitions.Count == 0) RowDefinitions.Add(new RowDefinitionInternal());
            if (ColumnDefinitions.Count == 0) ColumnDefinitions.Add(new ColumnDefinitionInternal());
            return;
        }
        
        // 找到最大的行列索引
        int maxRow = 0, maxCol = 0;
        foreach (var child in children)
        {
            maxRow = Math.Max(maxRow, GetRow(child) + GetRowSpan(child));
            maxCol = Math.Max(maxCol, GetColumn(child) + GetColumnSpan(child));
        }
        
        // 确保行列定义数量足够
        while (RowDefinitions.Count < maxRow)
            RowDefinitions.Add(new RowDefinitionInternal());
        while (ColumnDefinitions.Count < maxCol)
            ColumnDefinitions.Add(new ColumnDefinitionInternal());
    }
    
    #region 附加属性
    
    public static readonly int RowProperty = 0;
    public static readonly int ColumnProperty = 1;
    public static readonly int RowSpanProperty = 2;
    public static readonly int ColumnSpanProperty = 3;
    
    public static void SetRow(EclipseElement element, int value)
        => element.SetValue(RowProperty, value);
    
    public static int GetRow(EclipseElement element)
        => element.GetValue<int>(RowProperty, 0);
    
    public static void SetColumn(EclipseElement element, int value)
        => element.SetValue(ColumnProperty, value);
    
    public static int GetColumn(EclipseElement element)
        => element.GetValue<int>(ColumnProperty, 0);
    
    public static void SetRowSpan(EclipseElement element, int value)
        => element.SetValue(RowSpanProperty, value);
    
    public static int GetRowSpan(EclipseElement element)
        => element.GetValue<int>(RowSpanProperty, 1);
    
    public static void SetColumnSpan(EclipseElement element, int value)
        => element.SetValue(ColumnSpanProperty, value);
    
    public static int GetColumnSpan(EclipseElement element)
        => element.GetValue<int>(ColumnSpanProperty, 1);
    
    #endregion
}

/// <summary>
/// Grid 子项元素
/// </summary>
public class GridItemElement : EclipseElement
{
    public override SKSize Measure(SKCanvas canvas, float availableWidth, float availableHeight)
    {
        if (Children.Count == 0)
            return new SKSize(0, 0);
        
        // 测量第一个子元素
        return Children[0].Measure(canvas, availableWidth, availableHeight);
    }
    
    public override void Arrange(SKCanvas canvas, float x, float y, float width, float height)
    {
        base.Arrange(canvas, x, y, width, height);
        
        if (Children.Count > 0)
            Children[0].Arrange(canvas, x, y, width, height);
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
            var clipRect = new SKRect(X, Y, X + Width, Y + Height);
            canvas.ClipRect(clipRect);
            
            // 渲染子元素
            if (Children.Count > 0)
                Children[0].Render(canvas);
        }
        finally
        {
            canvas.Restore();
        }
    }
}
