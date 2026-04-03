using Eclipse.Core;
using Eclipse.Core.Abstractions;
using SkiaSharp;

namespace Eclipse.Skia.Controls;

/// <summary>
/// StackLayout 渲染器
/// </summary>
public class StackLayoutRenderer : ISkiaControlRenderer
{
    public Type TargetType => typeof(StackLayout);
    
    public void Render(
        IComponent component, 
        SkiaRenderContext context, 
        SKRect bounds,
        Action<IComponent, SkiaRenderContext, SKRect> renderChildren)
    {
        var stack = (StackLayout)component;
        
        // 计算子组件布局
        var orientation = stack.Orientation;
        var spacing = (float)stack.Spacing * context.Scale;
        var padding = (float)stack.Padding * context.Scale;
        
        var contentBounds = new SKRect(
            bounds.Left + padding,
            bounds.Top + padding,
            bounds.Right - padding,
            bounds.Bottom - padding);
        
        if (orientation == Orientation.Vertical)
        {
            RenderVertical(stack, context, contentBounds, spacing, renderChildren);
        }
        else
        {
            RenderHorizontal(stack, context, contentBounds, spacing, renderChildren);
        }
    }
    
    private void RenderVertical(
        StackLayout stack,
        SkiaRenderContext context,
        SKRect bounds,
        float spacing,
        Action<IComponent, SkiaRenderContext, SKRect> renderChildren)
    {
        float y = bounds.Top;
        
        foreach (var child in stack.Children)
        {
            var childHeight = EstimateHeight(child, context, bounds.Width);
            var childBounds = new SKRect(bounds.Left, y, bounds.Right, y + childHeight);
            renderChildren(child, context, childBounds);
            y += childHeight + spacing;
        }
    }
    
    private void RenderHorizontal(
        StackLayout stack,
        SkiaRenderContext context,
        SKRect bounds,
        float spacing,
        Action<IComponent, SkiaRenderContext, SKRect> renderChildren)
    {
        float x = bounds.Left;
        var childWidth = (bounds.Width - spacing * (stack.Children.Count - 1)) / stack.Children.Count;
        
        foreach (var child in stack.Children)
        {
            var childBounds = new SKRect(x, bounds.Top, x + childWidth, bounds.Bottom);
            renderChildren(child, context, childBounds);
            x += childWidth + spacing;
        }
    }
    
    private float EstimateHeight(IComponent component, SkiaRenderContext context, float width)
    {
        // 简单估算，后续可以根据内容计算
        return component switch
        {
            Label => 24f * context.Scale,
            Button => 44f * context.Scale,
            TextContent => 20f * context.Scale,
            _ => 40f * context.Scale
        };
    }
}

/// <summary>
/// Label 渲染器
/// </summary>
public class LabelRenderer : ISkiaControlRenderer
{
    public Type TargetType => typeof(Label);
    
    public void Render(
        IComponent component, 
        SkiaRenderContext context, 
        SKRect bounds,
        Action<IComponent, SkiaRenderContext, SKRect> renderChildren)
    {
        var label = (Label)component;
        
        if (string.IsNullOrEmpty(label.Text))
            return;
        
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color = ParseColor(label.Color, SKColors.Black)
        };
        
        using var font = new SKFont
        {
            Size = (float)label.FontSize * context.Scale
        };
        
        // 处理字体粗细
        if (label.FontWeight == "Bold")
        {
            font.Typeface = SKTypeface.FromFamilyName(null, SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
        }
        
        // 计算文本位置
        var x = bounds.Left;
        var y = bounds.Top + font.Size;
        
        context.Canvas.DrawText(label.Text, x, y, font, paint);
    }
    
    private static SKColor ParseColor(string? color, SKColor defaultColor)
    {
        if (string.IsNullOrEmpty(color))
            return defaultColor;
        
        return SKColor.TryParse(color, out var result) ? result : defaultColor;
    }
}

/// <summary>
/// Button 渲染器
/// </summary>
public class ButtonRenderer : ISkiaControlRenderer
{
    public Type TargetType => typeof(Button);
    
    public void Render(
        IComponent component, 
        SkiaRenderContext context, 
        SKRect bounds,
        Action<IComponent, SkiaRenderContext, SKRect> renderChildren)
    {
        var button = (Button)component;
        
        // 绘制背景
        var bgColor = ParseColor(button.BackgroundColor, new SKColor(0x00, 0x7A, 0xFF));
        using var bgPaint = new SKPaint
        {
            IsAntialias = true,
            Color = bgColor,
            Style = SKPaintStyle.Fill
        };
        
        var cornerRadius = (float)button.CornerRadius * context.Scale;
        context.Canvas.DrawRoundRect(bounds, cornerRadius, cornerRadius, bgPaint);
        
        // 绘制文本
        if (!string.IsNullOrEmpty(button.Text))
        {
            using var textPaint = new SKPaint
            {
                IsAntialias = true,
                Color = ParseColor(button.TextColor, SKColors.White)
            };
            
            using var font = new SKFont
            {
                Size = (float)button.FontSize * context.Scale
            };
            
            // 居中绘制
            var textWidth = font.MeasureText(button.Text);
            var x = bounds.Left + (bounds.Width - textWidth) / 2;
            var y = bounds.Top + (bounds.Height + font.Size) / 2 - font.Size * 0.2f;
            
            context.Canvas.DrawText(button.Text, x, y, font, textPaint);
        }
    }
    
    private static SKColor ParseColor(string? color, SKColor defaultColor)
    {
        if (string.IsNullOrEmpty(color))
            return defaultColor;
        
        return SKColor.TryParse(color, out var result) ? result : defaultColor;
    }
}

/// <summary>
/// TextContent 渲染器
/// </summary>
public class TextContentRenderer : ISkiaControlRenderer
{
    public Type TargetType => typeof(TextContent);
    
    public void Render(
        IComponent component, 
        SkiaRenderContext context, 
        SKRect bounds,
        Action<IComponent, SkiaRenderContext, SKRect> renderChildren)
    {
        var textContent = (TextContent)component;
        
        if (string.IsNullOrEmpty(textContent.Text))
            return;
        
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color = SKColors.Black
        };
        
        using var font = new SKFont
        {
            Size = 14f * context.Scale
        };
        
        context.Canvas.DrawText(textContent.Text, bounds.Left, bounds.Top + font.Size, font, paint);
    }
}