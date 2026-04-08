using Eclipse.Core;
using Eclipse.Core.Abstractions;
using Eclipse.Input;
using Eclipse.Rendering;
using System.Collections.Generic;

namespace Eclipse.Controls;

/// <summary>
/// 容器面板 - 可设置内边距和背景的通用容器
/// </summary>
public class Container : ComponentBase
{
    public override bool IsVisible => true;
    public Color BackgroundColor { get; set; } = Color.Transparent;
    public double Padding { get; set; } = 0;
    public double CornerRadius { get; set; } = 0;

    /// <summary>
    /// 固定宽度（-1 表示自动）
    /// </summary>
    public double Width { get; set; } = -1;

    /// <summary>
    /// 固定高度（-1 表示自动）
    /// </summary>
    public double Height { get; set; } = -1;

    protected override IEnumerable<IInputElement> GetInputChildren()
    {
        foreach (var child in Children)
        {
            if (child is IInputElement inputElement)
            {
                yield return inputElement;
            }
        }
    }

    public override Size Measure(Size availableSize, IDrawingContext context)
    {
        var scaledPadding = Padding * context.Scale;

        // If fixed size is specified, use it
        if (Width > 0 && Height > 0)
            return new Size(Width * context.Scale, Height * context.Scale);

        // Otherwise, measure children
        double maxWidth = 0;
        double maxHeight = 0;

        foreach (var child in Children)
        {
            var childSize = child.Measure(
                new Size(availableSize.Width - scaledPadding * 2, availableSize.Height - scaledPadding * 2),
                context);
            maxWidth = Math.Max(maxWidth, childSize.Width);
            maxHeight = Math.Max(maxHeight, childSize.Height);
        }

        return new Size(
            maxWidth + scaledPadding * 2,
            maxHeight + scaledPadding * 2);
    }

    public override void Arrange(Rect finalBounds, IDrawingContext context)
    {
        UpdateBounds(finalBounds);

        var scaledPadding = Padding * context.Scale;
        var contentBounds = new Rect(
            finalBounds.X + scaledPadding,
            finalBounds.Y + scaledPadding,
            finalBounds.Width - scaledPadding * 2,
            finalBounds.Height - scaledPadding * 2);

        // Arrange children
        foreach (var child in Children)
        {
            child.Arrange(contentBounds, context);
        }
    }

    public override void Build(IBuildContext context) { }

    public override void Render(IDrawingContext context, Rect bounds)
    {
        if (BackgroundColor != Color.Transparent)
            context.DrawRoundRect(bounds, BackgroundColor, CornerRadius * context.Scale);

        var contentBounds = new Rect(
            bounds.X + Padding * context.Scale,
            bounds.Y + Padding * context.Scale,
            bounds.Width - Padding * 2 * context.Scale,
            bounds.Height - Padding * 2 * context.Scale);

        foreach (var child in Children)
            child.Render(context, contentBounds);
    }
}
