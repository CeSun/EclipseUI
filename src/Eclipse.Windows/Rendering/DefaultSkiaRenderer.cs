using Eclipse.Core;
using Eclipse.Core.Abstractions;
using Eclipse.Controls;
using Eclipse.Input;
using Eclipse.Skia;
using SkiaSharp;

namespace Eclipse.Windows.Rendering;

/// <summary>
/// 默认 Skia 渲染器 - 遍历组件树并渲染
/// </summary>
public class DefaultSkiaRenderer : ISkiaRenderer
{
    private readonly Dictionary<Type, ISkiaControlRenderer> _renderers = new();
    private readonly InputManager? _inputManager;
    
    public DefaultSkiaRenderer() : this(null)
    {
    }
    
    public DefaultSkiaRenderer(InputManager? inputManager)
    {
        _inputManager = inputManager;
        
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
        
        // 更新 InputManager 的 RootElement（因为 Rebuild 创建了新实例）
        if (_inputManager != null && root?.Children.Count > 0 && root.Children[0] is IInputElement firstChild)
        {
            _inputManager.SetRootElementForRender(firstChild);
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
            // 没有注册渲染器，直接渲染子组件
            foreach (var child in component.Children)
            {
                RenderChild(child, context, bounds);
            }
        }
    }
    
    private void RenderChild(IComponent child, SkiaRenderContext context, SKRect bounds)
    {
        RenderComponent(child, context, bounds);
    }
    
    private void RegisterRenderer<TRenderer>() where TRenderer : ISkiaControlRenderer, new()
    {
        var renderer = new TRenderer();
        _renderers[renderer.TargetType] = renderer;
    }
}