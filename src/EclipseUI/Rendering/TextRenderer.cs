using SkiaSharp;
using EclipseUI.Core;

namespace EclipseUI.Rendering;

/// <summary>
/// 文本渲染器 - 封装文本绘制逻辑
/// </summary>
public static class TextRenderer
{
    private static SKTypeface? _emojiTypeface;
    private static SKTypeface? _chineseTypeface;
    
    private static SKTypeface EmojiTypeface => 
        _emojiTypeface ??= SKTypeface.FromFamilyName("Segoe UI Emoji", SKFontStyle.Normal);
    
    private static SKTypeface ChineseTypeface => 
        _chineseTypeface ??= SKTypeface.FromFamilyName("Microsoft YaHei", SKFontStyle.Normal);
    
    /// <summary>
    /// 绘制文本（支持 Emoji）
    /// </summary>
    public static void DrawText(IRenderContext context, string text, float x, float y, float fontSize, Color color)
    {
        if (string.IsNullOrEmpty(text)) return;
        
        if (ContainsEmoji(text))
        {
            DrawMixedText(context, text, x, y, fontSize, color);
        }
        else
        {
            var font = new SkiaFont("Microsoft YaHei", fontSize);
            context.DrawText(text, x, y, font, color);
        }
    }
    
    /// <summary>
    /// 绘制文本（支持 Emoji，带文本对齐）
    /// </summary>
    public static void DrawText(IRenderContext context, string text, float x, float y, float fontSize, Color color, SKTextAlign align)
    {
        if (string.IsNullOrEmpty(text)) return;
        
        // 计算文本总宽度
        float totalWidth = MeasureText(text, fontSize);
        
        // 根据对齐方式调整 x 坐标
        float startX = x;
        if (align == SKTextAlign.Center)
        {
            startX = x - totalWidth / 2;
        }
        else if (align == SKTextAlign.Right)
        {
            startX = x - totalWidth;
        }
        
        DrawText(context, text, startX, y, fontSize, color);
    }
    
    /// <summary>
    /// 测量文本宽度（自动检测 Emoji）
    /// </summary>
    public static float MeasureText(string text, float fontSize)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        
        float totalWidth = 0;
        
        for (int i = 0; i < text.Length; i++)
        {
            var c = text[i];
            string character;
            
            if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                character = text.Substring(i, 2);
                totalWidth += MeasureCharacter(character, fontSize, true);
                i++;
            }
            else
            {
                bool isEmoji = IsEmojiChar(c);
                character = c.ToString();
                totalWidth += MeasureCharacter(character, fontSize, isEmoji);
            }
        }
        
        return totalWidth;
    }
    
    private static float MeasureCharacter(string character, float fontSize, bool isEmoji)
    {
        using var paint = new SKPaint
        {
            TextSize = fontSize,
            Typeface = isEmoji ? EmojiTypeface : ChineseTypeface
        };
        return paint.MeasureText(character);
    }
    
    private static void DrawMixedText(IRenderContext context, string text, float x, float y, float fontSize, Color color)
    {
        float currentX = x;
        var chineseFont = new SkiaFont("Microsoft YaHei", fontSize);
        var emojiFont = new SkiaFont("Segoe UI Emoji", fontSize);
        
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
            
            var font = isEmoji ? emojiFont : chineseFont;
            var width = MeasureCharacter(character, fontSize, isEmoji);
            context.DrawText(character, currentX, y, font, color);
            currentX += width;
        }
    }
    
    private static bool IsEmojiChar(char c)
    {
        return (c >= 0x2600 && c <= 0x26FF) ||
               (c >= 0x2700 && c <= 0x27BF) ||
               (c >= 0xFE00 && c <= 0xFE0F);
    }
    
    /// <summary>
    /// 检测文本是否包含 Emoji（公开方法供 IRenderContext 使用）
    /// </summary>
    public static bool ContainsEmoji(string text)
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
