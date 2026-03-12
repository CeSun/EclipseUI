namespace EclipseUI.Core;

/// <summary>
/// 元素处理器接口 - 用于获取和管理 Skia 元素
/// </summary>
public interface IElementHandler
{
    /// <summary>
    /// 获取对应的 Skia 元素
    /// </summary>
    EclipseElement Element { get; }
}

/// <summary>
/// 元素处理器扩展方法
/// </summary>
public static class ElementHandlerExtensions
{
    /// <summary>
    /// 添加子元素
    /// </summary>
    public static void AddChild(this IElementHandler handler, EclipseElement child)
    {
        handler.Element.AddChild(child);
    }
    
    /// <summary>
    /// 移除子元素
    /// </summary>
    public static void RemoveChild(this IElementHandler handler, EclipseElement child)
    {
        handler.Element.RemoveChild(child);
    }
}
