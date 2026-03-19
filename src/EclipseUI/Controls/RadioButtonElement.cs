using SkiaSharp;
using EclipseUI.Core;
using EclipseUI.Rendering;

namespace EclipseUI.Controls;

/// <summary>
/// 单选框元素
/// </summary>
public class RadioButtonElement : EclipseElement
{
    public bool? IsChecked { get; set; }
    public string Content { get; set; } = "";
    public string GroupName { get; set; } = "Default";
    public float FontSize { get; set; } = 14;
    public SKColor TextColor { get; set; } = SKColors.Black;
    
    public bool IsHovered { get; set; }
    public bool IsPressed { get; set; }
    
    /// <summary>
    /// 单选框尺寸
    /// </summary>
    private const float RadioButtonSize = 18;
    
    /// <summary>
    /// 单选框与文本的间距
    /// </summary>
    private const float Spacing = 8;
    
    /// <summary>
    /// 选中状态变更回调
    /// </summary>
    public Func<bool?, Task>? OnCheckedChanged { get; set; }
    
    /// <summary>
    /// 点击事件
    /// </summary>
    public Action<EclipseElement, SKPoint>? OnClick { get; set; }
    
    /// <summary>
    /// 单选框组管理器（用于同组互斥）
    /// </summary>
    private static Dictionary<string, List<RadioButtonElement>> Groups { get; } = new();
    
    /// <summary>
    /// 注册到组（处理组名变更）
    /// </summary>
    public void RegisterToGroup(string? oldGroupName)
    {
        // 从旧组移除
        if (!string.IsNullOrEmpty(oldGroupName) && oldGroupName != GroupName && Groups.TryGetValue(oldGroupName, out var oldGroup))
        {
            oldGroup.Remove(this);
        }
        
        // 添加到新组
        if (!Groups.ContainsKey(GroupName))
        {
            Groups[GroupName] = new List<RadioButtonElement>();
        }
        
        if (!Groups[GroupName].Contains(this))
        {
            Groups[GroupName].Add(this);
        }
    }
    
    /// <summary>
    /// 从组中移除
    /// </summary>
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
        float contentHeight = Math.Max(RadioButtonSize, FontSize + 4);
        
        float finalWidth = RequestedWidth ?? contentWidth;
        float finalHeight = RequestedHeight ?? contentHeight;
        
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
        float textY = Y + (Height + FontSize) / 2;
        
        float centerX = radioBoxX + RadioButtonSize / 2;
        float centerY = radioBoxY + RadioButtonSize / 2;
        float outerRadius = RadioButtonSize / 2;
        float innerRadius = outerRadius / 2;
        
        // 绘制外圆
        using var outerPaint = new SKPaint 
        { 
            Color = IsChecked == true ? SKColors.Blue : SKColors.White, 
            IsAntialias = true 
        };
        canvas.DrawCircle(centerX, centerY, outerRadius, outerPaint);
        
        // 绘制外圆边框
        using var borderPaint = new SKPaint 
        { 
            Color = IsChecked == true ? SKColors.Blue : SKColors.Gray, 
            IsAntialias = true,
            StrokeWidth = 1.5f,
            Style = SKPaintStyle.Stroke
        };
        canvas.DrawCircle(centerX, centerY, outerRadius, borderPaint);
        
        // 绘制内圆（选中状态）
        if (IsChecked == true)
        {
            using var innerPaint = new SKPaint 
            { 
                Color = SKColors.White, 
                IsAntialias = true 
            };
            canvas.DrawCircle(centerX, centerY, innerRadius, innerPaint);
        }
        
        // 绘制文本
        if (!string.IsNullOrEmpty(Content))
        {
            using var renderContext = new SkiaRenderContext(canvas);
            TextRenderer.DrawText(
                renderContext,
                Content,
                textX,
                textY,
                FontSize,
                new Color(TextColor.Red, TextColor.Green, TextColor.Blue, TextColor.Alpha)
            );
        }
    }
    
    public override bool HandleClick(SKPoint point)
    {
        if (!IsVisible) return false;
        
        // 检查点击是否在元素边界内
        var rect = new SKRect(X, Y, X + Width, Y + Height);
        if (!rect.Contains(point)) return false;
        
        // 选中此单选框（自动取消同组其他）
        SetChecked(true);
        
        OnClick?.Invoke(this, point);
        
        return true;
    }
    
    /// <summary>
    /// 设置选中状态（自动处理同组互斥）
    /// </summary>
    public void SetChecked(bool? isChecked)
    {
        IsChecked = isChecked;
        
        // 如果选中，取消同组其他单选框
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
