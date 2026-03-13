# EclipseUI 渲染架构

## 📐 渲染抽象层设计

为了支持多种渲染后端（SkiaSharp、Avalonia、Web Canvas 等），EclipseUI 采用了渲染抽象层设计。

## 🏗️ 架构层次

```
┌─────────────────────────────────────┐
│        EclipseElement 基类          │
│  (ButtonElement, TextBlock 等)       │
└─────────────────────────────────────┘
                   ↓
┌─────────────────────────────────────┐
│      TextRenderer / ImageRenderer   │
│         (封装绘制逻辑)               │
└─────────────────────────────────────┘
                   ↓
┌─────────────────────────────────────┐
│         IRenderContext 接口          │
│  (Clear, DrawText, DrawImage 等)     │
└─────────────────────────────────────┘
                   ↓
┌─────────────────────────────────────┐
│     SkiaRenderContext 实现           │
│      (AvaloniaRenderContext)        │
│      (WebCanvasRenderContext)       │
└─────────────────────────────────────┘
```

## 🔌 核心接口

### IRenderContext

```csharp
public interface IRenderContext
{
    void Clear(Color color);
    void DrawRectangle(float x, float y, float width, float height, IBrush? brush, IPen? pen);
    void DrawRoundedRectangle(float x, float y, float width, float height, float cornerRadius, IBrush? brush, IPen? pen);
    void DrawText(string text, float x, float y, IFont font, Color color);
    void DrawImage(IImage image, float x, float y, float? width, float? height);
    void Save();
    void Restore();
    void Translate(float dx, float dy);
    void Rotate(float degrees);
    void Scale(float sx, float sy);
}
```

### 辅助接口

- `IBrush` - 画刷（填充颜色）
- `IPen` - 画笔（描边）
- `IFont` - 字体
- `IImage` - 图片

## 📝 使用示例

### 使用 TextRenderer

```csharp
// 旧方式（直接依赖 SkiaSharp）
using var paint = new SKPaint { TextSize = 14, Typeface = ... };
canvas.DrawText("Hello", x, y, paint);

// 新方式（使用抽象层）
TextRenderer.DrawText(context, "Hello 🌑", x, y, 14, Colors.Black);
```

### 自定义渲染器

```csharp
public class MyElement : EclipseElement
{
    public override void Render(IRenderContext context)
    {
        // 绘制背景
        context.DrawRectangle(X, Y, Width, Height, 
            new SkiaBrush(Colors.Blue));
        
        // 绘制文本
        TextRenderer.DrawText(context, Text, X + 10, Y + 20, 
            16, Colors.White);
    }
}
```

## 🔄 切换渲染后端

### 当前：SkiaSharp

```csharp
var renderContext = new SkiaRenderContext(skCanvas);
element.Render(renderContext);
```

### 未来：Avalonia

```csharp
var renderContext = new AvaloniaRenderContext(drawingContext);
element.Render(renderContext);
```

### 未来：Web Canvas

```csharp
var renderContext = new WebCanvasRenderContext(canvasElement);
element.Render(renderContext);
```

## 📦 文件结构

```
src/EclipseUI/Rendering/
├── IRenderContext.cs          # 核心接口定义
├── SkiaRenderContext.cs       # SkiaSharp 实现
├── TextRenderer.cs            # 文本渲染封装
├── ImageRenderer.cs           # 图片渲染封装（TODO）
└── ShapeRenderer.cs           # 形状渲染封装（TODO）
```

## ✅ 优势

1. **后端无关** - 元素代码不依赖具体渲染库
2. **易于测试** - 可以创建 Mock 渲染器进行单元测试
3. **多平台支持** - 轻松切换到其他渲染后端
4. **代码复用** - 渲染逻辑集中在 Renderer 类中

## 🚧 待完成

- [ ] 迁移所有 Element 使用新的渲染接口
- [ ] 实现 ImageRenderer
- [ ] 实现 ShapeRenderer
- [ ] 添加 Avalonia 渲染后端实现
- [ ] 添加单元测试

---

_最后更新：2026-03-13_
