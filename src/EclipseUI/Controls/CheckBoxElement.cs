using SkiaSharp;
using EclipseUI.Core;
using EclipseUI.Rendering;

namespace EclipseUI.Controls;

/// <summary>
/// 复选框元素
/// </summary>
public class CheckBoxElement : EclipseElement
{
    public bool? IsChecked { get; set; }
    public string Content { get; set; } = "";
    public bool IsThreeState { get; set; }
    public float FontSize { get; set; } = 14;
    public SKColor TextColor { get; set; } = SKColors.Black;
    
    public bool IsHovered { get; set; }
    public bool IsPressed { get; set; }
    
    /// <summary>
    /// 复选框尺寸
    /// </summary>
    private const float CheckBoxSize = 18;
    
    /// <summary>
    /// 复选框与文本的间距
    /// </summary>
    private const float Spacing = 8;
    
    /// <summary>
    /// 选中状态变更回调
    /// </summary>
    public Func<bool?, Task>? OnCheckedChanged { get; set; }
    
    /// <summary>
    /// 点击事件
    /// </summary>
    public Action<EclipseElement, SKPoint>? OnClick { get; set; }
    
    public override SKSize Measure(SKCanvas canvas, float availableWidth, float availableHeight)
    {
        var textWidth = TextRenderer.MeasureText(Content, FontSize);
        float contentWidth = CheckBoxSize + Spacing + textWidth;
        float contentHeight = Math.Max(CheckBoxSize, FontSize + 4);
        
        float finalWidth = RequestedWidth ?? contentWidth;
        float finalHeight = RequestedHeight ?? contentHeight;
        
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
            RenderContent(canvas);
        }
        finally
        {
            canvas.Restore();
        }
    }
    
    protected override void RenderContent(SKCanvas canvas)
    {
        float checkBoxX = X;
        float checkBoxY = Y + (Height - CheckBoxSize) / 2;
        float textX = checkBoxX + CheckBoxSize + Spacing;
        float textY = Y + (Height + FontSize) / 2;
        
        // 绘制复选框背景
        using var bgPaint = new SKPaint 
        { 
            Color = IsChecked == true ? SKColors.Blue : SKColors.White, 
            IsAntialias = true 
        };
        
        var checkBoxRect = new SKRect(checkBoxX, checkBoxY, checkBoxX + CheckBoxSize, checkBoxY + CheckBoxSize);
        canvas.DrawRect(checkBoxRect, bgPaint);
        
        // 绘制复选框边框
        using var borderPaint = new SKPaint 
        { 
            Color = IsChecked == true ? SKColors.Blue : SKColors.Gray, 
            IsAntialias = true,
            StrokeWidth = 1.5f,
            Style = SKPaintStyle.Stroke
        };
        canvas.DrawRect(checkBoxRect, borderPaint);
        
        // 绘制勾选标记
        if (IsChecked == true)
        {
            using var checkPaint = new SKPaint 
            { 
                Color = SKColors.White, 
                IsAntialias = true,
                StrokeWidth = 2,
                Style = SKPaintStyle.Stroke
            };
            
            using var path = new SKPath();
            float checkStartX = checkBoxX + 4;
            float checkStartY = checkBoxY + CheckBoxSize / 2 + 1;
            float checkMidX = checkBoxX + CheckBoxSize / 2;
            float checkMidY = checkBoxY + CheckBoxSize - 5;
            float checkEndX = checkBoxX + CheckBoxSize - 4;
            float checkEndY = checkBoxY + 5;
            
            path.MoveTo(checkStartX, checkStartY);
            path.LineTo(checkMidX, checkMidY);
            path.LineTo(checkEndX, checkEndY);
            
            canvas.DrawPath(path, checkPaint);
        }
        else if (IsChecked == null && IsThreeState)
        {
            // 绘制不确定状态的横线
            using var linePaint = new SKPaint 
            { 
                Color = SKColors.White, 
                IsAntialias = true,
                StrokeWidth = 2
            };
            
            float lineStartX = checkBoxX + 4;
            float lineY = checkBoxY + CheckBoxSize / 2;
            float lineEndX = checkBoxX + CheckBoxSize - 4;
            
            canvas.DrawLine(lineStartX, lineY, lineEndX, lineY, linePaint);
        }
        
        // 绘制文本
        if (!string.IsNullOrEmpty(Content))
        {
            using var renderContext = new SkiaRenderContext(canvas);
            TextRenderer.DrawText(
                renderContext,
                Content,
                textX,
                textY,
                FontSize,
                new Color(TextColor.Red, TextColor.Green, TextColor.Blue, TextColor.Alpha)
            );
        }
    }
    
    public override bool HandleClick(SKPoint point)
    {
        if (!IsVisible) return false;
        
        var rect = new SKRect(X, Y, X + Width, Y + Height);
        if (!rect.Contains(point)) return false;
        
        // 切换选中状态
        if (IsThreeState)
        {
            IsChecked = IsChecked switch
            {
                false => true,
                true => null,
                null => false
            };
        }
        else
        {
            IsChecked = !IsChecked;
        }
        
        OnCheckedChanged?.Invoke(IsChecked);
        OnClick?.Invoke(this, point);
        
        return true;
    }
    
    public override bool HandleMouseDown(float x, float y)
    {
        var rect = new SKRect(X, Y, X + Width, Y + Height);
        if (rect.Contains(new SKPoint(x, y)))
        {
            IsPressed = true;
            return true;
        }
        return false;
    }
    
    public override bool HandleMouseMove(float x, float y)
    {
        var rect = new SKRect(X, Y, X + Width, Y + Height);
        var wasHovered = IsHovered;
        IsHovered = rect.Contains(new SKPoint(x, y));
        return IsHovered != wasHovered;
    }
    
    public override void HandleMouseUp()
    {
        IsPressed = false;
    }
    
    public override void HandleMouseLeave()
    {
        IsHovered = false;
        IsPressed = false;
    }
}
