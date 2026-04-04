using System;
using System.Runtime.InteropServices;

namespace Eclipse.Windows.OpenGL;

/// <summary>
/// Win32 API 声明 (OpenGL 相关)
/// </summary>
internal static class Wgl
{
    #region Constants

    public const uint PFD_DRAW_TO_WINDOW = 0x00000004;
    public const uint PFD_SUPPORT_OPENGL = 0x00000020;
    public const uint PFD_DOUBLEBUFFER = 0x00000001;
    public const byte PFD_TYPE_RGBA = 0;
    public const byte PFD_MAIN_PLANE = 0;

    public const int GL_FRAMEBUFFER_BINDING = 0x8CA6;
    public const int GL_STENCIL_BITS = 0x0D57;
    public const int GL_SAMPLES = 0x80A9;
    public const int GL_TEXTURE_2D = 0x0DE1;
    public const int GL_RGBA = 0x1908;
    public const int GL_UNSIGNED_BYTE = 0x1401;
    public const int GL_RGBA8 = 0x8058;

    #endregion

    #region DLL Imports

    [DllImport("opengl32.dll", SetLastError = true)]
    public static extern IntPtr wglCreateContext(IntPtr hdc);

    [DllImport("opengl32.dll", SetLastError = true)]
    public static extern bool wglDeleteContext(IntPtr hglrc);

    [DllImport("opengl32.dll", SetLastError = true)]
    public static extern bool wglMakeCurrent(IntPtr hdc, IntPtr hglrc);

    [DllImport("opengl32.dll", SetLastError = true)]
    public static extern IntPtr wglGetCurrentContext();

    [DllImport("opengl32.dll", SetLastError = true)]
    public static extern IntPtr wglGetCurrentDC();

    [DllImport("opengl32.dll")]
    public static extern IntPtr wglGetProcAddress(string name);

    [DllImport("gdi32.dll", SetLastError = true)]
    public static extern int ChoosePixelFormat(IntPtr hdc, ref PIXELFORMATDESCRIPTOR ppfd);

    [DllImport("gdi32.dll", SetLastError = true)]
    public static extern bool SetPixelFormat(IntPtr hdc, int iPixelFormat, ref PIXELFORMATDESCRIPTOR ppfd);

    [DllImport("gdi32.dll", SetLastError = true)]
    public static extern bool SwapBuffers(IntPtr hdc);

    // GetDC 和 ReleaseDC 在 user32.dll 中！
    [DllImport("user32.dll")]
    public static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("opengl32.dll")]
    public static extern void glGenTextures(int n, uint[] textures);

    [DllImport("opengl32.dll")]
    public static extern void glBindTexture(uint target, uint texture);

    [DllImport("opengl32.dll")]
    public static extern void glTexImage2D(uint target, int level, int internalformat, int width, int height, int border, uint format, uint type, IntPtr pixels);

    [DllImport("opengl32.dll")]
    public static extern void glDeleteTextures(int n, uint[] textures);

    [DllImport("opengl32.dll")]
    public static extern void glGetIntegerv(int pname, out int param);

    #endregion

    #region Structures

    [StructLayout(LayoutKind.Sequential)]
    public struct PIXELFORMATDESCRIPTOR
    {
        public ushort nSize;
        public ushort nVersion;
        public uint dwFlags;
        public byte iPixelType;
        public byte cColorBits;
        public byte cRedBits;
        public byte cRedShift;
        public byte cGreenBits;
        public byte cGreenShift;
        public byte cBlueBits;
        public byte cBlueShift;
        public byte cAlphaBits;
        public byte cAlphaShift;
        public byte cAccumBits;
        public byte cAccumRedBits;
        public byte cAccumGreenBits;
        public byte cAccumBlueBits;
        public byte cAccumAlphaBits;
        public byte cDepthBits;
        public byte cStencilBits;
        public byte cAuxBuffers;
        public byte iLayerType;
        public byte bReserved;
        public uint dwLayerMask;
        public uint dwVisibleMask;
        public uint dwDamageMask;
    }

    #endregion
}