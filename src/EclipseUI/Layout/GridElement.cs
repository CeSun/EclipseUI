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
    public float Spacing { get; set; }
    
    // 缓存 Measure 阶段计算的行列尺寸，避免 Arrange 时重新计算导致不一致
    private float[]? _cachedRowHeights;
    private float[]? _cachedColWidths;
    private float[]? _cachedRowOffsets;
    private float[]? _cachedColOffsets;
    private int _cachedRowCount;
    private int _cachedColCount;
    
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
        
        // 检查可用空间是否是无限的 - 分别处理宽度和高度
        bool isWidthInfinite = float.IsPositiveInfinity(availableWidth);
        bool isHeightInfinite = float.IsPositiveInfinity(availableHeight);
        
        // 第一次遍历：测量 Auto 和 Pixel 的行列
        MeasureAutoAndPixel(canvas, availableWidth, availableHeight, gridChildren, rowHeights, colWidths);
        
        // ========== 处理宽度方向 ==========
        float remainingWidth = 0;
        if (isWidthInfinite)
        {
            // 宽度无限：Star 列退化成 Auto，测量子元素实际需要的宽度
            MeasureChildrenForInfiniteWidth(canvas, gridChildren, colWidths, rowHeights);
        }
        else
        {
            // 宽度受限：Star 列按比例分配
            float usedWidth = 0;
            for (int i = 0; i < colCount; i++) usedWidth += colWidths[i];
            remainingWidth = Math.Max(0, availableWidth - PaddingLeft - PaddingRight - usedWidth);
            DistributeStarWidth(remainingWidth, colWidths);
        }
        
        // ========== 处理高度方向 ==========
        float remainingHeight = 0;
        if (isHeightInfinite)
        {
            // 高度无限：Star 行退化成 Auto，测量子元素实际需要的高度
            MeasureChildrenForInfiniteHeight(canvas, gridChildren, rowHeights, colWidths);
        }
        else
        {
            // 高度受限：Star 行按比例分配
            float usedHeight = 0;
            for (int i = 0; i < rowCount; i++) usedHeight += rowHeights[i];
            remainingHeight = Math.Max(0, availableHeight - PaddingTop - PaddingBottom - usedHeight);
            DistributeStarHeight(remainingHeight, rowHeights);
            
            // 第二次遍历：测量子元素并更新 Auto 类型的行高（可能会修改 Auto 类型的高度）
            MeasureChildrenForAutoRows(canvas, gridChildren, rowHeights, colWidths);
            
            // 重新计算 Star 类型的行高（因为 Auto 类型可能改变了）
            float usedHeightAfter = 0;
            for (int i = 0; i < rowCount; i++)
            {
                if (RowDefinitions[i].Height.GridUnitType != GridUnitType.Star)
                    usedHeightAfter += rowHeights[i];
            }
            remainingHeight = Math.Max(0, availableHeight - PaddingTop - PaddingBottom - usedHeightAfter);
            DistributeStarHeight(remainingHeight, rowHeights);
        }
        
        // 如果宽度受限，需要重新测量子元素以获取正确的尺寸（在 Star 列分配后）
        if (!isWidthInfinite)
        {
            MeasureChildrenForAutoColumns(canvas, gridChildren, rowHeights, colWidths);
            
            // 重新计算 Star 类型的列宽（因为 Auto 类型可能改变了）
            float usedWidthAfter = 0;
            for (int i = 0; i < colCount; i++)
            {
                if (ColumnDefinitions[i].Width.GridUnitType != GridUnitType.Star)
                    usedWidthAfter += colWidths[i];
            }
            remainingWidth = Math.Max(0, availableWidth - PaddingLeft - PaddingRight - usedWidthAfter);
            DistributeStarWidth(remainingWidth, colWidths);
        }
        
        // 计算总尺寸（包含 Spacing）
        float totalWidth = 0, totalHeight = 0;
        for (int i = 0; i < colCount; i++) totalWidth += colWidths[i];
        for (int i = 0; i < rowCount; i++) totalHeight += rowHeights[i];
        
        // 添加 Spacing（行数 -1 个间距）
        if (rowCount > 1) totalHeight += Spacing * (rowCount - 1);
        if (colCount > 1) totalWidth += Spacing * (colCount - 1);
        
        // 缓存行列尺寸供 Arrange 使用
        _cachedRowHeights = rowHeights;
        _cachedColWidths = colWidths;
        _cachedRowCount = rowCount;
        _cachedColCount = colCount;
        
        // 计算偏移量（包含 Spacing）
        _cachedRowOffsets = new float[rowCount];
        _cachedColOffsets = new float[colCount];
        
        float currentRowOffset = 0;
        for (int i = 0; i < rowCount; i++)
        {
            _cachedRowOffsets[i] = currentRowOffset;
            currentRowOffset += rowHeights[i];
            if (i < rowCount - 1)
                currentRowOffset += Spacing;
        }
        
        float currentColOffset = 0;
        for (int i = 0; i < colCount; i++)
        {
            _cachedColOffsets[i] = currentColOffset;
            currentColOffset += colWidths[i];
            if (i < colCount - 1)
                currentColOffset += Spacing;
        }
        
        return new SKSize(
            totalWidth + PaddingLeft + PaddingRight,
            totalHeight + PaddingTop + PaddingBottom
        );
    }
    
    public override void Arrange(SKCanvas canvas, float x, float y, float width, float height)
    {
        // 设置自身位置和尺寸
        X = x;
        Y = y;
        Width = width;
        Height = height;
        
        var gridChildren = GetGridChildren();
        
        // 使用 Measure 阶段缓存的行列尺寸，确保与 Measure 结果一致
        if (_cachedRowHeights == null || _cachedColWidths == null)
        {
            // 如果没有缓存（理论上不应该发生），回退到旧逻辑
            EnsureRowColumnDefinitions(gridChildren);
            // 简单处理：直接调用 Measure 获取尺寸
            Measure(canvas, width, height);
        }
        
        int rowCount = _cachedRowCount;
        int colCount = _cachedColCount;
        var rowHeights = _cachedRowHeights!;
        var colWidths = _cachedColWidths!;
        var rowOffsets = _cachedRowOffsets!;
        var colOffsets = _cachedColOffsets!;
        
        // 排列子元素（应用对齐）
        foreach (var child in gridChildren)
        {
            int row = GetRow(child);
            int col = GetColumn(child);
            int rowSpan = GetRowSpan(child);
            int colSpan = GetColumnSpan(child);
            
            // 计算单元格的位置和尺寸（使用缓存的偏移量）
            float cellX = x + PaddingLeft + colOffsets[col];
            float cellY = y + PaddingTop + rowOffsets[row];
            
            float cellWidth = 0;
            for (int i = col; i < Math.Min(col + colSpan, colCount); i++)
                cellWidth += colWidths[i];
            
            float cellHeight = 0;
            for (int i = row; i < Math.Min(row + rowSpan, rowCount); i++)
                cellHeight += rowHeights[i];
            
            // 测量子元素
            var childSize = child.Measure(canvas, cellWidth, cellHeight);
            
            // 应用水平对齐
            float childX = cellX;
            float childW = cellWidth;
            
            // 如果子元素有 RequestedWidth 或 MaxWidth，不使用 Stretch
            bool hasRequestedWidth = child.RequestedWidth.HasValue;
            bool hasMaxWidth = child.MaxWidth.HasValue;
            
            if (hasRequestedWidth || hasMaxWidth || child.HorizontalAlignment == HorizontalAlignment.Left)
            {
                childW = Math.Min(childSize.Width, cellWidth);
            }
            else if (child.HorizontalAlignment == HorizontalAlignment.Center)
            {
                childX = cellX + (cellWidth - childSize.Width) / 2;
                childW = Math.Min(childSize.Width, cellWidth);
            }
            else if (child.HorizontalAlignment == HorizontalAlignment.Right)
            {
                childX = cellX + cellWidth - childSize.Width;
                childW = Math.Min(childSize.Width, cellWidth);
            }
            // Stretch 且没有 RequestedWidth/MaxWidth: 使用 cellWidth
            
            // 应用垂直对齐
            float childY = cellY;
            float childH = cellHeight;
            
            // 如果子元素有 RequestedHeight 或 MaxHeight，不使用 Stretch
            bool hasRequestedHeight = child.RequestedHeight.HasValue;
            bool hasMaxHeight = child.MaxHeight.HasValue;
            
            if (hasRequestedHeight || hasMaxHeight || child.VerticalAlignment == VerticalAlignment.Top)
            {
                childH = Math.Min(childSize.Height, cellHeight);
            }
            else if (child.VerticalAlignment == VerticalAlignment.Center)
            {
                childY = cellY + (cellHeight - childSize.Height) / 2;
                childH = Math.Min(childSize.Height, cellHeight);
            }
            else if (child.VerticalAlignment == VerticalAlignment.Bottom)
            {
                childY = cellY + cellHeight - childSize.Height;
                childH = Math.Min(childSize.Height, cellHeight);
            }
            // Stretch 且没有 RequestedHeight/MaxHeight: 使用 cellHeight
            
            child.Arrange(canvas, childX, childY, childW, childH);
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
    /// 分配 Star 类型的列宽
    /// </summary>
    private void DistributeStarWidth(float remainingWidth, float[] colWidths)
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
    }
    
    /// <summary>
    /// 分配 Star 类型的行高
    /// </summary>
    private void DistributeStarHeight(float remainingHeight, float[] rowHeights)
    {
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
    /// 宽度无限时测量子元素（Star 列表现得像 Auto）
    /// </summary>
    private void MeasureChildrenForInfiniteWidth(SKCanvas canvas, List<GridItemElement> children, float[] colWidths, float[] rowHeights)
    {
        int colCount = ColumnDefinitions.Count;
        int rowCount = RowDefinitions.Count;
        
        foreach (var child in children)
        {
            int col = GetColumn(child);
            int colSpan = GetColumnSpan(child);
            int row = GetRow(child);
            int rowSpan = GetRowSpan(child);
            
            // 计算子元素可用的高度（使用行高之和）
            float availableHeight = 0;
            for (int i = row; i < Math.Min(row + rowSpan, rowCount); i++)
            {
                availableHeight += rowHeights[i];
            }
            
            // 测量子元素（宽度无限，让子元素决定需要的宽度）
            var childSize = child.Measure(canvas, float.PositiveInfinity, availableHeight);
            
            // 只更新 Auto 和 Star 类型的列宽，Pixel 类型保持固定值
            for (int i = col; i < Math.Min(col + colSpan, colCount); i++)
            {
                if (ColumnDefinitions[i].Width.GridUnitType != GridUnitType.Pixel)
                    colWidths[i] = Math.Max(colWidths[i], childSize.Width / colSpan);
            }
        }
    }
    
    /// <summary>
    /// 高度无限时测量子元素（Star 行表现得像 Auto）
    /// </summary>
    private void MeasureChildrenForInfiniteHeight(SKCanvas canvas, List<GridItemElement> children, float[] rowHeights, float[] colWidths)
    {
        int rowCount = RowDefinitions.Count;
        int colCount = ColumnDefinitions.Count;
        
        foreach (var child in children)
        {
            int row = GetRow(child);
            int rowSpan = GetRowSpan(child);
            int col = GetColumn(child);
            int colSpan = GetColumnSpan(child);
            
            // 计算子元素可用的宽度（使用列宽之和）
            float availableWidth = 0;
            for (int i = col; i < Math.Min(col + colSpan, colCount); i++)
            {
                availableWidth += colWidths[i];
            }
            
            // 测量子元素（高度无限，让子元素决定需要的高度）
            var childSize = child.Measure(canvas, availableWidth, float.PositiveInfinity);
            
            // 只更新 Auto 和 Star 类型的行高，Pixel 类型保持固定值
            for (int i = row; i < Math.Min(row + rowSpan, rowCount); i++)
            {
                if (RowDefinitions[i].Height.GridUnitType != GridUnitType.Pixel)
                    rowHeights[i] = Math.Max(rowHeights[i], childSize.Height / rowSpan);
            }
        }
    }
    
    /// <summary>
    /// 测量子元素并更新 Auto 类型的行高
    /// </summary>
    private void MeasureChildrenForAutoRows(SKCanvas canvas, List<GridItemElement> children, 
        float[] rowHeights, float[] colWidths)
    {
        int rowCount = RowDefinitions.Count;
        int colCount = ColumnDefinitions.Count;
        
        foreach (var child in children)
        {
            int row = GetRow(child);
            int rowSpan = GetRowSpan(child);
            int col = GetColumn(child);
            int colSpan = GetColumnSpan(child);
            
            // 计算子元素可用的宽度
            float availableWidth = 0;
            for (int i = col; i < Math.Min(col + colSpan, colCount); i++)
            {
                availableWidth += colWidths[i];
            }
            
            // 测量子元素
            var childSize = child.Measure(canvas, availableWidth, float.PositiveInfinity);
            
            // 只更新 Auto 类型的行高
            for (int i = row; i < Math.Min(row + rowSpan, rowCount); i++)
            {
                if (RowDefinitions[i].Height.GridUnitType == GridUnitType.Auto)
                    rowHeights[i] = Math.Max(rowHeights[i], childSize.Height / rowSpan);
            }
        }
    }
    
    /// <summary>
    /// 测量子元素并更新 Auto 类型的列宽
    /// </summary>
    private void MeasureChildrenForAutoColumns(SKCanvas canvas, List<GridItemElement> children, 
        float[] rowHeights, float[] colWidths)
    {
        int rowCount = RowDefinitions.Count;
        int colCount = ColumnDefinitions.Count;
        
        foreach (var child in children)
        {
            int col = GetColumn(child);
            int colSpan = GetColumnSpan(child);
            int row = GetRow(child);
            int rowSpan = GetRowSpan(child);
            
            // 计算子元素可用的高度
            float availableHeight = 0;
            for (int i = row; i < Math.Min(row + rowSpan, rowCount); i++)
            {
                availableHeight += rowHeights[i];
            }
            
            // 测量子元素
            var childSize = child.Measure(canvas, float.PositiveInfinity, availableHeight);
            
            // 只更新 Auto 类型的列宽
            for (int i = col; i < Math.Min(col + colSpan, colCount); i++)
            {
                if (ColumnDefinitions[i].Width.GridUnitType == GridUnitType.Auto)
                    colWidths[i] = Math.Max(colWidths[i], childSize.Width / colSpan);
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
        // 如果行列定义发生变化，清除缓存
        bool shouldClearCache = false;
        
        if (children.Count == 0)
        {
            if (RowDefinitions.Count == 0)
            {
                RowDefinitions.Add(new RowDefinitionInternal { Height = GridLength.Auto });
                shouldClearCache = true;
            }
            if (ColumnDefinitions.Count == 0)
            {
                ColumnDefinitions.Add(new ColumnDefinitionInternal { Width = GridLength.Auto });
                shouldClearCache = true;
            }
            if (shouldClearCache) ClearCache();
            return;
        }
        
        // 找到最大的行列索引
        int maxRow = 0, maxCol = 0;
        foreach (var child in children)
        {
            maxRow = Math.Max(maxRow, GetRow(child) + GetRowSpan(child));
            maxCol = Math.Max(maxCol, GetColumn(child) + GetColumnSpan(child));
        }
        
        // 确保行列定义数量足够（默认为 Auto）
        if (RowDefinitions.Count < maxRow)
        {
            while (RowDefinitions.Count < maxRow)
                RowDefinitions.Add(new RowDefinitionInternal { Height = GridLength.Auto });
            shouldClearCache = true;
        }
        if (ColumnDefinitions.Count < maxCol)
        {
            while (ColumnDefinitions.Count < maxCol)
                ColumnDefinitions.Add(new ColumnDefinitionInternal { Width = GridLength.Auto });
            shouldClearCache = true;
        }
        
        if (shouldClearCache) ClearCache();
    }
    
    /// <summary>
    /// 清除缓存的行列尺寸
    /// </summary>
    private void ClearCache()
    {
        _cachedRowHeights = null;
        _cachedColWidths = null;
        _cachedRowOffsets = null;
        _cachedColOffsets = null;
        _cachedRowCount = 0;
        _cachedColCount = 0;
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
    private SKSize? _measuredSize;
    
    public override SKSize Measure(SKCanvas canvas, float availableWidth, float availableHeight)
    {
        if (Children.Count == 0)
        {
            _measuredSize = new SKSize(0, 0);
            return _measuredSize.Value;
        }
        
        // 测量第一个子元素
        _measuredSize = Children[0].Measure(canvas, availableWidth, availableHeight);
        return _measuredSize.Value;
    }
    
    public override void Arrange(SKCanvas canvas, float x, float y, float width, float height)
    {
        // 设置自身位置和尺寸
        X = x;
        Y = y;
        Width = width;
        Height = height;
        
        if (Children.Count == 0 || !_measuredSize.HasValue)
            return;
        
        var childSize = _measuredSize.Value;
        
        // 应用水平对齐
        float childX = x;
        float childW = width;
        
        bool hasRequestedWidth = RequestedWidth.HasValue;
        bool hasMaxWidth = MaxWidth.HasValue;
        
        if (hasRequestedWidth || hasMaxWidth || HorizontalAlignment == HorizontalAlignment.Left)
        {
            childW = Math.Min(childSize.Width, width);
        }
        else if (HorizontalAlignment == HorizontalAlignment.Center)
        {
            childX = x + (width - childSize.Width) / 2;
            childW = Math.Min(childSize.Width, width);
        }
        else if (HorizontalAlignment == HorizontalAlignment.Right)
        {
            childX = x + width - childSize.Width;
            childW = Math.Min(childSize.Width, width);
        }
        // Stretch: 使用 width
        
        // 应用垂直对齐
        float childY = y;
        float childH = height;
        
        bool hasRequestedHeight = RequestedHeight.HasValue;
        bool hasMaxHeight = MaxHeight.HasValue;
        
        if (hasRequestedHeight || hasMaxHeight || VerticalAlignment == VerticalAlignment.Top)
        {
            childH = Math.Min(childSize.Height, height);
        }
        else if (VerticalAlignment == VerticalAlignment.Center)
        {
            childY = y + (height - childSize.Height) / 2;
            childH = Math.Min(childSize.Height, height);
        }
        else if (VerticalAlignment == VerticalAlignment.Bottom)
        {
            childY = y + height - childSize.Height;
            childH = Math.Min(childSize.Height, height);
        }
        // Stretch: 使用 height
        
        Children[0].Arrange(canvas, childX, childY, childW, childH);
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
