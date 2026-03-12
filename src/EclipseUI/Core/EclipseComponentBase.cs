using Microsoft.AspNetCore.Components;

namespace EclipseUI.Core;

/// <summary>
/// EclipseUI 组件基类
/// </summary>
public abstract class EclipseComponentBase : ComponentBase, IElementHandler, IDisposable
{
    [CascadingParameter] protected EclipseElement? ParentElement { get; set; }
    
    protected EclipseElement? _element;
    
    // 静态引用 Renderer，用于 Dispatcher 访问
    public static EclipseRenderer? CurrentRenderer { get; set; }
    
    protected EclipseRenderer? Renderer => CurrentRenderer;
    
    EclipseElement IElementHandler.Element => Element!;
    
    protected internal virtual EclipseElement Element
    {
        get
        {
            if (_element == null)
            {
                _element = CreateElement();
                
                // 如果有父元素，自动添加到父元素的 Children
                if (ParentElement != null)
                {
                    ParentElement.AddChild(_element);
                }
            }
            return _element;
        }
    }
    
    protected virtual EclipseElement CreateElement() => new EclipseElement();
    
    protected override void OnInitialized()
    {
        base.OnInitialized();
        // 强制创建元素
        _ = Element;
    }
    
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        UpdateElementFromParameters();
    }
    
    protected virtual void UpdateElementFromParameters() { }
    
    public virtual void Dispose() => _element?.ClearChildren();
}
