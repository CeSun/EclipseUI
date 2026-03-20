using System.Collections.Concurrent;

namespace EclipseUI.Styling;

/// <summary>
/// 样式管理器 - 管理全局样式表和类选择器
/// </summary>
public static class StyleManager
{
    private static readonly ConcurrentDictionary<string, Style> _classStyles = new();
    private static readonly Dictionary<string, string> _cssVariables = new();
    
    /// <summary>
    /// 注册 CSS 类样式
    /// </summary>
    public static void RegisterClass(string className, Style style)
    {
        _classStyles[className] = style;
    }
    
    /// <summary>
    /// 获取 CSS 类样式
    /// </summary>
    public static Style? GetClassStyle(string className)
    {
        return _classStyles.TryGetValue(className, out var style) ? style : null;
    }
    
    /// <summary>
    /// 设置 CSS 变量
    /// </summary>
    public static void SetVariable(string name, string value)
    {
        _cssVariables[name] = value;
    }
    
    /// <summary>
    /// 获取 CSS 变量
    /// </summary>
    public static string? GetVariable(string name)
    {
        return _cssVariables.TryGetValue(name, out var value) ? value : null;
    }
    
    /// <summary>
    /// 解析并注册样式表
    /// </summary>
    public static void ParseAndRegisterStylesheet(string cssContent)
    {
        // 简化实现：解析 .class { property: value; } 格式
        var classPattern = @"\.([a-zA-Z0-9_-]+)\s*\{([^}]+)\}";
        var matches = System.Text.RegularExpressions.Regex.Matches(cssContent, classPattern);
        
        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var className = match.Groups[1].Value;
            var styleContent = match.Groups[2].Value;
            
            var style = new Style();
            var properties = EclipseStyleParser.ParseInlineStyle(styleContent.Replace('\n', ';'));
            
            foreach (var prop in properties)
            {
                style.ApplyProperty(prop.Key, prop.Value);
            }
            
            RegisterClass(className, style);
        }
    }
}