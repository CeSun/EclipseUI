using Microsoft.AspNetCore.Components;

namespace EclipseUI.Controls;

/// <summary>
/// 样式组件 - 用于定义 CSS 样式
/// </summary>
public class Style : ComponentBase
{
    /// <summary>
    /// 样式内容
    /// </summary>
    [Parameter]
    public string? Content { get; set; }
    
    /// <summary>
    /// 子内容（样式规则）
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
    
    protected override void OnInitialized()
    {
        var styleContent = Content ?? (ChildContent?.ToString() ?? string.Empty);
        if (!string.IsNullOrEmpty(styleContent))
        {
            Styling.StyleManager.ParseAndRegisterStylesheet(styleContent);
        }
    }
}