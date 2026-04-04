using Eclipse.Core;
using Eclipse.Core.Abstractions;
using Eclipse.Controls;
using SkiaSharp;

namespace Eclipse.Skia.Renderers;

/// <summary>
/// StackLayout 渲染器
/// </summary>
public class StackLayoutRenderer : ISkiaControlRenderer
{
    public Type TargetType => typeof(StackLayout);
    
    public void Render(
        IComponent component, 
        SkiaRenderContext context, 
        SKRect bounds,
        Action<IComponent, SkiaRenderContext, SKRect> renderChild)
    {
        var stack = (StackLayout)component;
        
        var orientation = stack.Orientation;
        var spacing = (float)stack.GetSpacing() * context.Scale;
        var padding = (float)stack.GetPadding() * context.Scale;
        
        var contentBounds = new SKRect(
            bounds.Left + padding,
            bounds.Top + padding,
            bounds.Right - padding,
            bounds.Bottom - padding);
        
        if (orientation == Orientation.Vertical)
        {
            RenderVertical(stack, context, contentBounds, spacing, renderChild);
        }
        else
        {
            RenderHorizontal(stack, context, contentBounds, spacing, renderChild);
        }
    }
    
    private void RenderVertical(
        StackLayout stack,
        SkiaRenderContext context,
        SKRect bounds,
        float spacing,
        Action<IComponent, SkiaRenderContext, SKRect> renderChild)
    {
        float y = bounds.Top;
        
        foreach (var child in stack.Children)
        {
            var childHeight = EstimateHeight(child, context);
            var childBounds = new SKRect(bounds.Left, y, bounds.Right, y + childHeight);
            renderChild(child, context, childBounds);
            y += childHeight + spacing;
        }
    }
    
    private void RenderHorizontal(
        StackLayout stack,
        SkiaRenderContext context,
        SKRect bounds,
        float spacing,
        Action<IComponent, SkiaRenderContext, SKRect> renderChild)
    {
        float x = bounds.Left;
        var childWidth = (bounds.Width - spacing * (stack.Children.Count - 1)) / stack.Children.Count;
        
        foreach (var child in stack.Children)
        {
            var childBounds = new SKRect(x, bounds.Top, x + childWidth, bounds.Bottom);
            renderChild(child, context, childBounds);
            x += childWidth + spacing;
        }
    }
    
    private float EstimateHeight(IComponent component, SkiaRenderContext context)
    {
        return component switch
        {
            Label => 24f * context.Scale,
            Button => 44f * context.Scale,
            TextContent => 20f * context.Scale,
            _ => 40f * context.Scale
        };
    }
}

/// <summary>
/// Label 渲染器
/// </summary>
public class LabelRenderer : ISkiaControlRenderer
{
    public Type TargetType => typeof(Label);
    
    // 缓存中文字体
    private static SKTypeface? _chineseTypeface;
    
    /// <summary>
    /// 获取支持中文的字体
    /// </summary>
    public static SKTypeface GetChineseTypeface()
    {
        if (_chineseTypeface != null)
            return _chineseTypeface;
        
        // 尝试常见的中文字体
        var chineseFonts = new[] { 
            "Microsoft YaHei",      // 微软雅黑
            "SimSun",               // 宋体
            "SimHei",               // 黑体
            "PingFang SC",          // 苹方 (macOS)
            "Noto Sans CJK SC",     // 思源黑体
            "WenQuanYi Micro Hei",  // 文泉驿 (Linux)
            null                    // 默认字体
        };
        
        foreach (var fontName in chineseFonts)
        {
            var typeface = SKTypeface.FromFamilyName(fontName, SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
            if (typeface != null)
            {
                _chineseTypeface = typeface;
                Console.WriteLine($"Using font: {typeface.FamilyName}");
                return typeface;
            }
        }
        
        _chineseTypeface = SKTypeface.Default;
        return _chineseTypeface;
    }
    
    public void Render(
        IComponent component, 
        SkiaRenderContext context, 
        SKRect bounds,
        Action<IComponent, SkiaRenderContext, SKRect> renderChild)
    {
        var label = (Label)component;
        
        if (string.IsNullOrEmpty(label.Text))
            return;
        
        // 使用 SKFont + SKPaint 组合 (SkiaSharp 3.x 推荐方式)
        using var font = new SKFont
        {
            Size = (float)label.GetFontSize() * context.Scale,
            Edging = SKFontEdging.SubpixelAntialias,
            Subpixel = true
        };
        
        // 设置字体 - 支持中文
        if (label.FontWeight == "Bold")
        {
            font.Typeface = SKTypeface.FromFamilyName(GetChineseTypeface().FamilyName, SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright) 
                ?? GetChineseTypeface();
        }
        else
        {
            font.Typeface = GetChineseTypeface();
        }
        
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color = ParseColor(label.Color, SKColors.Black)
        };
        
        var x = bounds.Left;
        var y = bounds.Top + font.Spacing;
        
        context.Canvas.DrawText(label.Text, x, y, font, paint);
    }
    
    protected static SKColor ParseColor(string? color, SKColor defaultColor)
    {
        if (string.IsNullOrEmpty(color))
            return defaultColor;
        
        return SKColor.TryParse(color, out var result) ? result : defaultColor;
    }
}

/// <summary>
/// Button 渲染器
/// </summary>
public class ButtonRenderer : ISkiaControlRenderer
{
    public Type TargetType => typeof(Button);
    
    public void Render(
        IComponent component, 
        SkiaRenderContext context, 
        SKRect bounds,
        Action<IComponent, SkiaRenderContext, SKRect> renderChild)
    {
        var button = (Button)component;
        
        var bgColor = ParseColor(button.BackgroundColor, new SKColor(0x00, 0x7A, 0xFF));
        using var bgPaint = new SKPaint
        {
            IsAntialias = true,
            Color = bgColor,
            Style = SKPaintStyle.Fill
        };
        
        var cornerRadius = (float)button.GetCornerRadius() * context.Scale;
        context.Canvas.DrawRoundRect(bounds, cornerRadius, cornerRadius, bgPaint);
        
        if (!string.IsNullOrEmpty(button.Text))
        {
            // 使用 SKFont + SKPaint 组合 (SkiaSharp 3.x 推荐方式)
            using var font = new SKFont
            {
                Size = (float)button.GetFontSize() * context.Scale,
                Edging = SKFontEdging.SubpixelAntialias,
                Subpixel = true
            };
            
            // 使用支持中文的字体
            font.Typeface = LabelRenderer.GetChineseTypeface();
            
            using var textPaint = new SKPaint
            {
                IsAntialias = true,
                Color = ParseColor(button.TextColor, SKColors.White)
            };
            
            var textWidth = font.MeasureText(button.Text);
            var x = bounds.Left + (bounds.Width - textWidth) / 2;
            var y = bounds.Top + (bounds.Height + font.Spacing) / 2 - font.Metrics.Descent;
            
            context.Canvas.DrawText(button.Text, x, y, font, textPaint);
        }
    }
    
    protected static SKColor ParseColor(string? color, SKColor defaultColor)
    {
        if (string.IsNullOrEmpty(color))
            return defaultColor;
        
        return SKColor.TryParse(color, out var result) ? result : defaultColor;
    }
}

/// <summary>
/// TextContent 渲染器
/// </summary>
public class TextContentRenderer : ISkiaControlRenderer
{
    public Type TargetType => typeof(TextContent);
    
    public void Render(
        IComponent component, 
        SkiaRenderContext context, 
        SKRect bounds,
        Action<IComponent, SkiaRenderContext, SKRect> renderChild)
    {
        var textContent = (TextContent)component;
        
        if (string.IsNullOrEmpty(textContent.Text))
            return;
        
        // 使用 SKFont + SKPaint 组合 (SkiaSharp 3.x 推荐方式)
        using var font = new SKFont
        {
            Size = 14f * context.Scale,
            Edging = SKFontEdging.SubpixelAntialias,
            Subpixel = true
        };
        
        // 使用支持中文的字体
        font.Typeface = LabelRenderer.GetChineseTypeface();
        
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color = SKColors.Black
        };
        
        context.Canvas.DrawText(textContent.Text, bounds.Left, bounds.Top + font.Spacing, font, paint);
    }
}