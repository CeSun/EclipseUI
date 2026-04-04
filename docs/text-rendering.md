# 文本渲染系统

EclipseUI 基于 HarfBuzz 的现代文本渲染架构。

## 为什么需要 HarfBuzz？

传统文本渲染的问题：

| 问题 | 传统方案 | HarfBuzz 方案 |
|------|----------|---------------|
| Emoji 序列 | 逐字渲染，ZWJ 失效 | 整体塑形，正确显示 |
| 肤色修饰符 | 单独渲染 | 与基础 Emoji 组合 |
| 连字 | 不支持 | 自动处理 (fi → ﬁ) |
| 字距调整 | 手动 | 自动计算 |
| 复杂脚本 | 简单拼接 | 正确塑形 (阿拉伯语) |

## 架构

```
用户文本: "你好 👨‍👩‍👧‍👦 World 🎉"
    ↓
┌─────────────────────────────────────────────┐
│          HarfBuzzTextRenderer               │
│                                             │
│  1. SegmentText() - 按字体需求分段          │
│     ├── EmojiDetector.IsEmoji()             │
│     └── DetermineTypeface()                  │
│                                             │
│  2. 各段独立处理                             │
│     ├── 中文 → Microsoft YaHei              │
│     ├── Emoji → Segoe UI Emoji              │
│     └── 英文 → 默认字体                      │
│                                             │
│  3. DrawSegment() - 渲染并拼接              │
└─────────────────────────────────────────────┘
    ↓
正确渲染: "你好" + "👨‍👩‍👧‍👦" + "World" + "🎉"
```

## EmojiDetector

### Unicode TR#51 规范

基于 Unicode Technical Standard #51 实现的 Emoji 检测。

**核心属性：**

| 属性 | 检测方法 | 说明 |
|------|----------|------|
| `Emoji` | `IsEmoji()` | 是 Emoji 字符 |
| `Emoji_Presentation` | `HasEmojiPresentation()` | 默认彩色显示 |
| `Emoji_Modifier` | `IsEmojiModifier()` | 肤色修饰符 |
| `Regional_Indicator` | `IsRegionalIndicator()` | 国旗字母 (🇦-🇿) |

**特殊字符：**

| 字符 | 码点 | 作用 |
|------|------|------|
| ZWJ | U+200D | 连接 Emoji 组合 |
| VS16 | U+FE0F | 请求 Emoji 样式 |
| VS15 | U+FE0E | 请求文本样式 |
| Keycap | U+20E3 | 键帽组合 |

### Emoji 序列类型

```csharp
// 查找所有 Emoji 序列
var sequences = EmojiDetector.FindEmojiSequences(text);

// 支持的序列类型:
// - ZWJ 序列: 👨‍👩‍👧‍👦 (家庭)
// - 肤色修饰: 👍🏻 (拇指+浅肤色)
// - 国旗: 🇨🇳 (CN)
// - 键帽: 1️ (数字+VS16+Keycap)
```

### 使用示例

```csharp
using Eclipse.Skia.Text;

// 检测单个字符
int codePoint = char.ConvertToUtf32("😀", 0);
if (EmojiDetector.IsEmoji(codePoint))
{
    Console.WriteLine("这是 Emoji!");
}

// 检测是否默认彩色
if (EmojiDetector.HasEmojiPresentation(codePoint))
{
    // 使用 Emoji 字体渲染
}

// 检测国旗
if (EmojiDetector.IsRegionalIndicator(codePoint))
{
    // 可能是国旗的一部分，检查下一个字符
}
```

## HarfBuzzTextRenderer

### 智能分段渲染

```csharp
var renderer = new HarfBuzzTextRenderer();

// 渲染混合文本
renderer.DrawText(
    canvas, 
    "你好 🌍 World 👨‍👩‍👧‍👦", 
    x, y, 
    font, 
    paint);
```

**分段逻辑：**

```
输入: "你好🌍World"
      ↓
检查每个字素的字体需求:
      ↓
┌──────────────────────────────────────────┐
│ 字素  │ 码点  │ 需要字体      │ 分段   │
├───────┼───────┼───────────────┼────────┤
│ 你    │ U+4F60│ MS YaHei      │ 段1    │
│ 好    │ U+597D│ MS YaHei      │ 段1    │
│ 🌍    │ U+1F30│ Emoji Font    │ 段2    │
│ W     │ U+57  │ Default       │ 段3    │
│ o     │ U+6F  │ Default       │ 段3    │
│ r     │ U+72  │ Default       │ 段3    │
│ l     │ U+6C  │ Default       │ 段3    │
│ d     │ U+64  │ Default       │ 段3    │
└──────────────────────────────────────────┘
      ↓
三段渲染: "你好" + "🌍" + "World"
```

### 字体回退链

```
Emoji → Segoe UI Emoji / Noto Color Emoji / Apple Color Emoji
中文 → Microsoft YaHei / PingFang SC / SimSun
日文 → MS Gothic / Hiragino
韩文 → Malgun Gothic
其他 → SKFontManager.MatchCharacter()
```

### 测量文本

```csharp
// 获取渲染宽度
float width = renderer.MeasureText("测试 🎉 Test", font);

// 用于居中、换行计算等
float centerX = bounds.Left + (bounds.Width - width) / 2;
```

## HarfBuzzTextShaper

### 文本塑形

```csharp
var shaper = HarfBuzzTextShaper.GetChineseShaper();
var glyphs = shaper.Shape("测试文本", fontSize, Direction.LeftToRight);

// 输出字形信息
foreach (var g in glyphs)
{
    Console.WriteLine($"码点: {g.Codepoint}, 前进: {g.XAdvance}px");
}
```

### 获取专用塑形器

```csharp
// 中文
var chinese = HarfBuzzTextShaper.GetChineseShaper();

// Emoji
var emoji = HarfBuzzTextShaper.GetEmojiShaper();

// 自定义字体
var custom = HarfBuzzTextShaper.GetOrCreate(myTypeface);
```

## 实现细节

### Emoji 字体选择

```csharp
private static readonly string[] EmojiFontFamilies =
{
    "Segoe UI Emoji",      // Windows
    "Noto Color Emoji",    // Linux/Android
    "Apple Color Emoji",   // macOS/iOS
    "Twemoji Mozilla"      // Firefox
};
```

### 中文字体选择

```csharp
private static readonly string[] ChineseFontFamilies =
{
    "Microsoft YaHei",     // Windows
    "PingFang SC",         // macOS
    "SimSun",              // Windows 传统
    "Noto Sans CJK SC",    // Linux
    "Segoe UI"             // 兜底
};
```

### Emoji 大小调整

Emoji 通常比文字稍大：

```csharp
Size = segment.IsEmoji ? baseFont.Size * 1.1f : baseFont.Size
```

## 扩展方向

### 1. 完整 HarfBuzz 塑形

当前实现是简化版，完整塑形需要：

- 加载字体数据到 HarfBuzz
- 实现连字 (ligature)
- 实现字距调整 (kerning)
- 支持复杂脚本 (阿拉伯语、印度语)

### 2. RTL 语言支持

```csharp
// 阿拉伯语塑形
var glyphs = shaper.Shape("مرحبا", fontSize, Direction.RightToLeft);
```

### 3. RichTextKit 集成

Topten.RichTextKit 提供更完整的富文本支持：

- 多段落
- 文字样式变化
- 内联图片
- 文本选择

## 性能优化

### 字体缓存

所有字体都被缓存，避免重复查找：

```csharp
private static readonly Dictionary<string, SKTypeface> _typefaceCache = new();
private static SKTypeface? _emojiTypeface;
private static SKTypeface? _chineseTypeface;
```

### 分段优化

同字体字符合并为一段，减少渲染调用：

```
"Hello World" → 一段 (同字体)
"你好 World" → 两段 (中文 + 英文)
"你🌍好" → 三段 (中文 + Emoji + 中文)
```

## 参考

- [Unicode TR#51 - Emoji](https://unicode.org/reports/tr51/)
- [HarfBuzz 官方文档](https://harfbuzz.github.io/)
- [SkiaSharp.HarfBuzz NuGet](https://www.nuget.org/packages/SkiaSharp.HarfBuzz)