# EclipseUI 快速入门

## 安装

### 1. 创建项目

```bash
dotnet new console -n MyEclipseApp
cd MyEclipseApp
```

### 2. 添加 EclipseUI 引用

在 `.csproj` 文件中添加：

```xml
<ItemGroup>
    <ProjectReference Include="path/to/EclipseUI/src/Eclipse.Core/Eclipse.Core.csproj" />
    <ProjectReference Include="path/to/EclipseUI/src/Eclipse.Generator/Eclipse.Generator.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
</ItemGroup>
```

### 3. 配置 .eui 文件

在 `.csproj` 中添加 `.eui` 文件作为 AdditionalFiles：

```xml
<ItemGroup>
    <AdditionalFiles Include="**/*.eui" />
</ItemGroup>
```

## 创建组件

### 1. 定义控件

首先定义一些基础控件（或引用现有的控件库）：

```csharp
// Controls.cs
using Eclipse.Core;
using Eclipse.Core.Abstractions;

public class Label : ComponentBase
{
    public string? Text { get; set; }
    public double FontSize { get; set; } = 14;
    public string? Color { get; set; }
    
    public override void Render(IRenderContext context)
    {
        if (Text != null)
            context.SetText(Text);
        context.SetAttribute(nameof(FontSize), FontSize);
        if (Color != null)
            context.SetAttribute(nameof(Color), Color);
    }
}

public class Button : ComponentBase
{
    public string? Text { get; set; }
    public string? BackgroundColor { get; set; }
    public event EventHandler? OnClick;
    
    public override void Render(IRenderContext context)
    {
        if (Text != null)
            context.SetText(Text);
        if (BackgroundColor != null)
            context.SetAttribute(nameof(BackgroundColor), BackgroundColor);
    }
}

public class StackLayout : ComponentBase
{
    public double Spacing { get; set; } = 0;
    public double Padding { get; set; } = 0;
    
    public override void Render(IRenderContext context)
    {
        context.SetAttribute(nameof(Spacing), Spacing);
        context.SetAttribute(nameof(Padding), Padding);
    }
}
```

### 2. 创建 .eui 文件

```xml
<!-- Pages/MainPage.eui -->
@using MyEclipseApp.Controls

@code {
    private string message = "Hello EclipseUI!";
    private int clickCount = 0;
    
    private void OnButtonClick(object? sender, EventArgs e)
    {
        clickCount++;
        message = $"Clicked {clickCount} times!";
    }
}

<StackLayout Spacing=16 Padding=20>
    <Label Text=@message FontSize=24 />
    <Label Text=@$"Click count: {clickCount}" FontSize=16 Color="#666" />
    <Button Text="Click Me" OnClick=@OnButtonClick BackgroundColor="#007AFF" />
</StackLayout>
```

### 3. 使用生成的组件

```csharp
// Program.cs
using Eclipse.Core.Abstractions;

// 创建渲染上下文（具体实现取决于平台）
var context = new MyRenderContext();

// 实例化生成的组件
var page = new Pages.MainPage();

// 渲染
page.Render(context);
```

## 示例：计数器应用

### Counter.eui

```xml
@using MyEclipseApp.Controls

@code {
    private int count = 0;
    
    private void Increment(object? sender, EventArgs e)
    {
        count++;
    }
    
    private void Decrement(object? sender, EventArgs e)
    {
        count--;
    }
    
    private void Reset(object? sender, EventArgs e)
    {
        count = 0;
    }
}

<StackLayout Spacing=16 Padding=40>
    <Label Text="Counter" FontSize=32 FontWeight="Bold" />
    
    <Label 
        Text=@$"Current value: {count}" 
        FontSize=24 
        Color=@(count >= 0 ? "Green" : "Red") />
    
    <HStack Spacing=10>
        <Button Text="-" OnClick=@Decrement BackgroundColor="#FF3B30" />
        <Button Text="Reset" OnClick=@Reset BackgroundColor="#8E8E93" />
        <Button Text="+" OnClick=@Increment BackgroundColor="#34C759" />
    </HStack>
    
    @if (count == 0)
    {
        <Label Text="Count is zero" Color="#999" />
    }
    @else if (count > 0)
    {
        <Label Text="Count is positive" Color="Green" />
    }
    @else
    {
        <Label Text="Count is negative" Color="Red" />
    }
</StackLayout>
```

## 示例：列表渲染

### TodoList.eui

```xml
@using MyEclipseApp.Controls
@using System.Collections.Generic

@code {
    private List<TodoItem> todos = new()
    {
        new() { Title = "Learn EclipseUI", IsDone = true },
        new() { Title = "Build an app", IsDone = false },
        new() { Title = "Ship it!", IsDone = false }
    };
    
    private string newItem = "";
    
    private void AddItem()
    {
        if (!string.IsNullOrWhiteSpace(newItem))
        {
            todos.Add(new() { Title = newItem });
            newItem = "";
        }
    }
    
    private void ToggleItem(TodoItem item)
    {
        item.IsDone = !item.IsDone;
    }
}

<StackLayout Spacing=16 Padding=20>
    <Label Text="Todo List" FontSize=28 FontWeight="Bold" />
    
    <HStack Spacing=10>
        <TextInput 
            Text=@newItem 
            Placeholder="Add new item..." />
        <Button Text="Add" OnClick=@AddItem BackgroundColor="#007AFF" />
    </HStack>
    
    @if (todos.Count == 0)
    {
        <Label Text="No items yet. Add one above!" Color="#999" />
    }
    @else
    {
        @foreach (var item in todos)
        {
            <Card Padding=12 BackgroundColor=@(item.IsDone ? "#F0F0F0" : "#FFF")>
                <HStack Spacing=8>
                    <CheckBox 
                        IsChecked=@item.IsDone 
                        OnCheckedChanged=@(_ => ToggleItem(item)) />
                    <Label 
                        Text=@item.Title 
                        Color=@(item.IsDone ? "#999" : "#000") />
                </HStack>
            </Card>
        }
    }
    
    <Label 
        Text=@$"{todos.Count(x => x.IsDone)} of {todos.Count} completed" 
        FontSize=12 
        Color="#666" />
</StackLayout>
```

```csharp
// Models/TodoItem.cs
public class TodoItem
{
    public string Title { get; set; } = "";
    public bool IsDone { get; set; }
}
```

## 调试技巧

### 查看生成的代码

生成的代码在编译时的 `obj/Debug/netX.X/Eclipse.Generator` 目录下，格式为 `{ClassName}.ecl.g.cs`。

### 诊断错误

Source Generator 会在编译时报告语法错误：

```
error ECGEN001: Eclipse component generation failed for 'Pages/TestPage.eui': Unclosed if block at position 120, expected '}'
```

### 常见错误

| 错误 | 原因 | 解决 |
|-----|------|-----|
| `Unclosed string literal` | 字符串没有闭合引号 | 检查 `"..."` 或 `$"..."` 是否正确闭合 |
| `Mismatched end tag` | 开始和结束标签不匹配 | 确保 `<Label>...</Label>` 标签配对 |
| `Unclosed @code block` | `@code` 块缺少 `}` | 检查花括号配对 |
| `Empty or invalid tag name` | 标签名无效 | 确保标签名以字母开头 |

## 下一步

- 阅读 [语法参考](./syntax.md) 了解所有语法特性
- 阅读 [架构设计](./architecture.md) 了解框架原理
- 查看 `test/Eclipse.Demo` 目录获取更多示例