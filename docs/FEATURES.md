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
| 弹出层服务 | `PopupService.cs` | ✅ |
| iOS 主题常量 | `iOSTheme.cs` | ✅ |

### 2. 布局控件

| 控件 | 文件 | 功能 |
|------|------|------|
| **StackPanel** | `StackPanel.cs` | 垂直/水平堆叠布局 |
| **Grid** | `Grid.cs` | 网格布局，支持 Auto/Star/Pixel |
| **DockPanel** | `DockPanel.cs` | 停靠布局（Top/Bottom/Left/Right/Fill） |
| **ScrollView** | `ScrollView.cs` | 滚动视图，支持垂直/水平滚动 |

#### StackPanel 功能
- `Orientation` - 布局方向（Vertical/Horizontal）
- `Spacing` - 子元素间距
- `PaddingLeft/Right/Top/Bottom` - 内边距

#### Grid 功能
- `RowDefinitions` - 行定义（如 "Auto, *, 100"）
- `ColumnDefinitions` - 列定义
- `Spacing` - 行列间距
- 支持 `GridItem` 子元素，可设置 `Row`、`Column`、`RowSpan`、`ColumnSpan`

#### DockPanel 功能
- `LastChildFill` - 最后一个子元素是否填充剩余空间
- 子元素通过 `Dock` 属性指定停靠位置

#### ScrollView 功能
- `Orientation` - 滚动方向（Vertical/Horizontal）
- 自动显示滚动条
- 支持鼠标滚轮和拖动滚动条

### 3. 基础控件

| 控件 | 文件 | 功能 |
|------|------|------|
| **TextBlock** | `TextBlock.cs` | 文本显示 |
| **TextBox** | `TextBox.cs` | 文本输入，支持双向绑定 |
| **Button** | `Button.cs` | 按钮，iOS 风格 |
| **CheckBox** | `CheckBox.cs` | 复选框，支持三态 |
| **RadioButton** | `RadioButton.cs` | 单选框，支持分组互斥 |
| **ToggleSwitch** | `ToggleSwitch.cs` | 开关控件，iOS 风格 |
| **Slider** | `Slider.cs` | 滑块控件 |
| **ComboBox** | `ComboBox.cs` | 下拉选择框 |

#### TextBlock 功能
- `Text` - 显示文本
- `FontSize` - 字体大小
- `Foreground` - 文字颜色
- `FontWeight` - 是否粗体

#### TextBox 功能
- `Text` / `@bind-Text` - 文本内容（支持双向绑定）
- `Placeholder` - 占位符文本
- `TextChanged` - 文本变化事件

#### Button 功能
- `Text` - 按钮文本
- `Background` - 背景颜色
- `Foreground` - 文字颜色
- `OnClick` - 点击事件
- iOS 风格：系统蓝、圆角、按下变暗

#### CheckBox 功能
- `IsChecked` / `@bind-IsChecked` - 选中状态（支持双向绑定）
- `Content` - 显示文本
- `IsThreeState` - 是否支持三态（选中/未选中/不确定）

#### RadioButton 功能
- `IsChecked` / `@bind-IsChecked` - 选中状态
- `Content` - 显示文本
- `GroupName` - 分组名称（同组互斥）

#### ToggleSwitch 功能
- `IsOn` / `@bind-IsOn` - 开关状态（支持双向绑定）
- `OnColor` - 开启时的颜色
- iOS 风格：51x31 尺寸、绿色开启、白色滑块

#### Slider 功能
- `Value` / `@bind-Value` - 当前值（支持双向绑定）
- `Minimum` / `Maximum` - 范围
- `ShowTicks` - 是否显示刻度
- `TickFrequency` - 刻度间隔
- `IsSnapToTick` - 是否吸附刻度

#### ComboBox 功能
- `ItemsSource` - 选项列表
- `SelectedItem` / `@bind-SelectedItem` - 选中项（支持双向绑定）
- `Placeholder` - 占位符文本
- 下拉菜单通过 PopupService 管理

### 4. 事件系统

| 功能 | 描述 |
|------|------|
| **点击事件** | 所有元素支持 `OnClick` 事件 |
| **鼠标滚轮** | ScrollView 支持滚轮滚动 |
| **拖动** | Slider、ScrollView 滚动条支持拖动 |
| **双向绑定** | 输入控件支持 `@bind-*` 语法 |

---

## 🏗️ 使用示例

### 基本布局

```razor
@using EclipseUI.Controls
@using EclipseUI.Layout

<Grid RowDefinitions="Auto, *" Spacing="10">
    <GridItem Row="0">
        <TextBlock Text="标题" FontSize="24" FontWeight="true" />
    </GridItem>
    <GridItem Row="1">
        <ScrollView PaddingLeft="20" PaddingRight="20">
            <StackPanel Spacing="15">
                <TextBox @bind-Text="_name" Placeholder="请输入姓名" />
                <CheckBox @bind-IsChecked="_agreed" Content="同意条款" />
                <Button Text="提交" OnClick="OnSubmit" />
            </StackPanel>
        </ScrollView>
    </GridItem>
</Grid>

@code {
    private string _name = "";
    private bool? _agreed = false;
    
    private void OnSubmit(MouseEventArgs e) { /* ... */ }
}
```

### 水平滚动

```razor
<ScrollView Orientation="ScrollOrientation.Horizontal" Height="60">
    <StackPanel Orientation="StackOrientation.Horizontal" Spacing="10">
        <Button Text="选项1" Width="100" />
        <Button Text="选项2" Width="100" />
        <Button Text="选项3" Width="100" />
        <!-- 更多按钮... -->
    </StackPanel>
</ScrollView>
```

### 表单控件

```razor
<StackPanel Spacing="20">
    <!-- 文本输入 -->
    <TextBox @bind-Text="_username" Placeholder="用户名" />
    
    <!-- 下拉选择 -->
    <ComboBox ItemsSource="@_options" @bind-SelectedItem="_selected" />
    
    <!-- 开关 -->
    <StackPanel Orientation="StackOrientation.Horizontal" Spacing="10">
        <ToggleSwitch @bind-IsOn="_enabled" />
        <TextBlock Text="启用通知" />
    </StackPanel>
    
    <!-- 滑块 -->
    <Slider @bind-Value="_volume" Minimum="0" Maximum="100" />
    <TextBlock Text="@($"音量: {(int)_volume}")" />
</StackPanel>

@code {
    private string _username = "";
    private List<string> _options = new() { "选项A", "选项B", "选项C" };
    private string? _selected;
    private bool _enabled = true;
    private double _volume = 50;
}
```

---

## 🎨 iOS 风格主题

所有控件默认使用 iOS 设计语言：

- **Button**: 系统蓝 (#007AFF)、圆角、按下变暗
- **ToggleSwitch**: 51x31 尺寸、绿色开启、白色滑块
- **CheckBox**: 圆形勾选框、蓝色选中
- **RadioButton**: 圆形、蓝色选中 + 白色内圆点
- **Slider**: 白色圆形滑块、蓝色填充轨道
- **TextBox**: 圆角、浅灰背景、聚焦蓝色边框
- **ComboBox**: 圆角、iOS 风格下拉

主题常量定义在 `iOSTheme.cs` 中。

---

## 📁 项目结构

```
EclipseUI/
├── src/
│   ├── EclipseUI/                    # 核心库
│   │   ├── Core/
│   │   │   ├── EclipseRenderer.cs
│   │   │   ├── EclipseElement.cs
│   │   │   ├── PopupService.cs
│   │   │   └── iOSTheme.cs
│   │   ├── Layout/
│   │   │   ├── StackPanel.cs
│   │   │   ├── Grid.cs
│   │   │   ├── DockPanel.cs
│   │   │   └── ScrollView.cs
│   │   └── Controls/
│   │       ├── TextBlock.cs
│   │       ├── TextBox.cs
│   │       ├── Button.cs
│   │       ├── CheckBox.cs
│   │       ├── RadioButton.cs
│   │       ├── ToggleSwitch.cs
│   │       ├── Slider.cs
│   │       └── ComboBox.cs
│   └── EclipseUI.Host/               # 窗口宿主
├── samples/EclipseUI.Demo/           # 示例应用
└── docs/                             # 文档
```

---

_最后更新：2026-03-19_
