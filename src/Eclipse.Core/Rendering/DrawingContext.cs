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
    void Clear(string? color = null);
    
    /// <summary>
    /// 绘制矩形
    /// </summary>
    void DrawRectangle(Rect bounds, string? fillColor, string? strokeColor = null, double strokeWidth = 0, double cornerRadius = 0);
    
    /// <summary>
    /// 绘制圆角矩形
    /// </summary>
    void DrawRoundRect(Rect bounds, string fillColor, double cornerRadius);
    
    /// <summary>
    /// 绘制文本
    /// </summary>
    void DrawText(string text, double x, double y, double fontSize, string? fontFamily = null, string? fontWeight = null, string? color = null);
    
    /// <summary>
    /// 测量文本宽度
    /// </summary>
    double MeasureText(string text, double fontSize, string? fontFamily = null);
}