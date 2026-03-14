namespace EclipseUI.Layout;

/// <summary>
/// 指定元素在 DockPanel 中的停靠位置
/// </summary>
public enum Dock
{
    /// <summary>
    /// 停靠在顶部
    /// </summary>
    Top,
    
    /// <summary>
    /// 停靠在底部
    /// </summary>
    Bottom,
    
    /// <summary>
    /// 停靠在左侧
    /// </summary>
    Left,
    
    /// <summary>
    /// 停靠在右侧
    /// </summary>
    Right,
    
    /// <summary>
    /// 填充剩余空间（只能有一个元素使用此值）
    /// </summary>
    Fill
}
