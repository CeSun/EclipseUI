# Image 图片控件

[[返回目录](../README.md)]

## 概述

图片控件，用于显示图像资源。支持固定尺寸和拉伸模式。

## 命名空间

```csharp
using Eclipse.Controls;
```

## 继承关系

```
ComponentBase
        └── Image
```

## 属性

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Source` | `string?` | `null` | 图片资源路径或 URL |
| `Width` | `double` | `-1` | 固定宽度（-1 表示自动） |
| `Height` | `double` | `-1` | 固定高度（-1 表示自动） |
| `Stretch` | `Stretch` | `Stretch.Uniform` | 拉伸模式 |

## Stretch 枚举

```csharp
public enum Stretch
{
    None,      // 不拉伸，保持原始尺寸
    Fill,      // 填充整个区域，可能变形
    Uniform,   // 等比缩放，适应区域（默认）
    UniformToFill // 等比缩放，填充区域，可能裁剪
}
```

## 使用示例

### 基本用法

```csharp
var image = new Image
{
    Source = "pack://application:,,,/Assets/logo.png"
};
```

### 固定尺寸

```csharp
var thumbnail = new Image
{
    Source = "https://example.com/photo.jpg",
    Width = 200,
    Height = 150
};
```

### 填充模式

```csharp
var background = new Image
{
    Source = "background.png",
    Width = 800,
    Height = 600,
    Stretch = Stretch.Fill
};
```

### 自动尺寸

```csharp
var avatar = new Image
{
    Source = "avatar.png"
    // Width 和 Height 都是 -1，使用可用空间或默认 100x100
};
```

## EUI 语法

```eui
@using Eclipse.Controls

<Image Source="logo.png" />

<Image Source="photo.jpg" Width="200" Height="150" />

<Image Source="background.png" Width="800" Height="600" Stretch="Fill" />
```

## 尺寸计算

| Width | Height | 行为 |
|-------|--------|------|
| `-1` | `-1` | 使用可用空间或默认 100×100 |
| `> 0` | `-1` | 宽度固定，高度自适应 |
| `-1` | `> 0` | 高度固定，宽度自适应 |
| `> 0` | `> 0` | 使用固定尺寸 |

## 渲染行为

- **Source 为空**：绘制浅灰色矩形
- **图片加载失败**：绘制浅灰色矩形
- **图片加载成功**：根据 `Stretch` 模式绘制图片

## 相关控件

- [Label](label.md) - 文本标签
- [ScrollView](scrollview.md) - 滚动视图
