# EUI 语法参考

> EclipseUI 的类 Razor 声明式 UI 语法

## 概述

EUI (Eclipse UI) 是一种类似 Razor 的声明式 UI 语法，用于构建 EclipseUI 应用界面。它结合了 XML 标记和 C# 代码，实现零反射、强类型的编译时代码生成。

## 基本结构

```xml
<!-- HomePage.eui -->
@using Eclipse.Controls

<StackLayout Spacing="16" Padding="20">
    <Label Text="你好 EclipseUI!" FontSize="32" />
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

---

## 指令

### @using

引入命名空间，与 C# 的 `using` 语句相同。

```xml
@using Eclipse.Controls
@using Eclipse.Input
```

### @code

定义 C# 代码块，包含字段、属性、方法等。

```xml
@code {
    private string _message = "Hello";
    
    public string Title { get; set; } = "首页";
    
    private void HandleClick(object? sender, EventArgs e)
    {
        // 处理逻辑
    }
}
```

---

## 控件列表

### 布局控件

| 控件 | 说明 | 常用属性 |
|------|------|----------|
| `StackLayout` | 堆叠布局（垂直/水平） | `Orientation`, `Spacing`, `Padding`, `BackgroundColor` |
| `HStack` | 水平堆叠布局 | `Spacing`, `Padding`, `BackgroundColor` |
| `Grid` | 网格布局 | `RowDefinitions`, `ColumnDefinitions`, `RowSpacing`, `ColumnSpacing` |
| `ScrollView` | 滚动视图 | `HorizontalScrollBarVisible`, `VerticalScrollBarVisible` |
| `Container` | 容器控件 | `BackgroundColor`, `Padding`, `CornerRadius` |

### 基础控件

| 控件 | 说明 | 常用属性 |
|------|------|----------|
| `Label` | 文本显示 | `Text`, `FontSize`, `Color`, `FontWeight`, `TextAlignment` |
| `Button` | 按钮 | `Text`, `BackgroundColor`, `TextColor`, `CornerRadius`, `OnClick` |
| `TextInput` | 文本输入 | `Text`, `Placeholder`, `IsPassword`, `FontSize` |
| `CheckBox` | 复选框 | `IsChecked`, `Label`, `CheckedColor`, `Size` |
| `Image` | 图片显示 | `Source`, `Width`, `Height`, `Stretch` |

---

## 属性绑定

### 字符串属性

直接使用字符串值：

```xml
<Label Text="Hello World" Color="#333333" />
<Button BackgroundColor="#007AFF" TextColor="White" />
```

### 数值属性

数值可以直接使用 double 类型：

```xml
<Label FontSize="24" />
<StackLayout Spacing="16" Padding="8" />
<Button CornerRadius="8" />
```

### 变量绑定

使用 `@` 符号绑定变量：

```xml
<Label Text="@_message" FontSize="@_fontSize" />
<Button Text="@ButtonText" IsEnabled="@CanSubmit" />
```

```csharp
@code {
    private string _message = "动态文本";
    private double _fontSize = 18;
    public string ButtonText => _count > 0 ? $"点击次数: {_count}" : "点击我";
    public bool CanSubmit => _count > 0;
}
```

---

## 控制流

### @if 条件渲染

```xml
@if (_isLoggedIn)
{
    <Label Text="欢迎回来!" Color="Green" />
}
else
{
    <Label Text="请登录" Color="Red" />
}
```

```csharp
@code {
    private bool _isLoggedIn = false;
}
```

### @foreach 循环渲染

```xml
<StackLayout Spacing="8">
    @foreach (var item in _items)
    {
        <Label Text="@item.Name" FontSize="14" />
    }
</StackLayout>
```

```csharp
@code {
    private List<Item> _items = new()
    {
        new Item { Name = "项目 1" },
        new Item { Name = "项目 2" },
        new Item { Name = "项目 3" }
    };
}
```

---

## 事件绑定

### 点击事件

使用 `OnClick` 属性绑定点击事件：

```xml
<Button Text="保存" OnClick="@OnSaveClick" />
```

```csharp
@code {
    private void OnSaveClick(object? sender, EventArgs e)
    {
        SaveData();
        StateHasChanged();
    }
}
```

### Tapped 事件

任何输入元素都可以响应 Tapped 事件：

```xml
<Container OnTapped="@OnContainerTapped">
    <Label Text="点击整个区域" />
</Container>
```

### 键盘事件

TextInput 控件支持键盘事件：

```xml
<TextInput 
    Text="@_inputText"
    OnKeyDown="@OnKeyDown"
    OnTextInput="@OnTextInput" />
```

```csharp
@code {
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            SubmitText();
        }
    }
}
```

---

## 布局详解

### StackLayout 堆叠布局

```xml
<!-- 垂直堆叠 -->
<StackLayout Orientation="Vertical" Spacing="10" Padding="16">
    <Label Text="标题" FontSize="24" />
    <Label Text="副标题" FontSize="16" />
    <Button Text="按钮" />
</StackLayout>

<!-- 水平堆叠（使用 HStack 简化） -->
<HStack Spacing="8">
    <Button Text="取消" BackgroundColor="#999" />
    <Button Text="确定" />
</HStack>
```

**属性说明：**

- `Orientation`: 堆叠方向（`Vertical` / `Horizontal`）
- `Spacing`: 子元素间距
- `Padding`: 内边距
- `BackgroundColor`: 背景颜色

### Grid 网格布局

```xml
<Grid RowSpacing="10" ColumnSpacing="10">
    <!-- 定义行 -->
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto" />
        <RowDefinition Height="*" />
        <RowDefinition Height="50" />
    </Grid.RowDefinitions>
    
    <!-- 定义列 -->
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*" />
        <ColumnDefinition Width="2*" />
    </Grid.ColumnDefinitions>
    
    <!-- 子元素指定位置 -->
    <Label Grid.Row="0" Grid.Column="0" Text="标题" />
    <Label Grid.Row="0" Grid.Column="1" Text="副标题" />
    <Button Grid.Row="2" Grid.Column="0" Grid.ColumnSpan="2" Text="底部按钮" />
</Grid>
```

**GridLength 类型：**

| 类型 | 说明 | 示例 |
|------|------|------|
| `Auto` | 根据内容自动调整 | `Height="Auto"` |
| `*` (Star) | 按比例分配剩余空间 | `Height="*"`, `Width="2*"` |
| `Absolute` | 固定像素值 | `Height="50"` |

### ScrollView 滚动视图

```xml
<ScrollView VerticalScrollBarVisible="true">
    <StackLayout Spacing="16">
        @foreach (var item in _longList)
        {
            <Label Text="@item" />
        }
    </StackLayout>
</ScrollView>
```

---

## 控件详解

### Label 文本

```xml
<Label 
    Text="Hello EclipseUI"
    FontSize="24"
    Color="#333333"
    FontWeight="Bold"
    TextAlignment="Center" />
```

**属性说明：**

| 属性 | 类型 | 说明 |
|------|------|------|
| `Text` | string | 显示文本 |
| `FontSize` | double | 字体大小 |
| `Color` | string | 文本颜色 |
| `FontWeight` | string | 字体粗细（`Normal`, `Bold`） |
| `TextAlignment` | TextAlignment | 对齐方式（`Left`, `Center`, `Right`） |

### Button 按钮

```xml
<Button 
    Text="提交"
    BackgroundColor="#007AFF"
    TextColor="White"
    FontSize="16"
    CornerRadius="8"
    OnClick="@OnSubmit" />
```

**属性说明：**

| 属性 | 类型 | 说明 |
|------|------|------|
| `Text` | string | 按钮文本 |
| `BackgroundColor` | string | 背景颜色 |
| `TextColor` | string | 文本颜色 |
| `FontSize` | double | 字体大小 |
| `CornerRadius` | double | 圆角半径 |
| `IsEnabled` | bool | 是否启用 |
| `OnClick` | EventHandler | 点击事件 |

### TextInput 文本输入

```xml
<TextInput 
    Text="@_username"
    Placeholder="请输入用户名"
    FontSize="14"
    CornerRadius="4"
    Padding="8" />
```

**属性说明：**

| 属性 | 类型 | 说明 |
|------|------|------|
| `Text` | string | 输入文本 |
| `Placeholder` | string | 占位提示 |
| `IsPassword` | bool | 是否为密码输入 |
| `FontSize` | double | 字体大小 |
| `CornerRadius` | double | 圆角半径 |
| `Padding` | double | 内边距 |

### CheckBox 复选框

```xml
<CheckBox 
    IsChecked="@_isChecked"
    Label="同意条款"
    CheckedColor="#007AFF"
    Size="20" />
```

### Image 图片

```xml
<Image 
    Source="assets/logo.png"
    Width="100"
    Height="100"
    Stretch="Uniform" />
```

**Stretch 模式：**

| 模式 | 说明 |
|------|------|
| `None` | 原始大小 |
| `Fill` | 填充整个区域（可能变形） |
| `Uniform` | 保持比例填充 |
| `UniformToFill` | 保持比例填充，可能裁剪 |

---

## 状态更新

调用 `StateHasChanged()` 触发重新渲染：

```csharp
@code {
    private int _count = 0;
    
    private void Increment()
    {
        _count++;
        StateHasChanged(); // 触发 UI 更新
    }
}
```

---

## 输入事件系统

### 路由策略

| 策略 | 说明 |
|------|------|
| `Direct` | 直接事件，仅在源元素触发 |
| `Bubble` | 冒泡事件，从源元素向上传播到根 |
| `Tunnel` | 隧道事件，从根向下传播到源元素 |

### 指针事件

```csharp
// 可订阅的指针事件
PointerPressed      // 指针按下（Bubble）
PreviewPointerPressed // 指针按下（Tunnel）
PointerMoved        // 指针移动
PointerReleased     // 指针释放
PointerEntered      // 指针进入（Direct）
PointerExited       // 指针离开（Direct）
PointerWheelChanged // 滚轮滚动
Tapped              // 点击
```

### 键盘事件

```csharp
KeyDown             // 按键按下（Bubble）
PreviewKeyDown      // 按键按下（Tunnel）
KeyUp               // 按键释放
TextInput           // 文本输入
```

### Key 枚举

常用按键：

```csharp
Key.Enter, Key.Escape, Key.Tab, Key.Space
Key.Back, Key.Delete
Key.Left, Key.Right, Key.Up, Key.Down
Key.A, Key.B, ... Key.Z
Key.F1, Key.F2, ... Key.F12
```

---

## 最佳实践

### 1. 组件化

将重复使用的 UI 封装为独立组件：

```xml
<!-- Card.eui -->
@using Eclipse.Controls

<Container BackgroundColor="#FFFFFF" Padding="16" CornerRadius="8">
    <StackLayout Spacing="8">
        <Label Text="@Title" FontSize="18" FontWeight="Bold" />
        <Label Text="@Description" FontSize="14" Color="#666" />
    </StackLayout>
</Container>

@code {
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
}
```

### 2. 合理使用布局

- 简单线性布局使用 `StackLayout` 或 `HStack`
- 需要精确定位使用 `Grid`
- 内容超出屏幕使用 `ScrollView`

### 3. 事件处理

- Preview (Tunnel) 事件用于预处理或拦截
- 使用 `e.Handled = true` 停止事件传播

```csharp
private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
{
    if (e.Key == Key.Enter)
    {
        e.Handled = true; // 阻止后续处理
        SubmitForm();
    }
}
```

---

## 附录：颜色格式

支持多种颜色格式：

```xml
<!-- 十六进制 -->
<Label Color="#333333" />
<Label Color="#FF333333" /> <!-- 包含 Alpha -->

<!-- 颜色名称 -->
<Label Color="Red" />
<Label Color="White" />
<Label Color="Transparent" />

<!-- RGB/RGBA（部分支持） -->
<Label Color="rgb(255, 0, 0)" />
```

---

## 相关文档

- [架构设计](architecture.md)
- [快速开始](getting-started.md)
- [文本渲染系统](text-rendering.md)