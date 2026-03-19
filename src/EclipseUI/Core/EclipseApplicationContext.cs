using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace EclipseUI.Core;

/// <summary>
/// EclipseUI 应用上下�?/// </summary>
public class EclipseApplicationContext
{
    /// <summary>
    /// 服务提供�?    /// </summary>
    public IServiceProvider Services { get; }
    
    /// <summary>
    /// 渲染�?    /// </summary>
    public EclipseRenderer Renderer { get; }
    
    /// <summary>
    /// 根组�?    /// </summary>
    public IComponent? RootComponent { get; private set; }
    
    /// <summary>
    /// 表面宽度
    /// </summary>
    public int Width { get; private set; }
    
    /// <summary>
    /// 表面高度
    /// </summary>
    public int Height { get; private set; }
    
    /// <summary>
    /// 需要重新渲�?    /// </summary>
    public event Action? RenderRequested;
    
    public EclipseApplicationContext(IServiceProvider services, EclipseRenderer renderer)
    {
        Services = services;
        Renderer = renderer;
        
        Renderer.OnRenderRequested += () => RenderRequested?.Invoke();
    }
    
    /// <summary>
    /// 设置表面尺寸
    /// </summary>
    public void SetSurfaceSize(int width, int height)
    {
        Width = width;
        Height = height;
    }
    
    /// <summary>
    /// 运行应用
    /// </summary>
    public async Task RunAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TComponent>(Dictionary<string, object>? parameters = null) where TComponent : IComponent
    {
        RootComponent = await Renderer.AddRootComponent<TComponent>(parameters);
    }
    
    /// <summary>
    /// 处理点击
    /// </summary>
    public bool HandleClick(float x, float y)
    {
        return Renderer.HandleClick(x, y);
    }
    
    /// <summary>
    /// 执行渲染
    /// </summary>
    public void Render()
    {
        Renderer.PerformRender();
    }
}

/// <summary>
/// 应用构建�?/// </summary>
public class EclipseApplicationBuilder
{
    private readonly IServiceCollection _services = new ServiceCollection();
    
    /// <summary>
    /// 配置服务
    /// </summary>
    public EclipseApplicationBuilder ConfigureServices(Action<IServiceCollection> configure)
    {
        configure(_services);
        return this;
    }
    
    /// <summary>
    /// 构建应用
    /// </summary>
    public EclipseApplicationContext Build()
    {
        // 添加核心服务
        _services.AddLogging();
        _services.AddSingleton<EclipseApplicationContext>();
        _services.AddSingleton<EclipseRenderer>();
        
        var services = _services.BuildServiceProvider();
        var renderer = services.GetRequiredService<EclipseRenderer>();
        var context = services.GetRequiredService<EclipseApplicationContext>();
        
        return context;
    }
}
