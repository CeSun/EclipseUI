# Eclipse.Skia

EclipseUI SkiaSharp 渲染层。

## 架构

```
ISkiaRenderer (接口)
    ↓
DefaultSkiaRenderer (实现)
    ↓
SkiaControlRenderers (控件渲染器)
    ├── StackLayoutRenderer
    ├── LabelRenderer
    ├── ButtonRenderer
    └── TextContentRenderer
```

## 渲染上下文

```csharp
public class SkiaRenderContext
{
    public SKCanvas Canvas { get; }
    public float Width { get; }
    public float Height { get; }
    public float Scale { get; }
}
```

## 自定义渲染器

```csharp
public class MyControlRenderer : ISkiaControlRenderer
{
    public Type TargetType => typeof(MyControl);
    
    public void Render(
        IComponent component, 
        SkiaRenderContext context, 
        SKRect bounds,
        Action<IComponent, SkiaRenderContext, SKRect> renderChild)
    {
        var control = (MyControl)component;
        // 使用 context.Canvas 绘制
    }
}
```