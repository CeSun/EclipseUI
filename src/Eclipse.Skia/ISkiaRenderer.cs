using Eclipse.Core.Abstractions;
using SkiaSharp;

namespace Eclipse.Skia;

/// <summary>
/// Skia 渲染器接口 - 负责将组件树渲染到 Skia 画布
/// </summary>
public interface ISkiaRenderer
{
    /// <summary>
    /// 渲染组件树
    /// </summary>
    void Render(IComponent root, SkiaRenderContext context);
}

/// <summary>
/// Skia 渲染上下文
/// </summary>
public class SkiaRenderContext
{
    public SKCanvas Canvas { get; }
    public float Width { get; }
    public float Height { get; }
    public float Scale { get; }
    
    public SkiaRenderContext(SKCanvas canvas, float width, float height, float scale = 1f)
    {
        Canvas = canvas;
        Width = width;
        Height = height;
        Scale = scale;
    }
}

/// <summary>
/// 控件渲染器接口 - 由平台层实现具体控件的渲染
/// </summary>
public interface ISkiaControlRenderer
{
    /// <summary>
    /// 目标控件类型
    /// </summary>
    Type TargetType { get; }
    
    /// <summary>
    /// 渲染控件
    /// </summary>
    void Render(
        IComponent component, 
        SkiaRenderContext context, 
        SKRect bounds,
        Action<IComponent, SkiaRenderContext, SKRect> renderChild);
}