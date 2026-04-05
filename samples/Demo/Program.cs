namespace Eclipse.Demo;

static class Program
{
    [STAThread]
    static void Main()
    {
        // 简化的 API - 只需指定 EUI 组件类型
        Eclipse.Windows.Application.Run<Components.HomePage>();
    }
}