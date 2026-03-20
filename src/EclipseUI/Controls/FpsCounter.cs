using Microsoft.AspNetCore.Components;
using System.Diagnostics;

namespace EclipseUI.Controls;

/// <summary>
/// 帧率计数器组件 - 显示当前渲染帧率
/// </summary>
public class FpsCounter : ComponentBase
{
    /// <summary>
    /// 是否显示帧率（默认：true）
    /// </summary>
    [Parameter]
    public bool IsVisible { get; set; } = true;
    
    /// <summary>
    /// 帧率文本颜色（默认：红色）
    /// </summary>
    [Parameter]
    public string Color { get; set; } = "#FF0000";
    
    /// <summary>
    /// 字体大小（默认：14）
    /// </summary>
    [Parameter]
    public int FontSize { get; set; } = 14;
    
    /// <summary>
    /// 位置 X 坐标（默认：10，左上角）
    /// </summary>
    [Parameter]
    public int X { get; set; } = 10;
    
    /// <summary>
    /// 位置 Y 坐标（默认：10，左上角）
    /// </summary>
    [Parameter]
    public int Y { get; set; } = 10;
}