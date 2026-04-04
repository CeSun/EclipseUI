using System;

namespace Eclipse.Input;

/// <summary>
/// 指针类型
/// </summary>
public enum PointerType
{
    /// <summary>
    /// 鼠标或触控板
    /// </summary>
    Mouse,
    
    /// <summary>
    /// 触摸屏手指
    /// </summary>
    Touch,
    
    /// <summary>
    /// 触控笔/手写笔
    /// </summary>
    Pen
}

/// <summary>
/// 指针按键
/// </summary>
[Flags]
public enum PointerButtons
{
    None = 0,
    Left = 1,
    Right = 2,
    Middle = 4,
    XButton1 = 8,
    XButton2 = 16
}

/// <summary>
/// 键盘修饰键
/// </summary>
[Flags]
public enum KeyModifiers
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Windows = 8
}