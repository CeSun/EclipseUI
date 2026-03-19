using SkiaSharp;
using EclipseUI.Core;
using EclipseUI.Rendering;

namespace EclipseUI.Controls;

/// <summary>
/// 按钮元素 - iOS 风格
/// </summary>
public class ButtonElement : EclipseElement
{
    public string Text { get; set; } = "Button";
    public float FontSize { get; set; } = iOSTheme.FontSizeBody;
    public SKColor TextColor { get; set; } = SKColors.White;
    public SKColor ButtonColor { get; set; } = iOSTheme.SystemBlue;
    public float CornerRadius { get; set; } = iOSTheme.CornerRadiusMedium;
    public bool IsHovered { get; set; }
    public bool IsPressed { get; set; }
    
    public override SKSize Measure(SKCanvas canvas, float availableWidth, float availableHeight)
    {
        var textWidth = TextRenderer.MeasureText(Text, FontSize);
        
        // iOS 风格：更宽松的内边距
        float contentWidth = Math.Max(textWidth + 32, iOSTheme.ButtonMinWidth);
        float contentHeight = Math.Max(FontSize + 20, 44); // iOS 最小触摸目标 44pt
        
        float finalWidth = RequestedWidth ?? contentWidth;
        float finalHeight = RequestedHeight ?? contentHeight;
        
        if (MinWidth.HasValue) finalWidth = Math.Max(finalWidth, MinWidth.Value);
        if (MinHeight.HasValue) finalHeight = Math.Max(finalHeight, MinHeight.Value);
        if (MaxWidth.HasValue) finalWidth = Math.Min(finalWidth, MaxWidth.Value);
        if (MaxHeight.HasValue) finalHeight = Math.Min(finalHeight, MaxHeight.Value);
        
        finalWidth = Math.Min(finalWidth, availableWidth);
        finalHeight = Math.Min(finalHeight, availableHeight);
        
        return new SKSize(finalWidth, finalHeight);
    }
    
    protected override void RenderContent(SKCanvas canvas)
    {
        var rect = new SKRect(X, Y, X + Width, Y + Height);
        
        // iOS 风格：按下时整体变暗，而不是换颜色
        var color = IsPressed ? iOSTheme.GetPressedColor(ButtonColor) : ButtonColor;
        var alpha = IsPressed ? (byte)200 : (byte)255;
        color = color.WithAlpha(alpha);
        
        // 绘制背景
        using var bgPaint = new SKPaint { Color = color, IsAntialias = true };
        canvas.DrawRoundRect(rect, CornerRadius, CornerRadius, bgPaint);
        
        // 创建渲染上下文
        using var renderContext = new SkiaRenderContext(canvas);
        
        // 计算文本居中位置
        var textX = X + Width / 2;
        var textY = Y + Height / 2 + FontSize / 3; // 微调垂直居中
        
        // 绘制文本
        TextRenderer.DrawText(renderContext, Text, textX, textY, FontSize, 
            new Color(TextColor.Red, TextColor.Green, TextColor.Blue, TextColor.Alpha), SKTextAlign.Center);
    }
    
    public override bool HandleClick(SKPoint point)
    {
        if (!IsVisible) return false;
        
        var rect = new SKRect(X, Y, X + Width, Y + Height);
        if (!rect.Contains(point)) return false;
        
        OnClick?.Invoke(this, point);
        return true;
    }
    
    public override bool HandleMouseMove(float x, float y)
    {
        var rect = new SKRect(X, Y, X + Width, Y + Height);
        var point = new SKPoint(x, y);
        var wasHovered = IsHovered;
        IsHovered = rect.Contains(point);
        return IsHovered != wasHovered;
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
    
    public override void HandleMouseUp()
    {
        IsPressed = false;
        base.HandleMouseUp();
    }
}
