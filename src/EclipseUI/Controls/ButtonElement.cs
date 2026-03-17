using SkiaSharp;
using EclipseUI.Core;
using EclipseUI.Rendering;

namespace EclipseUI.Controls;

/// <summary>
/// 按钮元素
/// </summary>
public class ButtonElement : EclipseElement
{
    public string Text { get; set; } = "Button";
    public float FontSize { get; set; } = 14;
    public SKColor TextColor { get; set; } = SKColors.White;
    public SKColor ButtonColor { get; set; } = SKColors.Blue;
    public float CornerRadius { get; set; } = 4;
    public bool IsHovered { get; set; }
    public bool IsPressed { get; set; }
    
    public override SKSize Measure(SKCanvas canvas, float availableWidth, float availableHeight)
    {
        var textWidth = TextRenderer.MeasureText(Text, FontSize);
        
        // 计算内容尺寸（基于文本，而不是 availableWidth/Height）
        float contentWidth = Math.Max(textWidth + 24, 80);
        float contentHeight = Math.Max(FontSize + 16, 36);
        
        // 应用用户设置的尺寸
        float finalWidth = RequestedWidth ?? contentWidth;
        float finalHeight = RequestedHeight ?? contentHeight;
        
        // 应用 Min/Max 限制
        if (MinWidth.HasValue) finalWidth = Math.Max(finalWidth, MinWidth.Value);
        if (MinHeight.HasValue) finalHeight = Math.Max(finalHeight, MinHeight.Value);
        if (MaxWidth.HasValue) finalWidth = Math.Min(finalWidth, MaxWidth.Value);
        if (MaxHeight.HasValue) finalHeight = Math.Min(finalHeight, MaxHeight.Value);
        
        // 不要超过可用空间
        finalWidth = Math.Min(finalWidth, availableWidth);
        finalHeight = Math.Min(finalHeight, availableHeight);
        
        return new SKSize(finalWidth, finalHeight);
    }
    
    protected override void RenderContent(SKCanvas canvas)
    {
        var color = IsPressed ? SKColors.DarkBlue : (IsHovered ? SKColors.LightBlue : ButtonColor);
        var rect = new SKRect(X, Y, X + Width, Y + Height);
        
        // 绘制背景
        using var bgPaint = new SKPaint { Color = color, IsAntialias = true };
        if (CornerRadius > 0)
        {
            using var path = new SKPath();
            path.AddRoundRect(rect, CornerRadius, CornerRadius);
            canvas.DrawPath(path, bgPaint);
        }
        else
        {
            canvas.DrawRect(rect, bgPaint);
        }
        
        // 创建渲染上下文
        using var renderContext = new SkiaRenderContext(canvas);
        
        // 计算文本居中位置
        var textX = X + Width / 2;
        var textY = Y + Height / 2 + FontSize / 2;
        
        // 使用 TextRenderer 绘制文本（自动处理 Emoji）
        TextRenderer.DrawText(renderContext, Text, textX, textY, FontSize, 
            new Color(TextColor.Red, TextColor.Green, TextColor.Blue, TextColor.Alpha), SKTextAlign.Center);
    }
    
    public override bool HandleClick(SKPoint point)
    {
        if (!IsVisible) return false;
        
        var rect = new SKRect(X, Y, X + Width, Y + Height);
        if (!rect.Contains(point)) return false;
        
        // 触发点击回调
        Console.WriteLine($"[ButtonElement.HandleClick] '{Text}' clicked, OnClick: {OnClick != null}");
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
