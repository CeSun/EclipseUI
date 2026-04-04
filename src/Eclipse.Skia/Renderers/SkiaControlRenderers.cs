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
            "Microsoft YaHei", "SimSun", "SimHei", "PingFang SC", 
            "Noto Sans CJK SC", "WenQuanYi Micro Hei", "Segoe UI", null
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
    
    /// <summary>
    /// 获取支持 Emoji 的字体
    /// </summary>
    public static SKTypeface GetEmojiTypeface()
    {
        if (_emojiTypeface != null)
            return _emojiTypeface;
        
        var emojiFonts = new[] { "Segoe UI Emoji", "Noto Color Emoji", "Apple Color Emoji", null };
        
        foreach (var fontName in emojiFonts)
        {
            var typeface = SKTypeface.FromFamilyName(fontName, SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
            if (typeface != null)
            {
                _emojiTypeface = typeface;
                Console.WriteLine($"Using emoji font: {typeface.FamilyName}");
                return typeface;
            }
        }
        
        _emojiTypeface = SKTypeface.Default;
        return _emojiTypeface;
    }
    
    /// <summary>
    /// 检查文本是否包含 emoji
    /// </summary>
    public static bool ContainsEmoji(string text)
    {
        foreach (var rune in text.EnumerateRunes())
        {
            if (rune.Value >= 0x1F300 && rune.Value <= 0x1F9FF) return true;
            if (rune.Value >= 0x2600 && rune.Value <= 0x27BF) return true;
        }
        return false;
    }
    
    /// <summary>
    /// 使用字体 fallback 渲染文本（支持中文 + emoji 混合）
    /// </summary>
    public static void DrawTextWithFallback(
        SKCanvas canvas, 
        string text, 
        float x, 
        float y, 
        SKFont baseFont, 
        SKPaint paint,
        SKTypeface emojiTypeface)
    {
        if (string.IsNullOrEmpty(text)) return;
        
        var currentX = x;
        var chineseTypeface = GetChineseTypeface();
        
        foreach (var rune in text.EnumerateRunes())
        {
            var chars = rune.ToString();
            
            // 检查是否是 emoji
            bool isEmoji = (rune.Value >= 0x1F300 && rune.Value <= 0x1F9FF) || (rune.Value >= 0x2600 && rune.Value <= 0x27BF);
            
            // 检查是否是中文
            bool isChinese = rune.Value >= 0x4E00 && rune.Value <= 0x9FFF;
            
            if (isEmoji && emojiTypeface != null)
            {
                // 使用 emoji 字体
                using var emojiFont = new SKFont
                {
                    Typeface = emojiTypeface,
                    Size = baseFont.Size,
                    Edging = baseFont.Edging,
                    Subpixel = baseFont.Subpixel
                };
                canvas.DrawText(chars, currentX, y, emojiFont, paint);
                currentX += emojiFont.MeasureText(chars);
            }
            else if (isChinese && baseFont.Typeface != chineseTypeface)
            {
                // 中文字符且当前字体不是中文字体，fallback 到中文字体
                using var chineseFont = new SKFont
                {
                    Typeface = chineseTypeface,
                    Size = baseFont.Size,
                    Edging = baseFont.Edging,
                    Subpixel = baseFont.Subpixel
                };
                canvas.DrawText(chars, currentX, y, chineseFont, paint);
                currentX += chineseFont.MeasureText(chars);
            }
            else
            {
                // 使用基础字体（英文、数字、符号等）
                canvas.DrawText(chars, currentX, y, baseFont, paint);
                currentX += baseFont.MeasureText(chars);
            }
        }
    }
    
    /// <summary>
    /// 测量混合文本宽度（支持中文 + emoji）
    /// </summary>
    public static float MeasureTextWithFallback(string text, SKFont baseFont, SKTypeface emojiTypeface)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        
        float width = 0;
        var chineseTypeface = GetChineseTypeface();
        
        foreach (var rune in text.EnumerateRunes())
        {
            var chars = rune.ToString();
            bool isEmoji = (rune.Value >= 0x1F300 && rune.Value <= 0x1F9FF) || (rune.Value >= 0x2600 && rune.Value <= 0x27BF);
            bool isChinese = rune.Value >= 0x4E00 && rune.Value <= 0x9FFF;
            
            if (isEmoji && emojiTypeface != null)
            {
                using var emojiFont = new SKFont
                {
                    Typeface = emojiTypeface,
                    Size = baseFont.Size
                };
                width += emojiFont.MeasureText(chars);
            }
            else if (isChinese && baseFont.Typeface != chineseTypeface)
            {
                using var chineseFont = new SKFont
                {
                    Typeface = chineseTypeface,
                    Size = baseFont.Size
                };
                width += chineseFont.MeasureText(chars);
            }
            else
            {
                width += baseFont.MeasureText(chars);
            }
        }
        
        return width;
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
        
        // 使用 fallback 渲染（支持中文 + emoji 混合）
        DrawTextWithFallback(context.Canvas, label.Text, x, y, font, paint, GetEmojiTypeface());
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
            using var font = new SKFont
            {
                Size = (float)button.GetFontSize() * context.Scale,
                Edging = SKFontEdging.SubpixelAntialias,
                Subpixel = true
            };
            
            // 字体选择
            if (!string.IsNullOrEmpty(button.FontFamily))
            {
                font.Typeface = SKTypeface.FromFamilyName(button.FontFamily, SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
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
            
            // 先计算总宽度以居中
            var textWidth = LabelRenderer.MeasureTextWithFallback(button.Text, font, LabelRenderer.GetEmojiTypeface());
            var x = bounds.Left + (bounds.Width - textWidth) / 2;
            var y = bounds.Top + (bounds.Height + font.Spacing) / 2 - font.Metrics.Descent;
            
            // 使用 fallback 渲染
            LabelRenderer.DrawTextWithFallback(context.Canvas, button.Text, x, y, font, textPaint, LabelRenderer.GetEmojiTypeface());
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