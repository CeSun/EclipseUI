using System;
using System.Collections.Generic;

namespace Eclipse.Input;

/// <summary>
/// 事件路由器 - 处理路由事件的传播
/// </summary>
internal static class EventRouter
{
    /// <summary>
    /// 路由事件
    /// </summary>
    public static void RaiseEvent(IInputElement source, RoutedEventArgs e)
    {
        if (e.RoutedEvent == null)
            return;
        
        Console.WriteLine($"[EventRouter.RaiseEvent] event={e.RoutedEvent.Name}, source={source.GetType().Name}, strategy={e.RoutedEvent.RoutingStrategy}");
        
        var routedEvent = e.RoutedEvent;
        e.OriginalSource = source;
        
        switch (routedEvent.RoutingStrategy)
        {
            case RoutingStrategy.Direct:
                // 直接事件 - 仅处理源元素
                InvokeHandlers(source, e);
                break;
                
            case RoutingStrategy.Bubble:
                // 冒泡 - 从源到根
                RouteAndInvoke(source, e, bubble: true);
                break;
                
            case RoutingStrategy.Tunnel:
                // 隧道 - 从根到源
                RouteAndInvoke(source, e, bubble: false);
                break;
        }
    }
    
    private static void RouteAndInvoke(IInputElement source, RoutedEventArgs e, bool bubble)
    {
        // 构建路由
        var route = BuildRoute(source);
        
        if (!bubble)
        {
            // Tunnel 从根开始
            route.Reverse();
        }
        
        // 沿路由调用处理器
        foreach (var element in route)
        {
            if (e.Handled)
                break;
                
            e.Source = element;
            InvokeHandlers(element, e);
        }
    }
    
    private static List<IInputElement> BuildRoute(IInputElement source)
    {
        var route = new List<IInputElement>();
        var current = source;
        
        while (current != null)
        {
            route.Add(current);
            current = current.Parent;
        }
        
        return route;
    }
    
    private static void InvokeHandlers(IInputElement element, RoutedEventArgs e)
    {
        if (element is InputElementBase inputElement)
        {
            inputElement.InvokeHandlersInternal(e);
        }
    }
}