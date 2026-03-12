# EclipseUI 功能文档

EclipseUI 是一个基于 SkiaSharp + Blazor 的跨平台 UI 框架，使用 Silk.NET 作为窗口系统。

---

## 📦 核心架构

### 渲染引擎
- **SkiaSharp** - 2D 图形渲染
- **Blazor** - 组件模型和渲染树管理
- **Silk.NET** - 跨平台窗口和输入处理

### 组件层次
```
EclipseRenderer (Blazor Renderer 继承)
    └── EclipseComponentAdapter (组件适配器)
        └── EclipseElement (UI 元素基类)
            └── 各种 UI 控件 (TextBlock, Button, StackPanel 等)
```

---

## ✅ 已实现功能

### 1. 核心系统

| 功能 | 文件 | 状态 |
|------|------|------|
| 渲染器引擎 | `EclipseRenderer.cs` | ✅ |
| 组件基类 | `EclipseComponentBase.cs` | ✅ |
| 元素基类 | `EclipseElement.cs` | ✅ |
| 应用上下文 | `EclipseApplicationContext.cs` | ✅ |
| 窗口宿主 | `EclipseWindow.cs` | ✅ |

### 2. 布局控件

| 控件 | 文件 | 功能 |
|------|------|------|
| **StackPanel** | `StackPanel.cs` | 垂直/水平堆叠布局，支持 Spacing、Padding、Margin |

#### StackPanel 功能详情
- `Orientation` - 布局方向（Vertical/Horizontal）
- `Spacing` - 子元素间距
- `PaddingLeft/Right/Top/Bottom` - 内边距
- `MarginLeft/Right/Top/Bottom` - 外边距
- `OnClick` - 点击事件

### 3. 基础控件

| 控件 | 文件 | 功能 |
|------|------|------|
| **TextBlock** | `TextBlock.cs` | 文本显示，支持字体大小、颜色、粗体、中文字体 |
| **Button** | `Button.cs` | 按钮，支持文本、背景色、前景色、圆角、点击事件 |

#### TextBlock 功能详情
- `Text` - 显示文本
- `FontSize` - 字体大小
- `Foreground` - 文字颜色（16 进制）
- `FontWeight` - 是否粗体
- `OnClick` - 点击事件
- 自动使用中文字体（Microsoft YaHei/SimSun/SimHei/KaiTi）

#### Button 功能详情
- `Text` - 按钮文本
- `FontSize` - 字体大小
- `Background` - 背景颜色
- `Foreground` - 文字颜色
- `CornerRadius` - 圆角半径
- `OnClick` - 点击事件
- 支持 Hover 和 Pressed 状态（视觉反馈）

### 4. 事件系统

| 功能 | 描述 |
|------|------|
| **点击事件** | 所有元素支持 `OnClick` 事件，通过 `HandleClick` 方法处理 |
| **事件冒泡** | 点击事件从子元素向父元素冒泡传递 |
| **Dispatcher 线程安全** | 通过 `Renderer.Dispatcher.InvokeAsync` 确保在正确的线程上执行 |

### 5. 布局系统

| 功能 | 描述 |
|------|------|
| **Measure** | 测量元素所需尺寸，考虑 Padding |
| **Arrange** | 排列元素及其子元素的位置 |
| **Render** | 绘制元素内容和子元素 |
| **CascadingValue** | 通过 Blazor CascadingValue 传递父元素引用 |

---

## 🏗️ 使用示例

### 基本用法

```razor
@using EclipseUI.Controls
@using EclipseUI.Layout
@using EclipseUI.Core
@inherits EclipseComponentBase

<CascadingValue Value="Element" IsFixed="true">
    <StackPanel Orientation="StackOrientation.Vertical" Spacing="20" 
                PaddingLeft="40" PaddingTop="40" PaddingRight="40" PaddingBottom="40">
        <TextBlock Text="欢迎使用 EclipseUI!" FontSize="32" />
        <Button Text="点我" Background="#4CAF50" FontSize="20" OnClick="HandleClick" />
        <TextBlock Text="@GetCountText()" FontSize="24" FontWeight="true" Foreground="#4CAF50" />
    </StackPanel>
</CascadingValue>

@code {
    private int _count = 0;

    private async Task HandleClick()
    {
        _count++;
        await InvokeAsync(StateHasChanged);
    }

    private string GetCountText() => $"点击了{_count}次";
}
```

### 宿主窗口

```csharp
using EclipseUI.Host;

var window = new EclipseWindow
{
    Title = "EclipseUI Demo",
    Width = 800,
    Height = 600
};

window.Show<MainPage>();
```

---

## 📋 开发待办

### 布局控件
- [ ] Grid - 网格布局
- [ ] DockPanel - 停靠布局
- [ ] WrapPanel - 自动换行布局
- [ ] Canvas - 绝对定位布局

### 基础控件
- [ ] Image - 图片显示
- [ ] Border - 边框容器
- [ ] Input/TextBox - 文本输入
- [ ] CheckBox - 复选框
- [ ] RadioButton - 单选框
- [ ] Slider - 滑块
- [ ] ProgressBar - 进度条
- [ ] ListBox - 列表框

### 高级功能
- [ ] 样式系统 (Styles)
- [ ] 资源字典 (ResourceDictionary)
- [ ] 数据绑定 (DataBinding)
- [ ] 动画系统 (Animations)
- [ ] 滚动容器 (ScrollViewer)
- [ ] 菜单系统 (Menu/ContextMenu)
- [ ] 对话框 (Dialog/MessageBox)

---

## 🔧 技术细节

### 线程模型
- UI 渲染在 Silk.NET 的渲染线程执行
- 组件状态变更通过 `Renderer.Dispatcher.InvokeAsync` 切换到 Blazor Dispatcher 线程
- `EclipseComponentBase.CurrentRenderer` 提供静态 Renderer 引用

### 渲染流程
1. **Measure** - 从根元素开始递归测量所有子元素
2. **Arrange** - 根据测量结果排列所有元素位置
3. **Render** - 使用 SkiaSharp 绘制所有元素

### 事件处理
1. 鼠标点击触发 `EclipseWindow.OnLoad` 中的事件处理
2. 调用 `Renderer.HandleClick` 开始事件冒泡
3. 从根元素递归检查点击命中
4. 触发对应元素的 `OnClick` 回调

---

## 📁 项目结构

```
EclipseUI/
├── src/
│   ├── EclipseUI/                    # 核心库
│   │   ├── Core/
│   │   │   ├── EclipseRenderer.cs
│   │   │   ├── EclipseComponentBase.cs
│   │   │   ├── EclipseElement.cs
│   │   │   ├── EclipseComponentAdapter.cs
│   │   │   └── EclipseApplicationContext.cs
│   │   ├── Layout/
│   │   │   └── StackPanel.cs
│   │   └── Controls/
│   │       ├── TextBlock.cs
│   │       └── Button.cs
│   ├── EclipseUI.Host/               # 窗口宿主
│   │   └── EclipseWindow.cs
│   └── EclipseUI.Demo/               # 示例应用
│       ├── Program.cs
│       └── MainPage.razor
├── docs/                             # 文档
│   └── FEATURES.md
└── README.md
```

---

_最后更新：2026-03-12_
