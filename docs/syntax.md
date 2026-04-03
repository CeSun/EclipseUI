# EclipseUI 语法参考

## 文件结构

`.eui` 文件由三部分组成：

```xml
@using Namespace.One
@using Namespace.Two

@code {
    // C# 代码块
    private string name = "World";
    private void OnClick() { }
}

<!-- UI 标记 -->
<StackLayout>
    <Label Text=@name />
</StackLayout>
```

## 指令

### @using

引入命名空间：

```xml
@using System.Collections.Generic
@using Eclipse.Demo.Controls
```

### @inject

依赖注入：

```xml
@inject IAuthService AuthService
@inject ILogger<LoginPage> Logger
```

### @inherits

指定基类：

```xml
@inherits MyCustomComponentBase
```

### @attribute

添加特性：

```xml
@attribute [Authorize]
```

### @code

定义 C# 代码块：

```xml
@code {
    private int counter = 0;
    
    private void Increment()
    {
        counter++;
    }
}
```

## 控件语法

### 基本控件

```xml
<Label Text="Hello" FontSize=16 />
```

### 自闭合标签

```xml
<Label Text="Hello" />
<Image Source="icon.png" />
```

### 嵌套控件

```xml
<StackLayout Spacing=10 Padding=20>
    <Label Text="Title" />
    <Button Text="Submit" />
</StackLayout>
```

### 子内容

```xml
<Card Padding=16>
    <Label Text="Card content" />
</Card>
```

## 属性绑定

### 字符串字面量

```xml
<Label Text="Hello World" />
```

### 变量绑定

使用 `@` 前缀绑定变量：

```xml
<Label Text=@userName />
<Button Text=@buttonText />
```

### 插值字符串

使用 `$"..."` 或 `@$"..."` 进行字符串插值：

```xml
<!-- 简单插值 -->
<Label Text=$"Hello {name}!" />

<!-- 多行插值 -->
<Label Text=@$"Name: {name}
Age: {age}
Score: {score}" />
```

### 表达式绑定

使用 `@(...)` 绑定复杂表达式：

```xml
<Label Text=@(isChecked ? "Enabled" : "Disabled") />
<Label Text=@(items.Count.ToString()) />
```

### 多行字符串

使用 `@"..."` 定义多行字符串：

```xml
<Label Text=@"Line 1
Line 2
Line 3" />
```

### 数值类型

```xml
<Label FontSize=16 />
<Label FontSize=14.5 />
<StackLayout Spacing=-10 />
```

### 布尔类型

```xml
<Button IsEnabled=true />
<Button IsEnabled=false />
```

### null 值

```xml
<Image Source=null />
```

### 枚举类型

```xml
<Label TextAlignment=Center />
<StackLayout Orientation=Horizontal />
```

### 成员访问

```xml
<Label Text=@user.Name />
<Label Text=@user.Address.City />
```

### 方法调用

```xml
<Label Text=@GetName() />
<Label Text=@items.Count.ToString() />
```

### 枚举组合

```xml
<Border BorderColor=Colors.Red />
```

## 事件绑定

事件属性以 `On` 开头，绑定事件处理方法：

```xml
<Button OnClick=@OnClick />
<CheckBox OnCheckedChanged=@OnCheckedChanged />
<TextInput OnTextChanged=@OnTextChanged />
```

```csharp
@code {
    private void OnClick(object? sender, EventArgs e)
    {
        counter++;
    }
    
    private void OnCheckedChanged(object? sender, ValueChangedEventArgs<bool> e)
    {
        isChecked = e.NewValue;
    }
}
```

## 控制流

### @if 条件

```xml
@if (isLoggedIn)
{
    <Label Text="Welcome!" />
}

@if (count > 10)
{
    <Label Text="Count is greater than 10" Color="Green" />
}
```

### @if-else 条件

```xml
@if (count == 0)
{
    <Label Text="No items" />
}
@else
{
    <Label Text=@$"You have {count} items" />
}
```

### @foreach 循环

```xml
@foreach (var item in items)
{
    <Label Text=@item.Name />
}

@foreach (var user in users)
{
    <Card>
        <Label Text=@user.Name />
        <Label Text=@user.Email />
    </Card>
}
```

## 完整示例

```xml
@using Eclipse.Demo.Controls

@code {
    private string title = "Todo App";
    private List<TodoItem> items = new();
    private string newItem = "";

    private void AddItem()
    {
        if (!string.IsNullOrWhiteSpace(newItem))
        {
            items.Add(new TodoItem { Title = newItem });
            newItem = "";
        }
    }

    private void RemoveItem(int id)
    {
        items.RemoveAll(x => x.Id == id);
    }
}

<StackLayout Spacing=16 Padding=20>
    <Label Text=@title FontSize=24 FontWeight="Bold" />
    
    <HStack Spacing=10>
        <TextInput Text=@newItem Placeholder="Add item..." />
        <Button Text="Add" OnClick=@AddItem />
    </HStack>
    
    @if (items.Count == 0)
    {
        <Label Text="No items yet" Color="#999" />
    }
    @else
    {
        @foreach (var item in items)
        {
            <Card Padding=12>
                <HStack Spacing=8>
                    <Label Text=@item.Title />
                    <Button Text="Delete" OnClick=@(() => RemoveItem(item.Id)) />
                </HStack>
            </Card>
        }
    }
</StackLayout>
```

## 语法错误诊断

编译时会检测以下语法错误：

| 错误类型 | 描述 |
|---------|------|
| `Unclosed if condition` | `@if` 条件括号未闭合 |
| `Unclosed if block` | `@if` 块花括号未闭合 |
| `Unclosed foreach condition` | `@foreach` 条件括号未闭合 |
| `Unclosed foreach block` | `@foreach` 块花括号未闭合 |
| `Unclosed expression` | `@()` 表达式括号未闭合 |
| `Unclosed string literal` | 字符串字面量未闭合 |
| `Unclosed @code block` | `@code` 块花括号未闭合 |
| `Empty or invalid tag name` | 空标签名或无效标签名 |
| `Mismatched end tag` | 结束标签与开始标签不匹配 |