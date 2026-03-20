using System.Text.RegularExpressions;
using SkiaSharp;

namespace EclipseUI.Styling;

/// <summary>
/// EclipseStyle 解析器 - 将类 CSS 语法转换为样式属性
/// </summary>
public static class EclipseStyleParser
{
    /// <summary>
    /// 解析 CSS 样式字符串
    /// </summary>
    public static Dictionary<string, string> ParseInlineStyle(string style)
    {
        var properties = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(style)) return properties;
        
        // 分割属性 (支持分号分隔)
        var declarations = style.Split(';');
        foreach (var declaration in declarations)
        {
            var trimmed = declaration.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;
            
            var colonIndex = trimmed.IndexOf(':');
            if (colonIndex <= 0) continue;
            
            var property = trimmed.Substring(0, colonIndex).Trim().ToLowerInvariant();
            var value = trimmed.Substring(colonIndex + 1).Trim();
            properties[property] = value;
        }
        
        return properties;
    }
    
    /// <summary>
    /// 解析 CSS 颜色值
    /// </summary>
    public static SKColor ParseColor(string colorValue)
    {
        if (string.IsNullOrEmpty(colorValue)) return SKColors.Black;
        
        // 支持 #RRGGBB, #RGB, rgb(r,g,b), rgba(r,g,b,a), 颜色名称
        colorValue = colorValue.Trim();
        
        if (colorValue.StartsWith("#"))
        {
            // 十六进制颜色
            return SKColor.Parse(colorValue);
        }
        else if (colorValue.StartsWith("rgb") || colorValue.StartsWith("hsl"))
        {
            // 函数式颜色 (简化处理)
            return SKColor.Parse("#FF0000"); // TODO: 完整实现
        }
        else
        {
            // 颜色名称
            return SKColor.Parse(colorValue);
        }
    }
    
    /// <summary>
    /// 解析尺寸值 (支持 px, %, em 等单位)
    /// </summary>
    public static float ParseSize(string sizeValue, float containerSize = 0)
    {
        if (string.IsNullOrEmpty(sizeValue)) return 0;
        
        sizeValue = sizeValue.Trim();
        
        if (sizeValue.EndsWith("px"))
        {
            return float.Parse(sizeValue.Substring(0, sizeValue.Length - 2));
        }
        else if (sizeValue.EndsWith("%"))
        {
            var percentage = float.Parse(sizeValue.Substring(0, sizeValue.Length - 1));
            return containerSize * (percentage / 100f);
        }
        else if (sizeValue == "auto")
        {
            return -1; // 自动尺寸标记
        }
        else
        {
            // 默认为像素值
            return float.Parse(sizeValue);
        }
    }
}