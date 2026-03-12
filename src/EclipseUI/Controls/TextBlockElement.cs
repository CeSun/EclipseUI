using SkiaSharp;
using EclipseUI.Core;

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
        using var paint = CreateTextPaint();
        
        var textWidth = paint.MeasureText(Text);
        var metrics = paint.FontMetrics;
        var textHeight = metrics.Bottom - metrics.Top;
        
        return new SKSize(
            textWidth + PaddingLeft + PaddingRight, 
            textHeight + PaddingTop + PaddingBottom
        );
    }
    
    protected override void RenderContent(SKCanvas canvas)
    {
        if (string.IsNullOrEmpty(Text)) return;
        
        using var paint = CreateTextPaint();
        var metrics = paint.FontMetrics;
        var baselineY = Y + PaddingTop - metrics.Top;
        
        canvas.DrawText(Text, X + PaddingLeft, baselineY, paint);
    }
    
    private SKPaint CreateTextPaint()
    {
        return new SKPaint
        {
            TextSize = FontSize,
            IsAntialias = true,
            Color = TextColor,
            Typeface = GetChineseTypeface()
        };
    }
    
    internal static SKTypeface? _chineseTypeface;
    
    internal static SKTypeface GetChineseTypeface()
    {
        if (_chineseTypeface == null)
        {
            var fontNames = new[] { "Microsoft YaHei", "SimSun", "SimHei", "KaiTi" };
            foreach (var fontName in fontNames)
            {
                try
                {
                    _chineseTypeface = SKTypeface.FromFamilyName(fontName, SKFontStyle.Normal);
                    if (_chineseTypeface != null) break;
                }
                catch { }
            }
            _chineseTypeface ??= SKTypeface.Default;
        }
        return _chineseTypeface;
    }
}
