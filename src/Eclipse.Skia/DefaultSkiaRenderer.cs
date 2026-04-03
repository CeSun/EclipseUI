using Eclipse.Core;
using Eclipse.Core.Abstractions;
using Eclipse.Skia.Controls;
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
        RenderComponent(root, context, new SKRect(0, 0, context.Width, context.Height));
    }
    
    private void RenderComponent(IComponent component, SkiaRenderContext context, SKRect bounds)
    {
        var type = component.GetType();
        
        if (_renderers.TryGetValue(type, out var renderer))
        {
            renderer.Render(component, context, bounds, RenderChildren);
        }
        else
        {
            // 没有注册渲染器，只渲染子组件
            RenderChildren(component, context, bounds);
        }
    }
    
    private void RenderChildren(IComponent parent, SkiaRenderContext context, SKRect bounds)
    {
        // 简单的垂直布局子组件
        float y = bounds.Top;
        float childWidth = bounds.Width;
        
        foreach (var child in parent.Children)
        {
            var childHeight = EstimateChildHeight(child, context);
            var childBounds = new SKRect(bounds.Left, y, bounds.Right, y + childHeight);
            RenderComponent(child, context, childBounds);
            y += childHeight;
        }
    }
    
    private float EstimateChildHeight(IComponent component, SkiaRenderContext context)
    {
        // 简单的高度估算，后续可以根据实际组件属性计算
        return component switch
        {
            Label label => 24f * context.Scale,
            Button button => 44f * context.Scale,
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
        height += (float)stack.Spacing * Math.Max(0, stack.Children.Count - 1);
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
        Action<IComponent, SkiaRenderContext, SKRect> renderChildren);
}