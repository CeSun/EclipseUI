# EUI 语法参考

> EclipseUI 的类 Razor 声明式 UI 语法

## 概述

EUI (Eclipse UI) 是一种类似 Razor 的声明式 UI 语法，用于构建 EclipseUI 应用界面。它结合了 XML 标记和 C# 代码，通过 Source Generator 实现零反射、强类型的编译时代码生成。

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

引入命名空间，与 C# 的 `using` 语句相同。Source Generator 会根据 `@using` 指令解析控件类型。

```xml
@using Eclipse.Controls
@using Eclipse.Input
@using MyApp.CustomControls
```

**重要**：所有使用的控件类型都必须通过 `@using` 引入其命名空间，否则会生成警告（ECGEN003）。

### @namespace

指定生成的组件命名空间。

```xml
@namespace MyApp.Pages

<StackLayout>
    <!-- ... -->
</StackLayout>
```

**命名空间推断规则**（按优先级）：

1. `@namespace` 指令指定的值
2. MSBuild `RootNamespace` + 相对于项目目录的路径
3. 默认值 `Eclipse.Generated`

**示例**：
```
项目: MyApp.csproj (<RootNamespace>MyApp</RootNamespace>)
文件: Pages/Admin/Users.eui

结果命名空间: MyApp.Pages.Admin
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

### @inherits

指定组件的基类。

```xml
@inherits LayoutComponentBase

<StackLayout>
    @ChildContent
</StackLayout>
```

默认基类为 `ComponentBase`。

### @inject

注入依赖服务。

```xml
@inject IUserService UserService
@inject ILoggingService Logger

<Label Text="@UserService.CurrentUser.Name" />
```

生成的代码：
```csharp
[Inject]
public IUserService UserService { get; set; } = null!;

[Inject]
public ILoggingService Logger { get; set; } = null!;
```

### @attribute

为生成的类添加特性。

```xml
@attribute [Obsolete("使用 NewPage 替代")]
```

---

## 控件列表

### 布局控件

| 控件 | 说明 | 常用属性 |
|------|------|----------|
| `StackLayout` | 堆叠布局（垂直/水平） | `Orientation`, `Spacing`, `Padding`, `BackgroundColor` |
| `HStack` | 水平堆叠布局（StackLayout 的简化版） | `Spacing`, `Padding`, `BackgroundColor` |
| `Grid` | 网格布局 | `RowCount`, `ColumnCount`, `RowSpacing`, `ColumnSpacing` |
| `ScrollView` | 滚动视图 | `ScrollX`, `ScrollY`, `VerticalScrollBarVisible`, `HorizontalScrollBarVisible` |
| `Container` | 容器控件 | `BackgroundColor`, `Padding`, `CornerRadius` |

### 基础控件

| 控件 | 说明 | 常用属性 |
|------|------|----------|
| `Label` | 文本显示 | `Text`, `FontSize`, `Color`, `FontWeight`, `TextAlignment` |
| `Button` | 按钮 | `Text`, `BackgroundColor`, `TextColor`, `CornerRadius`, `OnClick` |
| `TextInput` | 文本输入 | `Text`, `Placeholder`, `IsPassword`, `FontSize`, `Padding` |
| `CheckBox` | 复选框 | `IsChecked`, `Label`, `CheckedColor`, `Size` |
| `Image` | 图片显示 | `Source`, `Width`, `Height`, `Stretch` |

---

## 属性系统

### 强类型属性

所有控件属性都是强类型的，Source Generator 会根据属性类型自动转换字面量值。

### 数值类型

数值属性使用双引号包裹，Generator 自动转换为正确类型：

```xml
<Label FontSize="24" />
<StackLayout Spacing="16" Padding="8" />
<Button CornerRadius="8" />
<TextInput Padding="8" FontSize="14" />
```

**支持的数值类型**：`int`, `double`, `float`, `long`, `byte`, etc.

### 布尔类型

```xml
<CheckBox IsChecked="true" />
<TextInput IsPassword="false" />
<Button IsEnabled="true" />
<ScrollView VerticalScrollBarVisible="true" />
```

### 枚举类型

枚举值直接写名称，Generator 自动添加类型前缀：

```xml
<StackLayout Orientation="Vertical" />
<HStack Orientation="Horizontal" />
<Label TextAlignment="Center" />
<Image Stretch="Uniform" />
```

生成的代码：
```csharp
stackLayout.Orientation = Orientation.Vertical;
label.TextAlignment = TextAlignment.Center;
image.Stretch = Stretch.Uniform;
```

### 复杂类型转换

#### Color 颜色

支持多种颜色格式：

```xml
<!-- 十六进制 -->
<Label Color="#FF0000" />
<Label Color="#80FF0000" />  <!-- 带 Alpha -->

<!-- 颜色名称 -->
<Label Color="Red" />
<Label Color="Blue" />

<!-- RGB 格式 -->
<Label Color="rgb(255,0,0)" />
<Label Color="rgba(255,0,0,0.5)" />
```

生成的代码：
```csharp
label.Color = Color.FromHex("#FF0000");
label.Color = Colors.Red;
label.Color = Color.Parse("rgb(255,0,0)");
```

#### Thickness 边距

支持多种格式：

```xml
<!-- 统一边距 -->
<StackLayout Padding="16" />
<!-- 生成: new Thickness(16) -->

<!-- 水平、垂直 -->
<StackLayout Padding="16,8" />
<!-- 生成: new Thickness(16, 8) -->

<!-- 左、上、右、下 -->
<Button Margin="10,20,10,20" />
<!-- 生成: new Thickness(10, 20, 10, 20) -->
```

#### Point / Size / Vector

```xml
<Point Position="100,50" />     <!-- new Point(100, 50) -->
<Size Size="200,100" />         <!-- new Size(200, 100) -->
<Vector Offset="10,20" />       <!-- new Vector(10, 20) -->
```

#### Rect

```xml
<Rect Bounds="0,0,100,50" />
<!-- 生成: new Rect(0, 0, 100, 50) -->
```

#### 其他类型

```xml
<!-- TimeSpan -->
<Duration Value="00:01:30" />
<!-- 生成: TimeSpan.Parse("00:01:30") -->

<!-- DateTime -->
<Date Value="2024-01-15" />
<!-- 生成: DateTime.Parse("2024-01-15") -->

<!-- Guid -->
<Id Value="550e8400-e29b-41d4-a716-446655440000" />
<!-- 生成: Guid.Parse("...") -->

<!-- Uri -->
<Link Url="https://example.com" />
<!-- 生成: new Uri("https://example.com") -->
```

### 表达式绑定

使用 `@` 前缀绑定 C# 表达式或变量：

```xml
<!-- 变量绑定 -->
<Label Text="@_message" FontSize="@_fontSize" />

<!-- 表达式绑定 -->
<Label Text="@($"计数: {_count}")" />
<Button IsEnabled="@_count > 0" />

<!-- 属性绑定 -->
<Label Text="@CurrentUser.Name" />
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

### @foreach 循环渲染

```xml
<StackLayout Spacing="8">
    @foreach (var item in _items)
    {
        <Label Text="@item.Name" FontSize="14" />
    }
</StackLayout>
```

---

## 事件绑定

### 点击事件

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

### Lambda 表达式

```xml
<Button Text="增加" OnClick="@(s => _count++)" />
```

### 键盘事件

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

<!-- 水平堆叠 -->
<HStack Spacing="8">
    <Button Text="取消" BackgroundColor="#999" />
    <Button Text="确定" />
</HStack>
```

**属性说明：**

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Orientation` | Orientation | Vertical | 堆叠方向（`Vertical` / `Horizontal`） |
| `Spacing` | double | 0 | 子元素间距 |
| `Padding` | double | 0 | 内边距 |
| `BackgroundColor` | string | null | 背景颜色 |

### Grid 网格布局

```xml
<Grid RowCount="3" ColumnCount="2" RowSpacing="10" ColumnSpacing="10">
    <Label Grid.Row="0" Grid.Column="0" Text="标题" />
    <Label Grid.Row="0" Grid.Column="1" Text="副标题" />
    <Button Grid.Row="2" Grid.Column="0" Grid.ColumnSpan="2" Text="底部按钮" />
</Grid>
```

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

| 属性 | 类型 | 说明 |
|------|------|------|
| `Text` | string | 显示文本 |
| `FontSize` | double | 字体大小（默认 14） |
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

| 属性 | 类型 | 说明 |
|------|------|------|
| `Text` | string | 按钮文本 |
| `BackgroundColor` | string | 背景颜色（默认 `#007AFF`） |
| `TextColor` | string | 文本颜色（默认 `White`） |
| `FontSize` | double | 字体大小（默认 14） |
| `CornerRadius` | double | 圆角半径（默认 4） |
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

| 属性 | 类型 | 说明 |
|------|------|------|
| `Text` | string | 输入文本 |
| `Placeholder` | string | 占位提示 |
| `IsPassword` | bool | 是否为密码输入 |
| `FontSize` | double | 字体大小（默认 14） |
| `CornerRadius` | double | 圆角半径（默认 4） |
| `Padding` | double | 内边距（默认 8） |

**支持键盘操作**：
- `Back` / `Delete` - 删除字符
- `Left` / `Right` - 移动光标
- `Home` / `End` - 光标到开头/末尾

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
PointerPressed          // 指针按下（Bubble）
PreviewPointerPressed   // 指针按下（Tunnel）
PointerMoved            // 指针移动
PointerReleased         // 指针释放
PointerEntered          // 指针进入（Direct）
PointerExited           // 指针离开（Direct）
PointerWheelChanged     // 滚轮滚动
Tapped                  // 点击
```

### 键盘事件

```csharp
KeyDown             // 按键按下（Bubble）
PreviewKeyDown      // 按键按下（Tunnel）
KeyUp               // 按键释放
TextInput           // 文本输入
```

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

**注意**：组件使用脏标记机制，只有调用 `StateHasChanged()` 后才会重新构建。

---

## 诊断信息

Source Generator 会报告以下诊断信息：

| 诊断码 | 严重性 | 说明 |
|--------|--------|------|
| ECGEN001 | Error | 组件生成失败 |
| ECGEN002 | Error | EUI markup 解析错误 |
| ECGEN003 | Warning | 控件类型未找到 |

**示例**：

拼写错误的控件名：
```xml
<Lable Text="Hello" />  <!-- 正确应为 Label -->
```

警告信息：
```
warning ECGEN003: Control type 'Lable' not found in 'HomePage.eui'. 
Make sure the type exists and the namespace is imported via @using.
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

### 2. 使用正确的类型

属性是强类型的，使用字面量时 Generator 会自动转换：

```xml
<!-- ✅ 正确 - 字面量会自动转换 -->
<Label FontSize="24" />
<StackLayout Spacing="16" />

<!-- ✅ 正确 - 表达式绑定 -->
<Label FontSize="@fontSize" />

<!-- ❌ 错误 - 类型不匹配 -->
<Label FontSize="abc" />  <!-- 无法转换为 double -->
```

### 3. 事件处理

使用 `e.Handled = true` 停止事件传播：

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

## 相关文档

- [架构设计](architecture.md)
- [快速开始](getting-started.md)