namespace Eclipse.Rendering;

/// <summary>
/// 图片拉伸模式
/// </summary>
public enum Stretch
{
    /// <summary>
    /// 不拉伸，保持原始尺寸
    /// </summary>
    None,
    
    /// <summary>
    /// 填充整个区域，可能改变比例
    /// </summary>
    Fill,
    
    /// <summary>
    /// 保持比例缩放以适应区域（可能留空白）
    /// </summary>
    Uniform,
    
    /// <summary>
    /// 保持比例缩放以填充区域（可能裁剪）
    /// </summary>
    UniformToFill
}