using Eclipse.Skia.Text;
using Xunit;

namespace Eclipse.Tests;

/// <summary>
/// EmojiDetector 单元测试 - 基于 Unicode TR#51 规范
/// </summary>
public class EmojiDetectorTests
{
    // === 基础 Emoji 检测测试 ===
    
    [Fact]
    public void IsEmoji_ShouldReturnTrue_ForBasicEmoji()
    {
        // Assert - 常见 Emoji
        Assert.True(EmojiDetector.IsEmoji(0x1F600)); // 😀 Grinning Face
        Assert.True(EmojiDetector.IsEmoji(0x1F389)); // 🎉 Party Popper
        Assert.True(EmojiDetector.IsEmoji(0x1F44D)); // 👍 Thumbs Up
        Assert.True(EmojiDetector.IsEmoji(0x2764));  // ❤ Heavy Black Heart
    }
    
    [Fact]
    public void IsEmoji_ShouldReturnFalse_ForNonEmoji()
    {
        // Assert - 非 Emoji 字符
        Assert.False(EmojiDetector.IsEmoji('A'));
        Assert.False(EmojiDetector.IsEmoji('a'));
        Assert.False(EmojiDetector.IsEmoji('0'));
        Assert.False(EmojiDetector.IsEmoji(' '));
        Assert.False(EmojiDetector.IsEmoji(0x4E00)); // 中文 "一"
    }
    
    [Fact]
    public void IsEmoji_ShouldReturnTrue_ForRegionalIndicator()
    {
        // Assert - Regional Indicator (用于国旗)
        Assert.True(EmojiDetector.IsEmoji(0x1F1E6)); // 🇦 Regional Indicator A
        Assert.True(EmojiDetector.IsEmoji(0x1F1FF)); // 🇿 Regional Indicator Z
    }
    
    // === Emoji 表现样式测试 ===
    
    [Fact]
    public void HasEmojiPresentation_ShouldReturnTrue_ForDefaultEmojiPresentation()
    {
        // Assert - 默认显示为 Emoji 样式
        Assert.True(EmojiDetector.HasEmojiPresentation(0x1F600)); // 😀
        Assert.True(EmojiDetector.HasEmojiPresentation(0x1F44D)); // 👍
        Assert.True(EmojiDetector.HasEmojiPresentation(0x1F389)); // 🎉
    }
    
    [Fact]
    public void HasEmojiPresentation_ShouldReturnTrue_ForKeycapBaseCharacters()
    {
        // Assert - Keycap 基础字符
        Assert.True(EmojiDetector.HasEmojiPresentation('0'));
        Assert.True(EmojiDetector.HasEmojiPresentation('9'));
        Assert.True(EmojiDetector.HasEmojiPresentation('#'));
        Assert.True(EmojiDetector.HasEmojiPresentation('*'));
    }
    
    // === Variation Selector 测试 ===
    
    [Fact]
    public void IsEmojiPresentationSelector_ShouldReturnTrue_ForVS16()
    {
        // Assert - VS16 (Emoji 表现选择器)
        Assert.True(EmojiDetector.IsEmojiPresentationSelector(0xFE0F));
    }
    
    [Fact]
    public void IsTextPresentationSelector_ShouldReturnTrue_ForVS15()
    {
        // Assert - VS15 (文本表现选择器)
        Assert.True(EmojiDetector.IsTextPresentationSelector(0xFE0E));
    }
    
    [Fact]
    public void IsEmojiPresentationSelector_ShouldReturnFalse_ForOtherCharacters()
    {
        Assert.False(EmojiDetector.IsEmojiPresentationSelector('A'));
        Assert.False(EmojiDetector.IsEmojiPresentationSelector(0xFE0E));
    }
    
    // === ZWJ (Zero Width Joiner) 测试 ===
    
    [Fact]
    public void IsZWJ_ShouldReturnTrue_ForZWJCharacter()
    {
        // Assert - ZWJ (U+200D)
        Assert.True(EmojiDetector.IsZWJ(0x200D));
    }
    
    [Fact]
    public void IsZWJ_ShouldReturnFalse_ForOtherCharacters()
    {
        Assert.False(EmojiDetector.IsZWJ('A'));
        Assert.False(EmojiDetector.IsZWJ(0x200C)); // Zero Width Non-Joiner
    }
    
    // === 肤色修饰符测试 ===
    
    [Fact]
    public void IsEmojiModifier_ShouldReturnTrue_ForSkinToneModifiers()
    {
        // Assert - 肤色修饰符 (Fitzpatrick scale)
        Assert.True(EmojiDetector.IsEmojiModifier(0x1F3FB)); // 🏻 Light Skin Tone
        Assert.True(EmojiDetector.IsEmojiModifier(0x1F3FC)); // 🏼 Medium-Light Skin Tone
        Assert.True(EmojiDetector.IsEmojiModifier(0x1F3FD)); // 🏽 Medium Skin Tone
        Assert.True(EmojiDetector.IsEmojiModifier(0x1F3FE)); // 🏾 Medium-Dark Skin Tone
        Assert.True(EmojiDetector.IsEmojiModifier(0x1F3FF)); // 🏿 Dark Skin Tone
    }
    
    [Fact]
    public void IsEmojiModifier_ShouldReturnFalse_ForOtherCharacters()
    {
        Assert.False(EmojiDetector.IsEmojiModifier('A'));
        Assert.False(EmojiDetector.IsEmojiModifier(0x1F600)); // Emoji 本身
    }
    
    // === Regional Indicator 测试 ===
    
    [Fact]
    public void IsRegionalIndicator_ShouldReturnTrue_ForValidRange()
    {
        // Assert - A-Z 的 Regional Indicator
        for (int i = 0x1F1E6; i <= 0x1F1FF; i++)
        {
            Assert.True(EmojiDetector.IsRegionalIndicator(i));
        }
    }
    
    [Fact]
    public void IsRegionalIndicator_ShouldReturnFalse_ForOtherCharacters()
    {
        Assert.False(EmojiDetector.IsRegionalIndicator('A'));
        Assert.False(EmojiDetector.IsRegionalIndicator(0x1F600));
    }
    
    // === Keycap 测试 ===
    
    [Fact]
    public void IsKeycap_ShouldReturnTrue_ForKeycapCharacter()
    {
        // Assert - Combining Enclosing Keycap (U+20E3)
        Assert.True(EmojiDetector.IsKeycap(0x20E3));
    }
    
    [Fact]
    public void IsKeycap_ShouldReturnFalse_ForOtherCharacters()
    {
        Assert.False(EmojiDetector.IsKeycap('#'));
        Assert.False(EmojiDetector.IsKeycap(0x1F600));
    }
    
    // === MightBeEmoji 快速检测测试 ===
    
    [Fact]
    public void MightBeEmoji_ShouldReturnTrue_ForEmojiBase()
    {
        Assert.True(EmojiDetector.MightBeEmoji(0x1F600)); // 😀
        Assert.True(EmojiDetector.MightBeEmoji(0x1F44D)); // 👍
    }
    
    [Fact]
    public void MightBeEmoji_ShouldReturnTrue_ForEmojiComponents()
    {
        Assert.True(EmojiDetector.MightBeEmoji(0xFE0F));  // VS16
        Assert.True(EmojiDetector.MightBeEmoji(0x200D));  // ZWJ
        Assert.True(EmojiDetector.MightBeEmoji(0x1F3FB)); // Skin Tone
        Assert.True(EmojiDetector.MightBeEmoji(0x1F1E6)); // Regional Indicator
    }
    
    [Fact]
    public void MightBeEmoji_ShouldReturnFalse_ForRegularText()
    {
        Assert.False(EmojiDetector.MightBeEmoji('A'));
        Assert.False(EmojiDetector.MightBeEmoji('a'));
        Assert.False(EmojiDetector.MightBeEmoji('0')); // 注意：数字是 Keycap 基础，但有 emoji 属性
        Assert.False(EmojiDetector.MightBeEmoji(0x4E00)); // 中文
    }
    
    // === Emoji 序列检测测试 ===
    
    [Fact]
    public void FindEmojiSequences_ShouldFindSingleEmoji()
    {
        // Arrange
        var text = "Hello 😀 World";
        
        // Act
        var sequences = EmojiDetector.FindEmojiSequences(text);
        
        // Assert
        Assert.Single(sequences);
        Assert.Equal(6, sequences[0].Start); // 😀 在索引 6
        Assert.Equal(1, sequences[0].Length);
    }
    
    [Fact]
    public void FindEmojiSequences_ShouldFindMultipleEmoji()
    {
        // Arrange
        var text = "😀🎉👍";
        
        // Act
        var sequences = EmojiDetector.FindEmojiSequences(text);
        
        // Assert
        Assert.Equal(3, sequences.Count);
    }
    
    [Fact]
    public void FindEmojiSequences_ShouldFindEmojiWithVS16()
    {
        // Arrange - ❤️ (Heart + VS16)
        var text = "❤\uFE0F";
        
        // Act
        var sequences = EmojiDetector.FindEmojiSequences(text);
        
        // Assert
        Assert.Single(sequences);
        Assert.Equal(2, sequences[0].Length); // Heart + VS16
    }
    
    [Fact]
    public void FindEmojiSequences_ShouldFindEmojiWithSkinTone()
    {
        // Arrange - 👍🏻 (Thumbs Up + Light Skin Tone)
        var text = "👍\uFE0F🏻"; // Thumbs up + VS16 + Light skin
        
        // Act
        var sequences = EmojiDetector.FindEmojiSequences(text);
        
        // Assert
        Assert.Single(sequences);
        // 长度应该包含 Emoji + VS16 + Skin Tone
    }
    
    [Fact]
    public void FindEmojiSequences_ShouldFindZWJSequence()
    {
        // Arrange - 👨‍👩‍👧‍👦 (Family: Man + ZWJ + Woman + ZWJ + Girl + ZWJ + Boy)
        var text = "👨\u200D👩\u200D👧\u200D👦";
        
        // Act
        var sequences = EmojiDetector.FindEmojiSequences(text);
        
        // Assert
        Assert.Single(sequences);
        // ZWJ 序列应该被识别为一个整体
        Assert.True(sequences[0].Length >= 4);
    }
    
    [Fact]
    public void FindEmojiSequences_ShouldFindFlagSequence()
    {
        // Arrange - 🇺🇸 (US Flag: Regional Indicator U + Regional Indicator S)
        var text = "🇺🇸";
        
        // Act
        var sequences = EmojiDetector.FindEmojiSequences(text);
        
        // Assert
        Assert.Single(sequences);
        Assert.Equal(2, sequences[0].Length); // 两个 Regional Indicator
    }
    
    [Fact]
    public void FindEmojiSequences_ShouldFindKeycapSequence()
    {
        // Arrange - 1️⃣ (Keycap 1)
        // 注意：实际序列是 "1" + VS16 + Keycap
        // 但简化测试
        var text = "#\uFE0F\u20E3"; // # keycap
        
        // Act
        var sequences = EmojiDetector.FindEmojiSequences(text);
        
        // Assert - Keycap 序列应该被识别
        Assert.NotEmpty(sequences);
    }
    
    [Fact]
    public void FindEmojiSequences_ShouldReturnEmpty_ForNoEmoji()
    {
        // Arrange
        var text = "Hello World";
        
        // Act
        var sequences = EmojiDetector.FindEmojiSequences(text);
        
        // Assert
        Assert.Empty(sequences);
    }
    
    [Fact]
    public void FindEmojiSequences_ShouldReturnEmpty_ForEmptyString()
    {
        // Arrange
        var text = "";
        
        // Act
        var sequences = EmojiDetector.FindEmojiSequences(text);
        
        // Assert
        Assert.Empty(sequences);
    }
    
    [Fact]
    public void FindEmojiSequences_ShouldReturnEmpty_ForNullString()
    {
        // Arrange
        string? text = null;
        
        // Act
        var sequences = EmojiDetector.FindEmojiSequences(text!);
        
        // Assert
        Assert.Empty(sequences);
    }
    
    // === 复杂 Emoji 序列测试 ===
    
    [Fact]
    public void FindEmojiSequences_ShouldHandleMixedContent()
    {
        // Arrange - 文本和 Emoji 混合
        var text = "你好 🌍 World 👨‍👩‍👧‍👦";
        
        // Act
        var sequences = EmojiDetector.FindEmojiSequences(text);
        
        // Assert
        Assert.Equal(2, sequences.Count);
    }
    
    [Fact]
    public void FindEmojiSequences_ShouldHandleConsecutiveEmoji()
    {
        // Arrange
        var text = "😀😀😀";
        
        // Act
        var sequences = EmojiDetector.FindEmojiSequences(text);
        
        // Assert
        Assert.Equal(3, sequences.Count);
    }
    
    // === 边界条件测试 ===
    
    [Fact]
    public void IsEmoji_ShouldHandleAllEmojiRanges()
    {
        // 测试不同 Emoji 范围的边界
        // Emoticons
        Assert.True(EmojiDetector.IsEmoji(0x1F600));
        Assert.True(EmojiDetector.IsEmoji(0x1F64F));
        
        // Misc Symbols and Pictographs
        Assert.True(EmojiDetector.IsEmoji(0x1F300));
        Assert.True(EmojiDetector.IsEmoji(0x1F5FF));
        
        // Supplemental Symbols
        Assert.True(EmojiDetector.IsEmoji(0x1F900));
    }
    
    [Fact]
    public void HasEmojiPresentation_ShouldHandleWeatherSymbols()
    {
        // Weather symbols 应该有 emoji 表现
        Assert.True(EmojiDetector.HasEmojiPresentation(0x2600)); // ☀ Sun
        Assert.True(EmojiDetector.HasEmojiPresentation(0x2601)); // ☁ Cloud
        Assert.True(EmojiDetector.HasEmojiPresentation(0x2614)); // ☂ Umbrella with Rain
    }
    
    // === 特殊字符测试 ===
    
    [Fact]
    public void IsEmoji_ShouldReturnTrue_ForCopyrightAndRegistered()
    {
        // Assert - © ® 是 Emoji 属性字符
        Assert.True(EmojiDetector.IsEmoji(0x00A9)); // © Copyright
        Assert.True(EmojiDetector.IsEmoji(0x00AE)); // ® Registered
    }
    
    [Fact]
    public void IsEmoji_ShouldReturnTrue_ForTrademark()
    {
        // Assert - ™ 是 Emoji 属性字符
        Assert.True(EmojiDetector.IsEmoji(0x2122)); // ™ Trade Mark
    }
}