using System;

namespace Eclipse.Input;

/// <summary>
/// 指针点信息
/// </summary>
public readonly struct PointerPoint
{
    /// <summary>
    /// 指针实例
    /// </summary>
    public Pointer Pointer { get; init; }
    
    /// <summary>
    /// 相对于指定元素的位置
    /// </summary>
    public Point Position { get; init; }
    
    /// <summary>
    /// 指针属性
    /// </summary>
    public PointerPointProperties Properties { get; init; }
    
    /// <summary>
    /// 时间戳 (毫秒)
    /// </summary>
    public ulong Timestamp { get; init; }
}

/// <summary>
/// 指针属性
/// </summary>
public readonly struct PointerPointProperties
{
    // === 按键状态 ===
    
    /// <summary>
    /// 左键是否按下
    /// </summary>
    public bool IsLeftButtonPressed { get; init; }
    
    /// <summary>
    /// 右键是否按下
    /// </summary>
    public bool IsRightButtonPressed { get; init; }
    
    /// <summary>
    /// 中键是否按下
    /// </summary>
    public bool IsMiddleButtonPressed { get; init; }
    
    /// <summary>
    /// X1 按钮是否按下
    /// </summary>
    public bool IsXButton1Pressed { get; init; }
    
    /// <summary>
    /// X2 按钮是否按下
    /// </summary>
    public bool IsXButton2Pressed { get; init; }
    
    // === Pen 专用属性 ===
    
    /// <summary>
    /// 压感 (0-1)
    /// </summary>
    public float Pressure { get; init; }
    
    /// <summary>
    /// X 轴倾斜角度
    /// </summary>
    public float XTilt { get; init; }
    
    /// <summary>
    /// Y 轴倾斜角度
    /// </summary>
    public float YTilt { get; init; }
    
    /// <summary>
    /// 是否是橡皮擦端
    /// </summary>
    public bool IsEraser { get; init; }
    
    /// <summary>
    /// 笔杆按钮是否按下
    /// </summary>
    public bool IsBarrelButtonPressed { get; init; }
    
    // === 便捷方法 ===
    
    /// <summary>
    /// 获取按下的按键
    /// </summary>
    public PointerButtons GetPressedButtons()
    {
        var buttons = PointerButtons.None;
        if (IsLeftButtonPressed) buttons |= PointerButtons.Left;
        if (IsRightButtonPressed) buttons |= PointerButtons.Right;
        if (IsMiddleButtonPressed) buttons |= PointerButtons.Middle;
        if (IsXButton1Pressed) buttons |= PointerButtons.XButton1;
        if (IsXButton2Pressed) buttons |= PointerButtons.XButton2;
        return buttons;
    }
    
    /// <summary>
    /// 默认属性 (无按键按下)
    /// </summary>
    public static PointerPointProperties Default => new();
}