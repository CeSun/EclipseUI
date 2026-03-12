using SkiaSharp;
using EclipseUI.Core;

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
        using var paint = new SKPaint { TextSize = FontSize, IsAntialias = true };
        var bounds = new SKRect();
        paint.MeasureText(Text, ref bounds);
        return new SKSize(Math.Max(bounds.Width + 24, 80), Math.Max(bounds.Height + 16, 36));
    }
    
    protected override void RenderContent(SKCanvas canvas)
    {
        var color = IsPressed ? SKColors.DarkBlue : (IsHovered ? SKColors.LightBlue : ButtonColor);
        var rect = new SKRect(X, Y, X + Width, Y + Height);
        
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
        
        using var textPaint = new SKPaint
        {
            TextSize = FontSize,
            Color = TextColor,
            IsAntialias = true,
            TextAlign = SKTextAlign.Center,
            Typeface = TextBlockElement.GetChineseTypeface()
        };
        
        var bounds = new SKRect();
        textPaint.MeasureText(Text, ref bounds);
        canvas.DrawText(Text, X + Width / 2, Y + Height / 2 + bounds.Height / 4, textPaint);
    }
}
