using System;
using System.Runtime.InteropServices;
using Eclipse.Core;
using Eclipse.Core.Abstractions;
using Eclipse.Input;
using Eclipse.Windows.Input;
using Eclipse.Skia;
using Eclipse.Windows.Rendering;
using Eclipse.Windows.OpenGL;
using Eclipse.Windows.Angle;
using SkiaSharp;

namespace Eclipse.Windows;

/// <summary>
/// 基于 Win32 API 的窗口实现，支持 CPU、OpenGL 和 ANGLE 渲染
/// </summary>
public class WindowImpl : IDisposable
{
    private static readonly NativeMethods.WndProcDelegate WndProcDelegate = WndProc;
    private static readonly IntPtr DefaultCursor = NativeMethods.LoadCursor(IntPtr.Zero, NativeMethods.IDC_ARROW);

    private IntPtr _hwnd;
    private string _className = string.Empty;
    private ISkiaRenderer? _renderer;
    private IComponent? _content;
    private float _scaling = 1.0f;
    private bool _isDisposed;

    // 输入系统
    private InputManager? _inputManager;
    private WindowsInputAdapter? _inputAdapter;

    // 渲染后端
    private RenderBackend _backend = RenderBackend.OpenGL; // 默认使用 OpenGL，ANGLE 有兼容性问题
    private WglContext? _glContext;
    private AngleD3D11Context? _angleContext;
    private GRContext? _grContext;

    // CPU 双缓冲
    private IntPtr _hBitmap;
    private IntPtr _ppvBits;
    private int _lastWidth;
    private int _lastHeight;

    /// <summary>
    /// 渲染后端类型
    /// </summary>
    public enum RenderBackend
    {
        CPU,        // CPU 光栅化
        OpenGL,     // GPU 加速 (原生 OpenGL)
        Angle       // GPU 加速 (ANGLE/D3D11)
    }

    public string Title
    {
        get => GetWindowTitle();
        set => SetWindowTitle(value);
    }

    public int Width { get; private set; } = 800;
    public int Height { get; private set; } = 600;

    public IComponent? Content
    {
        get => _content;
        set
        {
            // 取消旧内容的订阅
            if (_content != null)
            {
                _content.StateChanged -= OnContentStateChanged;
            }
            
            _content = value;
            
            // 订阅新内容的状态变化
            if (_content != null)
            {
                _content.StateChanged += OnContentStateChanged;
            }
            
            // 设置输入系统的根元素
            // 使用 Content 的第一个子元素（通常是布局容器）作为 RootElement
            if (_inputManager != null)
            {
                if (_content is IInputElement inputElement)
                {
                    _inputManager.RootElement = inputElement;
                }
                else if (_content?.Children.Count > 0 && _content.Children[0] is IInputElement firstChild)
                {
                    _inputManager.RootElement = firstChild;
                }
            }
            
            Invalidate();
        }
    }
    
    private void OnContentStateChanged(object? sender, EventArgs e)
    {
        // 状态改变时触发重绘
        Invalidate();
    }

    public IntPtr Handle => _hwnd;

    /// <summary>
    /// 获取或设置渲染后端
    /// </summary>
    public RenderBackend Backend
    {
        get => _backend;
        set
        {
            if (_backend != value)
            {
                _backend = value;
                if (_hwnd != IntPtr.Zero)
                {
                    InitializeBackend();
                }
            }
        }
    }

    public WindowImpl() : this(RenderBackend.Angle, null, null)
    {
    }

    public WindowImpl(RenderBackend backend) : this(backend, null, null)
    {
    }

    public WindowImpl(RenderBackend backend, InputManager? inputManager, ISkiaRenderer? renderer)
    {
        _backend = backend;
        RegisterWindowClass();
        CreateWindow();
        InitializeRenderer(inputManager, renderer);
        InitializeBackend();
    }

    private void RegisterWindowClass()
    {
        _className = $"EclipseUI_{Guid.NewGuid():N}";

        var wc = new NativeMethods.WNDCLASSEX
        {
            cbSize = Marshal.SizeOf<NativeMethods.WNDCLASSEX>(),
            style = 0,
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(WndProcDelegate),
            cbClsExtra = 0,
            cbWndExtra = 0,
            hInstance = NativeMethods.GetModuleHandle(null),
            hIcon = IntPtr.Zero,
            hCursor = DefaultCursor,
            hbrBackground = IntPtr.Zero,
            lpszMenuName = null,
            lpszClassName = _className,
            hIconSm = IntPtr.Zero
        };

        NativeMethods.RegisterClassEx(ref wc);
    }

    private void CreateWindow()
    {
        var style = NativeMethods.WS_OVERLAPPEDWINDOW | NativeMethods.WS_CLIPCHILDREN | NativeMethods.WS_CLIPSIBLINGS;
        var exStyle = NativeMethods.WS_EX_APPWINDOW;

        _hwnd = NativeMethods.CreateWindowEx(
            exStyle,
            _className,
            "EclipseUI",
            style,
            NativeMethods.CW_USEDEFAULT,
            NativeMethods.CW_USEDEFAULT,
            Width,
            Height,
            IntPtr.Zero,
            IntPtr.Zero,
            NativeMethods.GetModuleHandle(null),
            IntPtr.Zero);

        if (_hwnd == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Failed to create window. Error: {Marshal.GetLastWin32Error()}");
        }

        _windowMap[_hwnd] = this;
    }

    private void InitializeRenderer(InputManager? inputManager, ISkiaRenderer? renderer)
    {
        // 使用注入的 renderer 或创建默认的
        _renderer = renderer ?? new ComponentRenderer();
        
        // 使用注入的 InputManager，如果没有则创建新的
        _inputManager = inputManager ?? new InputManager();
        _inputAdapter = new WindowsInputAdapter(_hwnd, _inputManager);
    }

    private void InitializeBackend()
    {
        CleanupBackend();

        if (_backend == RenderBackend.Angle)
        {
            try
            {
                _angleContext = new AngleD3D11Context(_hwnd);
                _grContext = _angleContext.GrContext;
                Console.WriteLine("Using ANGLE (D3D11) backend");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize ANGLE: {ex.Message}");
                Console.WriteLine("Falling back to OpenGL backend");
                _backend = RenderBackend.OpenGL;
            }
        }

        if (_backend == RenderBackend.OpenGL)
        {
            try
            {
                _glContext = new WglContext(_hwnd);
                _grContext = _glContext.GrContext;
                Console.WriteLine("Using OpenGL backend");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize OpenGL: {ex.Message}");
                Console.WriteLine("Falling back to CPU backend");
                _backend = RenderBackend.CPU;
            }
        }

        if (_backend == RenderBackend.CPU)
        {
            Console.WriteLine("Using CPU backend");
        }
    }

    private void CleanupBackend()
    {
        _grContext = null;
        _angleContext?.Dispose();
        _angleContext = null;
        _glContext?.Dispose();
        _glContext = null;

        if (_hBitmap != IntPtr.Zero)
        {
            NativeMethods.DeleteObject(_hBitmap);
            _hBitmap = IntPtr.Zero;
        }
    }

    public void Show()
    {
        NativeMethods.ShowWindow(_hwnd, NativeMethods.SW_SHOWNORMAL);
        NativeMethods.UpdateWindow(_hwnd);
    }

    public void ShowDialog()
    {
        Show();
        RunMessageLoop();
    }

    private void RunMessageLoop()
    {
        while (NativeMethods.GetMessage(out var msg, IntPtr.Zero, 0, 0))
        {
            NativeMethods.TranslateMessage(ref msg);
            NativeMethods.DispatchMessage(ref msg);
        }
    }

    public void Close()
    {
        NativeMethods.DestroyWindow(_hwnd);
    }

    public void Invalidate()
    {
        if (_hwnd != IntPtr.Zero)
        {
            NativeMethods.InvalidateRect(_hwnd, IntPtr.Zero, false);
        }
    }

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<IntPtr, WindowImpl> _windowMap = new();

    private static IntPtr WndProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam)
    {
        if (_windowMap.TryGetValue(hWnd, out var window))
        {
            return window.HandleMessage(uMsg, wParam, lParam);
        }
        return NativeMethods.DefWindowProc(hWnd, uMsg, wParam, lParam);
    }

    private IntPtr HandleMessage(uint uMsg, IntPtr wParam, IntPtr lParam)
    {
        // 输入消息优先处理
        if (IsInputMessage(uMsg))
        {
            if (_inputAdapter != null)
            {
                _inputAdapter.ProcessMessage(uMsg, wParam, lParam);
                return IntPtr.Zero;
            }
        }
        
        switch (uMsg)
        {
            case NativeMethods.WM_PAINT:
                OnPaint();
                return IntPtr.Zero;

            case NativeMethods.WM_ERASEBKGND:
                return new IntPtr(1);

            case NativeMethods.WM_SIZE:
                OnSize(wParam, lParam);
                return IntPtr.Zero;
                
            case NativeMethods.WM_SETFOCUS:
                OnGotFocus();
                return IntPtr.Zero;
                
            case NativeMethods.WM_KILLFOCUS:
                OnLostFocus();
                return IntPtr.Zero;

            case NativeMethods.WM_CLOSE:
                Close();
                return IntPtr.Zero;

            case NativeMethods.WM_DESTROY:
                _windowMap.TryRemove(_hwnd, out _);
                CleanupBackend();
                NativeMethods.PostQuitMessage(0);
                return IntPtr.Zero;

            default:
                return NativeMethods.DefWindowProc(_hwnd, uMsg, wParam, lParam);
        }
    }
    
    private static bool IsInputMessage(uint uMsg)
    {
        return uMsg switch
        {
            >= 0x0200 and <= 0x020E => true,  // 鼠标消息
            0x0100 or 0x0101 or 0x0102 or 0x0104 or 0x0105 => true,  // 键盘消息
            0x0240 => true,  // 触摸消息
            _ => false
        };
    }

    private void OnPaint()
    {
        var ps = new NativeMethods.PAINTSTRUCT();
        var hdc = NativeMethods.BeginPaint(_hwnd, out ps);

        try
        {
            NativeMethods.GetClientRect(_hwnd, out var rect);
            Width = rect.Width;
            Height = rect.Height;

            if (Width > 0 && Height > 0 && _content != null)
            {
                if (_backend == RenderBackend.Angle && _angleContext != null)
                {
                    RenderAngle(rect);
                }
                else if (_backend == RenderBackend.OpenGL && _glContext != null)
                {
                    RenderOpenGL(rect);
                }
                else
                {
                    RenderCPU(hdc, rect);
                }
            }
        }
        finally
        {
            NativeMethods.EndPaint(_hwnd, ref ps);
        }
    }

    private void RenderAngle(NativeMethods.RECT rect)
    {
        if (_angleContext == null || _renderer == null || _grContext == null) return;

        _angleContext.MakeCurrent();

        // 使用 SkiaSharp 的方式更新视口 - 通过在 canvas 上设置尺寸
        // 创建渲染目标
        var framebufferInfo = new GRGlFramebufferInfo(0, SKColorType.Rgba8888.ToGlSizedFormat());
        using var backendRenderTarget = new GRBackendRenderTarget(rect.Width, rect.Height, 0, 8, framebufferInfo);

        // 创建 Skia 表面
        using var surface = SKSurface.Create(_grContext, backendRenderTarget, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888);
        if (surface == null)
            return;

        var canvas = surface.Canvas;
        
        // 重置 canvas 矩阵以匹配新尺寸
        canvas.ResetMatrix();
        
        canvas.Clear(SKColors.White);

        var context = new SkiaRenderContext(canvas, rect.Width, rect.Height, _scaling);
        _renderer.Render(_content!, context);

        canvas.Flush();
        _grContext.Flush();

        _angleContext.SwapBuffers();
    }

    private void RenderOpenGL(NativeMethods.RECT rect)
    {
        if (_glContext == null || _renderer == null || _grContext == null) return;

        _glContext.MakeCurrent();
        _glContext.GetFramebufferInfo(out var framebuffer, out var samples, out var stencil);

        var framebufferInfo = new GRGlFramebufferInfo((uint)framebuffer, SKColorType.Rgba8888.ToGlSizedFormat());
        using var backendRenderTarget = new GRBackendRenderTarget(rect.Width, rect.Height, samples, stencil, framebufferInfo);

        using var surface = SKSurface.Create(_grContext, backendRenderTarget, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888);
        if (surface == null)
        {
            _backend = RenderBackend.CPU;
            Invalidate();
            return;
        }

        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        var context = new SkiaRenderContext(canvas, rect.Width, rect.Height, _scaling);
        _renderer.Render(_content!, context);

        canvas.Flush();
        _grContext.Flush();

        _glContext.SwapBuffers();
    }

    private void RenderCPU(IntPtr hdc, NativeMethods.RECT rect)
    {
        if (_renderer == null) return;

        EnsureBitmapBuffer(hdc, rect.Width, rect.Height);

        var memDc = NativeMethods.CreateCompatibleDC(hdc);
        var oldBitmap = NativeMethods.SelectObject(memDc, _hBitmap);

        try
        {
            RenderWithSkiaCpu(rect);
            NativeMethods.BitBlt(hdc, 0, 0, rect.Width, rect.Height, memDc, 0, 0, NativeMethods.SRCCOPY);
        }
        finally
        {
            NativeMethods.SelectObject(memDc, oldBitmap);
            NativeMethods.DeleteDC(memDc);
        }
    }

    private unsafe void RenderWithSkiaCpu(NativeMethods.RECT rect)
    {
        if (_content == null || _renderer == null) return;

        var info = new SKImageInfo(rect.Width, rect.Height, SKColorType.Bgra8888, SKAlphaType.Premul);

        using var surface = SKSurface.Create(info, _ppvBits, info.RowBytes);
        if (surface == null) return;

        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        var context = new SkiaRenderContext(canvas, rect.Width, rect.Height, _scaling);
        _renderer.Render(_content, context);

        canvas.Flush();
    }

    private void EnsureBitmapBuffer(IntPtr hdc, int width, int height)
    {
        if (_hBitmap != IntPtr.Zero && _lastWidth == width && _lastHeight == height)
            return;

        if (_hBitmap != IntPtr.Zero)
        {
            NativeMethods.DeleteObject(_hBitmap);
            _hBitmap = IntPtr.Zero;
        }

        var bmi = new NativeMethods.BITMAPINFOHEADER
        {
            biSize = (uint)Marshal.SizeOf<NativeMethods.BITMAPINFOHEADER>(),
            biWidth = width,
            biHeight = -height,
            biPlanes = 1,
            biBitCount = 32,
            biCompression = 0
        };

        _hBitmap = NativeMethods.CreateDIBSection(hdc, ref bmi, 0, out _ppvBits, IntPtr.Zero, 0);
        _lastWidth = width;
        _lastHeight = height;
    }

    private void OnSize(IntPtr wParam, IntPtr lParam)
    {
        Width = lParam.ToInt32() & 0xFFFF;
        Height = (lParam.ToInt32() >> 16) & 0xFFFF;
        
        // 调整 EGL Surface 大小
        if (_backend == RenderBackend.Angle && _angleContext != null)
        {
            _angleContext.ResizeSurface();
        }
        
        // 强制重绘整个窗口
        var rect = new NativeMethods.RECT
        {
            Left = 0,
            Top = 0,
            Right = Width,
            Bottom = Height
        };
        NativeMethods.RedrawWindow(_hwnd, ref rect, IntPtr.Zero, 
            NativeMethods.RDW_INVALIDATE | NativeMethods.RDW_ERASE | NativeMethods.RDW_ALLCHILDREN);
    }
    
    private void OnGotFocus()
    {
        // 窗口获得焦点
    }
    
    private void OnLostFocus()
    {
        // 窗口失去焦点
    }

    private string GetWindowTitle() => string.Empty;
    private void SetWindowTitle(string title) { }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        CleanupBackend();

        if (_hwnd != IntPtr.Zero)
        {
            _windowMap.TryRemove(_hwnd, out _);
            NativeMethods.DestroyWindow(_hwnd);
            _hwnd = IntPtr.Zero;
        }
    }
}