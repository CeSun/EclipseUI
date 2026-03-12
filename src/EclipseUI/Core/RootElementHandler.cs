namespace EclipseUI.Core;

/// <summary>
/// 根元素处理器 - 实现 IElementHandler 用于根元素
/// </summary>
internal class RootElementHandler : IElementHandler
{
    public EclipseElement Element { get; } = new EclipseElement();
}
