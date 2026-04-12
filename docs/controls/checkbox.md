# CheckBox 复选框控件

[[返回目录](../README.md)]

## 概述

复选框控件，用于二元选择。支持选中/未选中状态切换，带有文本标签。

## 命名空间

```csharp
using Eclipse.Controls;
```

## 继承关系

```
ComponentBase
    └── InteractiveControl
            └── CheckBox
```

## 属性

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `IsChecked` | `bool` | `false` | 是否选中 |
| `Label` | `string?` | `null` | 复选框旁边的文本标签 |
| `CheckedColor` | `Color` | `Color.SystemBlue` | 选中状态的颜色 |
| `Size` | `double` | `20` | 复选框方块大小 |

## 继承属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `IsEnabled` | `bool` | 是否启用（继承自 `ComponentBase`） |
| `IsFocused` | `bool` | 是否获得焦点（继承自 `ComponentBase`） |
| `IsFocusable` | `bool` | 是否可获得焦点（默认 true） |

## 事件

| 事件 | 类型 | 说明 |
|------|------|------|
| `CheckedChanged` | `EventHandler<ValueChangedEventArgs<bool>>?` | 选中状态改变事件 |

## 使用示例

### 基本用法

```csharp
var checkbox = new CheckBox
{
    Label = "I agree to the terms"
};

checkbox.CheckedChanged += (s, e) =>
{
    Console.WriteLine($"Checked: {e.NewValue}");
};
```

### 代码控制选中状态

```csharp
var checkbox = new CheckBox { Label = "Option 1" };

// 设置选中
checkbox.IsChecked = true;

// 获取状态
if (checkbox.IsChecked)
{
    Console.WriteLine("Option is selected");
}
```

### 自定义样式

```csharp
var checkbox = new CheckBox
{
    Label = "Remember me",
    CheckedColor = Color.Green,
    Size = 24
};
```

### 无标签复选框

```csharp
var checkbox = new CheckBox { Size = 30 };
```

## EUI 语法

```eui
@using Eclipse.Controls

<CheckBox Label="I agree" />

<CheckBox Label="Remember me" IsChecked="true" />

<CheckBox Label="Green option" CheckedColor="Green" />
```

## 交互行为

1. **点击**：点击复选框切换选中状态
2. **事件触发**：状态改变时触发 `CheckedChanged` 事件
3. **视觉反馈**：
   - 未选中：浅灰色方块
   - 选中：使用 `CheckedColor` 的方块
4. **禁用**：不响应点击，颜色变灰

## 尺寸计算

- **无标签**：宽度 = 高度 = `Size`
- **有标签**：宽度 = `Size` + 8 + 文字宽度，高度 = `Size`

## ValueChangedEventArgs<bool>

```csharp
public class ValueChangedEventArgs<T>
{
    public T OldValue { get; }  // 旧值
    public T NewValue { get; }  // 新值
}
```

## 相关控件

- [Button](button.md) - 按钮控件
- [TextInput](textinput.md) - 文本输入框
