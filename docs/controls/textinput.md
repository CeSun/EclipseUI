# TextInput 文本输入框

[[返回目录](../README.md)]

## 概述

功能完整的文本输入控件，支持光标、文本选择、键盘输入、IME 输入法（中文/日文等）、剪贴板操作和鼠标选择。

## 命名空间

```csharp
using Eclipse.Controls;
```

## 继承关系

```
ComponentBase
    └── InteractiveControl
            └── TextInput
```

## 属性

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Text` | `string?` | `null` | 输入的文本内容 |
| `Placeholder` | `string?` | `null` | 占位符文本（无内容时显示） |
| `FontSize` | `double` | `14` | 字体大小 |
| `BackgroundColor` | `Color` | `Color.White` | 背景颜色 |
| `FocusBorderColor` | `Color` | `Color.FromArgb(0, 122, 255)` | 聚焦时边框颜色 |
| `CornerRadius` | `double` | `4` | 圆角半径 |
| `Padding` | `double` | `8` | 内边距 |
| `IsPassword` | `bool` | `false` | 是否为密码输入模式 |
| `SelectionBackgroundColor` | `Color` | `Color.FromArgb(128, 0, 122, 255)` | 选择背景色 |
| `SelectionTextColor` | `Color` | `Color.Black` | 选择文本色 |

## 只读属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `CursorPosition` | `int` | 光标位置（字符索引） |
| `SelectionStart` | `int` | 选择起始位置 |
| `SelectionEnd` | `int` | 选择结束位置 |
| `SelectionLength` | `int` | 选择长度 |
| `HasSelection` | `bool` | 是否有选中文本 |
| `SelectedText` | `string?` | 选中的文本内容 |
| `CompositionText` | `string` | IME 组合文本 |
| `IsComposing` | `bool` | 是否正在组合输入 |

## 事件

| 事件 | 类型 | 说明 |
|------|------|------|
| `TextChanged` | `EventHandler<ValueChangedEventArgs<string?>>?` | 文本变化事件 |
| `CompositionChanged` | `EventHandler<CompositionEventArgs>?` | IME 组合文本变化事件 |
| `SelectionChanged` | `EventHandler<EventArgs>?` | 选择变化事件 |

## 使用示例

### 基本用法

```csharp
var input = new TextInput
{
    Placeholder = "Enter your name"
};

input.TextChanged += (s, e) =>
{
    Console.WriteLine($"Text changed: {e.NewValue}");
};
```

### 密码输入

```csharp
var password = new TextInput
{
    Placeholder = "Password",
    IsPassword = true
};
```

### 自定义样式

```csharp
var input = new TextInput
{
    BackgroundColor = Color.LightGray,
    FocusBorderColor = Color.Green,
    CornerRadius = 8,
    Padding = 12,
    FontSize = 16
};
```

### 文本选择操作

```csharp
var input = new TextInput { Text = "Hello World" };

// 全选
input.SelectAll();

// 选择前 5 个字符
input.Select(0, 5);

// 获取选中文本
var selected = input.SelectedText;

// 清除选择
input.ClearSelection();
```

### 剪贴板操作

```csharp
var input = new TextInput { Text = "Copy me" };

// Ctrl+A 全选后：
// Ctrl+C 复制选中文本
// Ctrl+X 剪切选中文本
// Ctrl+V 粘贴文本
```

## EUI 语法

```eui
@using Eclipse.Controls

<TextInput Placeholder="Enter text..." />

<TextInput Placeholder="Password" IsPassword="true" />

<TextInput Placeholder="Custom" FontSize="16" CornerRadius="8" />
```

## 键盘快捷键

| 快捷键 | 功能 |
|--------|------|
| `Ctrl+A` | 全选 |
| `Ctrl+C` | 复制 |
| `Ctrl+X` | 剪切 |
| `Ctrl+V` | 粘贴 |
| `←` / `→` | 移动光标 |
| `Shift+←` / `Shift+→` | 扩展选择 |
| `Home` / `End` | 光标移到行首/行尾 |
| `Shift+Home` / `Shift+End` | 选择到行首/行尾 |
| `Backspace` | 删除光标前字符 |
| `Delete` | 删除光标后字符 |

## 鼠标操作

| 操作 | 功能 |
|------|------|
| 单击 | 移动光标到点击位置 |
| 双击 | 选择点击位置所在的单词 |
| 三击 | 选择全部文本 |
| 拖动 | 选择文本 |

## IME 输入支持

支持中文、日文、韩文等输入法的组合输入：
- 输入时显示组合下划线
- 组合完成前文本不下发
- 支持组合过程中的光标移动

## CompositionEventArgs

```csharp
public class CompositionEventArgs : EventArgs
{
    public string CompositionText { get; }  // 组合文本
    public int CursorPosition { get; }      // 光标位置
}
```

## 相关控件

- [Label](label.md) - 文本标签
- [Button](button.md) - 按钮控件
