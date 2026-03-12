using EclipseUI.Host;

// 创建并显示窗口
var window = new EclipseWindow
{
    Title = "EclipseUI Demo 🌑",
    Width = 800,
    Height = 600
};

window.Show<EclipseUI.Demo.MainPage>();
