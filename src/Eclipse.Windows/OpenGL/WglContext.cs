using System;
using System.Runtime.InteropServices;
using SkiaSharp;

namespace Eclipse.Windows.OpenGL;

/// <summary>
/// Windows OpenGL 上下文
/// </summary>
public class WglContext : IDisposable
{
    private IntPtr _hdc;
    private IntPtr _hglrc;
    private IntPtr _hwnd;
    private GRContext? _grContext;
    private GRGlInterface? _glInterface;
    private bool _isDisposed;

    public IntPtr Hdc => _hdc;
    public IntPtr Hglrc => _hglrc;
    public GRContext? GrContext => _grContext;

    public WglContext(IntPtr hwnd)
    {
        _hwnd = hwnd;
        Initialize();
    }

    private void Initialize()
    {
        // 获取设备上下文
        _hdc = Wgl.GetDC(_hwnd);
        if (_hdc == IntPtr.Zero)
        {
            throw new InvalidOperationException("Failed to get device context.");
        }

        // 设置像素格式
        var pfd = new Wgl.PIXELFORMATDESCRIPTOR
        {
            nSize = (ushort)Marshal.SizeOf<Wgl.PIXELFORMATDESCRIPTOR>(),
            nVersion = 1,
            dwFlags = Wgl.PFD_DRAW_TO_WINDOW | Wgl.PFD_SUPPORT_OPENGL | Wgl.PFD_DOUBLEBUFFER,
            iPixelType = Wgl.PFD_TYPE_RGBA,
            cColorBits = 32,
            cDepthBits = 24,
            cStencilBits = 8,
            cAlphaBits = 8,
            iLayerType = Wgl.PFD_MAIN_PLANE
        };

        var pixelFormat = Wgl.ChoosePixelFormat(_hdc, ref pfd);
        if (pixelFormat == 0)
        {
            throw new InvalidOperationException("Failed to choose pixel format.");
        }

        if (!Wgl.SetPixelFormat(_hdc, pixelFormat, ref pfd))
        {
            throw new InvalidOperationException("Failed to set pixel format.");
        }

        // 创建 OpenGL 上下文
        _hglrc = Wgl.wglCreateContext(_hdc);
        if (_hglrc == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Failed to create OpenGL context. Error: {Marshal.GetLastWin32Error()}");
        }

        // 激活上下文
        MakeCurrent();

        // 创建 SkiaSharp GRContext
        // 尝试使用带委托的方式，并回退到空参数
        try
        {
            _glInterface = GRGlInterface.Create(name => 
            {
                var ptr = Wgl.wglGetProcAddress(name);
                if (ptr == IntPtr.Zero)
                {
                    // 尝试从 opengl32.dll 获取
                    ptr = NativeMethods.GetProcAddress(GetOpenGL32Handle(), name);
                }
                return ptr;
            });
        }
        catch
        {
            // 回退
            _glInterface = GRGlInterface.Create();
        }
        
        if (_glInterface == null)
        {
            throw new InvalidOperationException("Failed to create GL interface.");
        }

        _grContext = GRContext.CreateGl(_glInterface);
        if (_grContext == null)
        {
            throw new InvalidOperationException("Failed to create GRContext.");
        }

        Console.WriteLine("OpenGL context created successfully.");
    }

    public void MakeCurrent()
    {
        if (_hglrc != IntPtr.Zero)
        {
            if (!Wgl.wglMakeCurrent(_hdc, _hglrc))
            {
                throw new InvalidOperationException("Failed to make context current.");
            }
        }
    }

    public void SwapBuffers()
    {
        if (_hdc != IntPtr.Zero)
        {
            Wgl.SwapBuffers(_hdc);
        }
    }

    public void GetFramebufferInfo(out int framebuffer, out int samples, out int stencil)
    {
        Wgl.glGetIntegerv(Wgl.GL_FRAMEBUFFER_BINDING, out framebuffer);
        Wgl.glGetIntegerv(Wgl.GL_STENCIL_BITS, out stencil);
        Wgl.glGetIntegerv(Wgl.GL_SAMPLES, out samples);
    }

    private static IntPtr _openGL32Handle;
    private static IntPtr GetOpenGL32Handle()
    {
        if (_openGL32Handle == IntPtr.Zero)
        {
            _openGL32Handle = NativeMethods.LoadLibrary("opengl32.dll");
        }
        return _openGL32Handle;
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _grContext?.Dispose();
        _grContext = null;
        _glInterface?.Dispose();
        _glInterface = null;

        if (_hglrc != IntPtr.Zero)
        {
            Wgl.wglMakeCurrent(IntPtr.Zero, IntPtr.Zero);
            Wgl.wglDeleteContext(_hglrc);
            _hglrc = IntPtr.Zero;
        }

        if (_hdc != IntPtr.Zero && _hwnd != IntPtr.Zero)
        {
            Wgl.ReleaseDC(_hwnd, _hdc);
            _hdc = IntPtr.Zero;
        }
    }
}