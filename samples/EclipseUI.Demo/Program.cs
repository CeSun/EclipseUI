using EclipseUI.Host;

// 创建并显示窗口
var window = new EclipseWindow
{
    Title = "EclipseUI Demo - TextBox 测试",
    Width = 900,
    Height = 700
};

window.Show<EclipseUI.Demo.TextBoxTestPage>();
