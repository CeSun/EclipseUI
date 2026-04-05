using Eclipse.Core;
using Eclipse.Core.Abstractions;
using Eclipse.Controls;
using Eclipse.Input;
using SkiaSharp;
using Eclipse.Skia.Text;

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
        
        // 更新 Bounds 用于 Hit Testing
        stack.UpdateBounds(new Rect(bounds.Left, bounds.Top, bounds.Width, bounds.Height));
        
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
/// Label 渲染器 - 使用 HarfBuzz 文本塑形
/// </summary>
public class LabelRenderer : ISkiaControlRenderer
{
    public Type TargetType => typeof(Label);
    
    // 共享渲染器实例
    private static readonly HarfBuzzTextRenderer _textRenderer = new();
    
    // 缓存字体
    private static SKTypeface? _chineseTypeface;
    private static SKTypeface? _emojiTypeface;
    
    /// <summary>
    /// 获取支持中文的字体
    /// </summary>
    public static SKTypeface GetChineseTypeface()
    {
        if (_chineseTypeface != null)
            return _chineseTypeface;
        
        var chineseFonts = new[] { 
            "Microsoft YaHei", "PingFang SC", "SimSun", 
            "Noto Sans CJK SC", "Segoe UI"
        };
        
        foreach (var fontName in chineseFonts)
        {
            var typeface = SKTypeface.FromFamilyName(fontName, 
                SKFontStyleWeight.Normal, 
                SKFontStyleWidth.Normal, 
                SKFontStyleSlant.Upright);
            if (typeface != null && typeface.CountGlyphs("中") > 0)
            {
                _chineseTypeface = typeface;
                return typeface;
            }
        }
        
        _chineseTypeface = SKTypeface.Default;
        return _chineseTypeface;
    }
    
    /// <summary>
    /// 获取 Emoji 字体
    /// </summary>
    public static SKTypeface GetEmojiTypeface()
    {
        if (_emojiTypeface != null)
            return _emojiTypeface;
        
        var emojiFonts = new[] { 
            "Segoe UI Emoji", "Noto Color Emoji", 
            "Apple Color Emoji", "Twemoji Mozilla"
        };
        
        foreach (var fontName in emojiFonts)
        {
            var typeface = SKTypeface.FromFamilyName(fontName, 
                SKFontStyleWeight.Normal, 
                SKFontStyleWidth.Normal, 
                SKFontStyleSlant.Upright);
            if (typeface != null && typeface.CountGlyphs("\U0001F600") > 0)
            {
                _emojiTypeface = typeface;
                return typeface;
            }
        }
        
        _emojiTypeface = SKTypeface.Default;
        return _emojiTypeface;
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
        
        using var font = new SKFont
        {
            Size = (float)label.GetFontSize() * context.Scale,
            Edging = SKFontEdging.SubpixelAntialias,
            Subpixel = true
        };
        
        // 字体选择优先级：FontFamily > 默认中文
        if (!string.IsNullOrEmpty(label.FontFamily))
        {
            var weight = label.FontWeight == "Bold" ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal;
            font.Typeface = SKTypeface.FromFamilyName(label.FontFamily, weight, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
                ?? GetChineseTypeface();
        }
        else if (label.FontWeight == "Bold")
        {
            font.Typeface = SKTypeface.FromFamilyName(GetChineseTypeface().FamilyName, 
                SKFontStyleWeight.Bold, 
                SKFontStyleWidth.Normal, 
                SKFontStyleSlant.Upright) 
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
        
        // 使用 HarfBuzz 渲染器（支持 emoji、复杂脚本、字体回退）
        _textRenderer.DrawText(context.Canvas, label.Text, x, y, font, paint);
    }
    
    /// <summary>
    /// 测量文本宽度（使用 HarfBuzz 渲染器）
    /// </summary>
    public static float MeasureText(string text, SKFont font)
    {
        return _textRenderer.MeasureText(text, font);
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
    
    private static readonly HarfBuzzTextRenderer _textRenderer = new();
    
    public void Render(
        IComponent component, 
        SkiaRenderContext context, 
        SKRect bounds,
        Action<IComponent, SkiaRenderContext, SKRect> renderChild)
    {
        var button = (Button)component;
        
        // 更新 Bounds 用于 Hit Testing
        button.UpdateBounds(new Rect(bounds.Left, bounds.Top, bounds.Width, bounds.Height));
        
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
            using var font = new SKFont
            {
                Size = (float)button.GetFontSize() * context.Scale,
                Edging = SKFontEdging.SubpixelAntialias,
                Subpixel = true
            };
            
            // 字体选择
            if (!string.IsNullOrEmpty(button.FontFamily))
            {
                font.Typeface = SKTypeface.FromFamilyName(button.FontFamily, 
                    SKFontStyleWeight.Normal, 
                    SKFontStyleWidth.Normal, 
                    SKFontStyleSlant.Upright)
                    ?? LabelRenderer.GetChineseTypeface();
            }
            else
            {
                font.Typeface = LabelRenderer.GetChineseTypeface();
            }
            
            using var textPaint = new SKPaint
            {
                IsAntialias = true,
                Color = ParseColor(button.TextColor, SKColors.White)
            };
            
            // 使用 HarfBuzz 测量和渲染
            var textWidth = _textRenderer.MeasureText(button.Text, font);
            var x = bounds.Left + (bounds.Width - textWidth) / 2;
            var y = bounds.Top + (bounds.Height + font.Spacing) / 2 - font.Metrics.Descent;
            
            _textRenderer.DrawText(context.Canvas, button.Text, x, y, font, textPaint);
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