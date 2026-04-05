using System;
using Eclipse.Core;
using Eclipse.Core.Abstractions;

namespace Eclipse.Windows;

/// <summary>
/// 应用程序入口
/// </summary>
public static class Application
{
    /// <summary>
    /// 运行指定的 EUI 组件（泛型方式）
    /// </summary>
    /// <typeparam name="T">EUI 组件类型</typeparam>
    public static void Run<T>() where T : ComponentBase, new()
    {
        var rootComponent = BuildComponent<T>();
        Run(rootComponent);
    }

    /// <summary>
    /// 运行指定的 EUI 组件实例
    /// </summary>
    /// <param name="component">EUI 组件实例</param>
    public static void Run(ComponentBase component)
    {
        var rootComponent = BuildComponent(component);
        Run(rootComponent);
    }

    /// <summary>
    /// 运行已构建好的根组件
    /// </summary>
    /// <param name="rootComponent">根组件</param>
    public static void Run(IComponent rootComponent)
    {
        using var window = new WindowImpl
        {
            Content = rootComponent
        };

        window.ShowDialog();
    }

    /// <summary>
    /// 构建 EUI 组件并返回根组件
    /// </summary>
    private static IComponent BuildComponent<T>() where T : ComponentBase, new()
    {
        var component = new T();
        return BuildComponent(component);
    }

    /// <summary>
    /// 构建 EUI 组件实例并返回根组件
    /// </summary>
    private static IComponent BuildComponent(ComponentBase component)
    {
        var context = new BuildContext();
        component.Build(context);
        // 返回 component 本身，而不是 context.RootComponent
        // 这样 Rebuild() 会调用 component.Build()，而不是子组件的 Build()
        return component;
    }
}