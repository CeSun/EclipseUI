using Silk.NET.Windowing;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using SkiaSharp;
using EclipseUI.Core;
using Microsoft.AspNetCore.Components;
using Silk.NET.Maths;

namespace EclipseUI.Host;

public class EclipseWindow : IDisposable
{
    private IWindow? _window;
    private IInputContext? _input;
    private GL? _gl;
    private SKSurface? _surface;
    private GRContext? _grContext;
    private EclipseRenderer? _renderer;
    private EclipseApplicationContext? _context;
    private bool _disposed;
    
    public string Title { get; set; } = "EclipseUI";
    public int Width { get; set; } = 800;
    public int Height { get; set; } = 600;
    
    public void Show<TComponent>(Dictionary<string, object>? parameters = null) where TComponent : IComponent
    {
        var options = WindowOptions.Default with
        {
            Title = Title,
            Size = new Silk.NET.Maths.Vector2D<int>(Width, Height),
            WindowState = WindowState.Normal,
            IsVisible = true,
            API = GraphicsAPI.Default
        };
        
        _window = Window.Create(options);
        _window.Load += OnLoad;
        _window.Render += OnRender;
        _window.Closing += OnClosing;
        _window.FramebufferResize += OnResize;
        
        var builder = new EclipseApplicationBuilder();
        _context = builder.Build();
        _renderer = _context.Renderer;
        _ = _context.RunAsync<TComponent>(parameters);
        
        _context.RenderRequested += () => { };
        
        _window.Run();
        Dispose();
    }
    
    private void OnLoad()
    {
        if (_window == null || _renderer == null) return;
        
        _gl = _window.CreateOpenGL();
        _gl.Viewport(0, 0, (uint)_window.Size.X, (uint)_window.Size.Y);
        
        var glInterface = GRGlInterface.Create();
        _grContext = GRContext.CreateGl(glInterface);
        
        var fbInfo = new GRGlFramebufferInfo(0, SKColorType.Rgba8888.ToGlSizedFormat());
        var rt = new GRBackendRenderTarget(_window.Size.X, _window.Size.Y, 0, 8, fbInfo);
        _surface = SKSurface.Create(_grContext, rt, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888);
        
        _renderer.SetSurface(_surface.Canvas, _window.Size.X, _window.Size.Y);
        _context?.SetSurfaceSize(_window.Size.X, _window.Size.Y);
        
        _input = _window.CreateInput();
        foreach (var mouse in _input.Mice)
        {
            mouse.Click += (m, b, p) =>
            {
                var pos = m.Position;
                if (_renderer != null)
                {
                    _renderer.HandleClick(pos.X, pos.Y);
                }
            };
        }
    }
    
    private unsafe void OnResize(Silk.NET.Maths.Vector2D<int> newSize)
    {
        if (_gl == null || _grContext == null || _renderer == null) return;
        
        _gl.Viewport(0, 0, (uint)newSize.X, (uint)newSize.Y);
        
        _surface?.Dispose();
        
        var fbInfo = new GRGlFramebufferInfo(0, SKColorType.Rgba8888.ToGlSizedFormat());
        var rt = new GRBackendRenderTarget(newSize.X, newSize.Y, 0, 8, fbInfo);
        _surface = SKSurface.Create(_grContext, rt, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888);
        
        _renderer.SetSurface(_surface.Canvas, newSize.X, newSize.Y);
        _context?.SetSurfaceSize(newSize.X, newSize.Y);
    }
    
    private void OnRender(double dt)
    {
        if (_surface == null || _renderer == null || _gl == null || _grContext == null) return;
        
        _gl.ClearColor(1, 1, 1, 1);
        _gl.Clear((uint)GLEnum.ColorBufferBit);
        
        _renderer.PerformRender();
        _surface.Flush();
        _grContext.Flush();
    }
    
    private void OnClosing()
    {
        // 在窗口关闭时先释放 SkiaSharp 资源
        _surface?.Dispose();
        _surface = null;
        _grContext?.Dispose();
        _grContext = null;
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            // 先释放输入
            _input?.Dispose();
            _input = null;
            
            // 窗口已经由 OnClosing 清理了 SkiaSharp 资源
            _window = null;
            _renderer = null;
            _context = null;
            _gl = null;
            
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
