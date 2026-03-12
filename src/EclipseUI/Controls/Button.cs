using SkiaSharp;
using Microsoft.AspNetCore.Components;
using EclipseUI.Core;

namespace EclipseUI.Controls;

/// <summary>
/// 按钮元素
/// </summary>
public class ButtonElement : EclipseElement
{
    public string Text { get; set; } = "Button";
    public float FontSize { get; set; } = 14;
    public SKColor TextColor { get; set; } = SKColors.White;
    public SKColor ButtonColor { get; set; } = SKColors.Blue;
    public float CornerRadius { get; set; } = 4;
    public bool IsHovered { get; set; }
    public bool IsPressed { get; set; }
    
    public override SKSize Measure(SKCanvas canvas, float availableWidth, float availableHeight)
    {
        using var paint = new SKPaint { TextSize = FontSize, IsAntialias = true };
        var bounds = new SKRect();
        paint.MeasureText(Text, ref bounds);
        return new SKSize(Math.Max(bounds.Width + 24, 80), Math.Max(bounds.Height + 16, 36));
    }
    
    protected override void RenderContent(SKCanvas canvas)
    {
        var color = IsPressed ? SKColors.DarkBlue : (IsHovered ? SKColors.LightBlue : ButtonColor);
        var rect = new SKRect(X, Y, X + Width, Y + Height);
        
        using var bgPaint = new SKPaint { Color = color, IsAntialias = true };
        if (CornerRadius > 0)
        {
            using var path = new SKPath();
            path.AddRoundRect(rect, CornerRadius, CornerRadius);
            canvas.DrawPath(path, bgPaint);
        }
        else
        {
            canvas.DrawRect(rect, bgPaint);
        }
        
        using var textPaint = new SKPaint
        {
            TextSize = FontSize,
            Color = TextColor,
            IsAntialias = true,
            TextAlign = SKTextAlign.Center,
            Typeface = TextBlockElement.GetChineseTypeface()
        };
        
        var bounds = new SKRect();
        textPaint.MeasureText(Text, ref bounds);
        canvas.DrawText(Text, X + Width / 2, Y + Height / 2 + bounds.Height / 4, textPaint);
    }
}

/// <summary>
/// Razor 组件
/// </summary>
public class Button : EclipseComponentBase
{
    [Parameter] public string? Text { get; set; }
    [Parameter] public float FontSize { get; set; } = 14;
    [Parameter] public string? Background { get; set; }
    [Parameter] public string? Foreground { get; set; }
    [Parameter] public float CornerRadius { get; set; } = 4;
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }
    
    protected override EclipseElement CreateElement()
    {
        return new ButtonElement();
    }
    
    protected override void UpdateElementFromParameters()
    {
        if (_element is ButtonElement btn)
        {
            btn.Text = Text ?? "Button";
            btn.FontSize = FontSize;
            btn.ButtonColor = ParseBackground(Background);
            btn.TextColor = ParseColor(Foreground);
            btn.CornerRadius = CornerRadius;
            
            // 获取 Renderer 用于 Dispatcher
            var renderer = Renderer;
            
            btn.OnClick = OnClick.HasDelegate ? async (e, p) => 
            {
                // 通过 Renderer 的 Dispatcher 执行，确保在正确的线程上
                if (renderer != null)
                {
                    await renderer.Dispatcher.InvokeAsync(async () =>
                    {
                        await OnClick.InvokeAsync(new MouseEventArgs { ClientX = p.X, ClientY = p.Y });
                    });
                }
                else
                {
                    await OnClick.InvokeAsync(new MouseEventArgs { ClientX = p.X, ClientY = p.Y });
                }
            } : null;
        }
    }
    
    private static SKColor ParseBackground(string? color)
    {
        if (!string.IsNullOrEmpty(color) && color.StartsWith('#') && color.Length == 7)
            return SKColor.Parse(color);
        return SKColors.Blue;
    }
    
    private static SKColor ParseColor(string? color)
    {
        if (!string.IsNullOrEmpty(color) && color.StartsWith('#') && color.Length == 7)
            return SKColor.Parse(color);
        return SKColors.White;
    }
}
