using Eclipse.Core;
using Eclipse.Core.Abstractions;
using Eclipse.Input;
using Eclipse.Rendering;
using System;

namespace Eclipse.Controls;

/// <summary>
/// 图片控件
/// </summary>
public class Image : ComponentBase
{
    public override bool IsVisible => true;
    private string? _loadedImageKey;

    public string? Source { get; set; }
    public double Width { get; set; } = -1;
    public double Height { get; set; } = -1;
    public Stretch Stretch { get; set; } = Stretch.Uniform;

    public override Size Measure(Size availableSize, IDrawingContext context)
    {
        var scaledWidth = Width > 0 ? Width * context.Scale : availableSize.Width;
        var scaledHeight = Height > 0 ? Height * context.Scale : availableSize.Height;

        // If fixed size is specified, use it
        if (Width > 0 && Height > 0)
            return new Size(scaledWidth, scaledHeight);

        // Otherwise, return available space or default size
        return new Size(
            Width > 0 ? scaledWidth : 100 * context.Scale,
            Height > 0 ? scaledHeight : 100 * context.Scale);
    }

    public override void Build(IBuildContext context) { }

    public override void Render(IDrawingContext context, Rect bounds)
    {
        if (string.IsNullOrEmpty(Source))
        {
            context.DrawRectangle(bounds, Color.LightGray);
            return;
        }

        if (_loadedImageKey == null || !string.Equals(_loadedImageKey, Source, StringComparison.OrdinalIgnoreCase))
            _loadedImageKey = context.LoadImage(Source);

        if (_loadedImageKey == null)
        {
            context.DrawRectangle(bounds, Color.LightGray);
            return;
        }

        context.DrawImage(_loadedImageKey, bounds, Stretch);
    }
}
