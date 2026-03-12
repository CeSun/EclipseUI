using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace EclipseUI.Core;

/// <summary>
/// EclipseUI 渲染器 - 核心渲染引擎
/// </summary>
public class EclipseRenderer : Renderer
{
    public EclipseRenderer(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
        : base(serviceProvider, loggerFactory)
    {
        // 设置静态引用
        EclipseComponentBase.CurrentRenderer = this;
        
        // 初始化根元素
        RootElement = RootElementHandler.Element;
    }
    
    protected override void HandleException(Exception exception)
    {
        // 记录异常但不抛出，避免崩溃
    }
    
    private readonly Dictionary<int, EclipseComponentAdapter> _componentAdapters = new();
    private readonly List<(int Id, IComponent Component)> _rootComponents = new();
    
    /// <summary>
    /// 根元素
    /// </summary>
    public EclipseElement? RootElement { get; private set; }
    
    /// <summary>
    /// 根元素处理器
    /// </summary>
    internal RootElementHandler RootElementHandler { get; } = new RootElementHandler();
    
    /// <summary>
    /// Skia 画布
    /// </summary>
    public SKCanvas? Canvas { get; private set; }
    
    /// <summary>
    /// 表面宽度
    /// </summary>
    public int SurfaceWidth { get; private set; }
    
    /// <summary>
    /// 表面高度
    /// </summary>
    public int SurfaceHeight { get; private set; }
    
    /// <summary>
    /// 需要重新渲染
    /// </summary>
    public Action? OnRenderRequested { get; set; }
    
    public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();
    
    /// <summary>
    /// 设置渲染表面
    /// </summary>
    public void SetSurface(SKCanvas canvas, int width, int height)
    {
        Canvas = canvas;
        SurfaceWidth = width;
        SurfaceHeight = height;
    }
    
    /// <summary>
    /// 添加根组件
    /// </summary>
    public async Task<T> AddRootComponent<T>(Dictionary<string, object>? parameters = null) where T : IComponent
    {
        return (T)await AddComponent(typeof(T), parameters);
    }
    
    /// <summary>
    /// 添加组件
    /// </summary>
    public async Task<IComponent> AddComponent(Type componentType, Dictionary<string, object>? parameters = null)
    {
        return await Dispatcher.InvokeAsync(async () =>
        {
            var component = InstantiateComponent(componentType);
            var componentId = AssignRootComponentId(component);
            
            _rootComponents.Add((componentId, component));
            
            // 创建根适配器，传入根元素作为 knownTargetElement
            var rootAdapter = new EclipseComponentAdapter(this, null, knownTargetElement: RootElementHandler)
            {
                Name = $"RootAdapter for {componentType.Name}",
            };
            
            RegisterComponentAdapter(rootAdapter, componentId);
            
            var parameterView = parameters?.Count > 0 
                ? ParameterView.FromDictionary(parameters) 
                : ParameterView.Empty;
            
            await RenderRootComponentAsync(componentId, parameterView);
            
            return component;
        });
    }
    
    /// <summary>
    /// 注册组件适配器
    /// </summary>
    internal void RegisterComponentAdapter(EclipseComponentAdapter adapter, int componentId)
    {
        _componentAdapters[componentId] = adapter;
    }
    
    /// <summary>
    /// 更新显示
    /// </summary>
    protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
    {
        var adaptersWithPendingEdits = new HashSet<EclipseComponentAdapter>();
        
        for (int i = 0; i < renderBatch.UpdatedComponents.Count; i++)
        {
            var updatedComponent = renderBatch.UpdatedComponents.Array[i];
            if (updatedComponent.Edits.Count > 0)
            {
                var adapter = _componentAdapters[updatedComponent.ComponentId];
                adapter.ApplyEdits(updatedComponent.ComponentId, updatedComponent.Edits, renderBatch, adaptersWithPendingEdits);
            }
        }
        
        foreach (var adapter in adaptersWithPendingEdits.OrderByDescending(a => a.DeepLevel))
        {
            adapter.ApplyPendingEdits();
        }
        
        for (int i = 0; i < renderBatch.DisposedComponentIDs.Count; i++)
        {
            var disposedComponentId = renderBatch.DisposedComponentIDs.Array[i];
            if (_componentAdapters.Remove(disposedComponentId, out var adapter))
            {
                adapter.Dispose();
            }
        }
        
        OnRenderRequested?.Invoke();
        
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// 执行渲染
    /// </summary>
    public void PerformRender()
    {
        if (Canvas == null || RootElement == null) return;
        
        Canvas.Clear(SKColors.White);
        
        RootElement.Measure(Canvas, SurfaceWidth, SurfaceHeight);
        RootElement.Arrange(Canvas, 0, 0, SurfaceWidth, SurfaceHeight);
        
        RootElement.Render(Canvas);
    }
    
    /// <summary>
    /// 处理点击事件
    /// </summary>
    public bool HandleClick(float x, float y)
    {
        if (RootElement == null) return false;
        
        var point = new SKPoint(x, y);
        var handled = RootElement.HandleClick(point);
        return handled;
    }
}
