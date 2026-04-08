using Eclipse.Core;
using Eclipse.Core.Abstractions;
using Eclipse.Input;
using Eclipse.Rendering;
using System;

namespace Eclipse.Controls;

/// <summary>
/// 文本标签控件
/// </summary>
public class Label : ComponentBase
{
    public string? Text { get; set; }
    public override bool IsVisible => true;
    public double FontSize { get; set; } = 14;
    public Color Color { get; set; } = Color.Black;
    public string? FontWeight { get; set; }
    public string? FontFamily { get; set; }
    public TextAlignment TextAlignment { get; set; } = TextAlignment.Left;
    public Color BackgroundColor { get; set; } = Color.Transparent;
    public double Padding { get; set; } = 0;
    
    public override Size Measure(Size availableSize, IDrawingContext context)
    {
        if (string.IsNullOrEmpty(Text))
        {
            _desiredSize = Size.Zero;
            return _desiredSize;
        }
        
        var scaledFontSize = FontSize * context.Scale;
        var scaledPadding = Padding * context.Scale;
        var textWidth = context.MeasureText(Text, scaledFontSize, FontFamily);
        var lineHeight = scaledFontSize * 1.3;
        
        _desiredSize = new Size(textWidth + scaledPadding * 2, lineHeight + scaledPadding * 2);
        return _desiredSize;
    }
    
    public override void Build(IBuildContext context) { }
    
    public override void Render(IDrawingContext context, Rect bounds)
    {
        UpdateBounds(bounds);
        
        var scaledPadding = Padding * context.Scale;
        
        if (BackgroundColor != Color.Transparent)
            context.DrawRectangle(bounds, BackgroundColor);
        
        if (!string.IsNullOrEmpty(Text))
        {
            var scaledFontSize = FontSize * context.Scale;
            
            var textBounds = new Rect(
                bounds.X + scaledPadding,
                bounds.Y + scaledPadding,
                bounds.Width - scaledPadding * 2,
                bounds.Height - scaledPadding * 2);
            
            double x = textBounds.X;
            if (TextAlignment == TextAlignment.Center)
            {
                var textWidth = context.MeasureText(Text, scaledFontSize, FontFamily);
                x = textBounds.X + (textBounds.Width - textWidth) / 2;
            }
            else if (TextAlignment == TextAlignment.Right)
            {
                var textWidth = context.MeasureText(Text, scaledFontSize, FontFamily);
                x = textBounds.X + textBounds.Width - textWidth;
            }
            
            var y = textBounds.Y + scaledFontSize * 0.5;
            context.DrawText(Text, x, y, scaledFontSize, FontFamily, FontWeight, Color);
        }
    }
}

/// <summary>
/// 文本对齐方式
/// </summary>
public enum TextAlignment
{
    Left,
    Center,
    Right
}
