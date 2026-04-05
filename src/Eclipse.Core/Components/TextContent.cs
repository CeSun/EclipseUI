using Eclipse.Core.Abstractions;
using Eclipse.Input;
using Eclipse.Rendering;

namespace Eclipse.Core;

public class TextContent : ComponentBase
{
    public string? Text { get; set; }
    
    public override void Build(IBuildContext context) { }
    
    public override void Render(IDrawingContext context, Rect bounds)
    {
        if (!string.IsNullOrEmpty(Text))
        {
            context.DrawText(Text, bounds.X, bounds.Y, 14.0 * context.Scale);
        }
    }
}