using System;
using Eclipse.Core;
using Eclipse.Core.Abstractions;
using Eclipse.Input;
using Eclipse.Skia;
using Eclipse.Windows.Rendering;

namespace Eclipse.Windows;

/// <summary>
/// 应用程序入口（简化模式）
/// </summary>
public static class Application
{
    /// <summary>
    /// 运行指定的 EUI 组件（泛型方式）
    /// </summary>
    /// <typeparam name="T">EUI 组件类型</typeparam>
    public static void Run<T>() where T : ComponentBase, new()
    {
        var app = AppBuilder.Create()
            .UseBackend(WindowImpl.RenderBackend.Angle)
            .Build();
        app.Run<T>();
    }

    /// <summary>
    /// 运行指定的 EUI 组件实例
    /// </summary>
    /// <param name="component">EUI 组件实例</param>
    public static void Run(ComponentBase component)
    {
        var app = AppBuilder.Create()
            .UseBackend(WindowImpl.RenderBackend.Angle)
            .Build();
        app.Run(component);
    }

    /// <summary>
    /// 运行已构建好的根组件
    /// </summary>
    /// <param name="rootComponent">根组件</param>
    public static void Run(IComponent rootComponent)
    {
        var inputManager = new InputManager();
        var renderer = new ComponentRenderer(inputManager);
        
        using var window = new WindowImpl(WindowImpl.RenderBackend.Angle, inputManager, renderer)
        {
            Content = rootComponent
        };

        window.ShowDialog();
    }
}