# Eclipse.Skia

EclipseUI SkiaSharp 渲染层。

## 架构

```
ISkiaRenderer (接口)
    ↓
DefaultSkiaRenderer (实现)
    ↓
SkiaControlRenderers (控件渲染器)
    ├── StackLayoutRenderer
    ├── LabelRenderer (使用 HarfBuzzTextRenderer)
    ├── ButtonRenderer
    └── TextContentRenderer
```

## 文本渲染系统

### 目录结构

```
Text/
├── EmojiDetector.cs          # Unicode TR#51 Emoji 检测
├── HarfBuzzTextShaper.cs     # HarfBuzz 文本塑形器
└── HarfBuzzTextRenderer.cs   # 智能分段渲染器
```

### EmojiDetector

基于 Unicode TR#51 规范的 Emoji 检测：

```csharp
// 检查是否是 Emoji
bool isEmoji = EmojiDetector.IsEmoji(codePoint);

// 检查是否默认显示为 Emoji 样式 (彩色)
bool hasEmojiPresentation = EmojiDetector.HasEmojiPresentation(codePoint);

// 检查是否是 Regional Indicator (国旗字母)
bool isRI = EmojiDetector.IsRegionalIndicator(codePoint);

// 检查是否是肤色修饰符
bool isModifier = EmojiDetector.IsEmojiModifier(codePoint);

// 检查是否是 ZWJ
bool isZWJ = EmojiDetector.IsZWJ(codePoint);

// 查找文本中的所有 Emoji 序列
var sequences = EmojiDetector.FindEmojiSequences("Hello 👨‍👩‍👧‍👦 World");
// 返回: [(6, 7)] - 从位置 6 开始，长度 7 个字素
```

### HarfBuzzTextRenderer

智能文本渲染器，自动处理：

1. **文本分段** - 按字体需求切分
2. **Emoji 优先** - Emoji 使用专用字体
3. **字体回退** - 不支持的字符自动查找合适字体

```csharp
var renderer = new HarfBuzzTextRenderer();

// 渲染文本
renderer.DrawText(canvas, "你好 🌍 世界", x, y, font, paint);

// 测量宽度
float width = renderer.MeasureText("Hello 🎉", font);
```

**渲染流程：**

```
输入文本: "你好 🌍 World"
    ↓
SegmentText() 分段
    ↓
┌─────────────┬─────────────┬─────────────┐
│ "你好"      │ "🌍"       │ "World"     │
│ 中文字体    │ Emoji字体   │ 默认字体    │
└─────────────┴─────────────┴─────────────┘
    ↓
DrawSegment() 各段渲染
    ↓
最终输出
```

### HarfBuzzTextShaper

文本塑形器（可扩展）：

```csharp
var shaper = HarfBuzzTextShaper.GetChineseShaper();
var glyphs = shaper.Shape("测试文本", fontSize);

// 获取字形信息
foreach (var glyph in glyphs)
{
    Console.WriteLine($"Codepoint: {glyph.Codepoint}, Advance: {glyph.XAdvance}");
}
```

## 渲染上下文

```csharp
public class SkiaRenderContext
{
    public SKCanvas Canvas { get; }   // Skia 画布
    public float Width { get; }       // 窗口宽度
    public float Height { get; }      // 窗口高度
    public float Scale { get; }       // DPI 缩放因子
}
```

## 自定义渲染器

```csharp
public class MyControlRenderer : ISkiaControlRenderer
{
    public Type TargetType => typeof(MyControl);
    
    public void Render(
        IComponent component, 
        SkiaRenderContext context, 
        SKRect bounds,
        Action<IComponent, SkiaRenderContext, SKRect> renderChild)
    {
        var control = (MyControl)component;
        
        // 使用 context.Canvas 绘制
        using var paint = new SKPaint { Color = SKColors.Blue };
        context.Canvas.DrawRect(bounds, paint);
        
        // 渲染子组件
        foreach (var child in control.Children)
        {
            renderChild(child, context, childBounds);
        }
    }
}
```

## 注册渲染器

```csharp
// 在 DefaultSkiaRenderer 构造函数中
RegisterRenderer<MyControlRenderer>();
```

## 字体缓存

渲染器自动缓存常用字体：

```csharp
// 获取中文字体
var chineseTypeface = LabelRenderer.GetChineseTypeface();

// 获取 Emoji 字体
var emojiTypeface = LabelRenderer.GetEmojiTypeface();
```

## 依赖

| 包 | 版本 | 说明 |
|---|------|------|
| SkiaSharp | 3.119.2 | 核心渲染 |
| SkiaSharp.HarfBuzz | 3.119.2 | 文本塑形 |

## 后续扩展

- 完整 HarfBuzz 塑形（连字、字距）
- RTL 语言支持
- 富文本格式 (RichTextKit)