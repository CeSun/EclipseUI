using SkiaSharp;
using EclipseUI.Core;

namespace EclipseUI.Controls;

/// <summary>
/// 按钮元素
/// </summary>
public class ButtonElement : EclipseElement
{
    public string Text { get; set; } = "Button";
    public float FontSize { get; set; } = 14;
    public SKColor TextColor { get; set; } = SKColors.White;
    public SKColor ButtonColor { get; set; } = SKColors.Blue;
    public float CornerRadius { get; set; } = 4;
    public bool IsHovered { get; set; }
    public bool IsPressed { get; set; }
    
    // 缓存 emoji 字体和中文字体
    private static SKTypeface? _emojiTypeface;
    private static SKTypeface? _chineseTypeface;
    
    private static SKTypeface EmojiTypeface => 
        _emojiTypeface ??= SKTypeface.FromFamilyName("Segoe UI Emoji", SKFontStyle.Normal);
    
    private static SKTypeface ChineseTypeface => 
        _chineseTypeface ??= SKTypeface.FromFamilyName("Microsoft YaHei", SKFontStyle.Normal);
    
    public override SKSize Measure(SKCanvas canvas, float availableWidth, float availableHeight)
    {
        using var paint = new SKPaint { TextSize = FontSize, IsAntialias = true, Typeface = ChineseTypeface };
        var bounds = new SKRect();
        paint.MeasureText(Text, ref bounds);
        return new SKSize(Math.Max(bounds.Width + 24, 80), Math.Max(bounds.Height + 16, 36));
    }
    
    protected override void RenderContent(SKCanvas canvas)
    {
        var color = IsPressed ? SKColors.DarkBlue : (IsHovered ? SKColors.LightBlue : ButtonColor);
        var rect = new SKRect(X, Y, X + Width, Y + Height);
        
        using var bgPaint = new SKPaint { Color = color, IsAntialias = true };
        if (CornerRadius > 0)
        {
            using var path = new SKPath();
            path.AddRoundRect(rect, CornerRadius, CornerRadius);
            canvas.DrawPath(path, bgPaint);
        }
        else
        {
            canvas.DrawRect(rect, bgPaint);
        }
        
        // 检测是否包含 Emoji
        if (ContainsEmoji(Text))
        {
            RenderMixedText(canvas, Text);
        }
        else
        {
            using var textPaint = new SKPaint
            {
                TextSize = FontSize,
                Color = TextColor,
                IsAntialias = true,
                TextAlign = SKTextAlign.Center,
                Typeface = ChineseTypeface
            };
            
            var bounds = new SKRect();
            textPaint.MeasureText(Text, ref bounds);
            canvas.DrawText(Text, X + Width / 2, Y + Height / 2 + bounds.Height / 4, textPaint);
        }
    }
    
    /// <summary>
    /// 渲染混合文本（中文 + Emoji）
    /// </summary>
    private void RenderMixedText(SKCanvas canvas, string text)
    {
        // 计算文本总宽度
        float totalWidth = 0;
        
        using var chinesePaint = new SKPaint
        {
            TextSize = FontSize,
            Color = TextColor,
            IsAntialias = true,
            TextAlign = SKTextAlign.Left,
            Typeface = ChineseTypeface
        };
        
        using var emojiPaint = new SKPaint
        {
            TextSize = FontSize,
            Color = TextColor,
            IsAntialias = true,
            TextAlign = SKTextAlign.Left,
            Typeface = EmojiTypeface
        };
        
        int i = 0;
        while (i < text.Length)
        {
            var c = text[i];
            string character;
            bool isEmoji;
            
            if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                character = text.Substring(i, 2);
                isEmoji = true;
                i += 2;
            }
            else
            {
                character = c.ToString();
                isEmoji = IsEmojiChar(c);
                i++;
            }
            
            totalWidth += isEmoji 
                ? emojiPaint.MeasureText(character) 
                : chinesePaint.MeasureText(character);
        }
        
        // 居中绘制
        float currentX = X + Width / 2 - totalWidth / 2;
        float baselineY = Y + Height / 2;
        
        var metrics = chinesePaint.FontMetrics;
        baselineY += -metrics.Top + (metrics.Bottom - metrics.Top) / 2;
        
        i = 0;
        while (i < text.Length)
        {
            var c = text[i];
            string character;
            bool isEmoji;
            
            if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                character = text.Substring(i, 2);
                isEmoji = true;
                i += 2;
            }
            else
            {
                character = c.ToString();
                isEmoji = IsEmojiChar(c);
                i++;
            }
            
            float width;
            var paint = isEmoji ? emojiPaint : chinesePaint;
            
            if (isEmoji)
            {
                width = emojiPaint.MeasureText(character);
                canvas.DrawText(character, currentX, baselineY, emojiPaint);
            }
            else
            {
                width = chinesePaint.MeasureText(character);
                canvas.DrawText(character, currentX, baselineY, chinesePaint);
            }
            currentX += width;
        }
    }
    
    private static bool IsEmojiChar(char c)
    {
        return (c >= 0x2600 && c <= 0x26FF) ||
               (c >= 0x2700 && c <= 0x27BF) ||
               (c >= 0xFE00 && c <= 0xFE0F);
    }
    
    private static bool ContainsEmoji(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        
        for (int i = 0; i < text.Length; i++)
        {
            var c = text[i];
            
            if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                return true;
            }
            
            if (IsEmojiChar(c))
            {
                return true;
            }
        }
        return false;
    }
}
