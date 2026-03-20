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

    private static readonly string[] EmojiFontFamilies =
    {
        "Apple Color Emoji",
        "Segoe UI Emoji",
        "Noto Color Emoji"
    };

    private static readonly string[] ChineseFontFamilies =
    {
        "PingFang SC",
        "Hiragino Sans GB",
        "Heiti SC",
        "STHeiti",
        "Microsoft YaHei",
        "SimSun",
        "Noto Sans CJK SC",
        "Source Han Sans SC",
        "Arial Unicode MS"
    };

    private static SKTypeface? TryCreateTypeface(string[] families)
    {
        foreach (var family in families)
        {
            var typeface = SKTypeface.FromFamilyName(family, SKFontStyle.Normal);
            if (typeface != null && !string.IsNullOrWhiteSpace(typeface.FamilyName))
            {
                return typeface;
            }
        }

        return null;
    }

    private static SKTypeface EmojiTypeface
    {
        get
        {
            if (_emojiTypeface == null)
            {
                _emojiTypeface = TryCreateTypeface(EmojiFontFamilies);
                _emojiTypeface ??= SKTypeface.Default;
            }
            return _emojiTypeface;
        }
    }

    private static SKTypeface ChineseTypeface =>
        _chineseTypeface ??= TryCreateTypeface(ChineseFontFamilies)
            ?? SKTypeface.Default;

    private static string EmojiFontFamily => EmojiTypeface.FamilyName;
    private static string ChineseFontFamily => ChineseTypeface.FamilyName;

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
            var font = new SkiaFont(ChineseFontFamily, fontSize);
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
    /// 测量文本宽度（自动检测 Emoji，支持 surrogate pair 和变体选择器）
    /// </summary>
    public static float MeasureText(string text, float fontSize)
    {
        if (string.IsNullOrEmpty(text)) return 0;

        float totalWidth = 0;

        for (int i = 0; i < text.Length; i++)
        {
            var c = text[i];
            string character;

            // 处理 surrogate pair (如 🌑 U+1F311)
            if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                character = text.Substring(i, 2);
                totalWidth += MeasureCharacter(character, fontSize, true);
                i++;
            }
            // 处理带变体选择器的字符 (如 ⬆️ = U+2B06 + U+FE0F)
            else if (i + 1 < text.Length && text[i + 1] >= 0xFE00 && text[i + 1] <= 0xFE0F)
            {
                character = text.Substring(i, 2);
                totalWidth += MeasureCharacter(character, fontSize, true);
                i++; // 跳过变体选择器
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
        
        // 对于带变体选择器的字符，只测量基础字符
        // 变体选择器 (U+FE00-U+FE0F) 本身宽度为 0
        string measureChar = character;
        if (character.Length == 2 && character[1] >= 0xFE00 && character[1] <= 0xFE0F)
        {
            measureChar = character[0].ToString();
        }
        
        return paint.MeasureText(measureChar);
    }

    /// <summary>
    /// 测量带换行的文本
    /// </summary>
    public static (float Width, float Height) MeasureTextWithWrap(string text, float fontSize, float maxWidth)
    {
        if (string.IsNullOrEmpty(text))
            return (0, 0);
        
        float lineHeight = fontSize * 1.2f;
        float totalWidth = 0;
        float currentLineWidth = 0;
        int lineCount = 1;
        
        // 按字符遍历（正确处理 emoji surrogate pair）
        for (int i = 0; i < text.Length; i++)
        {
            var c = text[i];
            string character;
            float charWidth;
            
            // 跳过换行符
            if (c == '\n' || c == '\r')
            {
                totalWidth = Math.Max(totalWidth, currentLineWidth);
                currentLineWidth = 0;
                lineCount++;
                continue;
            }
            
            // 处理 emoji surrogate pair
            if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                character = text.Substring(i, 2);
                charWidth = MeasureText(character, fontSize);
                i++; // 跳过下一个 low surrogate
            }
            else
            {
                character = c.ToString();
                charWidth = MeasureText(character, fontSize);
            }
            
            // 如果加上字符后超过最大宽度，换行
            if (currentLineWidth + charWidth > maxWidth && currentLineWidth > 0)
            {
                totalWidth = Math.Max(totalWidth, currentLineWidth);
                currentLineWidth = charWidth;
                lineCount++;
            }
            else
            {
                currentLineWidth += charWidth;
            }
        }
        
        totalWidth = Math.Max(totalWidth, currentLineWidth);
        float totalHeight = lineCount * lineHeight;
        
        return (totalWidth, totalHeight);
    }

    /// <summary>
    /// 绘制带换行的文本（支持 surrogate pair 和变体选择器）
    /// </summary>
    public static void DrawTextWithWrap(IRenderContext context, string text, float x, float y, float fontSize, Color color, float maxWidth)
    {
        if (string.IsNullOrEmpty(text))
            return;

        float lineHeight = fontSize * 1.2f;
        float currentY = y;
        float currentX = x;
        float currentLineWidth = 0;

        for (int i = 0; i < text.Length; i++)
        {
            var c = text[i];
            string character;
            float charWidth;

            // 跳过换行符
            if (c == '\n' || c == '\r')
            {
                currentX = x;
                currentY += lineHeight;
                currentLineWidth = 0;
                continue;
            }

            // 处理 surrogate pair
            if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                character = text.Substring(i, 2);
                charWidth = MeasureText(character, fontSize);
                i++;
            }
            // 处理带变体选择器的字符
            else if (i + 1 < text.Length && text[i + 1] >= 0xFE00 && text[i + 1] <= 0xFE0F)
            {
                character = text.Substring(i, 2);
                charWidth = MeasureText(character, fontSize);
                i++; // 跳过变体选择器
            }
            else
            {
                character = c.ToString();
                charWidth = MeasureText(character, fontSize);
            }

            // 如果加上字符后超过最大宽度，换行
            if (currentLineWidth + charWidth > maxWidth && currentLineWidth > 0)
            {
                currentX = x;
                currentY += lineHeight;
                currentLineWidth = 0;
            }

            // 绘制字符
            DrawMixedText(context, character, currentX, currentY, fontSize, color);
            currentX += charWidth;
            currentLineWidth += charWidth;
        }
    }

    private static void DrawMixedText(IRenderContext context, string text, float x, float y, float fontSize, Color color)
    {
        float currentX = x;

        int i = 0;
        while (i < text.Length)
        {
            var c = text[i];
            string character;
            bool isEmoji;

            // 处理 surrogate pair
            if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                character = text.Substring(i, 2);
                isEmoji = true;
                i += 2;
            }
            // 处理带变体选择器的字符
            else if (i + 1 < text.Length && text[i + 1] >= 0xFE00 && text[i + 1] <= 0xFE0F)
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

            var width = MeasureCharacter(character, fontSize, isEmoji);
            
            // 绘制时只绘制基础字符（变体选择器不需要单独绘制，它只是修饰符）
            string drawChar = character;
            if (character.Length == 2 && character[1] >= 0xFE00 && character[1] <= 0xFE0F)
            {
                drawChar = character[0].ToString();
            }
            
            // 使用 DrawTextDirect 直接指定字体
            string fontFamily = isEmoji ? EmojiFontFamily : ChineseFontFamily;
            context.DrawTextDirect(drawChar, currentX, y, fontSize, fontFamily, color);
            currentX += width;
        }
    }

    private static bool IsEmojiChar(char c)
    {
        return (c >= 0x2190 && c <= 0x21FF) ||  // 箭头
               (c >= 0x2600 && c <= 0x26FF) ||  // 杂项符号
               (c >= 0x2700 && c <= 0x27BF) ||  // 丁巴特斯符号
               (c >= 0x1F300 && c <= 0x1F9FF) || // 杂项符号和表情（需要 surrogate pair）
               (c >= 0xFE00 && c <= 0xFE0F);    // 变体选择器
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
