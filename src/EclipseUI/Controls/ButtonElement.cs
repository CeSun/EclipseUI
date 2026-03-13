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
        return new SKSize(Math.Max(textWidth + 24, 80), Math.Max(FontSize + 16, 36));
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
}
