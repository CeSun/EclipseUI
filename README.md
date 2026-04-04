# EclipseUI

一个现代化的跨平台 .NET UI 框架，使用 SkiaSharp 渲染，支持 EUI 声明式语法。

## 特性

- 🎨 **SkiaSharp 渲染** - 高质量矢量图形渲染
- 🚀 **多后端支持** - CPU、OpenGL、ANGLE (D3D11)
- 📝 **EUI 语法** - 类似 Razor 的声明式 UI 语法
- 🔧 **Source Generator** - 编译时代码生成，零运行时开销
- 🪟 **纯 Win32** - 无 WinForms/WPF 依赖
- 🌐 **多语言文本** - HarfBuzz 塑形，Unicode Emoji 支持

## 快速开始

### 安装

```bash
dotnet add package EclipseUI
```

### Hello World

```csharp
using Eclipse.Windows;

// Program.cs
Eclipse.Windows.Application.Run<HomePage>();
```

```xml
<!-- HomePage.eui -->
@using Eclipse.Controls

<StackLayout Spacing="16" Padding="20">
    <Label Text="Hello EclipseUI! 🎉" FontSize="32" FontWeight="Bold" />
    <Label Text="支持中文和 Emoji 混排 😀" FontSize="16" />
    <Button Text="点击我" OnClick="@OnButtonClick" />
</StackLayout>

@code {
    private int _count = 0;
    
    private void OnButtonClick(object? sender, EventArgs e)
    {
        _count++;
        StateHasChanged();
    }
}
```

## 项目结构

```
EclipseUI/
├── src/
│   ├── Eclipse.Core/       # 核心抽象层 (IComponent, BuildContext)
│   ├── Eclipse.Controls/   # UI 控件库 (Label, Button, StackLayout...)
│   ├── Eclipse.Skia/       # SkiaSharp 渲染层
│   │   └── Text/           # 文本渲染系统 (HarfBuzz, Emoji)
│   ├── Eclipse.Generator/  # EUI Source Generator
│   └── Eclipse.Windows/    # Windows 平台支持 (Win32, ANGLE, OpenGL)
├── samples/
│   └── SkiaDemo/           # Demo 应用
└── tests/
    └── Eclipse.Tests/      # 单元测试
```

## 渲染后端

| 后端 | 说明 | 性能 | 兼容性 |
|------|------|------|--------|
| **ANGLE** | D3D11 后端 (默认) | ⭐⭐⭐⭐⭐ | 最佳 |
| **OpenGL** | 原生 WGL | ⭐⭐⭐⭐ | 需 GPU 驱动 |
| **CPU** | 软件渲染 | ⭐⭐ | 兜底 |

```csharp
// 切换后端
var window = new WindowImpl(RenderBackend.OpenGL);
```

## 文本渲染系统

基于 **HarfBuzz** 的现代文本渲染，支持：

- ✅ Emoji 序列 (ZWJ 组合、肤色修饰、国旗)
- ✅ 多语言字体回退 (中文 → Emoji → 其他)
- ✅ Unicode TR#51 规范 Emoji 检测
- ✅ 中文 + Emoji 混排

```csharp
// EmojiDetector - Unicode 规范检测
bool isEmoji = EmojiDetector.IsEmoji(codePoint);
bool hasEmojiPresentation = EmojiDetector.HasEmojiPresentation(codePoint);

// HarfBuzzTextRenderer - 智能渲染
var renderer = new HarfBuzzTextRenderer();
renderer.DrawText(canvas, "你好 🌍 世界 👨‍👩‍👧‍👦", x, y, font, paint);
```

## 内置控件

| 控件 | 说明 |
|------|------|
| `StackLayout` | 垂直/水平堆叠布局 |
| `HStack` | 水平堆叠布局 (简化版) |
| `Label` | 文本标签 (支持 Emoji) |
| `Button` | 按钮 |
| `TextInput` | 文本输入框 |
| `CheckBox` | 复选框 |
| `Image` | 图片 |
| `Container` | 容器 |

## EUI 语法

### 属性绑定

```xml
<Label Text="Hello" FontSize="24" Color="#333" />
```

### 事件绑定

```xml
<Button Text="Click" OnClick="@OnButtonClick" />
```

### 条件渲染

```xml
@if (_count > 0)
{
    <Label Text=$"Count: {_count}" />
}
```

### 循环渲染

```xml
@foreach (var item in items)
{
    <Label Text=@item.Name />
}
```

### 代码块

```xml
@code {
    private int _count = 0;
    
    private void OnButtonClick(object? sender, EventArgs e)
    {
        _count++;
        StateHasChanged();
    }
}
```

## 开发路线

### 已完成 ✅

- [x] 核心组件系统
- [x] EUI 语法解析
- [x] Source Generator
- [x] SkiaSharp 渲染
- [x] 多渲染后端 (CPU/OpenGL/ANGLE)
- [x] Win32 窗口
- [x] HarfBuzz 文本塑形
- [x] Emoji 检测与渲染
- [x] 多语言字体回退

### 进行中 🚧

- [ ] 布局系统 (测量/排列)
- [ ] 事件处理 (鼠标/键盘)
- [ ] TextInput 完整实现 (光标、IME)

### 计划中 📋

- [ ] 动画系统
- [ ] 样式系统
- [ ] 数据绑定 (MVVM)
- [ ] 更多控件 (ScrollView, ListView...)
- [ ] 跨平台支持 (macOS/Linux/Android/iOS)
- [ ] RTL 语言支持 (阿拉伯语、希伯来语)

## 文档

- [架构设计](docs/architecture.md)
- [快速开始](docs/getting-started.md)
- [EUI 语法](docs/syntax.md)
- [文本渲染](docs/text-rendering.md)

## 贡献

欢迎提交 Issue 和 Pull Request！

## 许可证

MIT License