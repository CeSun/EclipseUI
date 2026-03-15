using EclipseUI.Host;

// 创建并显示窗口
var window = new EclipseWindow
{
    Title = "EclipseUI Demo - 控件测试",
    Width = 600,
    Height = 700
};

window.Show<EclipseUI.Demo.MainPage>();
