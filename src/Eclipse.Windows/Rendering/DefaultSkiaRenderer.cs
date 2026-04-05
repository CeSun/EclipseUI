using Eclipse.Core;
using Eclipse.Core.Abstractions;
using Eclipse.Input;
using Eclipse.Rendering;
using Eclipse.Skia;
using SkiaSharp;

namespace Eclipse.Windows.Rendering;

/// <summary>
/// 组件树渲染器 - 遍历组件树并调用 Render
/// </summary>
public class ComponentRenderer : ISkiaRenderer
{
    private readonly InputManager? _inputManager;
    
    public ComponentRenderer() : this(null) { }
    
    public ComponentRenderer(InputManager? inputManager)
    {
        _inputManager = inputManager;
    }
    
    public void Render(IComponent root, SkiaRenderContext context)
    {
        context.Canvas.Clear(SKColors.White);
        
        // 重建组件树以反映状态变化
        if (root is ComponentBase componentBase)
        {
            componentBase.Rebuild();
        }
        
        // 更新 InputManager 的 RootElement
        if (_inputManager != null && root?.Children.Count > 0 && root.Children[0] is IInputElement firstChild)
        {
            _inputManager.SetRootElementForRender(firstChild);
        }
        
        // 创建 DrawingContext 并渲染
        var drawingContext = new SkiaDrawingContext(context.Canvas, context.Width, context.Height, context.Scale);
        drawingContext.SetDrawChildCallback((child, bounds) => RenderChild(child, drawingContext, bounds));
        
        var bounds = new Rect(0, 0, context.Width, context.Height);
        root.Render(drawingContext, bounds);
    }
    
    private void RenderChild(IComponent component, DrawingContext context, Rect bounds)
    {
        component.Render(context, bounds);
    }
}