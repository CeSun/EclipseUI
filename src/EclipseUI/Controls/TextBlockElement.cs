using SkiaSharp;
using EclipseUI.Core;
using EclipseUI.Rendering;

namespace EclipseUI.Controls;

/// <summary>
/// 文本块元素
/// </summary>
public class TextBlockElement : EclipseElement
{
    public string Text { get; set; } = "";
    public float FontSize { get; set; } = 14;
    public SKColor TextColor { get; set; } = SKColors.Black;
    public bool IsBold { get; set; }
    
    public override SKSize Measure(SKCanvas canvas, float availableWidth, float availableHeight)
    {
        var width = TextRenderer.MeasureText(Text, FontSize);
        var height = FontSize * 1.2f; // 估算行高
        
        return new SKSize(
            width + PaddingLeft + PaddingRight, 
            height + PaddingTop + PaddingBottom
        );
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
            
            // 绘制内容
            RenderContent(canvas);
        }
        finally
        {
            canvas.Restore();
        }
    }
    
    protected override void RenderContent(SKCanvas canvas)
    {
        if (string.IsNullOrEmpty(Text)) return;
        
        // 创建 SkiaRenderContext
        using var renderContext = new SkiaRenderContext(canvas);
        
        // 计算基线
        var baselineY = Y + PaddingTop + FontSize;
        
        // 使用 TextRenderer 绘制（自动处理 Emoji）
        TextRenderer.DrawText(renderContext, Text, X + PaddingLeft, baselineY, FontSize, 
            new Color(TextColor.Red, TextColor.Green, TextColor.Blue, TextColor.Alpha));
    }
}
