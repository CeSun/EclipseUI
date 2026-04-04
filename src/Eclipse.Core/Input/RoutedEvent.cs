using System;

namespace Eclipse.Input;

/// <summary>
/// 路由策略
/// </summary>
public enum RoutingStrategy
{
    /// <summary>
    /// 直接事件 - 仅在源元素触发
    /// </summary>
    Direct,
    
    /// <summary>
    /// 冒泡 - 从源元素向上传播到根
    /// </summary>
    Bubble,
    
    /// <summary>
    /// 隧道 - 从根向下传播到源元素
    /// </summary>
    Tunnel
}

/// <summary>
/// 路由事件
/// </summary>
public sealed class RoutedEvent
{
    /// <summary>
    /// 事件名称
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// 路由策略
    /// </summary>
    public RoutingStrategy RoutingStrategy { get; }
    
    /// <summary>
    /// 事件参数类型
    /// </summary>
    public Type EventArgsType { get; }
    
    /// <summary>
    /// 所属类型
    /// </summary>
    public Type OwnerType { get; }
    
    private RoutedEvent(string name, RoutingStrategy strategy, Type eventArgsType, Type ownerType)
    {
        Name = name;
        RoutingStrategy = strategy;
        EventArgsType = eventArgsType;
        OwnerType = ownerType;
    }
    
    /// <summary>
    /// 注册路由事件
    /// </summary>
    public static RoutedEvent Register<TOwner, TArgs>(
        string name, 
        RoutingStrategy routingStrategy)
        where TArgs : RoutedEventArgs, new()
    {
        return new RoutedEvent(name, routingStrategy, typeof(TArgs), typeof(TOwner));
    }
    
    public override string ToString() => $"{Name} ({RoutingStrategy})";
}

/// <summary>
/// 路由事件泛型版本
/// </summary>
public sealed class RoutedEvent<TArgs> where TArgs : RoutedEventArgs, new()
{
    public RoutedEvent InnerEvent { get; }
    
    public string Name => InnerEvent.Name;
    public RoutingStrategy RoutingStrategy => InnerEvent.RoutingStrategy;
    public Type OwnerType => InnerEvent.OwnerType;
    
    private RoutedEvent(RoutedEvent innerEvent)
    {
        InnerEvent = innerEvent;
    }
    
    public static RoutedEvent<TArgs> Register<TOwner>(
        string name, 
        RoutingStrategy routingStrategy)
    {
        var inner = RoutedEvent.Register<TOwner, TArgs>(name, routingStrategy);
        return new RoutedEvent<TArgs>(inner);
    }
    
    public static implicit operator RoutedEvent(RoutedEvent<TArgs> e) => e.InnerEvent;
}