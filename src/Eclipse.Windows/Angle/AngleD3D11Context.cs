using System;
using System.Runtime.InteropServices;
using SkiaSharp;

namespace Eclipse.Windows.Angle;

/// <summary>
/// ANGLE D3D11 后端上下文
/// </summary>
internal class AngleD3D11Context : IDisposable
{
    private AngleEglInterface _egl;
    private IntPtr _eglDisplay;
    private IntPtr _eglContext;
    private IntPtr _eglSurface;
    private IntPtr _hwnd;
    private GRContext? _grContext;
    private bool _isDisposed;
    private IntPtr _config;
    private int[] _contextAttribs = Array.Empty<int>();

    public IntPtr Display => _eglDisplay;
    public IntPtr Context => _eglContext;
    public GRContext? GrContext => _grContext;

    public AngleD3D11Context(IntPtr hwnd)
    {
        _hwnd = hwnd;
        _egl = new AngleEglInterface();
        Initialize();
    }

    private void Initialize()
    {
        if (_egl.GetPlatformDisplayExt == null)
        {
            throw new InvalidOperationException("eglGetPlatformDisplayEXT not available");
        }

        var displayAttribs = new int[]
        {
            EglConsts.EGL_PLATFORM_ANGLE_TYPE_ANGLE, EglConsts.EGL_PLATFORM_ANGLE_TYPE_D3D11_ANGLE,
            EglConsts.EGL_NONE
        };

        _eglDisplay = _egl.GetPlatformDisplayExt(EglConsts.EGL_PLATFORM_ANGLE_ANGLE, IntPtr.Zero, displayAttribs);
        if (_eglDisplay == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Failed to get ANGLE display, error: {_egl.GetError?.Invoke()}");
        }

        if (_egl.Initialize?.Invoke(_eglDisplay, out _, out _) != true)
        {
            throw new InvalidOperationException("Failed to initialize EGL");
        }

        _egl.BindAPI?.Invoke(EglConsts.EGL_OPENGL_ES_API);

        int[] contextAttribs = new int[] { EglConsts.EGL_CONTEXT_MAJOR_VERSION, 2, EglConsts.EGL_NONE };
        var config = IntPtr.Zero;
        int totalConfigs = 0;
        
        var result = _egl.ChooseConfig?.Invoke(_eglDisplay, new int[] { EglConsts.EGL_NONE }, out config, 1, out totalConfigs);
        
        if (result != true || totalConfigs == 0)
        {
            throw new InvalidOperationException("No EGL configs available");
        }
        
        _config = config;
        _contextAttribs = contextAttribs;

        CreateSurface();

        _eglContext = _egl.CreateContext?.Invoke(_eglDisplay, config, IntPtr.Zero, contextAttribs) ?? IntPtr.Zero;
        if (_eglContext == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Failed to create EGL context, error: {_egl.GetError?.Invoke()}");
        }

        MakeCurrent();

        var glInterface = GRGlInterface.Create(name => _egl.GetProcAddressByName(name));
        if (glInterface == null)
        {
            throw new InvalidOperationException("Failed to create GL interface");
        }

        _grContext = GRContext.CreateGl(glInterface);
        if (_grContext == null)
        {
            throw new InvalidOperationException("Failed to create GRContext");
        }
    }

    private void CreateSurface()
    {
        var surfaceAttribs = new int[] { EglConsts.EGL_NONE, EglConsts.EGL_NONE };
        var surface = _egl.CreateWindowSurface?.Invoke(_eglDisplay, _config, _hwnd, surfaceAttribs);
        _eglSurface = surface ?? IntPtr.Zero;
        
        var error = _egl.GetError?.Invoke() ?? 0;
        if (_eglSurface == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Failed to create EGL window surface, error: {error}");
        }
    }

    public void ResizeSurface()
    {
        if (_eglSurface != IntPtr.Zero)
        {
            _egl.MakeCurrent?.Invoke(_eglDisplay, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            _egl.DestroySurface?.Invoke(_eglDisplay, _eglSurface);
            _eglSurface = IntPtr.Zero;
        }
        
        CreateSurface();
        MakeCurrent();
    }

    public void MakeCurrent()
    {
        if (_egl.MakeCurrent?.Invoke(_eglDisplay, _eglSurface, _eglSurface, _eglContext) != true)
        {
            throw new InvalidOperationException("Failed to make EGL context current");
        }
    }

    public void SwapBuffers()
    {
        _egl.SwapBuffers?.Invoke(_eglDisplay, _eglSurface);
    }

    public void GetFramebufferInfo(out int framebuffer, out int samples, out int stencil)
    {
        framebuffer = 0;
        samples = 0;
        stencil = 8;
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _grContext?.Dispose();
        _grContext = null;

        if (_eglContext != IntPtr.Zero)
        {
            _egl.MakeCurrent?.Invoke(_eglDisplay, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            _egl.DestroyContext?.Invoke(_eglDisplay, _eglContext);
            _eglContext = IntPtr.Zero;
        }

        if (_eglSurface != IntPtr.Zero)
        {
            _egl.DestroySurface?.Invoke(_eglDisplay, _eglSurface);
            _eglSurface = IntPtr.Zero;
        }

        if (_eglDisplay != IntPtr.Zero)
        {
            _egl.Terminate?.Invoke(_eglDisplay);
            _eglDisplay = IntPtr.Zero;
        }
    }
}