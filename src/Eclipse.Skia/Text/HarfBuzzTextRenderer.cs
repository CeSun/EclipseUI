using HarfBuzzSharp;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace Eclipse.Skia.Text;

/// <summary>
/// HarfBuzz 文本渲染器 - 支持多语言、Emoji、复杂脚本
/// </summary>
public class HarfBuzzTextRenderer
{
    private readonly SKFontManager _fontManager = SKFontManager.Default;
    
    // 缓存
    private static readonly Dictionary<string, SKTypeface> _typefaceCache = new();
    private static SKTypeface? _emojiTypeface;
    private static SKTypeface? _chineseTypeface;
    
    /// <summary>
    /// 渲染文本（自动处理 emoji 和字体回退）
    /// </summary>
    public void DrawText(
        SKCanvas canvas,
        string text,
        float x,
        float y,
        SKFont baseFont,
        SKPaint paint)
    {
        if (string.IsNullOrEmpty(text)) return;
        
        // 分段处理
        var segments = SegmentText(text, baseFont);
        
        var currentX = x;
        foreach (var segment in segments)
        {
            DrawSegment(canvas, segment, currentX, y, baseFont, paint);
            currentX += segment.Width;
        }
    }
    
    /// <summary>
    /// 测量文本宽度
    /// </summary>
    public float MeasureText(string text, SKFont baseFont)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        
        var segments = SegmentText(text, baseFont);
        return segments.Sum(s => s.Width);
    }
    
    /// <summary>
    /// 文本分段 - 按字体需求切分
    /// </summary>
    private List<TextSegment> SegmentText(string text, SKFont baseFont)
    {
        var result = new List<TextSegment>();
        
        if (string.IsNullOrEmpty(text)) return result;
        
        var si = new System.Globalization.StringInfo(text);
        var baseTypeface = baseFont.Typeface ?? SKTypeface.Default;
        
        int i = 0;
        TextSegment? currentSegment = null;
        
        while (i < si.LengthInTextElements)
        {
            var str = si.SubstringByTextElements(i, 1);
            var codePoint = char.ConvertToUtf32(str, 0);
            
            // 确定需要的字体
            var neededTypeface = DetermineTypeface(codePoint, baseTypeface);
            
            // 如果字体变化，开始新段
            if (currentSegment == null || currentSegment.Typeface != neededTypeface)
            {
                if (currentSegment != null)
                {
                    currentSegment.Width = MeasureSegment(currentSegment, baseFont.Size);
                    result.Add(currentSegment);
                }
                
                currentSegment = new TextSegment
                {
                    Text = str,
                    Typeface = neededTypeface,
                    IsEmoji = EmojiDetector.IsEmoji(codePoint) || EmojiDetector.HasEmojiPresentation(codePoint)
                };
            }
            else
            {
                currentSegment.Text += str;
            }
            
            i++;
        }
        
        // 最后一段
        if (currentSegment != null)
        {
            currentSegment.Width = MeasureSegment(currentSegment, baseFont.Size);
            result.Add(currentSegment);
        }
        
        return result;
    }
    
    /// <summary>
    /// 确定渲染字符需要的字体
    /// </summary>
    private SKTypeface DetermineTypeface(int codePoint, SKTypeface baseTypeface)
    {
        // Emoji 优先
        if (EmojiDetector.IsEmoji(codePoint) || EmojiDetector.HasEmojiPresentation(codePoint))
        {
            return GetEmojiTypeface();
        }
        
        // 检查基础字体是否支持
        var utf16 = EncodeCodePoint(codePoint);
        if (baseTypeface.CountGlyphs(utf16) > 0)
        {
            return baseTypeface;
        }
        
        // 使用 font manager 查找 - MatchCharacter 需要 char，对于超过 0xFFFF 的码点简化处理
        char searchChar = codePoint <= 0xFFFF ? (char)codePoint : '中';
        
        var matched = _fontManager.MatchCharacter(
            baseTypeface.FamilyName,
            SKFontStyleWeight.Normal,
            SKFontStyleWidth.Normal,
            SKFontStyleSlant.Upright,
            new[] { "zh", "ja", "ko", "en" },
            searchChar);
        
        if (matched != null)
        {
            return CacheTypeface(matched);
        }
        
        // 最后回退到中文字体
        return GetChineseTypeface();
    }
    
    /// <summary>
    /// 渲染单个分段
    /// </summary>
    private void DrawSegment(
        SKCanvas canvas,
        TextSegment segment,
        float x,
        float y,
        SKFont baseFont,
        SKPaint paint)
    {
        using var font = new SKFont
        {
            Typeface = segment.Typeface,
            Size = segment.IsEmoji ? baseFont.Size * 1.1f : baseFont.Size, // Emoji 稍大
            Edging = baseFont.Edging,
            Subpixel = baseFont.Subpixel
        };
        
        // 使用 HarfBuzz 塑形（可选）
        // var shaper = HarfBuzzTextShaper.GetOrCreate(segment.Typeface);
        // var glyphs = shaper.Shape(segment.Text, font.Size);
        
        // 简单渲染
        canvas.DrawText(segment.Text, x, y, font, paint);
    }
    
    /// <summary>
    /// 测量分段宽度
    /// </summary>
    private float MeasureSegment(TextSegment segment, float fontSize)
    {
        using var font = new SKFont
        {
            Typeface = segment.Typeface,
            Size = segment.IsEmoji ? fontSize * 1.1f : fontSize
        };
        
        return font.MeasureText(segment.Text);
    }
    
    /// <summary>
    /// 获取 Emoji 字体
    /// </summary>
    private static SKTypeface GetEmojiTypeface()
    {
        if (_emojiTypeface != null) return _emojiTypeface;
        
        var emojiFonts = new[] { "Segoe UI Emoji", "Noto Color Emoji", "Apple Color Emoji", "Twemoji Mozilla" };
        
        foreach (var family in emojiFonts)
        {
            var typeface = SKTypeface.FromFamilyName(family,
                SKFontStyleWeight.Normal,
                SKFontStyleWidth.Normal,
                SKFontStyleSlant.Upright);
            
            if (typeface != null && typeface.CountGlyphs("\U0001F600") > 0)
            {
                _emojiTypeface = CacheTypeface(typeface);
                return _emojiTypeface;
            }
        }
        
        _emojiTypeface = SKTypeface.Default;
        return _emojiTypeface;
    }
    
    /// <summary>
    /// 获取中文字体
    /// </summary>
    private static SKTypeface GetChineseTypeface()
    {
        if (_chineseTypeface != null) return _chineseTypeface;
        
        var chineseFonts = new[] { "Microsoft YaHei", "PingFang SC", "SimSun", "Noto Sans CJK SC", "Segoe UI" };
        
        foreach (var family in chineseFonts)
        {
            var typeface = SKTypeface.FromFamilyName(family,
                SKFontStyleWeight.Normal,
                SKFontStyleWidth.Normal,
                SKFontStyleSlant.Upright);
            
            if (typeface != null && typeface.CountGlyphs("中") > 0)
            {
                _chineseTypeface = CacheTypeface(typeface);
                return _chineseTypeface;
            }
        }
        
        _chineseTypeface = SKTypeface.Default;
        return _chineseTypeface;
    }
    
    /// <summary>
    /// 缓存字体
    /// </summary>
    private static SKTypeface CacheTypeface(SKTypeface typeface)
    {
        var key = typeface.FamilyName;
        if (!_typefaceCache.ContainsKey(key))
        {
            _typefaceCache[key] = typeface;
        }
        return _typefaceCache[key];
    }
    
    /// <summary>
    /// 码点转 UTF-16
    /// </summary>
    private static string EncodeCodePoint(int codePoint)
    {
        if (codePoint < 0x10000)
        {
            return new string((char)codePoint, 1);
        }
        else
        {
            var hi = (char)((codePoint - 0x10000) / 0x400 + 0xD800);
            var lo = (char)((codePoint - 0x10000) % 0x400 + 0xDC00);
            return new string(new[] { hi, lo });
        }
    }
}

/// <summary>
/// 文本分段
/// </summary>
internal class TextSegment
{
    public string Text { get; set; } = "";
    public SKTypeface Typeface { get; set; } = SKTypeface.Default;
    public bool IsEmoji { get; set; }
    public float Width { get; set; }
}