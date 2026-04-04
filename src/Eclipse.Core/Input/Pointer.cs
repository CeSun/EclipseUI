using System;
using System.Collections.Generic;

namespace Eclipse.Input;

/// <summary>
/// 指针实例 - 代表一个输入设备
/// </summary>
public sealed class Pointer
{
    private static readonly Dictionary<int, Pointer> _pointers = new();
    
    /// <summary>
    /// 指针唯一标识
    /// </summary>
    public int Id { get; }
    
    /// <summary>
    /// 指针类型
    /// </summary>
    public PointerType Type { get; }
    
    /// <summary>
    /// 当前捕获的元素
    /// </summary>
    public IInputElement? Captured { get; private set; }
    
    /// <summary>
    /// 是否已被捕获
    /// </summary>
    public bool IsCaptured => Captured != null;
    
    private Pointer(int id, PointerType type)
    {
        Id = id;
        Type = type;
    }
    
    /// <summary>
    /// 捕获指针 - 所有后续事件都发送到指定元素
    /// </summary>
    public void Capture(IInputElement? element)
    {
        Captured = element;
    }
    
    /// <summary>
    /// 获取或创建指针实例
    /// </summary>
    public static Pointer GetOrCreate(int id, PointerType type)
    {
        if (!_pointers.TryGetValue(id, out var pointer))
        {
            pointer = new Pointer(id, type);
            _pointers[id] = pointer;
        }
        return pointer;
    }
    
    /// <summary>
    /// 获取鼠标指针 (ID = 0)
    /// </summary>
    public static Pointer Mouse => GetOrCreate(0, PointerType.Mouse);
    
    /// <summary>
    /// 移除指针 (触摸/笔离开时)
    /// </summary>
    public static void Remove(int id)
    {
        _pointers.Remove(id);
    }
    
    /// <summary>
    /// 清除所有指针
    /// </summary>
    public static void Clear()
    {
        _pointers.Clear();
    }
    
    public override string ToString() => $"Pointer({Id}, {Type})";
}