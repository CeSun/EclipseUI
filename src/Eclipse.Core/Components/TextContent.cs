using Eclipse.Core.Abstractions;
using Eclipse.Input;
using Eclipse.Rendering;

namespace Eclipse.Core;

/// <summary>
/// 纯文本内容组件 - 用于表示标记中的文本节点和表达式节点
/// </summary>
public class TextContent : ComponentBase
{
    public string? Text { get; set; }
    
    public override void Build(IBuildContext context)
    {
        // Build 空实现 - 属性由生成代码通过 BeginComponent(out var component) 设置
    }
    
    public override void Render(DrawingContext context, Rect bounds)
    {
        if (!string.IsNullOrEmpty(Text))
        {
            context.DrawText(Text, bounds.X, bounds.Y, 14.0 * context.Scale);
        }
    }
}