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
        var contentHeight = lines.Height;
        var contentMaxWidth = lines.Width;
        
        // 计算内容尺寸（包含 Padding）
        // 如果设置了 MaxWidth，使用 MaxWidth 作为宽度（确保背景正确）
        float baseWidth = MaxWidth.HasValue 
            ? MaxWidth.Value 
            : (contentMaxWidth + PaddingLeft + PaddingRight);
        float baseHeight = contentHeight + PaddingTop + PaddingBottom;
        
        // 应用用户设置的尺寸
        float finalWidth = RequestedWidth ?? baseWidth;
        float finalHeight = RequestedHeight ?? baseHeight;
        
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
        
        // 计算可用宽度（使用 MaxWidth 如果设置了）
        float contentWidth = Width - PaddingLeft - PaddingRight;
        if (MaxWidth.HasValue)
        {
            contentWidth = Math.Min(contentWidth, MaxWidth.Value - PaddingLeft - PaddingRight);
        }
        
        // 测量文本高度
        var lines = TextRenderer.MeasureTextWithWrap(Text, FontSize, contentWidth);
        float textHeight = lines.Height;
        
        // 计算基线（根据垂直对齐方式）
        float contentHeight = Height - PaddingTop - PaddingBottom;
        float startY;
        
        if (VerticalAlignment == VerticalAlignment.Center)
        {
            // 垂直居中
            startY = Y + PaddingTop + (contentHeight - textHeight) / 2 + FontSize;
        }
        else if (VerticalAlignment == VerticalAlignment.Bottom)
        {
            // 底部对齐
            startY = Y + PaddingTop + (contentHeight - textHeight) + FontSize;
        }
        else
        {
            // 顶部对齐（默认）
            startY = Y + PaddingTop + FontSize;
        }
        
        // 使用 TextRenderer 绘制（自动处理 Emoji 和换行）
        TextRenderer.DrawTextWithWrap(renderContext, Text, X + PaddingLeft, startY, FontSize, 
            new Color(TextColor.Red, TextColor.Green, TextColor.Blue, TextColor.Alpha), contentWidth);
    }
}
