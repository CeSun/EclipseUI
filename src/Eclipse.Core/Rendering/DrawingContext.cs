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
    void Clear(Color color);
    
    /// <summary>
    /// 绘制矩形
    /// </summary>
    void DrawRectangle(Rect bounds, Color fillColor, Color? strokeColor = null, double strokeWidth = 0, double cornerRadius = 0);
    
    /// <summary>
    /// 绘制圆角矩形
    /// </summary>
    void DrawRoundRect(Rect bounds, Color fillColor, double cornerRadius);
    
    /// <summary>
    /// 绘制线条
    /// </summary>
    void DrawLine(double x1, double y1, double x2, double y2, Color color, double strokeWidth);
    
    /// <summary>
    /// 绘制文本
    /// </summary>
    void DrawText(string text, double x, double y, double fontSize, string? fontFamily = null, string? fontWeight = null, Color color = default);
    
    /// <summary>
    /// 测量文本宽度
    /// </summary>
    double MeasureText(string text, double fontSize, string? fontFamily = null);
    
    /// <summary>
    /// 绘制图片
    /// </summary>
    void DrawImage(string imageKey, Rect bounds, Stretch stretch = Stretch.Uniform);
    
    /// <summary>
    /// 加载图片并返回缓存键
    /// </summary>
    string? LoadImage(string source);
    
    /// <summary>
    /// 获取图片原始尺寸
    /// </summary>
    Size GetImageSize(string imageKey);
}