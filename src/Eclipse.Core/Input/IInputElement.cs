using System;
using System.Collections.Generic;

namespace Eclipse.Input;

/// <summary>
/// 可接收输入的元素
/// </summary>
public interface IInputElement
{
    /// <summary>
    /// 是否启用输入
    /// </summary>
    bool IsInputEnabled { get; }
    
    /// <summary>
    /// 是否可见
    /// </summary>
    bool IsVisible { get; }
    
    /// <summary>
    /// 是否可命中测试
    /// </summary>
    bool IsHitTestVisible { get; }
    
    /// <summary>
    /// 父元素
    /// </summary>
    IInputElement? Parent { get; }
    
    /// <summary>
    /// 子元素
    /// </summary>
    IEnumerable<IInputElement> Children { get; }
    
    /// <summary>
    /// 边界矩形
    /// </summary>
    Rect Bounds { get; }
    
    /// <summary>
    /// 命中测试
    /// </summary>
    bool HitTest(Point point);
    
    /// <summary>
    /// 添加事件处理器
    /// </summary>
    void AddHandler(RoutedEvent routedEvent, Delegate handler);
    
    /// <summary>
    /// 移除事件处理器
    /// </summary>
    void RemoveHandler(RoutedEvent routedEvent, Delegate handler);
    
    /// <summary>
    /// 触发事件
    /// </summary>
    void RaiseEvent(RoutedEventArgs e);
    
    // === 焦点 ===
    
    /// <summary>
    /// 是否可聚焦
    /// </summary>
    bool IsFocusable { get; }
    
    /// <summary>
    /// 是否聚焦
    /// </summary>
    bool IsFocused { get; }
    
    /// <summary>
    /// 聚焦
    /// </summary>
    bool Focus();
    
    // === 指针捕获 ===
    
    /// <summary>
    /// 捕获指针
    /// </summary>
    void CapturePointer(Pointer pointer);
    
    /// <summary>
    /// 释放指针捕获
    /// </summary>
    void ReleasePointerCapture(Pointer pointer);
}