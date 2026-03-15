using EclipseUI.Host;

// 创建并显示窗口
var window = new EclipseWindow
{
    Title = "EclipseUI Demo - 选择控件测试",
    Width = 700,
    Height = 900
};

window.Show<EclipseUI.Demo.SelectionControlsTestPage>();
