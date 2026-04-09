using System;
using System.Runtime.InteropServices;
using System.Text;
using Eclipse.Core.Abstractions;

namespace Eclipse.Windows;

/// <summary>
/// Windows IME 上下文 - 使用 IMM32 API 实现输入法支持
/// </summary>
public sealed class ImeContext : IImeContext, IDisposable
{
    private IntPtr _hwnd;
    private IntPtr _himc;
    private bool _isComposing;
    private string _compositionText = string.Empty;
    private int _compositionCursor;
    private string _resultText = string.Empty;
    
    /// <summary>
    /// 是否正在组合输入
    /// </summary>
    public bool IsComposing => _isComposing;
    
    /// <summary>
    /// 当前组合文本（拼音/笔画等）
    /// </summary>
    public string CompositionText => _compositionText;
    
    /// <summary>
    /// 组合文本中的光标位置
    /// </summary>
    public int CompositionCursor => _compositionCursor;
    
    /// <summary>
    /// 组合完成后的结果文本
    /// </summary>
    public string ResultText => _resultText;
    
    /// <summary>
    /// 组合开始事件
    /// </summary>
    public event EventHandler? CompositionStarted;
    
    /// <summary>
    /// 组合文本变化事件
    /// </summary>
    public event EventHandler<Eclipse.Core.Abstractions.CompositionChangedEventArgs>? CompositionChanged;
    
    /// <summary>
    /// 组合结束事件
    /// </summary>
    public event EventHandler? CompositionEnded;
    
    /// <summary>
    /// 结果文本事件（组合完成后触发）
    /// </summary>
    public event EventHandler<Eclipse.Core.Abstractions.ResultEventArgs>? ResultReceived;
    
    public ImeContext(IntPtr hwnd)
    {
        _hwnd = hwnd;
        _himc = NativeMethods.ImmGetContext(hwnd);
        
        if (_himc == IntPtr.Zero)
        {
            // 创建新的 IME 上下文
            _himc = NativeMethods.ImmCreateContext();
            if (_himc != IntPtr.Zero)
            {
                NativeMethods.ImmAssociateContext(hwnd, _himc);
            }
        }
    }
    
    /// <summary>
    /// 设置组合窗口位置（候选窗口跟随光标）
    /// </summary>
    public void SetCompositionWindow(int x, int y)
    {
        if (_himc == IntPtr.Zero) return;
        
        var cf = new NativeMethods.COMPOSITIONFORM
        {
            dwStyle = NativeMethods.CFS_RECT,
            ptCurrentPos = new NativeMethods.POINT { X = x, Y = y },
            rcArea = new NativeMethods.RECT { Left = x - 100, Top = y - 50, Right = x + 100, Bottom = y + 50 }
        };
        
        NativeMethods.ImmSetCompositionWindow(_himc, ref cf);
    }
    
    /// <summary>
    /// 设置候选窗口位置
    /// </summary>
    public void SetCandidateWindow(int x, int y)
    {
        if (_himc == IntPtr.Zero) return;
        
        var cf = new NativeMethods.CANDIDATEFORM
        {
            dwStyle = NativeMethods.CFS_RECT,
            ptCurrentPos = new NativeMethods.POINT { X = x, Y = y },
            rcArea = new NativeMethods.RECT { Left = x - 100, Top = y, Right = x + 100, Bottom = y + 200 }
        };
        
        NativeMethods.ImmSetCandidateWindow(_himc, ref cf);
    }
    
    /// <summary>
    /// 处理 WM_IME_STARTCOMPOSITION
    /// </summary>
    public void OnStartComposition()
    {
        _isComposing = true;
        _compositionText = string.Empty;
        _compositionCursor = 0;
        _resultText = string.Empty;
        
        CompositionStarted?.Invoke(this, EventArgs.Empty);
    }
    
    /// <summary>
    /// 处理 WM_IME_COMPOSITION
    /// </summary>
    public void OnComposition(IntPtr wParam, IntPtr lParam)
    {
        if (_himc == IntPtr.Zero) return;
        
        var flags = lParam.ToInt64();
        
        // 获取组合字符串
        if ((flags & NativeMethods.GCS_COMPSTR) != 0)
        {
            _compositionText = GetCompositionString(NativeMethods.GCS_COMPSTR);
        }
        
        // 获取组合光标位置
        if ((flags & NativeMethods.GCS_CURSORPOS) != 0)
        {
            _compositionCursor = NativeMethods.ImmGetCompositionString(_himc, NativeMethods.GCS_CURSORPOS, IntPtr.Zero, 0);
        }
        
        // 获取结果字符串（组合完成）
        if ((flags & NativeMethods.GCS_RESULTSTR) != 0)
        {
            _resultText = GetCompositionString(NativeMethods.GCS_RESULTSTR);
        }
        
        CompositionChanged?.Invoke(this, new Eclipse.Core.Abstractions.CompositionChangedEventArgs(_compositionText, _compositionCursor));
    }
    
    /// <summary>
    /// 处理 WM_IME_ENDCOMPOSITION
    /// </summary>
    public void OnEndComposition()
    {
        _isComposing = false;
        
        // 如果有结果文本，发送结果事件
        if (!string.IsNullOrEmpty(_resultText))
        {
            ResultReceived?.Invoke(this, new Eclipse.Core.Abstractions.ResultEventArgs(_resultText));
        }
        
        CompositionEnded?.Invoke(this, EventArgs.Empty);
        
        _compositionText = string.Empty;
        _compositionCursor = 0;
    }
    
    /// <summary>
    /// 处理 WM_IME_CHAR（某些输入法发送此消息）
    /// </summary>
    public void OnImeChar(IntPtr wParam, IntPtr lParam)
    {
        // IME 字符输入，通常不需要特殊处理
        // WM_CHAR 会在后面收到
    }
    
    /// <summary>
    /// 处理 WM_IME_NOTIFY
    /// </summary>
    public void OnImeNotify(IntPtr wParam, IntPtr lParam)
    {
        // 可以处理 IME 状态变化通知
        // 例如候选窗口打开/关闭等
    }
    
    /// <summary>
    /// 获取组合字符串
    /// </summary>
    private string GetCompositionString(uint dwIndex)
    {
        if (_himc == IntPtr.Zero) return string.Empty;
        
        // 获取缓冲区大小（返回值为字节数）
        var byteSize = NativeMethods.ImmGetCompositionString(_himc, dwIndex, IntPtr.Zero, 0);
        if (byteSize <= 0) return string.Empty;
        
        // 分配缓冲区并获取字符串
        var buffer = IntPtr.Zero;
        try
        {
            buffer = Marshal.AllocHGlobal(byteSize);
            NativeMethods.ImmGetCompositionString(_himc, dwIndex, buffer, (uint)byteSize);
            // Unicode 字符串，每个字符 2 字节
            return Marshal.PtrToStringUni(buffer, byteSize / 2) ?? string.Empty;
        }
        finally
        {
            if (buffer != IntPtr.Zero)
                Marshal.FreeHGlobal(buffer);
        }
    }
    
    /// <summary>
    /// 是否启用 IME
    /// </summary>
    public bool IsImeEnabled
    {
        get
        {
            if (_himc == IntPtr.Zero) return false;
            return NativeMethods.ImmGetOpenStatus(_himc);
        }
        set
        {
            if (_himc == IntPtr.Zero) return;
            NativeMethods.ImmSetOpenStatus(_himc, value);
        }
    }
    
    /// <summary>
    /// 获取当前输入法状态
    /// </summary>
    public int GetConversionStatus()
    {
        if (_himc == IntPtr.Zero) return 0;
        NativeMethods.ImmGetConversionStatus(_himc, out var conversion, out var sentence);
        return conversion;
    }
    
    /// <summary>
    /// 设置输入法状态
    /// </summary>
    public void SetConversionStatus(int conversion)
    {
        if (_himc == IntPtr.Zero) return;
        NativeMethods.ImmSetConversionStatus(_himc, conversion, 0);
    }
    
    /// <summary>
    /// 切换输入法开启/关闭状态
    /// </summary>
    public void ToggleIme()
    {
        IsImeEnabled = !IsImeEnabled;
    }
    
    public void Dispose()
    {
        if (_himc != IntPtr.Zero)
        {
            NativeMethods.ImmReleaseContext(_hwnd, _himc);
            _himc = IntPtr.Zero;
        }
    }
    
}

/// <summary>
/// IMM32 API 声明
/// </summary>
internal static partial class NativeMethods
{
    // IME 消息
    public const uint WM_IME_SETCONTEXT = 0x0281;
    public const uint WM_IME_STARTCOMPOSITION = 0x010D;
    public const uint WM_IME_ENDCOMPOSITION = 0x010E;
    public const uint WM_IME_COMPOSITION = 0x010F;
    public const uint WM_IME_CHAR = 0x0286;
    public const uint WM_IME_NOTIFY = 0x0282;
    public const uint WM_IME_CONTROL = 0x0283;
    public const uint WM_IME_COMPOSITIONFULL = 0x0284;
    public const uint WM_IME_SELECT = 0x0285;
    public const uint WM_IME_KEYDOWN = 0x0290;
    public const uint WM_IME_KEYUP = 0x0291;
    
    // 组合字符串标志
    public const uint GCS_COMPSTR = 0x0008;
    public const uint GCS_COMPREADSTR = 0x0001;
    public const uint GCS_COMPREADATTR = 0x0002;
    public const uint GCS_COMPREADCLAUSE = 0x0004;
    public const uint GCS_COMPATTR = 0x0010;
    public const uint GCS_COMPCLAUSE = 0x0020;
    public const uint GCS_CURSORPOS = 0x0080;
    public const uint GCS_DELTASTART = 0x0100;
    public const uint GCS_RESULTSTR = 0x0800;
    public const uint GCS_RESULTREADSTR = 0x0200;
    public const uint GCS_RESULTREADCLAUSE = 0x0400;
    public const uint GCS_RESULTCLAUSE = 0x1000;
    
    // 候选窗口样式
    public const uint CFS_DEFAULT = 0x0000;
    public const uint CFS_RECT = 0x0001;
    public const uint CFS_POINT = 0x0002;
    public const uint CFS_FORCE_POSITION = 0x0020;
    public const uint CFS_CANDIDATEPOS = 0x0040;
    public const uint CFS_EXCLUDE = 0x0080;
    
    // IME 函数
    [DllImport("imm32.dll")]
    public static extern IntPtr ImmGetContext(IntPtr hWnd);
    
    [DllImport("imm32.dll")]
    public static extern bool ImmReleaseContext(IntPtr hWnd, IntPtr hIMC);
    
    [DllImport("imm32.dll")]
    public static extern IntPtr ImmCreateContext();
    
    [DllImport("imm32.dll")]
    public static extern bool ImmDestroyContext(IntPtr hIMC);
    
    [DllImport("imm32.dll")]
    public static extern IntPtr ImmAssociateContext(IntPtr hWnd, IntPtr hIMC);
    
    [DllImport("imm32.dll")]
    public static extern int ImmGetCompositionString(IntPtr hIMC, uint dwIndex, IntPtr lpBuf, uint dwBufLen);
    
    [DllImport("imm32.dll")]
    public static extern int ImmGetCompositionString(IntPtr hIMC, uint dwIndex, [Out] byte[]? lpBuf, int dwBufLen);
    
    [DllImport("imm32.dll")]
    public static extern bool ImmSetCompositionWindow(IntPtr hIMC, ref COMPOSITIONFORM lpCompForm);
    
    [DllImport("imm32.dll")]
    public static extern bool ImmSetCandidateWindow(IntPtr hIMC, ref CANDIDATEFORM lpCandidate);
    
    [DllImport("imm32.dll")]
    public static extern bool ImmGetOpenStatus(IntPtr hIMC);
    
    [DllImport("imm32.dll")]
    public static extern bool ImmSetOpenStatus(IntPtr hIMC, bool fOpen);
    
    [DllImport("imm32.dll")]
    public static extern bool ImmGetConversionStatus(IntPtr hIMC, out int lpfdwConversion, out int lpfdwSentence);
    
    [DllImport("imm32.dll")]
    public static extern bool ImmSetConversionStatus(IntPtr hIMC, int fdwConversion, int fdwSentence);
    
    [DllImport("imm32.dll")]
    public static extern bool ImmNotifyIME(IntPtr hIMC, uint dwAction, uint dwIndex, uint dwValue);
    
    // 结构体
    [StructLayout(LayoutKind.Sequential)]
    public struct COMPOSITIONFORM
    {
        public uint dwStyle;
        public POINT ptCurrentPos;
        public RECT rcArea;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct CANDIDATEFORM
    {
        public uint dwStyle;
        public POINT ptCurrentPos;
        public RECT rcArea;
    }
}