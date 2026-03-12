# 🗺️ EclipseUI 开发路线图

本文档规划了 EclipseUI 框架的后续开发计划，按优先级和依赖关系分为多期完成。

---

## 📊 总体概览

| 期数 | 主题 | 预计控件数 | 优先级 |
|------|------|-----------|--------|
| **Phase 1** | 基础布局完善 | 3 | 🔴 高 |
| **Phase 2** | 输入控件 | 4 | 🔴 高 |
| **Phase 3** | 选择控件 | 4 | 🟡 中 |
| **Phase 4** | 高级控件 | 4 | 🟡 中 |
| **Phase 5** | 容器控件 | 3 | 🟢 低 |
| **Phase 6** | 高级特性 | - | 🟢 低 |

---

## 📋 Phase 1 - 基础布局完善

**目标：** 完善布局系统，支持更复杂的 UI 结构

**优先级：** 🔴 高

**依赖：** 无（基于现有 StackPanel）

---

### 1.1 Grid - 网格布局

**用途：** 行列网格布局，支持单元格合并

**功能列表：**

| 功能 | 说明 | 优先级 |
|------|------|--------|
| `RowDefinitions` | 定义行高（支持绝对值、自动、比例） | 🔴 必须 |
| `ColumnDefinitions` | 定义列宽（支持绝对值、自动、比例） | 🔴 必须 |
| `Row` / `Column` | 子元素行列位置 | 🔴 必须 |
| `RowSpan` / `ColumnSpan` | 跨行/跨列 | 🔴 必须 |
| `ShowGridLines` | 调试模式显示网格线 | 🟡 可选 |

**API 示例：**
```razor
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto" />
        <RowDefinition Height="*" />
        <RowDefinition Height="2*" />
    </Grid.RowDefinitions>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="100" />
        <ColumnDefinition Width="*" />
    </Grid.ColumnDefinitions>
    
    <TextBlock Text="Header" Grid.Row="0" Grid.Column="0" Grid.ColumnSpan="2" />
    <TextBlock Text="Sidebar" Grid.Row="1" Grid.Column="0" />
    <TextBlock Text="Content" Grid.Row="1" Grid.Column="1" />
</Grid>
```

---

### 1.2 WrapPanel - 自动换行布局

**用途：** 子元素自动换行排列（类似 Flexbox）

**功能列表：**

| 功能 | 说明 | 优先级 |
|------|------|--------|
| `Orientation` | 主轴方向（Horizontal/Vertical） | 🔴 必须 |
| `Spacing` | 元素间距 | 🔴 必须 |
| `ItemWidth` / `ItemHeight` | 固定子元素尺寸 | 🟡 可选 |

**API 示例：**
```razor
<WrapPanel Orientation="Horizontal" Spacing="10">
    <Button Text="Button 1" />
    <Button Text="Button 2" />
    <Button Text="Button 3" />
    <!-- 自动换行 -->
    <Button Text="Button 4" />
</WrapPanel>
```

---

### 1.3 DockPanel - 停靠布局

**用途：** 子元素停靠到边缘（类似 WinForms/WPF Dock）

**功能列表：**

| 功能 | 说明 | 优先级 |
|------|------|--------|
| `Dock` | 停靠方向（Left/Right/Top/Bottom） | 🔴 必须 |
| `LastChildFill` | 最后一个子元素填充剩余空间 | 🔴 必须 |

**API 示例：**
```razor
<DockPanel LastChildFill="True">
    <TextBlock Text="Top" DockPanel.Dock="Top" />
    <TextBlock Text="Bottom" DockPanel.Dock="Bottom" />
    <TextBlock Text="Left" DockPanel.Dock="Left" />
    <TextBlock Text="Right" DockPanel.Dock="Right" />
    <TextBlock Text="Content (fills remaining)" />
</DockPanel>
```

---

## 📋 Phase 2 - 输入控件

**目标：** 支持用户输入和交互

**优先级：** 🔴 高

**依赖：** Phase 1 布局控件

---

### 2.1 TextBox - 单行文本输入

**功能列表：**

| 功能 | 说明 | 优先级 |
|------|------|--------|
| `Text` | 文本内容 | 🔴 必须 |
| `Placeholder` | 占位提示文本 | 🔴 必须 |
| `MaxLength` | 最大字符数 | 🔴 必须 |
| `IsReadOnly` | 只读模式 | 🔴 必须 |
| `IsPassword` | 密码模式（隐藏输入） | 🔴 必须 |
| `CaretPosition` | 光标位置 | 🟡 可选 |
| `SelectedText` | 选中文本 | 🟡 可选 |
| `TextAlignment` | 文本对齐（Left/Center/Right） | 🟡 可选 |

**事件：**
- `TextChanged` - 文本变化
- `EnterPressed` - 回车键按下
- `GotFocus` / `LostFocus` - 焦点变化

---

### 2.2 TextEditor - 多行文本编辑

**功能列表：**

| 功能 | 说明 | 优先级 |
|------|------|--------|
| `Text` | 文本内容 | 🔴 必须 |
| `AcceptsReturn` | 支持回车换行 | 🔴 必须 |
| `WordWrap` | 自动换行 | 🔴 必须 |
| `LineCount` | 行数（只读） | 🟡 可选 |
| `ScrollToLine` | 滚动到指定行 | 🟡 可选 |

**事件：**
- `TextChanged`
- `SelectionChanged`

---

### 2.3 NumberBox - 数字输入

**功能列表：**

| 功能 | 说明 | 优先级 |
|------|------|--------|
| `Value` | 数值 | 🔴 必须 |
| `Minimum` / `Maximum` | 最小/最大值 | 🔴 必须 |
| `Increment` | 步进值 | 🔴 必须 |
| `DecimalPlaces` | 小数位数 | 🟡 可选 |
| `SpinButtons` | 显示增减按钮 | 🟡 可选 |

**事件：**
- `ValueChanged`

---

### 2.4 ComboBox - 下拉选择

**功能列表：**

| 功能 | 说明 | 优先级 |
|------|------|--------|
| `ItemsSource` | 数据源 | 🔴 必须 |
| `SelectedItem` | 选中项 | 🔴 必须 |
| `SelectedIndex` | 选中索引 | 🔴 必须 |
| `DisplayMemberPath` | 显示字段 | 🟡 可选 |
| `IsEditable` | 可编辑模式 | 🟡 可选 |
| `MaxDropDownHeight` | 下拉最大高度 | 🟡 可选 |

**事件：**
- `SelectionChanged`
- `DropDownOpened` / `DropDownClosed`

---

## 📋 Phase 3 - 选择控件

**目标：** 支持多项选择和开关操作

**优先级：** 🟡 中

**依赖：** Phase 2 输入控件

---

### 3.1 CheckBox - 复选框

**功能列表：**

| 功能 | 说明 | 优先级 |
|------|------|--------|
| `IsChecked` | 选中状态（true/false/null） | 🔴 必须 |
| `Content` | 显示文本 | 🔴 必须 |
| `IsThreeState` | 支持三态（选中/未选中/不确定） | 🟡 可选 |

**事件：**
- `Checked` / `Unchecked` / `Indeterminate`

---

### 3.2 RadioButton - 单选框

**功能列表：**

| 功能 | 说明 | 优先级 |
|------|------|--------|
| `IsChecked` | 选中状态 | 🔴 必须 |
| `Content` | 显示文本 | 🔴 必须 |
| `GroupName` | 分组名称（同组互斥） | 🔴 必须 |

**事件：**
- `Checked`

---

### 3.3 ToggleSwitch - 开关

**功能列表：**

| 功能 | 说明 | 优先级 |
|------|------|--------|
| `IsOn` | 开关状态 | 🔴 必须 |
| `OnContent` / `OffContent` | 开/关状态文本 | 🟡 可选 |

**事件：**
- `Toggled`

---

### 3.4 Slider - 滑块

**功能列表：**

| 功能 | 说明 | 优先级 |
|------|------|--------|
| `Value` | 当前值 | 🔴 必须 |
| `Minimum` / `Maximum` | 范围 | 🔴 必须 |
| `Orientation` | 方向（Horizontal/Vertical） | 🔴 必须 |
| `TickFrequency` | 刻度间隔 | 🟡 可选 |
| `ShowTicks` | 显示刻度 | 🟡 可选 |
| `IsSnapToTick` | 自动吸附到刻度 | 🟡 可选 |

**事件：**
- `ValueChanged`

---

## 📋 Phase 4 - 高级控件

**目标：** 支持复杂交互和展示

**优先级：** 🟡 中

**依赖：** Phase 2 输入控件

---

### 4.1 Image - 图片显示

**功能列表：**

| 功能 | 说明 | 优先级 |
|------|------|--------|
| `Source` | 图片路径/URL | 🔴 必须 |
| `Stretch` | 拉伸模式（Uniform/Fill/None） | 🔴 必须 |
| `Width` / `Height` | 尺寸 | 🔴 必须 |

---

### 4.2 Border - 边框容器

**功能列表：**

| 功能 | 说明 | 优先级 |
|------|------|--------|
| `BorderThickness` | 边框厚度 | 🔴 必须 |
| `BorderBrush` | 边框颜色 | 🔴 必须 |
| `CornerRadius` | 圆角半径 | 🔴 必须 |
| `Background` | 背景色 | 🔴 必须 |

---

### 4.3 ProgressBar - 进度条

**功能列表：**

| 功能 | 说明 | 优先级 |
|------|------|--------|
| `Value` | 当前进度 | 🔴 必须 |
| `Minimum` / `Maximum` | 范围 | 🔴 必须 |
| `Orientation` | 方向 | 🟡 可选 |
| `IsIndeterminate` | 不确定模式（动画） | 🟡 可选 |

---

### 4.4 ListBox - 列表框

**功能列表：**

| 功能 | 说明 | 优先级 |
|------|------|--------|
| `ItemsSource` | 数据源 | 🔴 必须 |
| `SelectedItem` / `SelectedItems` | 选中项 | 🔴 必须 |
| `SelectionMode` | 选择模式（Single/Multiple） | 🔴 必须 |
| `DisplayMemberPath` | 显示字段 | 🟡 可选 |

---

## 📋 Phase 5 - 容器控件

**目标：** 支持复杂 UI 结构

**优先级：** 🟢 低

**依赖：** Phase 1-4

---

### 5.1 ScrollViewer - 滚动容器

**功能列表：**

| 功能 | 说明 | 优先级 |
|------|------|--------|
| `HorizontalScrollBarVisibility` | 水平滚动条 | 🔴 必须 |
| `VerticalScrollBarVisibility` | 垂直滚动条 | 🔴 必须 |
| `ScrollToHorizontalOffset` / `ScrollToVerticalOffset` | 滚动到指定位置 | 🟡 可选 |

---

### 5.2 TabControl - 标签页

**功能列表：**

| 功能 | 说明 | 优先级 |
|------|------|--------|
| `ItemsSource` | 数据源 | 🔴 必须 |
| `SelectedItem` | 选中项 | 🔴 必须 |
| `SelectedIndex` | 选中索引 | 🔴 必须 |
| `TabPlacement` | 标签位置（Top/Bottom/Left/Right） | 🟡 可选 |

---

### 5.3 Expander - 折叠面板

**功能列表：**

| 功能 | 说明 | 优先级 |
|------|------|--------|
| `IsExpanded` | 展开状态 | 🔴 必须 |
| `Header` | 标题 | 🔴 必须 |
| `ExpandDirection` | 展开方向 | 🟡 可选 |

---

## 📋 Phase 6 - 高级特性

**目标：** 完善框架功能

**优先级：** 🟢 低

**依赖：** 所有控件

---

### 6.1 样式系统 (Styles)

- 支持 CSS 类似的选择器
- 支持继承和覆盖
- 支持资源字典

### 6.2 数据绑定 (DataBinding)

- 支持 `{Binding}` 语法
- 支持单向/双向绑定
- 支持绑定转换

### 6.3 动画系统 (Animations)

- 支持关键帧动画
- 支持缓动函数
- 支持动画故事板

### 6.4 主题系统 (Themes)

- 支持浅色/深色主题
- 支持自定义主题
- 支持主题切换

### 6.5 导航系统 (Navigation)

- 支持页面导航
- 支持路由
- 支持导航历史

---

## 📅 开发顺序建议

```
Phase 1 (布局) → Phase 2 (输入) → Phase 3 (选择) → Phase 4 (高级) → Phase 5 (容器) → Phase 6 (特性)
```

**理由：**
1. 布局是基础，所有控件都需要布局容器
2. 输入控件是最常用的交互元素
3. 选择控件依赖输入控件的焦点/事件系统
4. 高级控件依赖前面的基础
5. 容器控件需要前面的控件作为内容
6. 高级特性是框架层面的完善

---

## 📝 控件开发模板

每个控件应包含：

1. **Element 类** - 继承 `EclipseElement`
   - `Measure()` - 测量尺寸
   - `Arrange()` - 排列位置
   - `Render()` - 绘制内容

2. **Component 类** - 继承 `EclipseComponentBase`
   - `[Parameter]` 属性
   - `EventCallback` 事件
   - `CreateElement()` 创建元素
   - `UpdateElementFromParameters()` 更新元素

3. **文档** - 添加到 `docs/controls/` 目录

---

_最后更新：2026-03-12_
