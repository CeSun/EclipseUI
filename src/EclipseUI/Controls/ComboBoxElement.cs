using SkiaSharp;
using EclipseUI.Core;
using EclipseUI.Rendering;

namespace EclipseUI.Controls;

/// <summary>
/// 下拉选择框元素
/// </summary>
public class ComboBoxElement : EclipseElement
{
    public IList<string> ItemsSource { get; set; } = new List<string>();
    public int SelectedIndex { get; set; } = -1;
    public string? SelectedItem { get; set; }
    public string Placeholder { get; set; } = "请选择...";
    public float FontSize { get; set; } = 14;
    public SKColor TextColor { get; set; } = SKColors.Black;
    public SKColor? BackgroundColor { get; set; } = SKColors.White;
    
    public bool IsDropDownOpen { get; set; }
    public bool IsHovered { get; set; }
    
    /// <summary>
    /// 下拉列表最大高度
    /// </summary>
    public float MaxDropDownHeight { get; set; } = 200;
    
    /// <summary>
    /// 下拉列表项高度
    /// </summary>
    private const float ItemHeight = 36;
    
    /// <summary>
    /// 下拉列表滚动偏移
    /// </summary>
    private float _dropDownScrollOffset = 0;
    
    /// <summary>
    /// 悬停的项索引
    /// </summary>
    private int _hoveredItemIndex = -1;
    
    /// <summary>
    /// 项选择回调
    /// </summary>
    public Func<int, string?, Task>? OnItemSelected { get; set; }
    
    public override SKSize Measure(SKCanvas canvas, float availableWidth, float availableHeight)
    {
        // 测量最长文本的宽度
        float maxTextWidth = 0;
        foreach (var item in ItemsSource)
        {
            var textWidth = TextRenderer.MeasureText(item, FontSize);
            maxTextWidth = Math.Max(maxTextWidth, textWidth);
        }
        
        // 计算内容宽度（包括下拉箭头）
        float contentWidth = maxTextWidth + PaddingLeft + PaddingRight + 24;
        float contentHeight = FontSize + PaddingTop + PaddingBottom + 8;
        
        // 应用用户设置的尺寸
        float finalWidth = RequestedWidth ?? contentWidth;
        float finalHeight = RequestedHeight ?? contentHeight;
        
        // 应用 Min/Max 限制
        if (MinWidth.HasValue) finalWidth = Math.Max(finalWidth, MinWidth.Value);
        if (MinHeight.HasValue) finalHeight = Math.Max(finalHeight, MinHeight.Value);
        if (MaxWidth.HasValue) finalWidth = Math.Min(finalWidth, MaxWidth.Value);
        if (MaxHeight.HasValue) finalHeight = Math.Min(finalHeight, MaxHeight.Value);
        
        return new SKSize(finalWidth, finalHeight);
    }
    
    public override void Render(SKCanvas canvas)
    {
        if (!IsVisible) return;
        
        canvas.Save();
        
        try
        {
            // 绘制背景
            var bgColor = BackgroundColor ?? SKColors.White;
            var rect = new SKRect(X, Y, X + Width, Y + Height);
            using var bgPaint = new SKPaint { Color = bgColor, IsAntialias = true };
            canvas.DrawRect(rect, bgPaint);
            
            // 绘制边框
            var borderColor = IsDropDownOpen ? SKColors.Blue : (IsHovered ? SKColors.LightGray : SKColors.Gray);
            var borderWidth = IsDropDownOpen ? 2f : 1f;
            using var borderPaint = new SKPaint 
            { 
                Color = borderColor, 
                IsAntialias = true,
                StrokeWidth = borderWidth
            };
            var borderRect = new SKRect(
                X + borderWidth / 2, 
                Y + borderWidth / 2, 
                X + Width - borderWidth / 2, 
                Y + Height - borderWidth / 2
            );
            canvas.DrawRect(borderRect, borderPaint);
            
            // 绘制内容
            RenderContent(canvas);
            
            // 绘制下拉列表
            if (IsDropDownOpen)
            {
                RenderDropDown(canvas);
            }
        }
        finally
        {
            canvas.Restore();
        }
    }
    
    protected override void RenderContent(SKCanvas canvas)
    {
        float contentX = X + PaddingLeft;
        float contentY = Y + PaddingTop;
        
        using var renderContext = new SkiaRenderContext(canvas);
        using var typeface = SKTypeface.FromFamilyName("Microsoft YaHei", SKFontStyle.Normal);
        using var textPaint = new SKPaint
        {
            TextSize = FontSize,
            IsAntialias = true,
            Typeface = typeface,
            Color = TextColor
        };
        
        // 绘制选中的文本或占位符
        string displayText = SelectedIndex >= 0 && SelectedIndex < ItemsSource.Count 
            ? ItemsSource[SelectedIndex] 
            : Placeholder;
        
        if (string.IsNullOrEmpty(displayText))
        {
            displayText = Placeholder;
            textPaint.Color = new SKColor(153, 153, 153); // 灰色占位符
        }
        
        canvas.DrawText(displayText, contentX, contentY + FontSize, textPaint);
        
        // 绘制下拉箭头
        DrawDropDownArrow(canvas);
    }
    
    /// <summary>
    /// 绘制下拉箭头
    /// </summary>
    private void DrawDropDownArrow(SKCanvas canvas)
    {
        float arrowX = X + Width - 24;
        float arrowY = Y + Height / 2;
        float arrowSize = 6;
        
        using var arrowPaint = new SKPaint 
        { 
            Color = SKColors.Gray, 
            IsAntialias = true,
            StrokeWidth = 2,
            Style = SKPaintStyle.Stroke
        };
        
        using var path = new SKPath();
        path.MoveTo(arrowX - arrowSize, arrowY - 2);
        path.LineTo(arrowX, arrowY + arrowSize - 2);
        path.LineTo(arrowX + arrowSize, arrowY - 2);
        path.Close();
        
        canvas.DrawPath(path, arrowPaint);
    }
    
    /// <summary>
    /// 绘制下拉列表
    /// </summary>
    private void RenderDropDown(SKCanvas canvas)
    {
        float dropDownX = X;
        float dropDownY = Y + Height;
        float dropDownWidth = Width;
        float dropDownHeight = Math.Min(ItemsSource.Count * ItemHeight, MaxDropDownHeight);
        
        // 设置裁剪区域
        using var clipPath = new SKPath();
        clipPath.AddRect(new SKRect(dropDownX, dropDownY, dropDownX + dropDownWidth, dropDownY + dropDownHeight));
        canvas.ClipPath(clipPath);
        
        // 绘制下拉列表背景
        using var bgPaint = new SKPaint { Color = SKColors.White, IsAntialias = true };
        canvas.DrawRect(new SKRect(dropDownX, dropDownY, dropDownX + dropDownWidth, dropDownY + dropDownHeight), bgPaint);
        
        // 绘制下拉列表边框
        using var borderPaint = new SKPaint { Color = SKColors.Gray, IsAntialias = true, StrokeWidth = 1, Style = SKPaintStyle.Stroke };
        canvas.DrawRect(new SKRect(dropDownX + 0.5f, dropDownY + 0.5f, dropDownX + dropDownWidth - 0.5f, dropDownY + dropDownHeight - 0.5f), borderPaint);
        
        using var renderContext = new SkiaRenderContext(canvas);
        using var typeface = SKTypeface.FromFamilyName("Microsoft YaHei", SKFontStyle.Normal);
        using var textPaint = new SKPaint
        {
            TextSize = FontSize,
            IsAntialias = true,
            Typeface = typeface
        };
        
        // 绘制列表项
        int visibleStartIndex = (int)(_dropDownScrollOffset / ItemHeight);
        int visibleEndIndex = Math.Min(visibleStartIndex + (int)(dropDownHeight / ItemHeight) + 1, ItemsSource.Count);
        
        for (int i = visibleStartIndex; i < visibleEndIndex; i++)
        {
            float itemY = dropDownY + (i * ItemHeight) - _dropDownScrollOffset;
            
            // 绘制选中项背景
            if (i == SelectedIndex)
            {
                using var selectedPaint = new SKPaint { Color = SKColor.Parse("#E3F2FD"), IsAntialias = true };
                canvas.DrawRect(new SKRect(dropDownX, itemY, dropDownX + dropDownWidth, itemY + ItemHeight), selectedPaint);
            }
            // 绘制悬停项背景
            else if (i == _hoveredItemIndex)
            {
                using var hoverPaint = new SKPaint { Color = SKColor.Parse("#F5F5F5"), IsAntialias = true };
                canvas.DrawRect(new SKRect(dropDownX, itemY, dropDownX + dropDownWidth, itemY + ItemHeight), hoverPaint);
            }
            
            // 绘制文本
            textPaint.Color = i == SelectedIndex ? SKColors.Blue : SKColors.Black;
            canvas.DrawText(ItemsSource[i], dropDownX + PaddingLeft, itemY + FontSize + 8, textPaint);
        }
    }
    
    public override bool HandleClick(SKPoint point)
    {
        if (!IsVisible) return false;
        
        var rect = new SKRect(X, Y, X + Width, Y + Height);
        
        // 如果点击在下拉列表区域
        if (IsDropDownOpen)
        {
            float dropDownY = Y + Height;
            float dropDownHeight = Math.Min(ItemsSource.Count * ItemHeight, MaxDropDownHeight);
            var dropDownRect = new SKRect(X, dropDownY, X + Width, dropDownY + dropDownHeight);
            
            if (dropDownRect.Contains(point))
            {
                // 计算点击的项索引
                int clickedIndex = (int)((point.Y - dropDownY + _dropDownScrollOffset) / ItemHeight);
                if (clickedIndex >= 0 && clickedIndex < ItemsSource.Count)
                {
                    SelectedIndex = clickedIndex;
                    SelectedItem = ItemsSource[clickedIndex];
                    IsDropDownOpen = false;
                    OnItemSelected?.Invoke(clickedIndex, SelectedItem);
                    return true;
                }
            }
            else
            {
                // 点击外部，关闭下拉列表
                IsDropDownOpen = false;
                return true;
            }
        }
        
        // 点击 ComboBox 主体
        if (rect.Contains(point))
        {
            IsDropDownOpen = !IsDropDownOpen;
            _hoveredItemIndex = -1;
            return true;
        }
        
        return false;
    }
    
    public override bool HandleMouseMove(float x, float y)
    {
        var rect = new SKRect(X, Y, X + Width, Y + Height);
        var wasHovered = IsHovered;
        IsHovered = rect.Contains(new SKPoint(x, y));
        
        // 如果下拉列表打开，更新悬停项
        if (IsDropDownOpen)
        {
            float dropDownY = Y + Height;
            float dropDownHeight = Math.Min(ItemsSource.Count * ItemHeight, MaxDropDownHeight);
            
            if (y >= dropDownY && y <= dropDownY + dropDownHeight)
            {
                _hoveredItemIndex = (int)((y - dropDownY + _dropDownScrollOffset) / ItemHeight);
            }
            else
            {
                _hoveredItemIndex = -1;
            }
        }
        
        return IsHovered != wasHovered;
    }
    
    public override void HandleMouseLeave()
    {
        IsHovered = false;
        _hoveredItemIndex = -1;
    }
    
    public override bool HandleMouseWheel(float deltaY)
    {
        if (!IsDropDownOpen) return false;
        
        float dropDownHeight = Math.Min(ItemsSource.Count * ItemHeight, MaxDropDownHeight);
        float maxScroll = Math.Max(0, (ItemsSource.Count * ItemHeight) - dropDownHeight);
        
        _dropDownScrollOffset -= deltaY * 20;
        _dropDownScrollOffset = Math.Max(0, Math.Min(_dropDownScrollOffset, maxScroll));
        
        return true;
    }
}
