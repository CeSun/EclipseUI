using Eclipse.Core;
using Eclipse.Core.Abstractions;
using Eclipse.Controls;
using Eclipse.Skia.Renderers;
using SkiaSharp;

namespace Eclipse.Skia;

/// <summary>
/// 默认 Skia 渲染器 - 遍历组件树并渲染
/// </summary>
public class DefaultSkiaRenderer : ISkiaRenderer
{
    private readonly Dictionary<Type, ISkiaControlRenderer> _renderers = new();
    
    public DefaultSkiaRenderer()
    {
        // 注册内置渲染器
        RegisterRenderer<StackLayoutRenderer>();
        RegisterRenderer<LabelRenderer>();
        RegisterRenderer<ButtonRenderer>();
        RegisterRenderer<TextContentRenderer>();
    }
    
    public void Render(IComponent root, SkiaRenderContext context)
    {
        context.Canvas.Clear(SKColors.White);
        
        // 重建组件树以反映状态变化
        if (root is ComponentBase componentBase)
        {
            componentBase.Rebuild();
        }
        
        RenderComponent(root, context, new SKRect(0, 0, context.Width, context.Height));
    }
    
    private void RenderComponent(IComponent component, SkiaRenderContext context, SKRect bounds)
    {
        var type = component.GetType();
        
        if (_renderers.TryGetValue(type, out var renderer))
        {
            renderer.Render(component, context, bounds, RenderChild);
        }
        else
        {
            // 没有注册渲染器，尝试渲染子组件
            RenderChild(component, context, bounds);
        }
    }
    
    /// <summary>
    /// 渲染子组件 - 由容器渲染器调用，用于渲染单个子组件
    /// </summary>
    private void RenderChild(IComponent child, SkiaRenderContext context, SKRect bounds)
    {
        RenderComponent(child, context, bounds);
    }
    
    private float EstimateChildHeight(IComponent component, SkiaRenderContext context)
    {
        return component switch
        {
            Label => 24f * context.Scale,
            Button => 44f * context.Scale,
            StackLayout stack => EstimateStackHeight(stack, context),
            _ => 40f * context.Scale
        };
    }
    
    private float EstimateStackHeight(StackLayout stack, SkiaRenderContext context)
    {
        float height = 0;
        foreach (var child in stack.Children)
        {
            height += EstimateChildHeight(child, context);
        }
        height += (float)stack.GetSpacing() * Math.Max(0, stack.Children.Count - 1);
        return height;
    }
    
    private void RegisterRenderer<TRenderer>() where TRenderer : ISkiaControlRenderer, new()
    {
        var renderer = new TRenderer();
        _renderers[renderer.TargetType] = renderer;
    }
}

/// <summary>
/// 控件渲染器接口
/// </summary>
public interface ISkiaControlRenderer
{
    Type TargetType { get; }
    
    void Render(
        IComponent component, 
        SkiaRenderContext context, 
        SKRect bounds,
        Action<IComponent, SkiaRenderContext, SKRect> renderChild);
}