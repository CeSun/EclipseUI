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
        // 计算可用宽度（减去 Padding）
        float contentWidth = availableWidth - PaddingLeft - PaddingRight;
        
        // 应用 MaxWidth 限制
        if (MaxWidth.HasValue)
        {
            contentWidth = Math.Min(contentWidth, MaxWidth.Value - PaddingLeft - PaddingRight);
        }
        
        // 测量文本（考虑换行）
        var lines = TextRenderer.MeasureTextWithWrap(Text, FontSize, contentWidth);
        var totalHeight = lines.Height + PaddingTop + PaddingBottom;
        var maxWidth = lines.Width + PaddingLeft + PaddingRight;
        
        return new SKSize(maxWidth, totalHeight);
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
        
        // 计算可用宽度
        float contentWidth = Width - PaddingLeft - PaddingRight;
        
        // 计算基线
        var baselineY = Y + PaddingTop + FontSize;
        
        // 使用 TextRenderer 绘制（自动处理 Emoji 和换行）
        TextRenderer.DrawTextWithWrap(renderContext, Text, X + PaddingLeft, baselineY, FontSize, 
            new Color(TextColor.Red, TextColor.Green, TextColor.Blue, TextColor.Alpha), contentWidth);
    }
}
