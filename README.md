# 🌑 EclipseUI

> **由 AI 助手独立开发的跨平台 UI 框架**

EclipseUI 是一个**由 AI 助手独立开发**的 UI 框架，使用 **Razor 语法**描述界面，通过 **SkiaSharp** 进行原生渲染。从渲染引擎到布局系统，从控件库到窗口管理，全部由 AI 助手独立完成。

[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)
[![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS-blue)](https://github.com/EclipseUI/EclipseUI)

---

## 🎯 项目定位

EclipseUI 是 **AI 助手** 为用户开发的 UI 框架，提供：

- ✅ **AI 独立开发** - 从架构到代码，全部由 AI 助手完成
- ✅ **Razor 语法** - 使用熟悉的 Blazor 语法描述 UI
- ✅ **SkiaSharp 渲染** - 跨平台 2D 图形库，像素级控制
- ✅ **无框架依赖** - 不依赖 Avalonia、MAUI 等现有 UI 框架
- ✅ **跨平台** - Windows/Linux/macOS 一致表现

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
│   (AI 设计的渲染引擎，继承 Blazor Renderer) │
└─────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────┐
│    SkiaComponentAdapter                 │
│   (连接 Blazor 和 Skia 元素树)            │
└─────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────┐
│         SkiaElement 树                  │
│   (SkiaStackPanel, SkiaButton...)       │
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
- 支持 OpenGL 3.0+ 的显卡

### 运行示例

```bash
cd samples/EclipseUI.Demo
dotnet run
```

### 项目结构

```
EclipseUI/
├── src/EclipseUI/              # 核心渲染引擎
│   ├── Core/                   # 渲染器、适配器
│   ├── Layout/                 # 布局容器
│   └── Controls/               # 基础控件
├── src/EclipseUI.Host/         # Silk.NET 窗口外壳
└── samples/EclipseUI.Demo/     # 演示应用
```

---

## 📦 核心组件

### 布局控件

| 控件 | 说明 |
|------|------|
| `<StackPanel>` | 水平/垂直堆叠布局 |
| `<Grid>` | 网格布局（计划中） |
| `<WrapPanel>` | 自动换行布局（计划中） |

### 基础控件

| 控件 | 说明 |
|------|------|
| `<TextBlock>` | 文本显示 |
| `<Button>` | 按钮 |
| `<Image>` | 图片（计划中） |
| `<TextBox>` | 文本输入（计划中） |

---

## 🎯 技术特点

### 与现有框架的对比

| 特性 | EclipseUI | MAUI | Avalonia | Uno Platform |
|------|-----------|------|----------|--------------|
| UI 描述 | Razor | XAML/C# | XAML | XAML/WinUI |
| 渲染引擎 | SkiaSharp (自绘) | 原生控件 | SkiaSharp (自绘) | SkiaSharp/Wasm/原生 |
| 跨平台一致性 | 像素级一致 | 依赖平台 | 像素级一致 | 依赖平台 |
| 组件模型 | Blazor | MVVM | MVVM | MVVM |
| 学习曲线 | 低 (Web 背景) | 中 | 中 | 高 |
| 包大小 | 较小 | 大 | 中 | 大 |

### 技术栈

- **.NET 8.0** - 运行时
- **SkiaSharp** - 2D 图形渲染
- **Silk.NET** - 跨平台窗口管理
- **Blazor** - 组件模型和渲染树

---

## 📝 开发路线

### Phase 1 - 基础框架 ✅

- [x] 核心渲染引擎
- [x] SkiaElement 基类
- [x] StackPanel 布局
- [x] TextBlock 控件
- [x] Button 控件
- [x] 事件处理系统
- [x] Silk.NET 窗口集成

### Phase 2 - 完善控件

- [ ] Grid 布局
- [ ] Image 控件
- [ ] TextBox 输入
- [ ] CheckBox/RadioButton
- [ ] ListBox/ComboBox

### Phase 3 - 高级特性

- [ ] 数据绑定
- [ ] 样式系统
- [ ] 动画支持
- [ ] 主题系统
- [ ] 导航系统

### Phase 4 - 工具链

- [ ] Visual Studio 模板
- [ ] 设计器预览
- [ ] 热重载支持
- [ ] NuGet 发布

---

## 🤝 贡献

欢迎贡献代码！

1. Fork 项目
2. 创建特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交改动 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 创建 Pull Request

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

*由 AI 助手独立开发，为你而生*
