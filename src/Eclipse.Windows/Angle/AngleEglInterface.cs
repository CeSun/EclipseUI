using System;
using System.Runtime.InteropServices;

namespace Eclipse.Windows.Angle;

/// <summary>
/// EGL 常量
/// </summary>
internal static class EglConsts
{
    public const int EGL_NONE = 0x3038;
    public const int EGL_TRUE = 1;
    public const int EGL_FALSE = 0;

    // EGL API
    public const int EGL_OPENGL_ES_API = 0x30A0;
    public const int EGL_OPENGL_API = 0x30A2;

    // EGL Surface Type
    public const int EGL_WINDOW_BIT = 0x0004;
    public const int EGL_PBUFFER_BIT = 0x0001;
    public const int EGL_OPENGL_ES_BIT = 0x0001;
    public const int EGL_OPENGL_ES2_BIT = 0x0004;
    public const int EGL_OPENGL_ES3_BIT = 0x00000040;
    public const int EGL_RENDERABLE_TYPE = 0x3040;
    public const int EGL_RED_SIZE = 0x3024;
    public const int EGL_GREEN_SIZE = 0x3023;
    public const int EGL_BLUE_SIZE = 0x3022;
    public const int EGL_ALPHA_SIZE = 0x303E;
    public const int EGL_DEPTH_SIZE = 0x3025;
    public const int EGL_STENCIL_SIZE = 0x3026;
    public const int EGL_SURFACE_TYPE = 0x3033;
    public const int EGL_COLOR_BUFFER_TYPE = 0x303F;
    public const int EGL_RGB_BUFFER = 0x308C;

    // EGL Context
    public const int EGL_CONTEXT_CLIENT_VERSION = 0x3098;
    public const int EGL_CONTEXT_MAJOR_VERSION = 0x3098;
    public const int EGL_CONTEXT_MINOR_VERSION = 0x30FB;

    // ANGLE Extensions
    public const int EGL_PLATFORM_ANGLE_ANGLE = 0x3202;
    public const int EGL_PLATFORM_ANGLE_TYPE_ANGLE = 0x3203;
    public const int EGL_PLATFORM_ANGLE_MAX_VERSION_MAJOR_ANGLE = 0x3204;
    public const int EGL_PLATFORM_ANGLE_MAX_VERSION_MINOR_ANGLE = 0x3205;
    public const int EGL_PLATFORM_ANGLE_TYPE_DEFAULT_ANGLE = 0x3206;
    public const int EGL_PLATFORM_ANGLE_TYPE_D3D9_ANGLE = 0x3207;
    public const int EGL_PLATFORM_ANGLE_TYPE_D3D11_ANGLE = 0x3208;
    public const int EGL_PLATFORM_ANGLE_TYPE_OPENGL_ANGLE = 0x320D;
    public const int EGL_PLATFORM_ANGLE_TYPE_OPENGLES_ANGLE = 0x320E;
    public const int EGL_PLATFORM_ANGLE_TYPE_VULKAN_ANGLE = 0x3450;
    public const int EGL_PLATFORM_ANGLE_TYPE_METAL_ANGLE = 0x3489;

    // EGL Device
    public const int EGL_DEVICE_EXT = 0x322C;
    public const int EGL_D3D11_DEVICE_ANGLE = 0x33A1;
    public const int EGL_D3D9_DEVICE_ANGLE = 0x33A0;

    // EGL Platform
    public const int EGL_PLATFORM_DEVICE_EXT = 0x313F;
}

/// <summary>
/// ANGLE EGL 接口 - 使用 av_libGLESv2.dll
/// </summary>
internal class AngleEglInterface
{
    private const string AngleLibrary = "av_libGLESv2.dll";

    [DllImport(AngleLibrary, CharSet = CharSet.Ansi)]
    private static extern IntPtr EGL_GetProcAddress(string proc);

    // EGL 核心函数
    public delegate IntPtr EglGetDisplayDelegate(IntPtr nativeDisplay);
    public delegate bool EglInitializeDelegate(IntPtr display, out int major, out int minor);
    public delegate bool EglTerminateDelegate(IntPtr display);
    public delegate IntPtr EglGetProcAddressDelegate(IntPtr proc);
    public delegate bool EglBindAPIDelegate(int api);
    public delegate int EglGetErrorDelegate();

    // EGL 配置
    public delegate bool EglChooseConfigDelegate(IntPtr display, int[] attribs, out IntPtr config, int configSize, out int numConfig);

    // EGL 上下文
    public delegate IntPtr EglCreateContextDelegate(IntPtr display, IntPtr config, IntPtr shareContext, int[] attribs);
    public delegate bool EglDestroyContextDelegate(IntPtr display, IntPtr context);
    public delegate bool EglMakeCurrentDelegate(IntPtr display, IntPtr drawSurface, IntPtr readSurface, IntPtr context);
    public delegate IntPtr EglGetCurrentContextDelegate();

    // EGL Surface
    public delegate IntPtr EglCreateWindowSurfaceDelegate(IntPtr display, IntPtr config, IntPtr window, int[] attribs);
    public delegate IntPtr EglCreatePbufferSurfaceDelegate(IntPtr display, IntPtr config, int[] attribs);
    public delegate bool EglDestroySurfaceDelegate(IntPtr display, IntPtr surface);
    public delegate bool EglSwapBuffersDelegate(IntPtr display, IntPtr surface);

    // EGL 扩展
    public delegate IntPtr EglGetPlatformDisplayExtDelegate(int platform, IntPtr nativeDisplay, int[] attribs);
    public delegate IntPtr EglCreateDeviceAngleDelegate(int deviceType, IntPtr nativeDevice, int[] attribs);
    public delegate void EglReleaseDeviceAngleDelegate(IntPtr device);
    public delegate bool EglQueryDisplayAttribExtDelegate(IntPtr display, int attribute, out IntPtr value);
    public delegate bool EglQueryDeviceAttribExtDelegate(IntPtr device, int attribute, out IntPtr value);

    // 加载的函数
    public EglGetDisplayDelegate? GetDisplay { get; private set; }
    public EglInitializeDelegate? Initialize { get; private set; }
    public EglTerminateDelegate? Terminate { get; private set; }
    public EglGetProcAddressDelegate? GetProcAddress { get; private set; }
    public EglBindAPIDelegate? BindAPI { get; private set; }
    public EglGetErrorDelegate? GetError { get; private set; }
    public EglChooseConfigDelegate? ChooseConfig { get; private set; }
    public EglCreateContextDelegate? CreateContext { get; private set; }
    public EglDestroyContextDelegate? DestroyContext { get; private set; }
    public EglMakeCurrentDelegate? MakeCurrent { get; private set; }
    public EglGetCurrentContextDelegate? GetCurrentContext { get; private set; }
    public EglCreateWindowSurfaceDelegate? CreateWindowSurface { get; private set; }
    public EglCreatePbufferSurfaceDelegate? CreatePbufferSurface { get; private set; }
    public EglDestroySurfaceDelegate? DestroySurface { get; private set; }
    public EglSwapBuffersDelegate? SwapBuffers { get; private set; }
    public EglGetPlatformDisplayExtDelegate? GetPlatformDisplayExt { get; private set; }
    public EglCreateDeviceAngleDelegate? CreateDeviceANGLE { get; private set; }
    public EglReleaseDeviceAngleDelegate? ReleaseDeviceANGLE { get; private set; }

    public AngleEglInterface()
    {
        LoadFunctions();
    }

    private void LoadFunctions()
    {
        GetDisplay = LoadFunction<EglGetDisplayDelegate>("eglGetDisplay");
        Initialize = LoadFunction<EglInitializeDelegate>("eglInitialize");
        Terminate = LoadFunction<EglTerminateDelegate>("eglTerminate");
        GetProcAddress = LoadFunction<EglGetProcAddressDelegate>("eglGetProcAddress");
        BindAPI = LoadFunction<EglBindAPIDelegate>("eglBindAPI");
        GetError = LoadFunction<EglGetErrorDelegate>("eglGetError");
        ChooseConfig = LoadFunction<EglChooseConfigDelegate>("eglChooseConfig");
        CreateContext = LoadFunction<EglCreateContextDelegate>("eglCreateContext");
        DestroyContext = LoadFunction<EglDestroyContextDelegate>("eglDestroyContext");
        MakeCurrent = LoadFunction<EglMakeCurrentDelegate>("eglMakeCurrent");
        GetCurrentContext = LoadFunction<EglGetCurrentContextDelegate>("eglGetCurrentContext");
        CreateWindowSurface = LoadFunction<EglCreateWindowSurfaceDelegate>("eglCreateWindowSurface");
        CreatePbufferSurface = LoadFunction<EglCreatePbufferSurfaceDelegate>("eglCreatePbufferSurface");
        DestroySurface = LoadFunction<EglDestroySurfaceDelegate>("eglDestroySurface");
        SwapBuffers = LoadFunction<EglSwapBuffersDelegate>("eglSwapBuffers");
        GetPlatformDisplayExt = LoadFunction<EglGetPlatformDisplayExtDelegate>("eglGetPlatformDisplayEXT");
        CreateDeviceANGLE = LoadFunction<EglCreateDeviceAngleDelegate>("eglCreateDeviceANGLE");
        ReleaseDeviceANGLE = LoadFunction<EglReleaseDeviceAngleDelegate>("eglReleaseDeviceANGLE");
    }

    private T? LoadFunction<T>(string name) where T : Delegate
    {
        var ptr = EGL_GetProcAddress(name);
        if (ptr == IntPtr.Zero)
        {
            Console.WriteLine($"Failed to load EGL function: {name}");
            return null;
        }
        return Marshal.GetDelegateForFunctionPointer<T>(ptr);
    }

    public IntPtr GetProcAddressByName(string name)
    {
        return EGL_GetProcAddress(name);
    }
}