# Eclipse.Controls

EclipseUI 内置控件库。

## 控件列表

### 布局控件

| 控件 | 说明 |
|------|------|
| `StackLayout` | 垂直/水平堆叠布局 |
| `HStack` | 水平堆叠布局 (继承自 StackLayout) |

### 基础控件

| 控件 | 说明 |
|------|------|
| `Label` | 文本标签 |
| `Button` | 按钮 |
| `TextInput` | 文本输入框 |
| `CheckBox` | 复选框 |
| `Image` | 图片 |
| `Container` | 容器 |

## 用法

```csharp
// StackLayout
var stack = new StackLayout
{
    Orientation = Orientation.Vertical,
    Spacing = "16",
    Padding = "20"
};

// Label
var label = new Label
{
    Text = "Hello World",
    FontSize = "24",
    FontWeight = "Bold",
    Color = "#333333"
};

// Button
var button = new Button
{
    Text = "Click Me",
    BackgroundColor = "#007AFF",
    TextColor = "White",
    CornerRadius = "8"
};
button.OnClick += (s, e) => Console.WriteLine("Clicked!");
```