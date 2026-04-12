# Label 文本标签控件

[[返回目录](../README.md)]

## 概述

简单的文本标签控件，用于显示静态文本内容。支持字体样式、对齐方式和背景色。

## 命名空间

```csharp
using Eclipse.Controls;
```

## 继承关系

```
ComponentBase
        └── Label
```

## 属性

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Text` | `string?` | `null` | 显示的文本内容 |
| `FontSize` | `double` | `14` | 字体大小 |
| `FontWeight` | `string?` | `null` | 字体粗细（如 "Bold", "SemiBold"） |
| `FontFamily` | `string?` | `null` | 字体名称 |
| `Color` | `Color` | `Color.Black` | 文本颜色 |
| `BackgroundColor` | `Color` | `Color.Transparent` | 背景颜色 |
| `TextAlignment` | `TextAlignment` | `TextAlignment.Left` | 文本对齐方式 |
| `Padding` | `double` | `0` | 内边距 |

## TextAlignment 枚举

```csharp
public enum TextAlignment
{
    Left,    // 左对齐
    Center,  // 居中
    Right    // 右对齐
}
```

## 使用示例

### 基本用法

```csharp
var label = new Label
{
    Text = "Hello, World!"
};
```

### 居中对齐

```csharp
var title = new Label
{
    Text = "Welcome",
    FontSize = 24,
    FontWeight = "Bold",
    TextAlignment = TextAlignment.Center
};
```

### 带背景

```csharp
var badge = new Label
{
    Text = "New",
    BackgroundColor = Color.Red,
    Color = Color.White,
    Padding = 4
};
```

### 右对齐数字

```csharp
var price = new Label
{
    Text = "¥99.00",
    TextAlignment = TextAlignment.Right
};
```

## EUI 语法

```eui
@using Eclipse.Controls

<Label Text="Simple Label" />

<Label Text="Title" FontSize="24" FontWeight="Bold" TextAlignment="Center" />

<Label Text="Badge" BackgroundColor="Red" Color="White" Padding="4" />
```

## 尺寸计算

- **宽度** = 文字宽度 + 内边距 × 2
- **高度** = 字体大小 × 1.3 + 内边距 × 2
- **空文本** = `Size.Zero`

## 相关控件

- [Button](button.md) - 按钮控件
- [TextInput](textinput.md) - 文本输入框
