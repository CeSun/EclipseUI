using SkiaSharp;
using Microsoft.AspNetCore.Components;
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
        
        // 使用 MeasureText 获取文字宽度
        var textWidth = paint.MeasureText(Text);
        
        // 使用 FontMetrics 获取准确的行高
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
        
        // 获取 FontMetrics 用于计算基线位置
        var metrics = paint.FontMetrics;
        
        // 基线位置 = Y + PaddingTop - metrics.Top（metrics.Top 通常是负值）
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

/// <summary>
/// Razor 组件
/// </summary>
public class TextBlock : EclipseComponentBase
{
    [Parameter] public string? Text { get; set; }
    [Parameter] public float FontSize { get; set; } = 14;
    [Parameter] public string? Foreground { get; set; }
    [Parameter] public bool FontWeight { get; set; }
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }
    
    protected override EclipseElement CreateElement()
    {
        return new TextBlockElement();
    }
    
    protected override void UpdateElementFromParameters()
    {
        if (_element is TextBlockElement tb)
        {
            tb.Text = Text ?? "";
            tb.FontSize = FontSize;
            tb.TextColor = ParseColor(Foreground);
            tb.IsBold = FontWeight;
            tb.OnClick = OnClick.HasDelegate ? (e, p) => { _ = OnClick.InvokeAsync(new MouseEventArgs { ClientX = p.X, ClientY = p.Y }); } : null;
        }
    }
    
    private static SKColor ParseColor(string? color)
    {
        if (!string.IsNullOrEmpty(color) && color.StartsWith('#') && color.Length == 7)
            return SKColor.Parse(color);
        return SKColors.Black;
    }
}
