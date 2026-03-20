using SkiaSharp;

namespace EclipseUI.Styling;

/// <summary>
/// 样式属性集合
/// </summary>
public class Style
{
    // 背景相关
    public SKColor? BackgroundColor { get; set; }
    public string BackgroundImage { get; set; } = string.Empty;
    
    // 文本相关  
    public SKColor? Color { get; set; }
    public float FontSize { get; set; } = 14f;
    public string FontFamily { get; set; } = "Microsoft YaHei";
    public bool FontBold { get; set; } = false;
    public bool FontItalic { get; set; } = false;
    
    // 边框相关
    public SKColor? BorderColor { get; set; } = SKColors.Gray;
    public float BorderWidth { get; set; } = 1f;
    public float BorderRadius { get; set; } = 0f;
    
    // 布局相关
    public float PaddingLeft { get; set; } = 0f;
    public float PaddingTop { get; set; } = 0f;
    public float PaddingRight { get; set; } = 0f;
    public float PaddingBottom { get; set; } = 0f;
    
    public float MarginLeft { get; set; } = 0f;
    public float MarginTop { get; set; } = 0f;
    public float MarginRight { get; set; } = 0f;
    public float MarginBottom { get; set; } = 0f;
    
    public float Width { get; set; } = -1f; // -1 表示自动
    public float Height { get; set; } = -1f;
    
    // 视觉效果
    public float Opacity { get; set; } = 1f;
    public string BoxShadow { get; set; } = string.Empty;
    
    /// <summary>
    /// 应用 CSS 属性
    /// </summary>
    public void ApplyProperty(string property, string value)
    {
        switch (property.ToLowerInvariant())
        {
            case "background":
            case "background-color":
                BackgroundColor = EclipseStyleParser.ParseColor(value);
                break;
            case "color":
                Color = EclipseStyleParser.ParseColor(value);
                break;
            case "font-size":
                FontSize = EclipseStyleParser.ParseSize(value);
                break;
            case "font-family":
                FontFamily = value.Trim('\'', '"');
                break;
            case "font-weight":
                FontBold = value.ToLowerInvariant() == "bold" || value == "700";
                break;
            case "font-style":
                FontItalic = value.ToLowerInvariant() == "italic";
                break;
            case "border":
                ParseBorder(value);
                break;
            case "border-color":
                BorderColor = EclipseStyleParser.ParseColor(value);
                break;
            case "border-width":
                BorderWidth = EclipseStyleParser.ParseSize(value);
                break;
            case "border-radius":
                BorderRadius = EclipseStyleParser.ParseSize(value);
                break;
            case "padding":
                ParsePadding(value);
                break;
            case "margin":
                ParseMargin(value);
                break;
            case "width":
                Width = EclipseStyleParser.ParseSize(value);
                break;
            case "height":
                Height = EclipseStyleParser.ParseSize(value);
                break;
            case "opacity":
                Opacity = float.Parse(value);
                break;
            case "box-shadow":
                BoxShadow = value;
                break;
        }
    }
    
    private void ParseBorder(string borderValue)
    {
        var parts = borderValue.Split(' ');
        if (parts.Length >= 3)
        {
            BorderWidth = EclipseStyleParser.ParseSize(parts[0]);
            // parts[1] 是边框样式 (solid, dashed 等)
            BorderColor = EclipseStyleParser.ParseColor(parts[2]);
        }
    }
    
    private void ParsePadding(string paddingValue)
    {
        var values = paddingValue.Split(' ').Select(float.Parse).ToArray();
        switch (values.Length)
        {
            case 1:
                PaddingLeft = PaddingTop = PaddingRight = PaddingBottom = values[0];
                break;
            case 2:
                PaddingTop = PaddingBottom = values[0];
                PaddingLeft = PaddingRight = values[1];
                break;
            case 4:
                PaddingTop = values[0];
                PaddingRight = values[1];
                PaddingBottom = values[2];
                PaddingLeft = values[3];
                break;
        }
    }
    
    private void ParseMargin(string marginValue)
    {
        var values = marginValue.Split(' ').Select(float.Parse).ToArray();
        switch (values.Length)
        {
            case 1:
                MarginLeft = MarginTop = MarginRight = MarginBottom = values[0];
                break;
            case 2:
                MarginTop = MarginBottom = values[0];
                MarginLeft = MarginRight = values[1];
                break;
            case 4:
                MarginTop = values[0];
                MarginRight = values[1];
                MarginBottom = values[2];
                MarginLeft = values[3];
                break;
        }
    }
}