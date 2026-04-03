using Eclipse.Core.Abstractions;

namespace Eclipse.Core;

/// <summary>
/// 纯文本内容组件 - 用于表示标记中的文本节点和表达式节点
/// </summary>
public class TextContent : ComponentBase
{
    public string? Text { get; set; }
    
    public override void Render(IBuildContext context)
    {
        // Render 空实现 - 属性由生成代码通过 BeginComponent(out var component) 设置
        // 实际文本渲染由具体渲染器处理
    }
}