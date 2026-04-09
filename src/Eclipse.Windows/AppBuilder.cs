using Eclipse.Core;
using Eclipse.Core.Abstractions;
using Eclipse.Input;
using Microsoft.Extensions.DependencyInjection;

namespace Eclipse.Windows;

/// <summary>
/// 后端配置
/// </summary>
public class BackendConfig
{
    public WindowImpl.RenderBackend Backend { get; set; } = WindowImpl.RenderBackend.Angle;
}

/// <summary>
/// Windows 平台应用构建器
/// </summary>
public class AppBuilder : AppBuilderBase
{
    private WindowImpl.RenderBackend _backend = WindowImpl.RenderBackend.Angle;
    
    /// <summary>
    /// 设置渲染后端
    /// </summary>
    public AppBuilder UseBackend(WindowImpl.RenderBackend backend)
    {
        _backend = backend;
        return this;
    }
    
    protected override void ConfigureDefaultServices(IServiceCollection services)
    {
        base.ConfigureDefaultServices(services);
        
        // 后端配置
        services.AddSingleton(new BackendConfig { Backend = _backend });
        
        // Windows 平台服务
        services.AddSingleton<IClipboard, WindowsClipboard>();
    }
    
    public override IApp Build()
    {
        var serviceProvider = _services.BuildServiceProvider();
        return new App(serviceProvider);
    }
    
    /// <summary>
    /// 创建应用构建器
    /// </summary>
    public static AppBuilder Create()
    {
        return new AppBuilder();
    }
}

/// <summary>
/// Windows 平台应用
/// </summary>
public class App : IApp
{
    private readonly IServiceProvider _services;
    
    public IServiceProvider Services => _services;
    
    public App(IServiceProvider services)
    {
        _services = services;
    }
    
    public void Run<T>() where T : ComponentBase, new()
    {
        var component = new T();
        Run(component);
    }
    
    public void Run(ComponentBase component)
    {
        var rootComponent = BuildComponent(component);
        
        var inputManager = _services.GetRequiredService<InputManager>();
        var backendConfig = _services.GetRequiredService<BackendConfig>();
        
        using var window = new WindowImpl(backendConfig.Backend, inputManager)
        {
            Content = rootComponent
        };
        
        window.ShowDialog();
    }
    
    private IComponent BuildComponent(ComponentBase component)
    {
        var context = new BuildContext(component);
        component.Build(context);
        return component;
    }
}