using Eclipse.Core.Abstractions;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Eclipse.Core;

/// <summary>
/// 附加属性定义（类型安全）
/// </summary>
public readonly struct AttachedProperty<T>
{
    public string Name { get; }
    public T DefaultValue { get; }
    
    public AttachedProperty(string name, T defaultValue = default!)
    {
        Name = name;
        DefaultValue = defaultValue;
    }
    
    public static implicit operator string(AttachedProperty<T> prop) => prop.Name;
}

/// <summary>
/// 附加属性存储和扩展方法
/// </summary>
public static class AttachedProperty
{
    private static readonly ConditionalWeakTable<IComponent, Dictionary<string, object?>> _storage = new();
    
    /// <summary>
    /// 获取附加属性值
    /// </summary>
    public static T Get<T>(this IComponent target, AttachedProperty<T> property)
    {
        if (_storage.TryGetValue(target, out var dict) && dict.TryGetValue(property.Name, out var value))
            return (T)value!;
        return property.DefaultValue;
    }
    
    /// <summary>
    /// 设置附加属性值
    /// </summary>
    public static void Set<T>(this IComponent target, AttachedProperty<T> property, T value)
    {
        var dict = _storage.GetOrCreateValue(target);
        dict[property.Name] = value;
    }
    
    /// <summary>
    /// 检查是否有附加属性值
    /// </summary>
    public static bool HasValue<T>(this IComponent target, AttachedProperty<T> property)
    {
        return _storage.TryGetValue(target, out var dict) && dict.ContainsKey(property.Name);
    }
    
    /// <summary>
    /// 清除附加属性值
    /// </summary>
    public static void Clear<T>(this IComponent target, AttachedProperty<T> property)
    {
        if (_storage.TryGetValue(target, out var dict))
        {
            dict.Remove(property.Name);
        }
    }
}