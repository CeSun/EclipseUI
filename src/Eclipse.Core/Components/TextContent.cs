using Eclipse.Core.Abstractions;
using Eclipse.Input;
using Eclipse.Rendering;

namespace Eclipse.Core;

public class TextContent : ComponentBase
{
    public string? Text { get; set; }
    public override bool IsVisible => true;
    public double FontSize { get; set; } = 14;
    public string? FontFamily { get; set; }
    
    /// <summary>
    /// 测量文本所需尺寸
    /// </summary>
    public override Size Measure(Size availableSize, IDrawingContext context)
    {
        if (string.IsNullOrEmpty(Text))
        {
            _desiredSize = Size.Zero;
            return _desiredSize;
        }
        
        var scaledFontSize = FontSize * context.Scale;
        var textWidth = context.MeasureText(Text, scaledFontSize, FontFamily);
        
        // 行高通常是字体大小的 1.2-1.5 倍
        var lineHeight = scaledFontSize * 1.3;
        
        _desiredSize = new Size(textWidth, lineHeight);
        return _desiredSize;
    }
    
    public override void Build(IBuildContext context) { }
    
    public override void Render(IDrawingContext context, Rect bounds)
    {
        if (!string.IsNullOrEmpty(Text))
        {
            var scaledFontSize = FontSize * context.Scale;
            var y = bounds.Y + scaledFontSize * 0.5; // 视觉中心
            context.DrawText(Text, bounds.X, y, scaledFontSize, FontFamily);
        }
    }
}