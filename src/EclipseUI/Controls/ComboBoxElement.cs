using SkiaSharp;
using EclipseUI.Core;
using EclipseUI.Rendering;

namespace EclipseUI.Controls;

/// <summary>
/// 下拉选择框元素 - iOS 风格
/// </summary>
public class ComboBoxElement : EclipseElement
{
    public IList<string> ItemsSource { get; set; } = new List<string>();
    public int SelectedIndex { get; set; } = -1;
    public string? SelectedItem { get; set; }
    public string Placeholder { get; set; } = "请选择...";
    public float FontSize { get; set; } = iOSTheme.FontSizeBody;
    public SKColor TextColor { get; set; } = iOSTheme.LabelPrimary;
    public new SKColor? BackgroundColor { get; set; } = iOSTheme.SystemGray6;
    public float CornerRadius { get; set; } = iOSTheme.CornerRadiusMedium;
    
    public bool IsDropDownOpen { get; set; }
    public bool IsHovered { get; set; }
    
    public float MaxDropDownHeight { get; set; } = 200;
    private const float ItemHeight = 44; // iOS 标准行高
    private float _dropDownScrollOffset = 0;
    private int _hoveredItemIndex = -1;
    
    public Func<int, string?, Task>? OnItemSelected { get; set; }
    
    public override SKSize Measure(SKCanvas canvas, float availableWidth, float availableHeight)
    {
        float maxTextWidth = 0;
        foreach (var item in ItemsSource)
        {
            var textWidth = TextRenderer.MeasureText(item, FontSize);
            maxTextWidth = Math.Max(maxTextWidth, textWidth);
        }
        
        float contentWidth = maxTextWidth + PaddingLeft + PaddingRight + 32;
        float contentHeight = FontSize + PaddingTop + PaddingBottom + 16;
        
        float finalWidth = RequestedWidth ?? contentWidth;
        float finalHeight = RequestedHeight ?? Math.Max(contentHeight, 44); // iOS 最小触摸目标
        
        if (MinWidth.HasValue) finalWidth = Math.Max(finalWidth, MinWidth.Value);
        if (MinHeight.HasValue) finalHeight = Math.Max(finalHeight, MinHeight.Value);
        if (MaxWidth.HasValue) finalWidth = Math.Min(finalWidth, MaxWidth.Value);
        if (MaxHeight.HasValue) finalHeight = Math.Min(finalHeight, MaxHeight.Value);
        
        return new SKSize(finalWidth, finalHeight);
    }
    
    private PopupInfo? _popupInfo;
    
    public override void Render(SKCanvas canvas)
    {
        if (!IsVisible) return;
        
        canvas.Save();
        try
        {
            var rect = new SKRect(X, Y, X + Width, Y + Height);
            
            // iOS 风格背景
            var bgColor = BackgroundColor ?? iOSTheme.SystemGray6;
            using var bgPaint = new SKPaint { Color = bgColor, IsAntialias = true };
            canvas.DrawRoundRect(rect, CornerRadius, CornerRadius, bgPaint);
            
            // 边框：展开时蓝色，否则浅灰色
            var borderColor = IsDropDownOpen ? iOSTheme.SystemBlue : iOSTheme.SystemGray4;
            var borderWidth = IsDropDownOpen ? 2f : 1f;
            using var borderPaint = new SKPaint 
            { 
                Color = borderColor, 
                IsAntialias = true,
                StrokeWidth = borderWidth,
                Style = SKPaintStyle.Stroke
            };
            var borderRect = new SKRect(X + borderWidth / 2, Y + borderWidth / 2, X + Width - borderWidth / 2, Y + Height - borderWidth / 2);
            canvas.DrawRoundRect(borderRect, CornerRadius - borderWidth / 2, CornerRadius - borderWidth / 2, borderPaint);
            
            RenderContent(canvas);
        }
        finally
        {
            canvas.Restore();
        }
    }
    
    protected override void RenderContent(SKCanvas canvas)
    {
        float contentX = X + PaddingLeft + 12;
        float contentY = Y + Height / 2 + FontSize / 3;  // 垂直居中
        
        using var renderContext = new SkiaRenderContext(canvas);
        
        string displayText = SelectedIndex >= 0 && SelectedIndex < ItemsSource.Count 
            ? ItemsSource[SelectedIndex] 
            : Placeholder;
        
        Color textColor;
        if (string.IsNullOrEmpty(displayText))
        {
            displayText = Placeholder;
            textColor = new Color(153, 153, 153, 255); // 灰色占位符
        }
        else if (SelectedIndex < 0)
        {
            textColor = new Color(153, 153, 153, 255); // 灰色占位符
        }
        else
        {
            textColor = new Color(TextColor.Red, TextColor.Green, TextColor.Blue, TextColor.Alpha);
        }
        
        // contentY 已经是垂直居中位置，不需要再加 FontSize
        TextRenderer.DrawText(renderContext, displayText, contentX, contentY, FontSize, textColor);
        
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
        
        canvas.Save();
        
        try
        {
            // 设置裁剪区域
            canvas.ClipRect(new SKRect(dropDownX, dropDownY, dropDownX + dropDownWidth, dropDownY + dropDownHeight));
            
            // 绘制下拉列表背景
            using var bgPaint = new SKPaint { Color = SKColors.White, IsAntialias = true };
            canvas.DrawRect(new SKRect(dropDownX, dropDownY, dropDownX + dropDownWidth, dropDownY + dropDownHeight), bgPaint);
            
            // 绘制下拉列表边框
            using var borderPaint = new SKPaint { Color = SKColors.Gray, IsAntialias = true, StrokeWidth = 1, Style = SKPaintStyle.Stroke };
            canvas.DrawRect(new SKRect(dropDownX + 0.5f, dropDownY + 0.5f, dropDownX + dropDownWidth - 0.5f, dropDownY + dropDownHeight - 0.5f), borderPaint);
            
            using var renderContext = new SkiaRenderContext(canvas);
            
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
                
                // 使用 TextRenderer 绘制文本（支持 Emoji）
                var textColor = i == SelectedIndex 
                    ? new Color(0, 0, 255, 255) 
                    : new Color(0, 0, 0, 255);
                TextRenderer.DrawText(renderContext, ItemsSource[i], dropDownX + PaddingLeft, itemY + FontSize + 8, FontSize, textColor);
            }
        }
        finally
        {
            canvas.Restore();
        }
    }
    
    public override bool HandleClick(SKPoint point)
    {
        if (!IsVisible) return false;
        
        var rect = new SKRect(X, Y, X + Width, Y + Height);
        
        // 点击 ComboBox 主体
        if (rect.Contains(point))
        {
            if (IsDropDownOpen)
            {
                CloseDropDown();
            }
            else
            {
                OpenDropDown();
            }
            return true;
        }
        
        return false;
    }
    
    private void OpenDropDown()
    {
        IsDropDownOpen = true;
        _hoveredItemIndex = -1;
        
        var renderer = EclipseComponentBase.CurrentRenderer;
        if (renderer != null)
        {
            float dropDownY = Y + Height;
            float dropDownHeight = Math.Min(ItemsSource.Count * ItemHeight, MaxDropDownHeight);
            
            _popupInfo = new PopupInfo
            {
                Owner = this,
                Bounds = new SKRect(X, dropDownY, X + Width, dropDownY + dropDownHeight),
                RenderAction = RenderDropDown,
                HandleClick = HandleDropDownClick,
                HandleMouseMove = HandleDropDownMouseMove,
                HandleMouseWheel = HandleDropDownMouseWheel,
                CloseOnClickOutside = true,
                OnClose = () =>
                {
                    IsDropDownOpen = false;
                    _popupInfo = null;
                }
            };
            
            renderer.PopupService.Show(_popupInfo);
        }
    }
    
    private void CloseDropDown()
    {
        IsDropDownOpen = false;
        
        if (_popupInfo != null)
        {
            EclipseComponentBase.CurrentRenderer?.PopupService.Close(_popupInfo);
            _popupInfo = null;
        }
    }
    
    private bool HandleDropDownClick(SKPoint point)
    {
        float dropDownY = Y + Height;
        float dropDownHeight = Math.Min(ItemsSource.Count * ItemHeight, MaxDropDownHeight);
        var dropDownRect = new SKRect(X, dropDownY, X + Width, dropDownY + dropDownHeight);
        
        if (dropDownRect.Contains(point))
        {
            int clickedIndex = (int)((point.Y - dropDownY + _dropDownScrollOffset) / ItemHeight);
            if (clickedIndex >= 0 && clickedIndex < ItemsSource.Count)
            {
                SelectedIndex = clickedIndex;
                SelectedItem = ItemsSource[clickedIndex];
                CloseDropDown();
                OnItemSelected?.Invoke(clickedIndex, SelectedItem);
                return true;
            }
        }
        
        return false;
    }
    
    private bool HandleDropDownMouseMove(SKPoint point)
    {
        float dropDownY = Y + Height;
        float dropDownHeight = Math.Min(ItemsSource.Count * ItemHeight, MaxDropDownHeight);
        
        if (point.Y >= dropDownY && point.Y <= dropDownY + dropDownHeight &&
            point.X >= X && point.X <= X + Width)
        {
            _hoveredItemIndex = (int)((point.Y - dropDownY + _dropDownScrollOffset) / ItemHeight);
            return true;
        }
        else
        {
            _hoveredItemIndex = -1;
        }
        
        return false;
    }
    
    private bool HandleDropDownMouseWheel(float deltaY)
    {
        float dropDownHeight = Math.Min(ItemsSource.Count * ItemHeight, MaxDropDownHeight);
        float maxScroll = Math.Max(0, (ItemsSource.Count * ItemHeight) - dropDownHeight);
        
        _dropDownScrollOffset -= deltaY * 20;
        _dropDownScrollOffset = Math.Max(0, Math.Min(_dropDownScrollOffset, maxScroll));
        
        return true;
    }
    
    public override bool HandleMouseMove(float x, float y)
    {
        var rect = new SKRect(X, Y, X + Width, Y + Height);
        var wasHovered = IsHovered;
        IsHovered = rect.Contains(new SKPoint(x, y));
        return IsHovered != wasHovered;
    }
    
    public override void HandleMouseLeave()
    {
        IsHovered = false;
        _hoveredItemIndex = -1;
    }
}
