# EclipseUI 文档

> 由 [OpenClaw](https://github.com/openclaw/openclaw) 开发的跨平台 .NET UI 框架

## 概述

EclipseUI 是一个使用 **Skia 自绘渲染** 和 **类 Razor 语法** 的现代 UI 框架：

- **SkiaSharp 渲染** - Google Skia 图形库，高质量矢量渲染
- **EUI 语法** - 类似 Blazor/Razor 的声明式 UI
- **Source Generator** - 编译时代码生成，零运行时反射
- **HarfBuzz 文本** - 现代文本塑形，完整 Emoji 支持

## 文档目录

| 文档 | 说明 |
|------|------|
| [快速入门](./getting-started.md) | 环境配置、项目创建、基础用法 |
| [EUI 语法参考](./syntax.md) | 完整语法说明：指令、控件、绑定、控制流 |
| [架构设计](./architecture.md) | 框架架构、核心组件、代码生成策略 |
| [文本渲染系统](./text-rendering.md) | HarfBuzz、Emoji 检测、字体回退 |

## 快速示例

```xml
<!-- HomePage.eui -->
@using Eclipse.Controls

@code {
    private int count = 0;
    
    private void OnClick(object? sender, EventArgs e)
    {
        count++;
    }
}

<StackLayout Spacing="16" Padding="20">
    <Label Text="你好 EclipseUI! 🎉" FontSize="32" />
    <Label Text=@$"点击次数: {count}" />
    <Button Text="点击我" OnClick="@OnClick" />
</StackLayout>
```

## 项目状态

- ✅ 核心组件系统
- ✅ EUI 语法 + Source Generator
- ✅ SkiaSharp 多后端渲染
- ✅ HarfBuzz 文本塑形
- 🚧 布局系统、事件处理
- 📋 动画、样式、跨平台

## 许可证

MIT License