# ScrollView 滚动视图

[[返回目录](../README.md)]

## 概述

可滚动的容器控件，支持水平和垂直滚动、滚动条显示、鼠标滚轮操作和惯性滚动（可选）。

## 命名空间

```csharp
using Eclipse.Controls;
```

## 继承关系

```
ComponentBase
        └── ScrollView
```

## 属性

### 滚动控制

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `ScrollX` | `double` | `0` | 水平滚动偏移 |
| `ScrollY` | `double` | `0` | 垂直滚动偏移 |
| `MaxScrollX` | `double` | `只读` | 最大水平滚动偏移 |
| `MaxScrollY` | `double` | `只读` | 最大垂直滚动偏移 |
| `ScrollStep` | `double` | `50` | 鼠标滚轮滚动步长（像素） |
| `EnableInertia` | `bool` | `false` | 是否启用惯性滚动 |

### 尺寸

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Width` | `double` | `-1` | 固定宽度（-1 表示自动） |
| `Height` | `double` | `-1` | 固定高度（-1 表示自动） |
| `ViewportWidth` | `double` | `只读` | 视口宽度 |
| `ViewportHeight` | `double` | `只读` | 视口高度 |
| `ContentSize` | `Size` | `只读` | 内容尺寸 |

### 滚动条

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `HorizontalScrollBarVisible` | `bool` | `false` | 是否显示水平滚动条 |
| `VerticalScrollBarVisible` | `bool` | `true` | 是否显示垂直滚动条 |
| `ScrollBarWidth` | `double` | `10` | 滚动条宽度 |
| `ScrollBarColor` | `Color` | `Color.Gray` | 滚动条颜色 |
| `ScrollBarHoverColor` | `Color` | `Color.DarkGray` | 滚动条悬停颜色 |
| `ScrollBarTrackColor` | `Color` | `Color.LightGray` | 滚动条轨道颜色 |
| `ScrollBarCornerRadius` | `double` | `5` | 滚动条圆角 |
| `AutoHideScrollBar` | `bool` | `true` | 是否自动隐藏滚动条 |

### 样式

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `BackgroundColor` | `Color` | `Color.Transparent` | 背景颜色 |
| `Padding` | `double` | `0` | 内容内边距 |

## 事件

| 事件 | 类型 | 说明 |
|------|------|------|
| `ScrollChanged` | `EventHandler<ScrollChangedEventArgs>?` | 滚动位置改变事件 |

## 方法

### 滚动控制

```csharp
void ScrollTo(double x, double y)    // 滚动到指定位置
void ScrollBy(double dx, double dy)  // 滚动增量
void ScrollToTop()                   // 滚动到顶部
void ScrollToBottom()                // 滚动到底部
void ScrollToLeft()                  // 滚动到左侧
void ScrollToRight()                 // 滚动到右侧
void ScrollIntoView(Rect rect)       // 滚动使元素可见
```

## 使用示例

### 基本用法

```csharp
var scrollView = new ScrollView
{
    Width = 300,
    Height = 400,
    VerticalScrollBarVisible = true
};

// 添加内容
scrollView.Children.Add(new Label { Text = "Item 1" });
scrollView.Children.Add(new Label { Text = "Item 2" });
// ...
```

### 内容溢出时自动滚动

```csharp
var scrollView = new ScrollView
{
    VerticalScrollBarVisible = true,
    AutoHideScrollBar = true,
    ScrollBarWidth = 8
};

// 订阅滚动事件
scrollView.ScrollChanged += (s, e) =>
{
    Console.WriteLine($"Scrolled to: ({e.ScrollX}, {e.ScrollY})");
};
```

### 滚动到底部

```csharp
var listView = new ScrollView { Height = 300 };
// ... 添加很多内容

// 添加新内容后自动滚动到底部
listView.Children.Add(new Label { Text = "New Item" });
listView.ScrollToBottom();
```

### 显示水平滚动条

```csharp
var hScroll = new ScrollView
{
    HorizontalScrollBarVisible = true,
    Width = 400,
    Height = 200
};
```

### 自定义滚动条样式

```csharp
var scrollView = new ScrollView
{
    ScrollBarWidth = 12,
    ScrollBarColor = Color.FromArgb(100, 128, 128, 128),
    ScrollBarHoverColor = Color.FromArgb(150, 128, 128, 128),
    ScrollBarTrackColor = Color.LightGray,
    ScrollBarCornerRadius = 6
};
```

## EUI 语法

```eui
@using Eclipse.Controls

<ScrollView Width="300" Height="400">
    <!-- 内容 -->
</ScrollView>

<ScrollView VerticalScrollBarVisible="true" AutoHideScrollBar="true">
    <!-- 长内容 -->
</ScrollView>
```

## 交互行为

| 操作 | 效果 |
|------|------|
| 鼠标滚轮 | 垂直滚动 |
| Shift + 滚轮 | 水平滚动 |
| 点击滚动条轨道 | 跳转到对应位置 |
| 拖动滚动条滑块 | 平滑滚动 |
| 悬停滚动条 | 保持显示 |

## ScrollChangedEventArgs

```csharp
public class ScrollChangedEventArgs : EventArgs
{
    public double ScrollX { get; }      // 当前水平位置
    public double ScrollY { get; }     // 当前垂直位置
    public double MaxScrollX { get; }  // 最大水平位置
    public double MaxScrollY { get; }    // 最大垂直位置
    public bool IsAtTop { get; }        // 是否在顶部
    public bool IsAtBottom { get; }    // 是否在底部
    public bool IsAtLeft { get; }       // 是否在左侧
    public bool IsAtRight { get; }      // 是否在右侧
}
```

## 尺寸计算

- **视口大小**：由父布局决定，或使用固定的 `Width`/`Height`
- **内容大小**：由子元素最大尺寸决定
- **滚动范围**：内容大小 - 视口大小（最小为 0）

## 注意事项

1. 子元素使用完整内容区域排列，不受视口限制
2. 超出视口的内容会被裁剪，不会触发布局重算
3. 滚动条透明度会在滚动后自动淡出

## 相关控件

- [Label](label.md) - 文本标签
- [Image](image.md) - 图片控件
