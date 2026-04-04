using System;
using System.Collections.Generic;

namespace Eclipse.Skia.Text;

/// <summary>
/// Emoji 检测器 - 基于 Unicode TR#51 规范
/// </summary>
public static class EmojiDetector
{
    // Emoji 属性范围 (从 emoji-data.txt 简化提取)
    // 完整实现应从 Unicode 官方数据文件生成
    
    /// <summary>
    /// 默认 Emoji 表现样式的字符范围
    /// </summary>
    private static readonly (int Start, int End)[] EmojiPresentationRanges =
    {
        // Emoticons
        (0x1F600, 0x1F64F),
        // Misc Symbols and Pictographs
        (0x1F300, 0x1F5FF),
        // Transport and Map
        (0x1F680, 0x1F6FF),
        // Supplemental Symbols and Pictographs
        (0x1F900, 0x1F9FF),
        // Symbols and Pictographs Extended-A
        (0x1FA00, 0x1FA6F),
        (0x1FA70, 0x1FAFF),
        // Weather
        (0x2600, 0x26FF),
        // Dingbats
        (0x2700, 0x27BF),
        // Misc Technical (部分)
        (0x2300, 0x23FF),
        // Keycaps 基础字符
        (0x0030, 0x0039), // 0-9
        (0x0023, 0x0023), // #
        (0x002A, 0x002A), // *
    };
    
    /// <summary>
    /// Emoji 属性范围 (包含默认文本表现的 emoji)
    /// </summary>
    private static readonly (int Start, int End)[] EmojiRanges =
    {
        // 以上所有 + 额外的文本表现 emoji
        (0x00A9, 0x00AE),     // © ®
        (0x203C, 0x203C),     // ‼
        (0x2049, 0x2049),     // ⁉
        (0x2122, 0x2122),     // ™
        (0x2139, 0x2139),     // ℹ
        (0x2194, 0x2199),     // ↔ ↕ ↖ ↗ ↘ ↙
        (0x21A9, 0x21AA),     // ↩ ↪
        (0x231A, 0x231B),     // ⌚ ⌛
        (0x2328, 0x2328),     // ⌨
        (0x23CF, 0x23CF),     // ⏏
        (0x23E9, 0x23F3),     // ⏩-⏳
        (0x23F8, 0x23FA),     // ⏸⏹⏺
        (0x24C2, 0x24C2),     // Ⓜ
        (0x25AA, 0x25AB),     // ▪ ▫
        (0x25B6, 0x25C0),     // ▶ ◀
        (0x25FB, 0x25FE),     // ◻-◾
        (0x2600, 0x26FF),     // Weather + Misc
        (0x2700, 0x27BF),     // Dingbats
        (0x2934, 0x2935),     // ⤴ ⤵
        (0x2B05, 0x2B07),     // ⬅ ⬆ ⬇
        (0x2B1B, 0x2B1C),     // ⬛ ⬜
        (0x2B50, 0x2B50),     // ⭐
        (0x2B55, 0x2B55),     // ⭕
        (0x3030, 0x3030),     // 〰
        (0x303D, 0x303D),     // 〽
        (0x3297, 0x3297),     // ㊗
        (0x3299, 0x3299),     // ㊙
        (0x1F000, 0x1F02F),   // Mahjong tiles
        (0x1F0A0, 0x1F0FF),   // Playing cards
        (0x1F100, 0x1F1FF),   // Enclosed chars + Regional indicators
        (0x1F200, 0x1F2FF),   // Enclosed chars supplemental
        (0x1F300, 0x1F9FF),   // Main emoji blocks
        (0x1FA00, 0x1FAFF),   // Extended emoji
    };
    
    /// <summary>
    /// Regional Indicator 字符范围 (用于国旗)
    /// </summary>
    private static readonly (int Start, int End) RegionalIndicatorRange = (0x1F1E6, 0x1F1FF);
    
    /// <summary>
    /// Emoji Modifier (肤色) 范围
    /// </summary>
    private static readonly (int Start, int End) EmojiModifierRange = (0x1F3FB, 0x1F3FF);
    
    /// <summary>
    /// Variation Selectors
    /// </summary>
    private const int VS15 = 0xFE0E; // 文本表现
    private const int VS16 = 0xFE0F; // Emoji 表现
    
    /// <summary>
    /// Zero Width Joiner
    /// </summary>
    private const int ZWJ = 0x200D;
    
    /// <summary>
    /// Combining Enclosing Keycap
    /// </summary>
    private const int Keycap = 0x20E3;
    
    /// <summary>
    /// Tag 字符范围 (用于子区域国旗等)
    /// </summary>
    private static readonly (int Start, int End) TagRange = (0xE0020, 0xE007F);
    
    /// <summary>
    /// 快速检查是否可能是 emoji (用于扫描)
    /// </summary>
    public static bool MightBeEmoji(int codePoint)
    {
        return IsInRange(codePoint, EmojiRanges) ||
               IsInRange(codePoint, RegionalIndicatorRange) ||
               IsInRange(codePoint, EmojiModifierRange) ||
               codePoint == VS15 || codePoint == VS16 ||
               codePoint == ZWJ || codePoint == Keycap ||
               IsInRange(codePoint, TagRange);
    }
    
    /// <summary>
    /// 检查是否是 emoji (基础属性)
    /// </summary>
    public static bool IsEmoji(int codePoint)
    {
        return IsInRange(codePoint, EmojiRanges) ||
               IsInRange(codePoint, RegionalIndicatorRange);
    }
    
    /// <summary>
    /// 检查是否默认显示为 emoji 样式
    /// </summary>
    public static bool HasEmojiPresentation(int codePoint)
    {
        return IsInRange(codePoint, EmojiPresentationRanges);
    }
    
    /// <summary>
    /// 检查是否是 Regional Indicator (国旗字母)
    /// </summary>
    public static bool IsRegionalIndicator(int codePoint)
    {
        return IsInRange(codePoint, RegionalIndicatorRange);
    }
    
    /// <summary>
    /// 检查是否是肤色修饰符
    /// </summary>
    public static bool IsEmojiModifier(int codePoint)
    {
        return IsInRange(codePoint, EmojiModifierRange);
    }
    
    /// <summary>
    /// 检查是否是 VS16 (emoji 表现选择器)
    /// </summary>
    public static bool IsEmojiPresentationSelector(int codePoint)
    {
        return codePoint == VS16;
    }
    
    /// <summary>
    /// 检查是否是 VS15 (文本表现选择器)
    /// </summary>
    public static bool IsTextPresentationSelector(int codePoint)
    {
        return codePoint == VS15;
    }
    
    /// <summary>
    /// 检查是否是 ZWJ
    /// </summary>
    public static bool IsZWJ(int codePoint)
    {
        return codePoint == ZWJ;
    }
    
    /// <summary>
    /// 检查是否是 Keycap 组合字符
    /// </summary>
    public static bool IsKeycap(int codePoint)
    {
        return codePoint == Keycap;
    }
    
    /// <summary>
    /// 分析文本中的 emoji 序列
    /// </summary>
    /// <returns>每个 emoji 序列的起始位置和长度</returns>
    public static List<(int Start, int Length)> FindEmojiSequences(string text)
    {
        var result = new List<(int Start, int Length)>();
        
        if (string.IsNullOrEmpty(text))
            return result;
        
        var si = new System.Globalization.StringInfo(text);
        var i = 0;
        
        while (i < si.LengthInTextElements)
        {
            var start = i;
            var str = si.SubstringByTextElements(i, 1);
            var codePoint = char.ConvertToUtf32(str, 0);
            
            // 检查是否是 emoji 的开始
            if (!IsEmoji(codePoint) && !HasEmojiPresentation(codePoint))
            {
                i++;
                continue;
            }
            
            // 尝试扩展序列
            var length = 1;
            i++;
            
            while (i < si.LengthInTextElements)
            {
                var nextStr = si.SubstringByTextElements(i, 1);
                var nextCodePoint = char.ConvertToUtf32(nextStr, 0);
                
                // VS16 - emoji 表现
                if (IsEmojiPresentationSelector(nextCodePoint))
                {
                    length++;
                    i++;
                    continue;
                }
                
                // 肤色修饰符
                if (IsEmojiModifier(nextCodePoint))
                {
                    length++;
                    i++;
                    continue;
                }
                
                // ZWJ - 连接下一个 emoji
                if (IsZWJ(nextCodePoint))
                {
                    length++;
                    i++;
                    
                    if (i < si.LengthInTextElements)
                    {
                        var afterZwjStr = si.SubstringByTextElements(i, 1);
                        var afterZwj = char.ConvertToUtf32(afterZwjStr, 0);
                        
                        if (IsEmoji(afterZwj) || HasEmojiPresentation(afterZwj))
                        {
                            length++;
                            i++;
                        }
                    }
                    continue;
                }
                
                // Keycap 序列
                if (IsKeycap(nextCodePoint))
                {
                    length++;
                    i++;
                    continue;
                }
                
                // Regional Indicator - 国旗是两个 RI
                if (IsRegionalIndicator(codePoint) && IsRegionalIndicator(nextCodePoint))
                {
                    length++;
                    i++;
                    continue;
                }
                
                // 序列结束
                break;
            }
            
            result.Add((start, length));
        }
        
        return result;
    }
    
    /// <summary>
    /// 检查码点是否在范围内
    /// </summary>
    private static bool IsInRange(int codePoint, (int Start, int End) range)
    {
        return codePoint >= range.Start && codePoint <= range.End;
    }
    
    /// <summary>
    /// 检查码点是否在多个范围内
    /// </summary>
    private static bool IsInRange(int codePoint, (int Start, int End)[] ranges)
    {
        foreach (var range in ranges)
        {
            if (codePoint >= range.Start && codePoint <= range.End)
                return true;
        }
        return false;
    }
}