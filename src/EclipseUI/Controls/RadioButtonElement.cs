using SkiaSharp;
using EclipseUI.Core;
using EclipseUI.Rendering;

namespace EclipseUI.Controls;

/// <summary>
/// 单选框元素 - iOS 风格
/// </summary>
public class RadioButtonElement : EclipseElement
{
    public bool? IsChecked { get; set; }
    public string Content { get; set; } = "";
    public string GroupName { get; set; } = "Default";
    public float FontSize { get; set; } = iOSTheme.FontSizeBody;
    public SKColor TextColor { get; set; } = iOSTheme.LabelPrimary;
    
    public bool IsHovered { get; set; }
    public bool IsPressed { get; set; }
    
    // iOS 风格尺寸
    private const float RadioButtonSize = 22;
    private const float Spacing = 10;
    
    public Func<bool?, Task>? OnCheckedChanged { get; set; }
    public Action<EclipseElement, SKPoint>? OnClick { get; set; }
    
    private static Dictionary<string, List<RadioButtonElement>> Groups { get; } = new();
    
    public void RegisterToGroup(string? oldGroupName)
    {
        if (!string.IsNullOrEmpty(oldGroupName) && oldGroupName != GroupName && Groups.TryGetValue(oldGroupName, out var oldGroup))
        {
            oldGroup.Remove(this);
        }
        
        if (!Groups.ContainsKey(GroupName))
        {
            Groups[GroupName] = new List<RadioButtonElement>();
        }
        
        if (!Groups[GroupName].Contains(this))
        {
            Groups[GroupName].Add(this);
        }
    }
    
    public void UnregisterFromGroup()
    {
        if (Groups.TryGetValue(GroupName, out var group))
        {
            group.Remove(this);
        }
    }
    
    public override SKSize Measure(SKCanvas canvas, float availableWidth, float availableHeight)
    {
        var textWidth = TextRenderer.MeasureText(Content, FontSize);
        float contentWidth = RadioButtonSize + Spacing + textWidth;
        float contentHeight = Math.Max(RadioButtonSize, FontSize + 8);
        
        float finalWidth = RequestedWidth ?? contentWidth;
        float finalHeight = RequestedHeight ?? Math.Max(contentHeight, 44); // iOS 最小触摸目标
        
        if (MinWidth.HasValue) finalWidth = Math.Max(finalWidth, MinWidth.Value);
        if (MinHeight.HasValue) finalHeight = Math.Max(finalHeight, MinHeight.Value);
        if (MaxWidth.HasValue) finalWidth = Math.Min(finalWidth, MaxWidth.Value);
        if (MaxHeight.HasValue) finalHeight = Math.Min(finalHeight, MaxHeight.Value);
        
        return new SKSize(finalWidth, finalHeight);
    }
    
    public override void Render(SKCanvas canvas)
    {
        if (!IsVisible) return;
        
        canvas.Save();
        try
        {
            RenderContent(canvas);
        }
        finally
        {
            canvas.Restore();
        }
    }
    
    protected override void RenderContent(SKCanvas canvas)
    {
        float radioBoxX = X;
        float radioBoxY = Y + (Height - RadioButtonSize) / 2;
        float textX = radioBoxX + RadioButtonSize + Spacing;
        float textY = Y + Height / 2 + FontSize / 3;
        
        float centerX = radioBoxX + RadioButtonSize / 2;
        float centerY = radioBoxY + RadioButtonSize / 2;
        float outerRadius = RadioButtonSize / 2;
        
        if (IsChecked == true)
        {
            // 选中状态：蓝色填充圆 + 白色内圆
            using var fillPaint = new SKPaint 
            { 
                Color = iOSTheme.SystemBlue, 
                IsAntialias = true 
            };
            canvas.DrawCircle(centerX, centerY, outerRadius, fillPaint);
            
            // 白色内圆点
            using var innerPaint = new SKPaint 
            { 
                Color = SKColors.White, 
                IsAntialias = true 
            };
            canvas.DrawCircle(centerX, centerY, outerRadius * 0.35f, innerPaint);
        }
        else
        {
            // 未选中状态：灰色边框圆
            using var borderPaint = new SKPaint 
            { 
                Color = iOSTheme.SystemGray3, 
                IsAntialias = true,
                StrokeWidth = 2f,
                Style = SKPaintStyle.Stroke
            };
            canvas.DrawCircle(centerX, centerY, outerRadius - 1, borderPaint);
        }
        
        // 绘制文本
        if (!string.IsNullOrEmpty(Content))
        {
            using var renderContext = new SkiaRenderContext(canvas);
            TextRenderer.DrawText(renderContext, Content, textX, textY, FontSize,
                new Color(TextColor.Red, TextColor.Green, TextColor.Blue, TextColor.Alpha));
        }
    }
    
    public override bool HandleClick(SKPoint point)
    {
        if (!IsVisible) return false;
        
        var rect = new SKRect(X, Y, X + Width, Y + Height);
        if (!rect.Contains(point)) return false;
        
        SetChecked(true);
        OnClick?.Invoke(this, point);
        
        return true;
    }
    
    public void SetChecked(bool? isChecked)
    {
        IsChecked = isChecked;
        
        if (IsChecked == true && Groups.TryGetValue(GroupName, out var group))
        {
            foreach (var radio in group)
            {
                if (radio != this)
                {
                    radio.IsChecked = false;
                    radio.OnCheckedChanged?.Invoke(false);
                }
            }
        }
        
        OnCheckedChanged?.Invoke(IsChecked);
    }
    
    public override bool HandleMouseDown(float x, float y)
    {
        var rect = new SKRect(X, Y, X + Width, Y + Height);
        if (rect.Contains(new SKPoint(x, y)))
        {
            IsPressed = true;
            return true;
        }
        return false;
    }
    
    public override bool HandleMouseMove(float x, float y)
    {
        var rect = new SKRect(X, Y, X + Width, Y + Height);
        var wasHovered = IsHovered;
        IsHovered = rect.Contains(new SKPoint(x, y));
        return IsHovered != wasHovered;
    }
    
    public override void HandleMouseUp()
    {
        IsPressed = false;
    }
    
    public override void HandleMouseLeave()
    {
        IsHovered = false;
        IsPressed = false;
    }
}
