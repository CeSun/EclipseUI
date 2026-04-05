# 快速开始

> 5 分钟上手 EclipseUI

## 安装

### 前置要求

- .NET 10.0 SDK 或更高版本
- Windows 10/11（目前仅支持 Windows）

### 克隆项目

```bash
git clone https://github.com/CeSun/EclipseUI.git
cd EclipseUI
```

### 构建

```bash
dotnet build EclipseUI.sln
```

---

## 创建第一个应用

### 1. 创建项目

```bash
# 创建新的控制台应用
dotnet new console -n MyFirstApp

# 添加 EclipseUI 引用
cd MyFirstApp
dotnet add reference ../EclipseUI/src/Eclipse.Core
dotnet add reference ../EclipseUI/src/Eclipse.Controls
dotnet add reference ../EclipseUI/src/Eclipse.Skia
dotnet add reference ../EclipseUI/src/Eclipse.Windows
```

### 2. 创建 EUI 文件

创建 `HomePage.eui`：

```xml
@using Eclipse.Controls

<StackLayout Spacing="16" Padding="20">
    <Label Text="你好 EclipseUI! 🎉" FontSize="32" />
    <Label Text="@_countText" FontSize="18" Color="#666666" />
    <Button Text="点击我" OnClick="@OnButtonClick" />
</StackLayout>

@code {
    private int _count = 0;
    
    private string _countText => _count > 0 
        ? $"点击次数: {_count}" 
        : "点击按钮开始计数";
    
    private void OnButtonClick(object? sender, EventArgs e)
    {
        _count++;
        StateHasChanged();
    }
}
```

### 3. 配置入口

修改 `Program.cs`：

```csharp
using Eclipse.Core;
using Eclipse.Skia;
using Eclipse.Windows;

class Program
{
    static void Main(string[] args)
    {
        // 创建应用
        var app = AppBuilder.Create()
            .UseSkiaRenderer()
            .UseWindowsWindow()
            .Build();
        
        // 运行主页面
        app.Run<HomePage>();
    }
}
```

### 4. 运行应用

```bash
dotnet run
```

---

## 运行示例项目

项目包含一个完整的示例应用：

```bash
cd samples/SkiaDemo
dotnet run
```

示例应用展示：

- ✅ 基础控件（Label、Button、TextInput）
- ✅ 布局控件（StackLayout、Grid、ScrollView）
- ✅ 交互控件（CheckBox）
- ✅ 图片显示（Image）
- ✅ Emoji 文本渲染

---

## 项目结构

```
EclipseUI/
├── src/
│   ├── Eclipse.Core/       # 核心抽象层
│   │   ├── BuildContext    # 组件构建上下文
│   │   ├── ComponentBase   # 组件基类
│   │   └── Input/          # 输入系统
│   │
│   ├── Eclipse.Controls/   # UI 控件库
│   │   ├── Label           # 文本显示
│   │   ├── Button          # 按钮
│   │   ├── TextInput       # 文本输入
│   │   ├── StackLayout     # 堆叠布局
│   │   ├── Grid            # 网格布局
│   │   └── ScrollView      # 滚动视图
│   │
│   ├── Eclipse.Skia/       # SkiaSharp 渲染层
│   │   └── Text/           # 文本系统
│   │       ├── HarfBuzz    # 文本塑形
│   │       └ EmojiDetector # Emoji 检测
│   │
│   ├── Eclipse.Generator/  # EUI Source Generator
│   │
│   └── Eclipse.Windows/    # Windows 平台支持
│
├── samples/
│   └── SkiaDemo/           # 示例应用
│
├── tests/
│   └── Eclipse.Tests/      # 单元测试
│
└── docs/
    ├── architecture.md     # 架构设计
    ├── syntax.md           # EUI 语法参考
    └── getting-started.md  # 快速开始（本文档）
```

---

## 核心概念

### 组件 (Component)

组件是 EclipseUI 的基本构建单元。所有 UI 元素都继承自 `ComponentBase`：

```csharp
public abstract class ComponentBase : IComponent
{
    // 子元素集合
    public ComponentCollection Children { get; }
    
    // 构建 UI
    public abstract void Build(IBuildContext context);
    
    // 渲染
    public abstract void Render(IDrawingContext context, Rect bounds);
    
    // 触发重新渲染
    protected void StateHasChanged();
}
```

### 声明式 UI

EUI 语法将 UI 结构和逻辑代码分离：

```xml
<!-- UI 结构 -->
<StackLayout>
    <Label Text="Hello" />
</StackLayout>

<!-- 逻辑代码 -->
@code {
    private void DoSomething() { }
}
```

编译时，Source Generator 将 EUI 转换为强类型的 C# 代码：

```csharp
// 生成的代码（简化示例）
public override void Build(IBuildContext context)
{
    using (context.BeginComponent<StackLayout>(id1, out var stack))
    {
        using (context.BeginChildContent())
        {
            context.BeginComponent<Label>(id2, out var label);
            label.Text = "Hello";
        }
    }
}
```

### 输入系统

EclipseUI 提供完整的输入事件系统：

**指针事件：**
- `PointerPressed` / `PointerReleased`
- `PointerMoved` / `PointerEntered` / `PointerExited`
- `PointerWheelChanged`
- `Tapped`

**键盘事件：**
- `KeyDown` / `KeyUp`
- `TextInput`

**事件路由：**
- `Bubble` - 从源元素向上传播
- `Tunnel` - 从根元素向下传播
- `Direct` - 仅在源元素触发

---

## 下一步

### 学习 EUI 语法

阅读 [EUI 语法参考](syntax.md) 了解：

- 完整控件列表
- 属性绑定
- 控制流（@if, @foreach）
- 事件处理
- 布局详解

### 理解架构设计

阅读 [架构设计](architecture.md) 了解：

- SkiaSharp 渲染原理
- 文本塑形系统
- Source Generator 工作流程
- 跨平台架构设计

### 探索示例代码

查看 `samples/SkiaDemo` 目录：

- `Components/HomePage.eui` - 主页面
- `Components/DemoControls.eui` - 控件演示
- `Components/DemoLayout.eui` - 布局演示

---

## 常见问题

### Q: 为什么只支持 Windows？

EclipseUI 目前处于早期开发阶段，优先支持 Windows 平台。未来计划支持：

- macOS
- Linux
- Android
- iOS

### Q: EUI 和 Razor 有什么区别？

EUI 借鉴了 Razor 的语法设计，但有以下不同：

| 特性 | Razor | EUI |
|------|-------|-----|
| 目标 | Web | 跨平台 UI |
| 渲染 | HTML | SkiaSharp |
| 运行时 | Blazor Server/WebAssembly | 原生窗口 |

### Q: 如何处理图片？

```xml
<Image Source="assets/photo.png" />
```

图片路径相对于应用目录。支持格式：PNG, JPEG, WEBP, GIF, BMP。

### Q: 如何自定义控件？

继承 `InteractiveControl` 或 `ComponentBase`：

```csharp
public class MyControl : InteractiveControl
{
    public string? MyProperty { get; set; }
    
    public override void Render(IDrawingContext context, Rect bounds)
    {
        // 自定义渲染逻辑
        context.DrawRectangle(bounds, "#FF0000");
    }
}
```

---

## 参与贡献

欢迎提交 Issue 和 Pull Request！

贡献指南：

1. Fork 项目
2. 创建功能分支 (`git checkout -b feature/amazing-feature`)
3. 提交更改 (`git commit -m 'Add amazing feature'`)
4. 推送分支 (`git push origin feature/amazing-feature`)
5. 提交 Pull Request

---

## 许可证

MIT License - 自由使用、修改和分发。