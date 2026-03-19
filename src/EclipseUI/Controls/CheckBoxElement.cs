using SkiaSharp;
using EclipseUI.Core;
using EclipseUI.Rendering;

namespace EclipseUI.Controls;

/// <summary>
/// 复选框元素 - iOS 风格
/// </summary>
public class CheckBoxElement : EclipseElement
{
    public bool? IsChecked { get; set; }
    public string Content { get; set; } = "";
    public bool IsThreeState { get; set; }
    public float FontSize { get; set; } = iOSTheme.FontSizeBody;
    public SKColor TextColor { get; set; } = iOSTheme.LabelPrimary;
    
    public bool IsHovered { get; set; }
    public bool IsPressed { get; set; }
    
    // iOS 风格：圆形勾选框
    private const float CheckBoxSize = 22;
    private const float Spacing = 10;
    
    public Func<bool?, Task>? OnCheckedChanged { get; set; }
    public Action<EclipseElement, SKPoint>? OnClick { get; set; }
    
    public override SKSize Measure(SKCanvas canvas, float availableWidth, float availableHeight)
    {
        var textWidth = TextRenderer.MeasureText(Content, FontSize);
        float contentWidth = CheckBoxSize + Spacing + textWidth;
        float contentHeight = Math.Max(CheckBoxSize, FontSize + 8);
        
        float finalWidth = RequestedWidth ?? contentWidth;
        float finalHeight = RequestedHeight ?? Math.Max(contentHeight, 44); // iOS 最小触摸目标
        
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
        float textY = Y + Height / 2 + FontSize / 3;
        
        float centerX = checkBoxX + CheckBoxSize / 2;
        float centerY = checkBoxY + CheckBoxSize / 2;
        float radius = CheckBoxSize / 2;
        
        if (IsChecked == true)
        {
            // 选中状态：蓝色填充圆形 + 白色勾
            using var fillPaint = new SKPaint 
            { 
                Color = iOSTheme.SystemBlue, 
                IsAntialias = true 
            };
            canvas.DrawCircle(centerX, centerY, radius, fillPaint);
            
            // 绘制白色勾选标记
            using var checkPaint = new SKPaint 
            { 
                Color = SKColors.White, 
                IsAntialias = true,
                StrokeWidth = 2.5f,
                Style = SKPaintStyle.Stroke,
                StrokeCap = SKStrokeCap.Round,
                StrokeJoin = SKStrokeJoin.Round
            };
            
            using var path = new SKPath();
            path.MoveTo(centerX - 5, centerY);
            path.LineTo(centerX - 1, centerY + 4);
            path.LineTo(centerX + 6, centerY - 4);
            canvas.DrawPath(path, checkPaint);
        }
        else if (IsChecked == null && IsThreeState)
        {
            // 不确定状态：蓝色填充圆形 + 白色横线
            using var fillPaint = new SKPaint 
            { 
                Color = iOSTheme.SystemBlue, 
                IsAntialias = true 
            };
            canvas.DrawCircle(centerX, centerY, radius, fillPaint);
            
            using var linePaint = new SKPaint 
            { 
                Color = SKColors.White, 
                IsAntialias = true,
                StrokeWidth = 2.5f,
                StrokeCap = SKStrokeCap.Round
            };
            canvas.DrawLine(centerX - 5, centerY, centerX + 5, centerY, linePaint);
        }
        else
        {
            // 未选中状态：灰色边框圆形
            using var borderPaint = new SKPaint 
            { 
                Color = iOSTheme.SystemGray3, 
                IsAntialias = true,
                StrokeWidth = 2f,
                Style = SKPaintStyle.Stroke
            };
            canvas.DrawCircle(centerX, centerY, radius - 1, borderPaint);
        }
        
        // 绘制文本
        if (!string.IsNullOrEmpty(Content))
        {
            using var renderContext = new SkiaRenderContext(canvas);
            TextRenderer.DrawText(renderContext, Content, textX, textY, FontSize,
                new Color(TextColor.Red, TextColor.Green, TextColor.Blue, TextColor.Alpha));
        }
    }
    
    public override bool HandleClick(SKPoint point)
    {
        if (!IsVisible) return false;
        
        var rect = new SKRect(X, Y, X + Width, Y + Height);
        if (!rect.Contains(point)) return false;
        
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
            IsChecked = IsChecked != true;
        }
        
        // 触发回调（不等待，让 UI 立即响应）
        _ = OnCheckedChanged?.Invoke(IsChecked);
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
