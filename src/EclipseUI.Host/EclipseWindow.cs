using Silk.NET.Windowing;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using SkiaSharp;
using EclipseUI.Core;
using Microsoft.AspNetCore.Components;
using Silk.NET.Maths;
using System.Diagnostics.CodeAnalysis;

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
    
    // 用于累积 surrogate pair（emoji）
    private char? _pendingHighSurrogate;
    private System.Numerics.Vector2? _lastMouseDownPosition;
    
    public string Title { get; set; } = "EclipseUI";
    public int Width { get; set; } = 800;
    public int Height { get; set; } = 600;
    
    /// <summary>
    /// 静态构造函数 - 注册 Silk.NET 平台（裁剪/AOT 兼容）
    /// </summary>
    static EclipseWindow()
    {
        // 注册 GLFW 窗口和输入平台，防止裁剪时被移除
        Silk.NET.Windowing.Glfw.GlfwWindowing.RegisterPlatform();
        Silk.NET.Input.Glfw.GlfwInput.RegisterPlatform();
    }
    
    public void Show<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TComponent>(Dictionary<string, object>? parameters = null) where TComponent : IComponent
    {
        var options = WindowOptions.Default with
        {
            Title = Title,
            Size = new Silk.NET.Maths.Vector2D<int>(Width, Height),
            WindowState = WindowState.Normal,
            IsVisible = true,
            API = GraphicsAPI.Default,
            VSync = true // 启用垂直同步，稳定帧率到显示器刷新率
        };
        
        _window = Window.Create(options);
        _window.Load += OnLoad;
        _window.Render += OnRender;
        _window.Closing += OnClosing;
        _window.FramebufferResize += OnResize;
        
        var builder = new EclipseApplicationBuilder();
        _context = builder.Build();
        _renderer = _context.Renderer;
        // 直接添加用户组件，适配器会自动处理元素传递
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
            mouse.MouseDown += (m, b) =>
            {
                var pos = m.Position;
                _lastMouseDownPosition = pos;  // 记录按下时的位置
                if (_renderer != null)
                {
                    _renderer.HandleMouseDown(pos.X, pos.Y);
                }
            };
            
            mouse.Click += (m, b, p) =>
            {
                if (_renderer != null)
                {
                    // 使用 MouseDown 时记录的位置，而非 Click 事件的位置
                    var pos = _lastMouseDownPosition ?? p;
                    _renderer.HandleClick(pos.X, pos.Y);
                }
            };
            
            mouse.Scroll += (m, s) =>
            {
                if (_renderer != null)
                {
                    var pos = m.Position;
                    _renderer.HandleMouseWheel(pos.X, pos.Y, s.Y);
                }
            };
            
            mouse.MouseMove += (m, p) =>
            {
                if (_renderer != null)
                {
                    _renderer.HandleMouseMove(p.X, p.Y);
                }
            };
        }
        
        // 鼠标释放事件
        foreach (var mouse in _input.Mice)
        {
            mouse.MouseUp += (m, b) =>
            {
                if (b == Silk.NET.Input.MouseButton.Left || b == Silk.NET.Input.MouseButton.Right)
                {
                    _renderer?.HandleMouseUp();
                }
            };
        }
        
        // 键盘事件
        foreach (var keyboard in _input.Keyboards)
        {
            keyboard.KeyDown += async (k, key, _) =>
            {
                if (_renderer != null)
                {
                    var keyName = GetKeyName(key);
                    await SafeHandleKeyDown(keyName);
                }
            };
            
            keyboard.KeyChar += async (k, c) =>
            {
                if (_renderer == null || char.IsControl(c)) return;
                
                string text;
                
                // 处理 surrogate pair（emoji 等）
                if (char.IsHighSurrogate(c))
                {
                    _pendingHighSurrogate = c;
                    return;
                }
                else if (char.IsLowSurrogate(c) && _pendingHighSurrogate.HasValue)
                {
                    text = new string(new char[] { _pendingHighSurrogate.Value, c });
                    _pendingHighSurrogate = null;
                }
                else if (char.IsLowSurrogate(c))
                {
                    // 孤立的 low surrogate，跳过
                    return;
                }
                // 修复 GLFW 截断的 emoji（U+1Fxxx 被截断成 U+Fxxx）
                else if (c >= 0xF000 && c <= 0xF8FF)
                {
                    int codePoint = 0x10000 + c;
                    int highSurrogate = 0xD800 + ((codePoint - 0x10000) >> 10);
                    int lowSurrogate = 0xDC00 + ((codePoint - 0x10000) & 0x3FF);
                    text = new string(new char[] { (char)highSurrogate, (char)lowSurrogate });
                    _pendingHighSurrogate = null;
                }
                else
                {
                    _pendingHighSurrogate = null;
                    text = c.ToString();
                }
                
                await SafeHandleTextInput(text);
            };
        }
    }
    
    private async Task SafeHandleKeyDown(string keyName)
    {
        try
        {
            var renderer = _renderer;
            if (renderer != null)
            {
                await renderer.HandleKeyDown(keyName);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[KeyDown Error] {keyName}: {ex.Message}");
        }
    }
    
    private async Task SafeHandleTextInput(string text)
    {
        try
        {
            var renderer = _renderer;
            if (renderer != null)
            {
                await renderer.HandleTextInput(text);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TextInput Error] '{text}': {ex.Message}");
        }
    }
    
    /// <summary>
    /// 将 Silk.NET 的 Key 枚举转换为字符串名称
    /// </summary>
    private static string GetKeyName(Silk.NET.Input.Key key)
    {
        return key switch
        {
            Silk.NET.Input.Key.Backspace => "Backspace",
            Silk.NET.Input.Key.Delete => "Delete",
            Silk.NET.Input.Key.Left => "ArrowLeft",
            Silk.NET.Input.Key.Right => "ArrowRight",
            Silk.NET.Input.Key.Up => "ArrowUp",
            Silk.NET.Input.Key.Down => "ArrowDown",
            Silk.NET.Input.Key.Home => "Home",
            Silk.NET.Input.Key.End => "End",
            Silk.NET.Input.Key.Enter => "Enter",
            Silk.NET.Input.Key.Tab => "Tab",
            Silk.NET.Input.Key.Escape => "Escape",
            Silk.NET.Input.Key.Space => "Space",
            _ => key.ToString()
        };
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
        
        // 标记 UI 为脏，触发完整重绘以适应新尺寸
        _renderer.MarkDirty();
    }
    
    private void OnRender(double dt)
    {
        if (_surface == null || _renderer == null || _gl == null || _grContext == null) return;
        
        _gl.ClearColor(1, 1, 1, 1);
        _gl.Clear((uint)GLEnum.ColorBufferBit);
        
        _renderer.PerformRender();
        
        // 优化 GPU 刷新：使用默认刷新
        _surface?.Flush();
        _grContext?.Flush();
        
        // 确保 OpenGL 命令执行（可选，可能影响性能）
        // _gl.Finish();
    }
    
    private void OnClosing()
    {
        try
        {
            // 在窗口关闭时先释放 SkiaSharp 资源
            _surface?.Dispose();
            _surface = null;
            _grContext?.Dispose();
            _grContext = null;
        }
        catch
        {
            // 忽略关闭时的任何异常
        }
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            // 注意：Silk.NET 的 Input 上下文在 Dispose 时可能会抛出内部 CLR 错误
            // 这是 Silk.NET 的已知问题，我们尽量安全地清理资源
            
            try
            {
                // 先释放渲染器（可能有待处理的工作）
                _renderer?.Dispose();
                _renderer = null;
            }
            catch { }
            
            try
            {
                // 释放应用上下文
                _context = null;
            }
            catch { }
            
            try
            {
                // 释放 OpenGL
                _gl?.Dispose();
                _gl = null;
            }
            catch { }
            
            try
            {
                // 输入上下文可能会导致 CLR 错误，跳过显式 Dispose
                // _input?.Dispose();
                _input = null;
            }
            catch { }
            
            try
            {
                // 最后释放窗口
                _window?.Dispose();
                _window = null;
            }
            catch { }
            
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
