using Microsoft.Extensions.DependencyInjection;

namespace Eclipse.Core;

/// <summary>
/// 应用构建器接口
/// </summary>
public interface IAppBuilder
{
    /// <summary>
    /// 服务集合
    /// </summary>
    IServiceCollection Services { get; }
    
    /// <summary>
    /// 配置服务
    /// </summary>
    IAppBuilder ConfigureServices(Action<IServiceCollection> configure);
    
    /// <summary>
    /// 构建应用
    /// </summary>
    IApp Build();
}

/// <summary>
/// 应用接口
/// </summary>
public interface IApp : IAppHost
{
    /// <summary>
    /// 运行应用
    /// </summary>
    void Run<T>() where T : ComponentBase, new();
    
    /// <summary>
    /// 运行指定组件
    /// </summary>
    void Run(ComponentBase component);
}

/// <summary>
/// 应用宿主接口 - 提供服务访问
/// </summary>
public interface IAppHost
{
    /// <summary>
    /// 服务提供者
    /// </summary>
    IServiceProvider Services { get; }
}

/// <summary>
/// 应用构建器基类
/// </summary>
public abstract class AppBuilderBase : IAppBuilder
{
    protected IServiceCollection _services;
    
    public IServiceCollection Services => _services;
    
    protected AppBuilderBase()
    {
        _services = new ServiceCollection();
        ConfigureDefaultServices(_services);
    }
    
    /// <summary>
    /// 配置默认服务
    /// </summary>
    protected virtual void ConfigureDefaultServices(IServiceCollection services)
    {
        // 核心服务
        services.AddSingleton<Eclipse.Input.InputManager>();
        services.AddSingleton<Eclipse.Input.FocusManager>();
    }
    
    public IAppBuilder ConfigureServices(Action<IServiceCollection> configure)
    {
        configure(_services);
        return this;
    }
    
    public abstract IApp Build();
}