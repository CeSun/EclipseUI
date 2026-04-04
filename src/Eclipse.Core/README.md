# Eclipse.Core

EclipseUI 核心抽象层，提供组件系统和构建上下文。

## 主要类型

### IComponent
所有 UI 组件的基础接口。

### ComponentBase
组件基类，提供 `Render()` 方法和 `StateHasChanged()` 状态更新。

### IBuildContext
组件树构建上下文，用于声明式构建 UI。

### BuildContext
`IBuildContext` 的默认实现。

### ComponentId
组件唯一标识符。

## 用法

```csharp
var context = new BuildContext();

using (context.BeginComponent<StackLayout>(new ComponentId(1), out var layout))
{
    layout.Spacing = "16";
    
    using (context.BeginChildContent())
    {
        using (context.BeginComponent<Label>(new ComponentId(2), out var label))
        {
            label.Text = "Hello";
        }
    }
}

// context.RootComponent 包含构建好的组件树
```