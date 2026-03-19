using SkiaSharp;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using EclipseUI.Core;


namespace EclipseUI.Controls;

/// <summary>
/// TabControl 选项卡项组件
/// </summary>
public class TabItem : ComponentBase, IElementHandler, IDisposable
{
    [Parameter] public string? Header { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    
    [CascadingParameter] public Action<TabItem>? RegisterTab { get; set; }
    
    private TabItemElement? _element;
    private bool _disposed;
    
    EclipseElement IElementHandler.Element
    {
        get
        {
            if (_element == null)
            {
                _element = new TabItemElement();
            }
            return _element;
        }
    }
    
    protected override void OnInitialized()
    {
        base.OnInitialized();
        _ = ((IElementHandler)this).Element;
        RegisterTab?.Invoke(this);
    }
    
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.AddContent(0, ChildContent);
    }
    
    void IDisposable.Dispose()
    {
        if (!_disposed)
        {
            _element = null;
            _disposed = true;
        }
    }
}

/// <summary>
/// TabItem 元素
/// </summary>
public class TabItemElement : EclipseElement
{
    public override SKSize Measure(SKCanvas canvas, float availableWidth, float availableHeight)
    {
        float maxWidth = 0, maxHeight = 0;
        
        foreach (var child in Children)
        {
            var size = child.Measure(canvas, availableWidth, availableHeight);
            maxWidth = Math.Max(maxWidth, size.Width);
            maxHeight = Math.Max(maxHeight, size.Height);
        }
        
        return new SKSize(availableWidth, availableHeight);
    }
    
    public override void Arrange(SKCanvas canvas, float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
        
        foreach (var child in Children)
        {
            child.Arrange(canvas, x, y, width, height);
        }
    }
    
    public override void Render(SKCanvas canvas)
    {
        if (!IsVisible) return;
        RenderChildren(canvas);
    }
}

/// <summary>
/// 选项卡控件组件
/// </summary>
public class TabControl : ComponentBase, IElementHandler, IDisposable
{
    [Parameter] public int SelectedIndex { get; set; } = 0;
    [Parameter] public EventCallback<int> SelectedIndexChanged { get; set; }
    [Parameter] public float HeaderHeight { get; set; } = 44;
    [Parameter] public float FontSize { get; set; } = 14;
    [Parameter] public string? Background { get; set; }
    [Parameter] public float? Width { get; set; }
    [Parameter] public float? Height { get; set; }
    [Parameter] public float? MinWidth { get; set; }
    [Parameter] public float? MinHeight { get; set; }
    
    [Parameter] public RenderFragment? ChildContent { get; set; }
    
    private TabControlElement? _element;
    private bool _disposed;
    private List<TabItem> _tabs = new();
    private bool _tabsCollected = false;
    
    [Inject] protected EclipseRenderer? Renderer { get; set; }
    
    EclipseElement IElementHandler.Element
    {
        get
        {
            if (_element == null)
            {
                _element = new TabControlElement();
                UpdateElementFromParameters();
            }
            return _element;
        }
    }
    
    protected override void OnInitialized()
    {
        base.OnInitialized();
        _ = ((IElementHandler)this).Element;
    }
    
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        UpdateElementFromParameters();
    }
    
    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);
        if (firstRender && _tabs.Count > 0)
        {
            _tabsCollected = true;
            UpdateElementFromParameters();
        }
    }
    
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (ChildContent != null && !_tabsCollected)
        {
            _tabs.Clear();
            
            builder.OpenComponent<CascadingValue<Action<TabItem>>>(0);
            builder.AddAttribute(1, "Value", (Action<TabItem>)RegisterTab);
            builder.AddAttribute(2, "ChildContent", ChildContent);
            builder.CloseComponent();
        }
        else if (_tabsCollected)
        {
            // 渲染所有 Tab 的内容
            int seq = 0;
            foreach (var tab in _tabs)
            {
                if (tab.ChildContent != null)
                {
                    builder.AddContent(seq++, tab.ChildContent);
                }
            }
        }
    }
    
    private void RegisterTab(TabItem tab)
    {
        _tabs.Add(tab);
    }
    
    internal List<TabItem> GetTabs() => _tabs;
    
    private void UpdateElementFromParameters()
    {
        if (_element == null) return;
        
        _element.Headers = _tabs.Select(t => t.Header ?? "").ToList();
        _element.SelectedIndex = SelectedIndex;
        _element.HeaderHeight = HeaderHeight;
        _element.FontSize = FontSize;
        _element.BackgroundColor = ParseBackground(Background);
        _element.RequestedWidth = Width;
        _element.RequestedHeight = Height;
        _element.MinWidth = MinWidth;
        _element.MinHeight = MinHeight;
        
        _element.OnTabSelected = async (index) =>
        {
            SelectedIndex = index;
            
            if (SelectedIndexChanged.HasDelegate)
            {
                if (Renderer != null)
                {
                    await Renderer.Dispatcher.InvokeAsync(async () =>
                    {
                        await SelectedIndexChanged.InvokeAsync(index);
                    });
                }
                else
                {
                    await SelectedIndexChanged.InvokeAsync(index);
                }
            }
        };
    }
    
    private static SKColor? ParseBackground(string? color)
    {
        if (!string.IsNullOrEmpty(color) && color.StartsWith('#') && color.Length == 7)
            return SKColor.Parse(color);
        return null;
    }
    
    void IDisposable.Dispose()
    {
        if (!_disposed)
        {
            _element = null;
            _disposed = true;
        }
    }
}
