using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using EclipseUI.Layout;
using System.Diagnostics.CodeAnalysis;
using EclipseUI.Controls;
using System.Diagnostics;

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
        
        // 初始化 FPS 计数器
        FpsCounterElement = new FpsCounterElement();
        
        // 初始化 Popup 服务
        PopupService = new PopupService();
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
    /// FPS 计数器元素（用于帧率显示）
    /// </summary>
    public FpsCounterElement? FpsCounterElement { get; private set; }
    
    /// <summary>
    /// 根元素处理器
    /// </summary>
    internal RootElementHandler RootElementHandler { get; } = new RootElementHandler();
    
    /// <summary>
    /// Popup 服务
    /// </summary>
    public PopupService PopupService { get; }
    
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
    public async Task<T> AddRootComponent<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(Dictionary<string, object>? parameters = null) where T : IComponent
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
        
        // 渲染 FPS 计数器（在 UI 上方）
        FpsCounterElement?.Render(Canvas);
        
        // 渲染所有 Popup（在最上层）
        PopupService.Render(Canvas);
    }
    
    /// <summary>
    /// 处理点击事件
    /// </summary>
    public bool HandleClick(float x, float y)
    {
        if (RootElement == null) return false;
        
        var point = new SKPoint(x, y);
        
        // 优先处理 Popup 层的点击
        if (PopupService.HandleClick(point))
        {
            return true;
        }
        
        var handled = RootElement.HandleClick(point);
        
        // 检查是否点击了 TextBox
        var tb = FindTextBox(RootElement, x, y);
        SetFocus(tb);
        
        return handled;
    }
    
    /// <summary>
    /// 处理鼠标滚轮事件
    /// </summary>
    public bool HandleMouseWheel(float x, float y, float deltaY)
    {
        if (RootElement == null) return false;
        
        // 优先处理 Popup 层的滚轮
        if (PopupService.HandleMouseWheel(x, y, deltaY))
        {
            return true;
        }
        
        // 从根元素开始分发滚轮事件，根据鼠标位置找到对应的可滚动元素
        return RootElement.HandleMouseWheel(x, y, deltaY);
    }
    
    /// <summary>
    /// 处理鼠标按下事件
    /// </summary>
    public bool HandleMouseDown(float x, float y)
    {
        if (RootElement == null) return false;
        return RootElement.HandleMouseDown(x, y);
    }
    
    /// <summary>
    /// 处理鼠标移动事件
    /// </summary>
    public bool HandleMouseMove(float x, float y)
    {
        if (RootElement == null) return false;
        
        // 处理 Popup 层的鼠标移动
        PopupService.HandleMouseMove(x, y);
        
        return RootElement.HandleMouseMove(x, y);
    }
    
    /// <summary>
    /// 处理鼠标释放事件
    /// </summary>
    public void HandleMouseUp()
    {
        RootElement?.HandleMouseUp();
    }
    
    /// <summary>
    /// 处理鼠标离开窗口事件
    /// </summary>
    public void HandleMouseLeave()
    {
        RootElement?.HandleMouseLeave();
    }
    
    /// <summary>
    /// 查找第一个 ScrollView
    /// </summary>
    private ScrollViewElement? FindScrollView(EclipseElement element)
    {
        if (element is ScrollViewElement scrollView)
            return scrollView;
        
        foreach (var child in element.Children)
        {
            var result = FindScrollView(child);
            if (result != null)
                return result;
        }
        
        return null;
    }
    
    /// <summary>
    /// 当前获得焦点的元素
    /// </summary>
    public TextBoxElement? FocusedElement { get; private set; }
    
    /// <summary>
    /// 处理文本输入事件
    /// </summary>
    public async Task HandleTextInput(string text)
    {
        if (FocusedElement != null)
        {
            await FocusedElement.HandleTextInput(text);
            OnRenderRequested?.Invoke();
        }
    }
    
    /// <summary>
    /// 处理按键按下事件
    /// </summary>
    public async Task HandleKeyDown(string key)
    {
        if (FocusedElement != null)
        {
            await FocusedElement.HandleKeyDown(key);
            OnRenderRequested?.Invoke();
        }
    }
    
    /// <summary>
    /// 设置焦点元素
    /// </summary>
    public void SetFocus(TextBoxElement? element)
    {
        // 清除之前的焦点
        if (FocusedElement != null && FocusedElement != element)
        {
            FocusedElement.Blur();
        }
        
        FocusedElement = element;
        
        if (FocusedElement != null)
        {
            FocusedElement.ResetCaretBlink();
        }
        
        OnRenderRequested?.Invoke();
    }
    
    /// <summary>
    /// 查找 TextBox 元素
    /// </summary>
    private TextBoxElement? FindTextBox(EclipseElement element, float x, float y)
    {
        if (element is TextBoxElement textBox)
        {
            var rect = new SKRect(textBox.X, textBox.Y, textBox.X + textBox.Width, textBox.Y + textBox.Height);
            if (rect.Contains(new SKPoint(x, y)))
                return textBox;
        }
        
        foreach (var child in element.Children)
        {
            var result = FindTextBox(child, x, y);
            if (result != null)
                return result;
        }
        
        return null;
    }
}
