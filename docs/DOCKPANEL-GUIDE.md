# DockPanel 使用指南

## 概述

`DockPanel` 是一个布局容器，用于将子元素停靠在顶部、底部、左侧、右侧或填充剩余空间。

## 基本用法

```razor
@using EclipseUI.Controls
@using EclipseUI.Layout

<DockPanel>
    <DockPanelItem Dock="Dock.Top">
        <TextBlock Text="顶部区域" />
    </DockPanelItem>
    
    <DockPanelItem Dock="Dock.Bottom">
        <TextBlock Text="底部区域" />
    </DockPanelItem>
    
    <DockPanelItem Dock="Dock.Left">
        <TextBlock Text="左侧区域" />
    </DockPanelItem>
    
    <DockPanelItem Dock="Dock.Right">
        <TextBlock Text="右侧区域" />
    </DockPanelItem>
    
    <DockPanelItem>
        <TextBlock Text="填充剩余空间（默认 Dock=Fill）" />
    </DockPanelItem>
</DockPanel>
```

## Dock 枚举值

| 值 | 说明 |
|----|------|
| `Dock.Top` | 停靠在顶部 |
| `Dock.Bottom` | 停靠在底部 |
| `Dock.Left` | 停靠在左侧 |
| `Dock.Right` | 停靠在右侧 |
| `Dock.Fill` | 填充剩余空间 |

## LastChildFill 属性

`DockPanel` 默认会将最后一个子元素视为 `Dock.Fill`，即使没有显式设置。

```razor
<!-- 最后一个 DockPanelItem 会自动填充剩余空间 -->
<DockPanel LastChildFill="true">
    <DockPanelItem Dock="Dock.Top">
        <TextBlock Text="顶部" />
    </DockPanelItem>
    <DockPanelItem Dock="Dock.Left">
        <TextBlock Text="左侧" />
    </DockPanelItem>
    <DockPanelItem>
        <TextBlock Text="这个会自动填充剩余空间" />
    </DockPanelItem>
</DockPanel>
```

如果要禁用此行为：

```razor
<DockPanel LastChildFill="false">
    <DockPanelItem Dock="Dock.Top">
        <TextBlock Text="顶部" />
    </DockPanelItem>
    <DockPanelItem Dock="Dock.Left">
        <TextBlock Text="左侧" />
    </DockPanelItem>
    <DockPanelItem Dock="Dock.Right">
        <TextBlock Text="右侧 - 不会自动填充" />
    </DockPanelItem>
</DockPanel>
```

## 完整示例 - 经典三栏布局

```razor
<DockPanel>
    <!-- 顶部标题栏 -->
    <DockPanelItem Dock="Dock.Top">
        <TextBlock Text="应用程序标题" FontSize="18" />
    </DockPanelItem>
    
    <!-- 底部状态栏 -->
    <DockPanelItem Dock="Dock.Bottom">
        <TextBlock Text="状态：就绪" FontSize="12" />
    </DockPanelItem>
    
    <!-- 左侧导航栏 -->
    <DockPanelItem Dock="Dock.Left">
        <StackPanel Orientation="StackOrientation.Vertical">
            <Button Text="首页" />
            <Button Text="设置" />
            <Button Text="关于" />
        </StackPanel>
    </DockPanelItem>
    
    <!-- 右侧主内容区（自动填充） -->
    <DockPanelItem>
        <TextBlock Text="主内容区域" />
    </DockPanelItem>
</DockPanel>
```

## 注意事项

1. **DockPanelItem 是必需的**：所有子元素必须包裹在 `DockPanelItem` 中
2. **Fill 只能有一个**：通常只有一个元素使用 `Dock.Fill`（或依赖 `LastChildFill`）
3. **顺序重要**：子元素的声明顺序会影响布局结果
4. **自动化**：不需要手动管理元素，EclipseUI 会自动处理布局

---

_最后更新：2026-03-14_
