using SkiaSharp;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using EclipseUI.Core;
using EclipseUI.Layout;

namespace EclipseUI.Controls;

/// <summary>
/// ComboBox 选项组件
/// </summary>
public class ComboBoxItem : ComponentBase
{
    /// <summary>
    /// 选项值
    /// </summary>
    [Parameter] public string? Value { get; set; }
    
    /// <summary>
    /// 显示文本（如果不设置则使用 Value）
    /// </summary>
    [Parameter] public string? Text { get; set; }
    
    /// <summary>
    /// 父 ComboBox 的回调
    /// </summary>
    [CascadingParameter] public Action<string, string>? RegisterItem { get; set; }
    
    protected override void OnInitialized()
    {
        base.OnInitialized();
        // 向父 ComboBox 注册选项
        RegisterItem?.Invoke(Value ?? "", Text ?? Value ?? "");
    }
}

/// <summary>
/// 下拉选择框组件
/// </summary>
public class ComboBox : ComponentBase, IElementHandler, IDisposable
{
    [Parameter] public IList<string>? ItemsSource { get; set; }
    [Parameter] public string? SelectedItem { get; set; }
    [Parameter] public EventCallback<string?> SelectedItemChanged { get; set; }
    [Parameter] public int SelectedIndex { get; set; } = -1;
    [Parameter] public EventCallback<int> SelectedIndexChanged { get; set; }
    [Parameter] public string? Placeholder { get; set; }
    [Parameter] public float FontSize { get; set; } = 14;
    [Parameter] public string? Foreground { get; set; }
    [Parameter] public string? Background { get; set; }
    [Parameter] public float? Width { get; set; }
    [Parameter] public float? Height { get; set; }
    [Parameter] public float? MinWidth { get; set; } = 120;
    [Parameter] public float? MinHeight { get; set; } = 36;
    
    [Parameter] public float PaddingLeft { get; set; } = 12;
    [Parameter] public float PaddingTop { get; set; } = 8;
    [Parameter] public float PaddingRight { get; set; } = 36;
    [Parameter] public float PaddingBottom { get; set; } = 8;
    
    [Parameter] public EventCallback<FocusEventArgs> OnFocus { get; set; }
    [Parameter] public EventCallback<FocusEventArgs> OnBlur { get; set; }
    
    /// <summary>
    /// 子内容（ComboBoxItem）
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }
    
    private ComboBoxElement? _element;
    private bool _disposed;
    private List<(string Value, string Text)> _childItems = new();
    private bool _childItemsCollected = false;
    
    [Inject] protected EclipseRenderer? Renderer { get; set; }
    
    EclipseElement IElementHandler.Element
    {
        get
        {
            if (_element == null)
            {
                _element = new ComboBoxElement();
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
        if (firstRender && _childItems.Count > 0)
        {
            _childItemsCollected = true;
            UpdateElementFromParameters();
        }
    }
    
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (ChildContent != null && !_childItemsCollected)
        {
            // 只在首次渲染时收集子项
            _childItems.Clear();
            
            // 提供 CascadingValue 让子组件可以注册
            builder.OpenComponent<CascadingValue<Action<string, string>>>(0);
            builder.AddAttribute(1, "Value", (Action<string, string>)RegisterChildItem);
            builder.AddAttribute(2, "ChildContent", ChildContent);
            builder.CloseComponent();
        }
    }
    
    private void RegisterChildItem(string value, string text)
    {
        _childItems.Add((value, text));
    }
    
    private void UpdateElementFromParameters()
    {
        if (_element == null) return;
        
        // 优先使用 ItemsSource，否则使用子组件注册的项
        if (ItemsSource != null && ItemsSource.Count > 0)
        {
            _element.ItemsSource = ItemsSource;
        }
        else if (_childItems.Count > 0)
        {
            _element.ItemsSource = _childItems.Select(x => x.Text).ToList();
            _element.ItemValues = _childItems.Select(x => x.Value).ToList();
        }
        else
        {
            _element.ItemsSource = new List<string>();
        }
        
        _element.SelectedIndex = SelectedIndex;
        _element.SelectedItem = SelectedItem;
        _element.Placeholder = Placeholder ?? "请选择...";
        _element.FontSize = FontSize;
        _element.TextColor = ParseColor(Foreground);
        _element.BackgroundColor = ParseBackground(Background);
        _element.RequestedWidth = Width;
        _element.RequestedHeight = Height;
        _element.MinWidth = MinWidth;
        _element.MinHeight = MinHeight;
        _element.PaddingLeft = PaddingLeft;
        _element.PaddingTop = PaddingTop;
        _element.PaddingRight = PaddingRight;
        _element.PaddingBottom = PaddingBottom;
        
        _element.OnItemSelected = async (index, item) =>
        {
            SelectedIndex = index;
            
            // 如果有 ItemValues，使用 Value；否则使用显示文本
            if (_element.ItemValues != null && index >= 0 && index < _element.ItemValues.Count)
            {
                SelectedItem = _element.ItemValues[index];
            }
            else
            {
                SelectedItem = item;
            }
            
            if (SelectedItemChanged.HasDelegate)
            {
                if (Renderer != null)
                {
                    await Renderer.Dispatcher.InvokeAsync(async () =>
                    {
                        await SelectedItemChanged.InvokeAsync(SelectedItem);
                    });
                }
                else
                {
                    await SelectedItemChanged.InvokeAsync(SelectedItem);
                }
            }
            
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
        return SKColors.White;
    }
    
    private static SKColor ParseColor(string? color)
    {
        if (!string.IsNullOrEmpty(color) && color.StartsWith('#') && color.Length == 7)
            return SKColor.Parse(color);
        return SKColors.Black;
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
