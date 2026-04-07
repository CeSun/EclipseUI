using System;
using System.Collections.Generic;
using Eclipse.Core;

namespace Eclipse.Input;

/// <summary>
/// 焦点管理器
/// </summary>
public sealed class FocusManager
{
    private IInputElement? _focusedElement;
    private IInputElement? _focusScope;
    
    /// <summary>
    /// 焦点变化事件
    /// </summary>
    public event EventHandler<FocusChangedEventArgs>? FocusChanged;
    
    /// <summary>
    /// 当前聚焦的元素
    /// </summary>
    public IInputElement? FocusedElement => _focusedElement;
    
    /// <summary>
    /// 当前焦点范围
    /// </summary>
    public IInputElement? FocusScope => _focusScope;
    
    /// <summary>
    /// 设置焦点
    /// </summary>
    public bool SetFocus(IInputElement? element)
    {
        // 相同元素，不做处理
        if (_focusedElement == element)
            return true;
        
        // 验证元素
        if (element != null)
        {
            if (!element.IsVisible || !element.IsFocusable)
                return false;
        }
        
        var oldFocus = _focusedElement;
        
        // 移除旧焦点
        if (oldFocus != null)
        {
            SetIsFocused(oldFocus, false);
        }
        
        // 设置新焦点
        _focusedElement = element;
        
        if (element != null)
        {
            SetIsFocused(element, true);
        }
        
        // 触发事件
        OnFocusChanged(oldFocus, element);
        
        return true;
    }
    
    /// <summary>
    /// 清除焦点
    /// </summary>
    public void ClearFocus()
    {
        SetFocus(null);
    }
    
    /// <summary>
    /// 焦点向后移动
    /// </summary>
    public bool MoveFocusForward()
    {
        if (_focusedElement == null)
            return false;
        
        var next = GetNextFocusable(_focusedElement, forward: true);
        if (next != null && next != _focusedElement)
        {
            return SetFocus(next);
        }
        
        return false;
    }
    
    /// <summary>
    /// 焦点向前移动
    /// </summary>
    public bool MoveFocusBackward()
    {
        if (_focusedElement == null)
            return false;
        
        var prev = GetNextFocusable(_focusedElement, forward: false);
        if (prev != null && prev != _focusedElement)
        {
            return SetFocus(prev);
        }
        
        return false;
    }
    
    /// <summary>
    /// 设置焦点范围
    /// </summary>
    public void SetFocusScope(IInputElement? scope)
    {
        _focusScope = scope;
    }
    
    /// <summary>
    /// 在指定范围内查找第一个可聚焦元素
    /// </summary>
    public IInputElement? GetFirstFocusable(IInputElement scope)
    {
        return FindFirstFocusable(scope);
    }
    
    // === 私有方法 ===
    
    private void OnFocusChanged(IInputElement? oldFocus, IInputElement? newFocus)
    {
        FocusChanged?.Invoke(this, new FocusChangedEventArgs(oldFocus, newFocus));
    }
    
    private void SetIsFocused(IInputElement element, bool focused)
    {
        if (element is ComponentBase inputElement)
        {
            inputElement.SetIsFocused(focused);
        }
    }
    
    private IInputElement? GetNextFocusable(IInputElement current, bool forward)
    {
        // 获取所有可聚焦元素
        var focusables = new List<IInputElement>();
        CollectFocusables(_focusScope, focusables);
        
        if (focusables.Count == 0)
            return null;
        
        var index = focusables.IndexOf(current);
        if (index < 0)
            return focusables[0];
        
        if (forward)
        {
            index = (index + 1) % focusables.Count;
        }
        else
        {
            index = (index - 1 + focusables.Count) % focusables.Count;
        }
        
        return focusables[index];
    }
    
    private void CollectFocusables(IInputElement? root, List<IInputElement> result)
    {
        if (root == null)
            return;
        
        if (root.IsVisible && root.IsFocusable)
        {
            result.Add(root);
        }
        
        foreach (var child in root.Children)
        {
            CollectFocusables(child, result);
        }
    }
    
    private IInputElement? FindFirstFocusable(IInputElement element)
    {
        if (element.IsVisible && element.IsFocusable)
            return element;
        
        foreach (var child in element.Children)
        {
            var result = FindFirstFocusable(child);
            if (result != null)
                return result;
        }
        
        return null;
    }
}

/// <summary>
/// 焦点变化事件参数
/// </summary>
public class FocusChangedEventArgs : EventArgs
{
    public IInputElement? OldFocus { get; }
    public IInputElement? NewFocus { get; }
    
    public FocusChangedEventArgs(IInputElement? oldFocus, IInputElement? newFocus)
    {
        OldFocus = oldFocus;
        NewFocus = newFocus;
    }
}

/// <summary>
/// InputElementBase 的扩展方法
/// </summary>
public static partial class InputElementExtensions
{
    // SetIsFocused 由 InputElementBase 内部调用
}