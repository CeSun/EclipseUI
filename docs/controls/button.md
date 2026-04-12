# Button 按钮控件

[[返回目录](../README.md)]

## 概述

按钮控件，提供可点击的按钮交互体验。支持悬停、按下、禁用等状态，自动计算尺寸或使用固定尺寸。

## 命名空间

```csharp
using Eclipse.Controls;
```

## 继承关系

```
ComponentBase
    └── InteractiveControl
            └── Button
```

## 属性

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Text` | `string?` | `null` | 按钮显示文本 |
| `FontSize` | `double` | `14` | 字体大小 |
| `FontFamily` | `string?` | `null` | 字体名称 |
| `TextColor` | `Color` | `Color.White` | 文本颜色 |
| `BackgroundColor` | `Color` | `Color.SystemBlue` | 背景颜色 |
| `HoverBackgroundColor` | `Color?` | `null` | 悬停背景颜色（为 null 时自动暗化 10%） |
| `PressedBackgroundColor` | `Color?` | `null` | 按下背景颜色（为 null 时自动暗化 20%） |
| `DisabledBackgroundColor` | `Color` | `Color.LightGray` | 禁用状态背景颜色 |
| `DisabledTextColor` | `Color` | `Color.Gray` | 禁用状态文本颜色 |
| `BorderColor` | `Color?` | `null` | 边框颜色 |
| `BorderWidth` | `double` | `0` | 边框宽度 |
| `CornerRadius` | `double` | `4` | 圆角半径 |
| `Width` | `double?` | `null` | 固定宽度（为 null 时自动计算） |
| `Height` | `double?` | `null` | 固定高度（为 null 时自动计算） |

## 继承属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `IsEnabled` | `bool` | 是否启用（继承自 `ComponentBase`） |
| `IsFocused` | `bool` | 是否获得焦点（继承自 `ComponentBase`） |
| `IsFocusable` | `bool` | 是否可获得焦点（默认 true） |

## 事件

| 事件 | 类型 | 说明 |
|------|------|------|
| `Click` | `EventHandler?` | 点击事件 |
| `OnClick` | `EventHandler?` | 点击事件（Click 的别名） |

## 使用示例

### 基本用法

```csharp
var button = new Button
{
    Text = "Click Me",
    BackgroundColor = Color.SystemBlue,
    TextColor = Color.White
};

button.OnClick += (s, e) => 
{
    Console.WriteLine("Button clicked!");
};
```

### 固定尺寸

```csharp
var button = new Button
{
    Text = "Submit",
    Width = 120,
    Height = 40
};
```

### 自定义样式

```csharp
var button = new Button
{
    Text = "Delete",
    BackgroundColor = Color.Red,
    HoverBackgroundColor = Color.DarkRed,
    PressedBackgroundColor = Color.Maroon,
    CornerRadius = 8,
    BorderColor = Color.Black,
    BorderWidth = 1
};
```

### 禁用状态

```csharp
var button = new Button
{
    Text = "Disabled Button",
    IsEnabled = false
};
```

## EUI 语法

```eui
@using Eclipse.Controls

<Button Text="Click Me" />

<Button Text="Submit" Width="120" Height="40" BackgroundColor="Red" />

<Button Text="Disabled" IsEnabled="false" />
```

## 交互行为

1. **悬停**：鼠标进入时背景变暗 10%（除非设置了 `HoverBackgroundColor`）
2. **按下**：鼠标按下时背景变暗 20%（除非设置了 `PressedBackgroundColor`）
3. **点击**：释放鼠标时触发 `Click` 事件
4. **焦点**：支持 Tab 键导航，获得焦点时显示蓝色边框
5. **禁用**：不响应任何交互，颜色变灰

## 尺寸计算

- **固定尺寸**：如果设置了 `Width` 或 `Height`，使用指定值
- **自动尺寸**：
  - 宽度 = 文字宽度 + 40（左右各 20 内边距）
  - 高度 = 44
- **无文字**：宽度 = 80，高度 = 44

## 相关控件

- [Label](label.md) - 文本标签
- [CheckBox](checkbox.md) - 复选框
