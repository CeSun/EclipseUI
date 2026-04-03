using Eclipse.Core;
using Eclipse.Core.Abstractions;
using Eclipse.Skia;
using SkiaSharp;
using SkiaSharp.Views.Desktop;

namespace Eclipse.Windows;

/// <summary>
/// Windows 窗口 - 使用 WinForms + SkiaSharp
/// </summary>
public class Window : IDisposable
{
    private readonly Form _form;
    private readonly SKControl _skControl;
    private readonly DefaultSkiaRenderer _renderer;
    private IComponent? _rootComponent;
    
    public string Title
    {
        get => _form.Text;
        set => _form.Text = value;
    }
    
    public int Width
    {
        get => _form.Width;
        set => _form.Width = value;
    }
    
    public int Height
    {
        get => _form.Height;
        set => _form.Height = value;
    }
    
    public IComponent? Content
    {
        get => _rootComponent;
        set
        {
            _rootComponent = value;
            _skControl.Invalidate();
        }
    }
    
    public Window()
    {
        _renderer = new DefaultSkiaRenderer();
        
        _form = new Form
        {
            Text = "EclipseUI",
            Width = 800,
            Height = 600,
            StartPosition = FormStartPosition.CenterScreen
        };
        
        _skControl = new SKControl
        {
            Dock = DockStyle.Fill
        };
        
        _skControl.PaintSurface += OnPaintSurface;
        _form.Controls.Add(_skControl);
        
        _form.Resize += (s, e) => _skControl.Invalidate();
    }
    
    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        if (_rootComponent == null)
            return;
        
        var canvas = e.Surface.Canvas;
        var scale = _skControl.DeviceDpi / 96f;
        
        var context = new SkiaRenderContext(
            canvas,
            e.Info.Width / scale,
            e.Info.Height / scale,
            scale);
        
        _renderer.Render(_rootComponent, context);
    }
    
    public void Show()
    {
        _form.Show();
    }
    
    public void ShowDialog()
    {
        System.Windows.Forms.Application.Run(_form);
    }
    
    public void Close()
    {
        _form.Close();
    }
    
    public void Dispose()
    {
        _skControl.Dispose();
        _form.Dispose();
    }
}

/// <summary>
/// 应用程序入口
/// </summary>
public static class Application
{
    public static void Run(IComponent rootComponent)
    {
        System.Windows.Forms.Application.EnableVisualStyles();
        System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
        
        using var window = new Window
        {
            Content = rootComponent
        };
        
        window.ShowDialog();
    }
    
    public static void Run(Window window)
    {
        System.Windows.Forms.Application.EnableVisualStyles();
        System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
        
        window.ShowDialog();
    }
}