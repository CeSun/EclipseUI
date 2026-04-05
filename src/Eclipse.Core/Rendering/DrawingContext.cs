using Eclipse.Core.Abstractions;
using Eclipse.Input;

namespace Eclipse.Rendering;

/// <summary>
/// 抽象渲染上下文 - 控件通过此接口绘制
/// </summary>
public abstract class DrawingContext
{
    /// <summary>
    /// 缩放因子
    /// </summary>
    public double Scale { get; protected set; } = 1.0;
    
    /// <summary>
    /// 宽度
    /// </summary>
    public double Width { get; protected set; }
    
    /// <summary>
    /// 高度
    /// </summary>
    public double Height { get; protected set; }
    
    /// <summary>
    /// 清空画布
    /// </summary>
    public abstract void Clear(string? color = null);
    
    /// <summary>
    /// 绘制矩形
    /// </summary>
    public abstract void DrawRectangle(Rect bounds, string? fillColor, string? strokeColor = null, double strokeWidth = 0, double cornerRadius = 0);
    
    /// <summary>
    /// 绘制圆角矩形
    /// </summary>
    public abstract void DrawRoundRect(Rect bounds, string fillColor, double cornerRadius);
    
    /// <summary>
    /// 绘制文本
    /// </summary>
    public abstract void DrawText(string text, double x, double y, double fontSize, string? fontFamily = null, string? fontWeight = null, string? color = null);
    
    /// <summary>
    /// 测量文本宽度
    /// </summary>
    public abstract double MeasureText(string text, double fontSize, string? fontFamily = null);
    
    /// <summary>
    /// 绘制子组件
    /// </summary>
    public abstract void DrawChild(IComponent child, Rect bounds);
    
    /// <summary>
    /// 设置子组件绘制回调
    /// </summary>
    protected Action<IComponent, Rect>? DrawChildCallback { get; set; }
    
    /// <summary>
    /// 初始化子组件绘制回调
    /// </summary>
    public void SetDrawChildCallback(Action<IComponent, Rect> callback)
    {
        DrawChildCallback = callback;
    }
}