using Eclipse.Core.Abstractions;
using Eclipse.Input;

namespace Eclipse.Rendering;

/// <summary>
/// 绘制上下文接口 - 控件通过此接口绘制
/// </summary>
public interface IDrawingContext
{
    /// <summary>
    /// 缩放因子
    /// </summary>
    double Scale { get; }
    
    /// <summary>
    /// 宽度
    /// </summary>
    double Width { get; }
    
    /// <summary>
    /// 高度
    /// </summary>
    double Height { get; }
    
    /// <summary>
    /// 清空画布
    /// </summary>
    void Clear(System.Drawing.Color color);
    
    /// <summary>
    /// 绘制矩形
    /// </summary>
    void DrawRectangle(Rect bounds, System.Drawing.Color fillColor, System.Drawing.Color? strokeColor = null, double strokeWidth = 0, double cornerRadius = 0);
    
    /// <summary>
    /// 绘制圆角矩形
    /// </summary>
    void DrawRoundRect(Rect bounds, System.Drawing.Color fillColor, double cornerRadius);
    
    /// <summary>
    /// 绘制线条
    /// </summary>
    void DrawLine(double x1, double y1, double x2, double y2, System.Drawing.Color color, double strokeWidth);
    
    /// <summary>
    /// 绘制文本
    /// </summary>
    void DrawText(string text, double x, double y, double fontSize, string? fontFamily = null, string? fontWeight = null, System.Drawing.Color color = default);
    
    /// <summary>
    /// 测量文本宽度
    /// </summary>
    double MeasureText(string text, double fontSize, string? fontFamily = null);
    
    /// <summary>
    /// 绘制图片
    /// </summary>
    /// <param name="imageKey">图片缓存键</param>
    /// <param name="bounds">绘制区域</param>
    /// <param name="stretch">拉伸模式</param>
    void DrawImage(string imageKey, Rect bounds, Stretch stretch = Stretch.Uniform);
    
    /// <summary>
    /// 加载图片并返回缓存键
    /// </summary>
    /// <param name="source">图片路径或 URI</param>
    /// <returns>图片缓存键，加载失败返回 null</returns>
    string? LoadImage(string source);
    
    /// <summary>
    /// 获取图片原始尺寸
    /// </summary>
    /// <param name="imageKey">图片缓存键</param>
    /// <returns>图片尺寸，无效键返回 Size.Zero</returns>
    Size GetImageSize(string imageKey);
}