# EclipseUI 控件文档

## 目录

- [Button 按钮控件](controls/button.md)
- [Label 文本标签](controls/label.md)
- [CheckBox 复选框](controls/checkbox.md)
- [Image 图片控件](controls/image.md)
- [TextInput 文本输入框](controls/textinput.md)
- [ScrollView 滚动视图](controls/scrollview.md)

---

## 快速导航

### 基础控件

| 控件 | 说明 |
|------|------|
| [Button](controls/button.md) | 可交互按钮 |
| [Label](controls/label.md) | 静态文本显示 |

### 输入控件

| 控件 | 说明 |
|------|------|
| [CheckBox](controls/checkbox.md) | 复选框 |
| [TextInput](controls/textinput.md) | 文本输入框 |

### 容器控件

| 控件 | 说明 |
|------|------|
| [ScrollView](controls/scrollview.md) | 滚动视图容器 |
| [Image](controls/image.md) | 图片显示 |

---

## 命名空间

所有控件都在 `Eclipse.Controls` 命名空间下：

```csharp
using Eclipse.Controls;
```

## EUI 语法

在 EUI 文件中使用控件：

```eui
@using Eclipse.Controls

<Button Text="Click Me" />

<Label Text="Hello" FontSize="16" />

<CheckBox Label="Option 1" />
```
