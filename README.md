# EclipseUI

> 由 [OpenClaw](https://github.com/openclaw/openclaw) 开发的跨平台 .NET UI 框架

一个现代化的跨平台 UI 框架，使用 **SkiaSharp 自绘渲染**，支持 **类 Razor 声明式语法 (EUI)**。

## 核心特性

### 🎨 Skia 自绘渲染

- 基于 Google Skia 图形库，高质量矢量渲染
- 多渲染后端：ANGLE (D3D11)、OpenGL、CPU 软渲染
- 纯 Win32 窗口，无 WinForms/WPF 依赖
- 跨平台潜力 (Windows / macOS / Linux / Android / iOS)

### 📝 类 Razor 语法 (EUI)

```xml
<!-- HomePage.eui -->
@using Eclipse.Controls

<StackLayout Spacing="16" Padding="20">
    <Label Text="你好 EclipseUI! 🎉" FontSize="32" />
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

**语法特性：**
- `@using` - 引入命名空间
- `@code { }` - C# 代码块
- `@if` / `@foreach` - 控制流
- `@变量` - 属性绑定
- `OnClick="@方法"` - 事件绑定

### 🌐 现代文本渲染

- 基于 **HarfBuzz** 的文本塑形
- Unicode TR#51 规范 Emoji 检测
- 支持 Emoji 序列 (ZWJ 组合、肤色修饰、国旗)
- 智能多语言字体回退

```csharp
// 支持：你好 🌍 World 👨‍👩‍👧‍👦
```

### 🔧 编译时代码生成

- Roslyn Source Generator
- 零反射、强类型
- 编译时语法检查

## 项目状态

### ✅ 已完成

| 模块 | 功能 | 状态 |
|------|------|------|
| **核心** | 组件系统、BuildContext | ✅ 完成 |
| **语法** | EUI 解析、Source Generator | ✅ 完成 |
| **渲染** | SkiaSharp 多后端 | ✅ 完成 |
| **窗口** | Win32 原生窗口 | ✅ 完成 |
| **文本** | HarfBuzz 塑形、Emoji | ✅ 完成 |
| **控件** | Label, Button, StackLayout 等 | ✅ 基础完成 |

### 🚧 进行中

| 功能 | 说明 |
|------|------|
| 布局系统 | Measure/Arrange 机制 |
| 事件处理 | 鼠标、键盘输入 |
| TextInput | 光标、选择、IME 输入 |

### 📋 计划中

| 功能 | 说明 |
|------|------|
| 动画系统 | 属性动画、过渡效果 |
| 样式系统 | CSS-like 样式 |
| 数据绑定 | MVVM 支持 |
| 更多控件 | ScrollView, ListView, Grid... |
| 跨平台 | macOS, Linux, Android, iOS |
| RTL 语言 | 阿拉伯语、希伯来语 |

## 架构

```
┌─────────────────────────────────────────────────────────┐
│                    .eui 文件                             │
│              (类 Razor 声明式 UI)                        │
└─────────────────────────────────────────────────────────┘
                        ↓ 编译时
┌─────────────────────────────────────────────────────────┐
│              EclipseSourceGenerator                      │
│           (Roslyn Source Generator)                      │
│         生成强类型 Render() 方法                          │
└─────────────────────────────────────────────────────────┘
                        ↓ 运行时
┌─────────────────────────────────────────────────────────┐
│                    Eclipse.Core                          │
│         IComponent, BuildContext, ComponentBase          │
└─────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────┐
│                    Eclipse.Skia                          │
│         SkiaSharp 渲染 + HarfBuzz 文本                   │
└─────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────┐
│                   平台适配层                              │
│     Windows (Win32) │ macOS (规划) │ Linux (规划)        │
└─────────────────────────────────────────────────────────┘
```

## 项目结构

```
EclipseUI/
├── src/
│   ├── Eclipse.Core/       # 核心抽象层
│   ├── Eclipse.Controls/   # UI 控件库
│   ├── Eclipse.Skia/       # SkiaSharp 渲染层
│   │   └── Text/           # 文本系统 (HarfBuzz, Emoji)
│   ├── Eclipse.Generator/  # EUI Source Generator
│   └── Eclipse.Windows/    # Windows 平台支持
├── samples/
│   └── SkiaDemo/           # 示例应用
└── tests/
    └── Eclipse.Tests/      # 单元测试
```

## 文档

- [架构设计](docs/architecture.md)
- [EUI 语法参考](docs/syntax.md)
- [快速开始](docs/getting-started.md)
- [文本渲染系统](docs/text-rendering.md)

## 开发

```bash
# 克隆项目
git clone https://github.com/CeSun/EclipseUI.git

# 构建
cd EclipseUI
dotnet build EclipseUI.sln

# 运行示例
dotnet run --project samples/SkiaDemo
```

## 贡献

欢迎提交 Issue 和 Pull Request！

## 许可证

MIT License