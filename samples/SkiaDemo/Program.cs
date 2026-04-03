using Eclipse.Core;
using Eclipse.Core.Abstractions;
using Eclipse.Skia.Controls;
using Eclipse.Windows;

namespace Eclipse.SkiaDemo;

static class Program
{
    [STAThread]
    static void Main()
    {
        // 构建组件树
        var context = new BuildContext();
        
        using (context.BeginComponent<StackLayout>(new ComponentId(1), out var root))
        {
            root.Spacing = 16;
            root.Padding = 20;
            
            using (context.BeginChildContent())
            {
                // 标题
                using (context.BeginComponent<Eclipse.Skia.Controls.Label>(new ComponentId(2), out var title))
                {
                    title.Text = "Hello EclipseUI!";
                    title.FontSize = 32;
                    title.FontWeight = "Bold";
                }
                
                // 副标题
                using (context.BeginComponent<Eclipse.Skia.Controls.Label>(new ComponentId(3), out var subtitle))
                {
                    subtitle.Text = "SkiaSharp 渲染演示";
                    subtitle.FontSize = 18;
                    subtitle.Color = "#666";
                }
                
                // 按钮
                using (context.BeginComponent<Eclipse.Skia.Controls.Button>(new ComponentId(4), out var button1))
                {
                    button1.Text = "Click Me";
                    button1.BackgroundColor = "#007AFF";
                    button1.TextColor = "White";
                    button1.FontSize = 16;
                    button1.CornerRadius = 8;
                }
                
                // 另一个按钮
                using (context.BeginComponent<Eclipse.Skia.Controls.Button>(new ComponentId(5), out var button2))
                {
                    button2.Text = "Secondary";
                    button2.BackgroundColor = "#6C757D";
                    button2.TextColor = "White";
                }
                
                // 更多文本
                using (context.BeginComponent<Eclipse.Skia.Controls.Label>(new ComponentId(6), out var desc))
                {
                    desc.Text = "这是一个使用 EclipseUI 框架和 SkiaSharp 渲染的示例应用。";
                    desc.FontSize = 14;
                    desc.Color = "#333";
                }
            }
        }
        
        // 运行窗口
        Eclipse.Windows.Application.Run(context.RootComponent!);
    }
}