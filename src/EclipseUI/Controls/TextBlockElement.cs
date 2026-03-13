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
    
    // 缓存 emoji 字体和中文字体
    private static SKTypeface? _emojiTypeface;
    private static SKTypeface? _chineseTypeface;
    
    private static SKTypeface EmojiTypeface
    {
        get
        {
            if (_emojiTypeface == null)
            {
                _emojiTypeface = SKTypeface.FromFamilyName("Segoe UI Emoji", SKFontStyle.Normal);
            }
            return _emojiTypeface;
        }
    }
    
    private static SKTypeface ChineseTypeface => 
        _chineseTypeface ??= SKTypeface.FromFamilyName("Microsoft YaHei", SKFontStyle.Normal);
    
    public override SKSize Measure(SKCanvas canvas, float availableWidth, float availableHeight)
    {
        using var paint = new SKPaint { TextSize = FontSize, IsAntialias = true, Typeface = ChineseTypeface };
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
        
        // 检测是否包含 Emoji
        if (ContainsEmoji(Text))
        {
            // 分别渲染中文和 Emoji
            RenderMixedText(canvas, Text);
        }
        else
        {
            // 普通文本直接渲染
            using var paint = new SKPaint
            {
                TextSize = FontSize,
                IsAntialias = true,
                Color = TextColor,
                Typeface = ChineseTypeface
            };
            
            var metrics = paint.FontMetrics;
            var baselineY = Y + PaddingTop - metrics.Top;
            canvas.DrawText(Text, X + PaddingLeft, baselineY, paint);
        }
    }
    
    /// <summary>
    /// 渲染混合文本（中文 + Emoji）
    /// </summary>
    private void RenderMixedText(SKCanvas canvas, string text)
    {
        float currentX = X + PaddingLeft;
        float baselineY = Y + PaddingTop;
        
        using var chinesePaint = new SKPaint
        {
            TextSize = FontSize,
            IsAntialias = true,
            Color = TextColor,
            Typeface = ChineseTypeface,
            TextAlign = SKTextAlign.Left
        };
        
        using var emojiPaint = new SKPaint
        {
            TextSize = FontSize,
            IsAntialias = true,
            Color = TextColor,
            Typeface = EmojiTypeface,
            TextAlign = SKTextAlign.Left
        };
        
        var metrics = chinesePaint.FontMetrics;
        baselineY += -metrics.Top;
        
        int i = 0;
        while (i < text.Length)
        {
            var c = text[i];
            
            // 处理代理对（Emoji 通常是两个 char）
            string character;
            bool isEmoji;
            
            if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                // 代理对 - Emoji
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
            
            // 根据字符类型选择字体
            var width = isEmoji 
                ? emojiPaint.MeasureText(character) 
                : chinesePaint.MeasureText(character);
            
            var paint = isEmoji ? emojiPaint : chinesePaint;
            canvas.DrawText(character, currentX, baselineY, paint);
            currentX += width;
        }
    }
    
    private static bool IsEmojiChar(char c)
    {
        // 单个字符的 Emoji 范围
        return (c >= 0x2600 && c <= 0x26FF) ||    // Misc symbols
               (c >= 0x2700 && c <= 0x27BF) ||    // Dingbats
               (c >= 0xFE00 && c <= 0xFE0F);      // Variation Selectors
    }
    
    private static bool ContainsEmoji(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        
        for (int i = 0; i < text.Length; i++)
        {
            var c = text[i];
            
            // 检查代理对（大多数 Emoji）
            if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                return true;
            }
            
            // 检查单个字符的 Emoji
            if (IsEmojiChar(c))
            {
                return true;
            }
        }
        return false;
    }
}
