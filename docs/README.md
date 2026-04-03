# EclipseUI

一个声明式 UI 框架，使用 `.eui` 文件定义用户界面。

## 概述

EclipseUI 是一个受 Blazor 启发的声明式 UI 框架，通过 Roslyn Source Generator 在编译时将 `.eui` 文件转换为高效的 C# 代码。框架提供：

- **声明式语法**：使用类似 HTML/JSX 的标签语法定义 UI
- **强类型绑定**：属性和事件与 C# 代码直接绑定
- **编译时生成**：无运行时反射开销
- **控制流支持**：`@if`、`@foreach` 等控制流语句
- **多平台潜力**：核心抽象与平台无关，可扩展到不同渲染后端

## 项目结构

```
EclipseUI/
├── src/
│   ├── Eclipse.Core/         # 核心抽象和组件基类
│   ├── Eclipse.Generator/    # Roslyn Source Generator
│   ├── Eclipse.Platform/     # 平台抽象接口（规划中）
│   └── Eclipse.Windows/      # Windows 平台适配（规划中）
└── test/
    └── Eclipse.Demo/         # 示例和测试项目
```

## 快速开始

### 1. 创建 .eui 文件

```xml
@using Eclipse.Demo.Controls

@code {
    private string name = "World";
    private int counter = 0;

    private void OnClick(object? sender, EventArgs e)
    {
        counter++;
    }
}

<StackLayout Spacing=16 Padding=20>
    <Label Text=$"Hello {name}!" FontSize=24 />
    <Button Text="Click Me" OnClick=@OnClick />
    <Label Text=@$"Counter: {counter}" />
</StackLayout>
```

### 2. 编译时生成

Source Generator 自动将 `.eui` 文件转换为 C# 代码，生成 `Render` 方法：

```csharp
public override void Render(IRenderContext context)
{
    using (context.BeginComponent<StackLayout>(new ComponentId(1), out var __stacklayout_1))
    {
        __stacklayout_1.Spacing = 16;
        __stacklayout_1.Padding = 20;
        
        using (context.BeginChildContent())
        {
            // ... 子组件渲染代码
        }
    }
}
```

## 文档

- [架构设计](./architecture.md)
- [语法参考](./syntax.md)
- [快速入门](./getting-started.md)

## 许可证

MIT License