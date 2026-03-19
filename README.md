# 🌑 EclipseUI

> **基于 Blazor + SkiaSharp 的跨平台 UI 框架**

EclipseUI 是一个使用 **Razor 语法**描述界面，通过 **SkiaSharp** 进行自绘渲染的跨平台 UI 框架。基于 Blazor 组件模型，从渲染引擎到布局系统，从控件库到窗口管理，全部独立实现。

> ⚠️ **实验性项目** - 本项目通过 [OpenClaw](https://github.com/openclaw/openclaw) 开发，旨在验证 OpenClaw 框架的 AI 辅助开发能力。**本项目仅供学习和研究使用，不建议用于生产环境或商业用途。**

[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)
[![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS-blue)](https://github.com/CeSun/EclipseUI)

---

## 🎯 项目定位

EclipseUI 提供一个轻量级的跨平台 UI 解决方案：

- ✅ **Razor 语法** - 使用熟悉的 Blazor 语法描述 UI
- ✅ **SkiaSharp 自绘** - 跨平台 2D 图形库，像素级控制
- ✅ **无 UI 框架依赖** - 不依赖 Avalonia、MAUI 等现有 UI 框架
- ✅ **跨平台一致性** - Windows/Linux/macOS 像素级一致表现
- ✅ **轻量级** - 最小依赖，快速启动

---

## 🌰 快速示例

```razor
<StackPanel Orientation="Vertical" Spacing="20">
    <TextBlock Text="🌑 欢迎使用 EclipseUI!" FontSize="32" />
    
    <Button Text="点我" OnClick="OnClick" Background="#4CAF50" />
    
    <TextBlock Text="点击了 @_count 次" FontSize="18" />
</StackPanel>

@code {
    int _count = 0;
    
    void OnClick(MouseEventArgs e)
    {
        _count++;
        StateHasChanged();
    }
}
```

---

## 📸 运行截图

![EclipseUI Demo](docs/screenshot.png)

*EclipseUI Demo 应用 - 展示 StackPanel 布局、TextBlock 文本显示和 Button 按钮控件*

---

## 🏗️ 架构设计

```
┌─────────────────────────────────────────┐
│           Razor 组件 (.razor)           │
│   (使用 Blazor 语法描述 UI)               │
└─────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────┐
│       Blazor 渲染树 (RenderTree)        │
└─────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────┐
│         EclipseRenderer                 │
│   (继承 Blazor Renderer，管理渲染生命周期) │
└─────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────┐
│    EclipseComponentAdapter              │
│   (将 RenderTree 转换为 EclipseElement 树) │
└─────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────┐
│         EclipseElement 树               │
│   (StackPanel, TextBlock, Button...)    │
└─────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────┐
│    SkiaSharp + OpenGL 绘制              │
│   (GRContext → SKSurface → GL)          │
└─────────────────────────────────────────┘
```

---

## 🚀 快速开始

### 环境要求

- .NET 8.0 或更高版本
- 支持 OpenGL 3.0+ / DirectX 11+ 的显卡

### 运行示例

```bash
cd samples/EclipseUI.Demo
dotnet run
```

### 项目结构

```
EclipseUI/
├── src/EclipseUI/              # 核心库
│   ├── Core/                   # 渲染器、组件基类、元素基类
│   ├── Layout/                 # 布局容器（StackPanel 等）
│   └── Controls/               # 基础控件（TextBlock, Button 等）
├── src/EclipseUI.Host/         # 窗口宿主（Silk.NET）
├── samples/EclipseUI.Demo/     # 演示应用
└── docs/                       # 文档
    ├── FEATURES.md             # 功能文档
    ├── ROADMAP.md              # 开发路线图
    └── guidelines/             # 开发规范
```

---

## 📦 核心组件

### 布局控件

| 控件 | 说明 |
|------|------|
| `<StackPanel>` | 水平/垂直堆叠布局 |
| `<Grid>` | 网格布局，支持 Auto/Star/Pixel 行列定义 |
| `<DockPanel>` | 停靠布局 |
| `<ScrollView>` | 滚动视图，支持垂直/水平滚动，可配置滚动条可见性 |
| `<WrapPanel>` | 流式布局，自动换行/换列 |
| `<Canvas>` | 绝对定位容器 |

### 基础控件

| 控件 | 说明 |
|------|------|
| `<TextBlock>` | 文本显示 |
| `<TextBox>` | 文本输入，支持双向绑定 |
| `<Button>` | 按钮 |
| `<CheckBox>` | 复选框，支持三态 |
| `<RadioButton>` | 单选框，支持分组 |
| `<ToggleSwitch>` | 开关控件 |
| `<Slider>` | 滑块控件 |
| `<ComboBox>` | 下拉选择框 |
| `<ListBox>` | 列表框，支持滚动和选中 |
| `<TabControl>` | 选项卡控件 |
| `<Border>` | 边框容器 |
| `<Image>` | 图片显示 |
| `<ProgressBar>` | 进度条 |

---

## 🎯 技术特点

### 与现有框架的对比

| 特性 | EclipseUI | MAUI | Avalonia | Uno Platform |
|------|-----------|------|----------|--------------|
| UI 描述 | Razor | XAML/C# | XAML | XAML/WinUI |
| 渲染引擎 | SkiaSharp (自绘) | 原生控件 | SkiaSharp (自绘) | SkiaSharp/Wasm/原生 |
| 跨平台 | Windows/Linux/macOS | 多平台 | 多平台 | 多平台 |
| 一致性 | 像素级一致 | 依赖平台 | 像素级一致 | 依赖平台 |
| 组件模型 | Blazor | MVVM | MVVM | MVVM |
| 学习曲线 | 低 (Web 背景) | 中 | 中 | 中 |
| 包大小 | 小 | 大 | 中 | 大 |
| 当前状态 | 早期开发 | 成熟 | 成熟 | 成熟 |

### 技术栈

- **.NET 8.0** - 运行时
- **SkiaSharp** - 2D 图形渲染
- **Silk.NET** - 跨平台窗口管理（基于 GLFW）
- **Blazor** - 组件模型和渲染树
- **OpenGL/DirectX** - GPU 加速（通过 SkiaSharp）

---

## 📝 开发路线

详细规划请参阅 [ROADMAP.md](docs/ROADMAP.md)

### 已完成 ✅

- [x] 核心渲染引擎（EclipseRenderer）
- [x] 组件模型（EclipseComponentBase）
- [x] 元素系统（EclipseElement）
- [x] StackPanel 布局
- [x] Grid 网格布局（Auto/Star/Pixel 行列）
- [x] DockPanel 停靠布局
- [x] ScrollView 滚动视图（垂直/水平）
- [x] WrapPanel 流式布局
- [x] Canvas 绝对定位
- [x] TextBlock 控件
- [x] TextBox 文本输入（双向绑定）
- [x] Button 控件
- [x] CheckBox 复选框（三态支持）
- [x] RadioButton 单选框（分组互斥）
- [x] ToggleSwitch 开关
- [x] Slider 滑块
- [x] ComboBox 下拉选择
- [x] ListBox 列表框
- [x] TabControl 选项卡
- [x] Border 边框容器
- [x] Image 图片显示
- [x] ProgressBar 进度条
- [x] PopupService 弹出层管理
- [x] 事件处理系统
- [x] 窗口宿主（Silk.NET）
- [x] iOS 风格主题

### 计划中 📋

**Phase 1 - 高级特性**
- [ ] 样式系统
- [ ] 数据绑定增强
- [ ] 动画系统
- [ ] 主题切换

**Phase 2 - 更多控件**
- [ ] TreeView 树形视图
- [ ] DataGrid 数据表格
- [ ] Menu 菜单
- [ ] ContextMenu 右键菜单
- [ ] Tooltip 提示框

---

## 🤝 贡献

欢迎贡献代码！

1. Fork 本仓库
2. 创建特性分支 (`git checkout -b feature/feature-name`)
3. 提交改动 (`git commit -m 'Add some feature'`)
4. 推送到分支 (`git push origin feature/feature-name`)
5. 创建 Pull Request

**开发前请阅读：** [docs/guidelines/development-rules.md](docs/guidelines/development-rules.md)

---

## 📄 许可证

MIT License - 详见 [LICENSE](LICENSE)

---

## 🌟 致谢

- **用户** - 提出需求，给予信任
- [SkiaSharp](https://github.com/mono/SkiaSharp) - 强大的 2D 图形库
- [Silk.NET](https://github.com/dotnet/Silk.NET) - 跨平台窗口管理
- [Blazor](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor) - 优秀的组件模型

---

**EclipseUI** - 用 Razor 绘制你的世界 🌑

*轻量 · 跨平台 · 像素级控制*
