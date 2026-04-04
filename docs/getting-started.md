# EclipseUI 快速入门

## 环境要求

- .NET 10 SDK
- Windows (目前仅支持 Windows)

## 获取源码

```bash
git clone https://github.com/CeSun/EclipseUI.git
cd EclipseUI
```

## 项目结构

```
EclipseUI/
├── src/
│   ├── Eclipse.Core/       # 核心抽象
│   ├── Eclipse.Controls/   # 控件库
│   ├── Eclipse.Skia/       # 渲染层
│   ├── Eclipse.Generator/  # Source Generator
│   └── Eclipse.Windows/    # Windows 平台
├── samples/
│   └── SkiaDemo/           # 示例应用
└── tests/
    └── Eclipse.Tests/      # 单元测试
```

## 创建新项目

### 1. 创建控制台项目

```bash
dotnet new console -n MyApp
cd MyApp
```

### 2. 添加项目引用

编辑 `.csproj` 文件：

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0-windows</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <!-- 引用 EclipseUI 项目 -->
    <ProjectReference Include="..\EclipseUI\src\Eclipse.Core\Eclipse.Core.csproj" />
    <ProjectReference Include="..\EclipseUI\src\Eclipse.Controls\Eclipse.Controls.csproj" />
    <ProjectReference Include="..\EclipseUI\src\Eclipse.Skia\Eclipse.Skia.csproj" />
    <ProjectReference Include="..\EclipseUI\src\Eclipse.Windows\Eclipse.Windows.csproj" />
    
    <!-- Source Generator -->
    <ProjectReference Include="..\EclipseUI\src\Eclipse.Generator\Eclipse.Generator.csproj" 
                      OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
  </ItemGroup>

  <!-- 注册 .eui 文件 -->
  <ItemGroup>
    <AdditionalFiles Include="**/*.eui" />
  </ItemGroup>
</Project>
```

### 3. 创建 EUI 组件

新建 `HomePage.eui` 文件：

```xml
@using Eclipse.Controls

@code {
    private int count = 0;
    
    private void OnClick(object? sender, EventArgs e)
    {
        count++;
    }
}

<StackLayout Spacing="16" Padding="20">
    <Label Text="你好 EclipseUI! 🎉" FontSize="32" FontWeight="Bold" />
    <Label Text=@$"点击次数: {count}" FontSize="16" />
    <Button Text="点击我" OnClick="@OnClick" />
</StackLayout>
```

### 4. 启动应用

```csharp
// Program.cs
using Eclipse.Windows;

Application.Run<HomePage>();
```

### 5. 运行

```bash
dotnet run
```

## 使用内置控件

### StackLayout - 堆叠布局

```xml
<StackLayout Spacing="10" Padding="20">
    <Label Text="第一行" />
    <Label Text="第二行" />
</StackLayout>
```

### HStack - 水平布局

```xml
<HStack Spacing="10">
    <Button Text="左" />
    <Button Text="中" />
    <Button Text="右" />
</HStack>
```

### Label - 文本标签

```xml
<Label Text="普通文本" />
<Label Text="大号文本" FontSize="24" />
<Label Text="粗体文本" FontWeight="Bold" />
<Label Text="红色文本" Color="#FF0000" />
```

### Button - 按钮

```xml
<Button Text="默认按钮" OnClick="@OnClick" />
<Button Text="自定义样式" BackgroundColor="#007AFF" TextColor="White" />
```

### TextInput - 文本输入

```xml
<TextInput Text="@inputText" Placeholder="请输入..." OnTextChanged="@OnTextChanged" />
```

### CheckBox - 复选框

```xml
<CheckBox IsChecked="true" OnCheckedChanged="@OnCheckedChanged" />
```

## 条件渲染

```xml
@if (count > 0)
{
    <Label Text=@$"计数: {count}" />
}
@else
{
    <Label Text="计数为零" Color="#999" />
}
```

## 循环渲染

```xml
@foreach (var item in items)
{
    <Label Text=@item.Name />
}
```

## 示例项目

查看 `samples/SkiaDemo/` 获取完整示例。

## 下一步

- [EUI 语法参考](./syntax.md)
- [架构设计](./architecture.md)
- [文本渲染系统](./text-rendering.md)