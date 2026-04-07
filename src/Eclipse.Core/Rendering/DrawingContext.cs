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
    void Clear(Color? color = null);
    
    /// <summary>
    /// 绘制矩形
    /// </summary>
    void DrawRectangle(Rect bounds, Color? fillColor, Color? strokeColor = null, double strokeWidth = 0, double cornerRadius = 0);
    
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
    void DrawText(string text, double x, double y, double fontSize, string? fontFamily = null, string? fontWeight = null, Color? color = null);
    
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

/// <summary>
/// IDrawingContext 扩展方法
/// </summary>
public static class DrawingContextExtensions
{
    /// <summary>
    /// 绘制矩形（兼容 string 参数）
    /// </summary>
    public static void DrawRectangle(this IDrawingContext context, Rect bounds, string? fillColor, string? strokeColor = null, double strokeWidth = 0, double cornerRadius = 0)
    {
        var fill = fillColor != null ? Color.Parse(fillColor) : (Color?)null;
        var stroke = strokeColor != null ? Color.Parse(strokeColor) : (Color?)null;
        context.DrawRectangle(bounds, fill, stroke, strokeWidth, cornerRadius);
    }
    
    /// <summary>
    /// 绘制圆角矩形（兼容 string 参数）
    /// </summary>
    public static void DrawRoundRect(this IDrawingContext context, Rect bounds, string fillColor, double cornerRadius)
    {
        context.DrawRoundRect(bounds, Color.Parse(fillColor), cornerRadius);
    }
    
    /// <summary>
    /// 绘制线条（兼容 string 参数）
    /// </summary>
    public static void DrawLine(this IDrawingContext context, double x1, double y1, double x2, double y2, string color, double strokeWidth)
    {
        context.DrawLine(x1, y1, x2, y2, Color.Parse(color), strokeWidth);
    }
    
    /// <summary>
    /// 绘制文本（兼容 string 参数）
    /// </summary>
    public static void DrawText(this IDrawingContext context, string text, double x, double y, double fontSize, string? fontFamily = null, string? fontWeight = null, string? color = null)
    {
        var textColor = color != null ? Color.Parse(color) : (Color?)null;
        context.DrawText(text, x, y, fontSize, fontFamily, fontWeight, textColor);
    }
    
    /// <summary>
    /// 清空画布（兼容 string 参数）
    /// </summary>
    public static void Clear(this IDrawingContext context, string? color)
    {
        context.Clear(color != null ? Color.Parse(color) : null);
    }
}