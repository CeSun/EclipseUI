# EclipseUI 架构设计

## 整体架构

```
┌─────────────────────────────────────────────────────────────┐
│                      .eui 文件                               │
│              (声明式 UI 定义)                                │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│              EclipseSourceGenerator                         │
│           (Roslyn Source Generator)                         │
│                                                              │
│  ┌─────────────────┐    ┌─────────────────────────────┐   │
│  │  指令解析        │ -> │  标记解析 (EclipseMarkupParser) │   │
│  │  @using, @inject │    │  控件、属性、控制流               │   │
│  └─────────────────┘    └─────────────────────────────┘   │
│                            │                                │
│                            ▼                                │
│                 ┌─────────────────────┐                    │
│                 │  C# 代码生成         │                    │
│                 │  Render() 方法       │                    │
│                 └─────────────────────┘                    │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                    Eclipse.Core                              │
│                                                              │
│  ┌───────────────────┐    ┌───────────────────────┐        │
│  │ IComponent        │    │ IRenderContext        │        │
│  │ ComponentBase     │    │ IRenderer             │        │
│  └───────────────────┘    └───────────────────────┘        │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                   平台适配层                                 │
│                                                              │
│  ┌─────────────┐   ┌─────────────┐   ┌─────────────┐       │
│  │ Windows    │   │ macOS       │   │ Linux       │       │
│  │ (规划中)    │   │ (规划中)    │   │ (规划中)    │       │
│  └─────────────┘   └─────────────┘   └─────────────┘       │
└─────────────────────────────────────────────────────────────┘
```

## 核心组件

### 1. EclipseSourceGenerator

编译时运行的 Roslyn Source Generator，负责：

- **文件发现**：自动发现项目中的 `.eui` 文件
- **指令解析**：处理 `@using`、`@inject`、`@code` 等指令
- **标记解析**：解析 XML 风格的控件标签和属性
- **代码生成**：生成强类型的 `Render` 方法

```csharp
[Generator]
public class EclipseSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var eclFiles = context.AdditionalTextsProvider
            .Where(file => file.Path.EndsWith(".eui", StringComparison.OrdinalIgnoreCase))
            .Select((file, ct) => (
                Path: file.Path,
                Content: file.GetText(ct)?.ToString() ?? string.Empty
            ));

        context.RegisterSourceOutput(eclFiles, GenerateSource);
    }
}
```

### 2. EclipseMarkupParser

标记解析器，处理 `.eui` 文件中的 UI 定义：

**支持的节点类型：**
- `ControlNode` - 控件节点
- `TextNode` - 文本节点
- `ExpressionNode` - 表达式节点
- `IfNode` - 条件分支
- `ForeachNode` - 循环

**语法检查：**
- 条件括号闭合检查
- 块花括号闭合检查
- 标签配对检查
- 字符串字面量闭合检查

### 3. ComponentBase

组件基类，提供生命周期管理：

```csharp
public abstract class ComponentBase : IComponent
{
    public ComponentId Id { get; }
    public IComponent? Parent { get; set; }
    public IReadOnlyList<IComponent> Children { get; }
    
    // 生命周期
    public virtual void OnInitialized() { }
    public virtual void OnParametersSet() { }
    public virtual void OnMounted() { }
    public virtual void OnUnmounted() { }
    
    // 状态通知
    protected void StateHasChanged() => StateChanged?.Invoke(this, EventArgs.Empty);
    
    // 抽象方法
    public abstract void Render(IRenderContext context);
}
```

### 4. IRenderContext

渲染上下文接口，定义组件渲染时的操作：

```csharp
public interface IRenderContext
{
    int Depth { get; }
    IReadOnlyList<ComponentId> ComponentPath { get; }
    
    // 组件渲染 - 返回组件实例用于强类型属性设置
    IDisposable BeginComponent<T>(ComponentId id, out T component) where T : IComponent, new();
    
    // 子内容
    IDisposable BeginChildContent();
    
    // 文本内容
    void SetText(string? text);
}
```

> **注意**：早期版本的 `SetAttribute`、`BindEvent`、`BindProperty`、`RenderTemplate` 已被移除。
> 现在采用强类型赋值模式：生成代码直接通过 `BeginComponent(out var component)` 获取组件实例并设置属性。

## 代码生成策略

### 属性绑定

生成强类型属性赋值：

```xml
<!-- .eui -->
<Label Text=@name FontSize=24 />

<!-- 生成的 C# -->
using (context.BeginComponent<Label>(new ComponentId(1), out var __label_1))
{
    __label_1.Text = name;
    __label_1.FontSize = 24;
}
```

### 事件绑定

事件属性自动识别并绑定：

```xml
<!-- .eui -->
<Button OnClick=@OnClick />

<!-- 生成的 C# -->
__button_1.OnClick += OnClick;
```

### 控制流

`@if` 和 `@foreach` 转换为 C# 控制流：

```xml
<!-- .eui -->
@if (counter > 5)
{
    <Label Text="More than 5" />
}

@foreach (var item in items)
{
    <Label Text=@item.Name />
}

<!-- 生成的 C# -->
if (counter > 5)
{
    using (context.BeginComponent<Label>(new ComponentId(1), out var __label_1))
    {
        __label_1.Text = "More than 5";
    }
}

foreach (var item in items)
{
    using (context.BeginComponent<Label>(new ComponentId(2), out var __label_2))
    {
        __label_2.Text = item.Name;
    }
}
```

## 设计原则

1. **编译时优化**：尽可能在编译时完成工作，减少运行时开销
2. **强类型**：生成的代码是类型安全的，编译期捕获错误
3. **零反射**：属性赋值直接通过强类型代码完成
4. **可扩展**：平台抽象层允许适配不同的渲染后端
5. **熟悉度**：语法借鉴 Blazor/Razor，降低学习成本