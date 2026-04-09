using System;
using System.Runtime.InteropServices;
using Eclipse.Core.Abstractions;
using Eclipse.Input;
using Eclipse.Windows;

namespace Eclipse.Windows.Input;

/// <summary>
/// Windows 平台输入适配器
/// </summary>
internal sealed class WindowsInputAdapter : IInputAdapter
{
    private readonly IntPtr _hwnd;
    private readonly InputManager _inputManager;
    private readonly ImeContext _imeContext;
    private bool _isDisposed;
    
    // IME 组合状态 - 组合期间忽略 WM_CHAR，避免重复输入
    private bool _isImeComposing = false;
    
    public WindowsInputAdapter(IntPtr hwnd, InputManager inputManager)
    {
        _hwnd = hwnd;
        _inputManager = inputManager;
        _imeContext = new ImeContext(hwnd);
        
        // 订阅 IME 事件
        _imeContext.CompositionStarted += OnImeCompositionStarted;
        _imeContext.CompositionChanged += OnImeCompositionChanged;
        _imeContext.CompositionEnded += OnImeCompositionEnded;
        _imeContext.ResultReceived += OnImeResultReceived;
    }
    
    /// <inheritdoc/>
    public IImeContext? ImeContext => _imeContext;
    
    /// <inheritdoc/>
    public void ProcessMessage(uint msg, IntPtr wParam, IntPtr lParam)
    {
        switch (msg)
        {
            case NativeMethods.WM_LBUTTONDOWN:
            case NativeMethods.WM_RBUTTONDOWN:
            case NativeMethods.WM_MBUTTONDOWN:
            case NativeMethods.WM_XBUTTONDOWN:
                OnPointerPressed(msg, wParam, lParam);
                break;
                
            case NativeMethods.WM_LBUTTONUP:
            case NativeMethods.WM_RBUTTONUP:
            case NativeMethods.WM_MBUTTONUP:
            case NativeMethods.WM_XBUTTONUP:
                OnPointerReleased(msg, wParam, lParam);
                break;
                
            case NativeMethods.WM_MOUSEMOVE:
                OnPointerMoved(wParam, lParam);
                break;
                
            case NativeMethods.WM_MOUSEWHEEL:
                OnPointerWheel(wParam, lParam);
                break;
                
            case NativeMethods.WM_MOUSEHWHEEL:
                OnPointerWheelHorizontal(wParam, lParam);
                break;
                
            case NativeMethods.WM_TOUCH:
                OnTouch(wParam, lParam);
                break;
                
            case NativeMethods.WM_KEYDOWN:
            case NativeMethods.WM_SYSKEYDOWN:
                OnKeyDown(wParam, lParam);
                break;
                
            case NativeMethods.WM_KEYUP:
            case NativeMethods.WM_SYSKEYUP:
                OnKeyUp(wParam, lParam);
                break;
                
            case NativeMethods.WM_CHAR:
                OnChar(wParam, lParam);
                break;
                
            // IME 消息
            case NativeMethods.WM_IME_STARTCOMPOSITION:
                OnImeStartComposition();
                break;
                
            case NativeMethods.WM_IME_COMPOSITION:
                OnImeComposition(wParam, lParam);
                break;
                
            case NativeMethods.WM_IME_ENDCOMPOSITION:
                OnImeEndComposition();
                break;
                
            case NativeMethods.WM_IME_CHAR:
                OnImeChar(wParam, lParam);
                break;
                
            case NativeMethods.WM_IME_NOTIFY:
                OnImeNotify(wParam, lParam);
                break;
        }
    }
    
    // === IME 处理 ===
    
    private void OnImeStartComposition()
    {
        _imeContext.OnStartComposition();
    }
    
    private void OnImeComposition(IntPtr wParam, IntPtr lParam)
    {
        _imeContext.OnComposition(wParam, lParam);
    }
    
    private void OnImeEndComposition()
    {
        _imeContext.OnEndComposition();
    }
    
    private void OnImeChar(IntPtr wParam, IntPtr lParam)
    {
        _imeContext.OnImeChar(wParam, lParam);
    }
    
    private void OnImeNotify(IntPtr wParam, IntPtr lParam)
    {
        _imeContext.OnImeNotify(wParam, lParam);
    }
    
    private void OnImeCompositionStarted(object? sender, EventArgs e)
    {
        _isImeComposing = true; // 开始组合，忽略 WM_CHAR
        _inputManager.ProcessCompositionStarted();
    }
    
    private void OnImeCompositionChanged(object? sender, Eclipse.Core.Abstractions.CompositionChangedEventArgs e)
    {
        _inputManager.ProcessCompositionChanged(e.CompositionText, e.CursorPosition);
    }
    
    private void OnImeCompositionEnded(object? sender, EventArgs e)
    {
        // 用户取消组合（如按 Escape）或组合已完成（ResultReceived 已处理）
        // 无论哪种情况，都允许后续 WM_CHAR
        _isImeComposing = false;
        _inputManager.ProcessCompositionEnded();
    }
    
    private void OnImeResultReceived(object? sender, Eclipse.Core.Abstractions.ResultEventArgs e)
    {
        _inputManager.ProcessTextInput(e.Result);
        _isImeComposing = false; // 结果已处理，允许后续 WM_CHAR
    }
    
    /// <summary>
    /// 更新组合窗口位置（用于 TextInput 控件）
    /// </summary>
    public void UpdateCompositionWindowPosition(double x, double y, float scale)
    {
        var scaledX = (int)(x * scale);
        var scaledY = (int)(y * scale);
        _imeContext.SetCompositionWindow(scaledX, scaledY);
        _imeContext.SetCandidateWindow(scaledX, scaledY + 20);
    }
    
    private void OnPointerPressed(uint msg, IntPtr wParam, IntPtr lParam)
    {
        var x = NativeMethods.GET_X_LPARAM(lParam);
        var y = NativeMethods.GET_Y_LPARAM(lParam);
        var keyModifiers = GetKeyModifiers(wParam);
        var button = GetButtonFromMsg(msg);
        
        if (msg == NativeMethods.WM_XBUTTONDOWN)
        {
            var xButton = NativeMethods.HIWORD(wParam);
            button = xButton == 1 ? PointerButtons.XButton1 : PointerButtons.XButton2;
        }
        
        var pointer = Pointer.Mouse;
        var properties = new PointerPointProperties
        {
            IsLeftButtonPressed = button == PointerButtons.Left,
            IsRightButtonPressed = button == PointerButtons.Right,
            IsMiddleButtonPressed = button == PointerButtons.Middle,
            IsXButton1Pressed = button == PointerButtons.XButton1,
            IsXButton2Pressed = button == PointerButtons.XButton2
        };
        
        var clickCount = GetClickCount(x, y, (int)msg);
        
        _inputManager.ProcessPointerPressed(pointer, new Point(x, y), properties, keyModifiers, clickCount);
    }
    
    private void OnPointerMoved(IntPtr wParam, IntPtr lParam)
    {
        var x = NativeMethods.GET_X_LPARAM(lParam);
        var y = NativeMethods.GET_Y_LPARAM(lParam);
        var keyModifiers = GetKeyModifiers(wParam);
        
        var properties = new PointerPointProperties
        {
            IsLeftButtonPressed = (NativeMethods.GetKeyState(NativeMethods.VK_LBUTTON) & 0x8000) != 0,
            IsRightButtonPressed = (NativeMethods.GetKeyState(NativeMethods.VK_RBUTTON) & 0x8000) != 0,
            IsMiddleButtonPressed = (NativeMethods.GetKeyState(NativeMethods.VK_MBUTTON) & 0x8000) != 0
        };
        
        _inputManager.ProcessPointerMoved(Pointer.Mouse, new Point(x, y), properties, keyModifiers);
    }
    
    private void OnPointerReleased(uint msg, IntPtr wParam, IntPtr lParam)
    {
        var x = NativeMethods.GET_X_LPARAM(lParam);
        var y = NativeMethods.GET_Y_LPARAM(lParam);
        var keyModifiers = GetKeyModifiers(wParam);
        var button = GetButtonFromMsg(msg);
        
        if (msg == NativeMethods.WM_XBUTTONUP)
        {
            var xButton = NativeMethods.HIWORD(wParam);
            button = xButton == 1 ? PointerButtons.XButton1 : PointerButtons.XButton2;
        }
        
        _inputManager.ProcessPointerReleased(Pointer.Mouse, new Point(x, y), button, keyModifiers);
    }
    
    private void OnPointerWheel(IntPtr wParam, IntPtr lParam)
    {
        var delta = NativeMethods.GET_WHEEL_DELTA_WPARAM(wParam) / 120.0;
        var screenX = NativeMethods.GET_X_LPARAM(lParam);
        var screenY = NativeMethods.GET_Y_LPARAM(lParam);
        var keyModifiers = GetKeyModifiers(wParam);
        
        // WM_MOUSEWHEEL uses screen coordinates, convert to client coordinates
        var pt = new NativeMethods.POINT { x = screenX, y = screenY };
        NativeMethods.ScreenToClient(_hwnd, ref pt);
        
        _inputManager.ProcessPointerWheel(Pointer.Mouse, new Point(pt.x, pt.y), new Vector(0, delta), keyModifiers);
    }
    
    private void OnPointerWheelHorizontal(IntPtr wParam, IntPtr lParam)
    {
        var delta = NativeMethods.GET_WHEEL_DELTA_WPARAM(wParam) / 120.0;
        var screenX = NativeMethods.GET_X_LPARAM(lParam);
        var screenY = NativeMethods.GET_Y_LPARAM(lParam);
        
        // WM_MOUSEHWHEEL uses screen coordinates, convert to client coordinates
        var pt = new NativeMethods.POINT { x = screenX, y = screenY };
        NativeMethods.ScreenToClient(_hwnd, ref pt);
        
        _inputManager.ProcessPointerWheel(Pointer.Mouse, new Point(pt.x, pt.y), new Vector(delta, 0));
    }
    
    private void OnTouch(IntPtr wParam, IntPtr lParam)
    {
        var touchCount = wParam.ToInt32();
        var touchInputs = new NativeMethods.TOUCHINPUT[touchCount];
        
        if (NativeMethods.GetTouchInputInfo(lParam, (uint)touchCount, touchInputs, Marshal.SizeOf(typeof(NativeMethods.TOUCHINPUT))))
        {
            foreach (var touchInput in touchInputs)
            {
                var touchId = touchInput.dwID;
                var x = touchInput.x / 100.0;
                var y = touchInput.y / 100.0;
                
                var pointer = Pointer.GetOrCreate(touchId, PointerType.Touch);
                var properties = new PointerPointProperties { Pressure = touchInput.dwMask != 0 ? 1.0f : 0f };
                
                if ((touchInput.dwFlags & NativeMethods.TOUCHEVENTF_DOWN) != 0)
                    _inputManager.ProcessPointerPressed(pointer, new Point(x, y), properties);
                else if ((touchInput.dwFlags & NativeMethods.TOUCHEVENTF_MOVE) != 0)
                    _inputManager.ProcessPointerMoved(pointer, new Point(x, y), properties);
                else if ((touchInput.dwFlags & NativeMethods.TOUCHEVENTF_UP) != 0)
                    _inputManager.ProcessPointerReleased(pointer, new Point(x, y), PointerButtons.Left);
            }
            
            NativeMethods.CloseTouchInputHandle(lParam);
        }
    }
    
    private KeyModifiers GetKeyModifiers(IntPtr wParam)
    {
        // 使用 ToInt64 避免在 64 位系统上溢出
        var keys = wParam.ToInt64() & 0xFFFF; // 只取低 16 位
        var modifiers = KeyModifiers.None;
        
        if ((keys & NativeMethods.MK_CONTROL) != 0) modifiers |= KeyModifiers.Control;
        if ((keys & NativeMethods.MK_SHIFT) != 0) modifiers |= KeyModifiers.Shift;
        if ((NativeMethods.GetKeyState(NativeMethods.VK_MENU) & 0x8000) != 0) modifiers |= KeyModifiers.Alt;
        
        return modifiers;
    }
    
    private PointerButtons GetButtonFromMsg(uint msg)
    {
        return msg switch
        {
            NativeMethods.WM_LBUTTONDOWN or NativeMethods.WM_LBUTTONUP => PointerButtons.Left,
            NativeMethods.WM_RBUTTONDOWN or NativeMethods.WM_RBUTTONUP => PointerButtons.Right,
            NativeMethods.WM_MBUTTONDOWN or NativeMethods.WM_MBUTTONUP => PointerButtons.Middle,
            _ => PointerButtons.None
        };
    }
    
    private int _lastClickMsg;
    private Point _lastClickPos;
    private int _clickCount;
    private long _lastClickTime;
    
    private int GetClickCount(int x, int y, int msg)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var pos = new Point(x, y);
        
        if (msg == _lastClickMsg && (pos - _lastClickPos).Length < 4 && now - _lastClickTime < 500)
            _clickCount++;
        else
            _clickCount = 1;
        
        _lastClickMsg = msg;
        _lastClickPos = pos;
        _lastClickTime = now;
        
        return _clickCount;
    }
    
    // === 键盘处理 ===
    
    private void OnKeyDown(IntPtr wParam, IntPtr lParam)
    {
        var keyCode = wParam.ToInt32();
        var key = (Key)keyCode;
        var modifiers = GetKeyboardModifiers();
        
        // 检查是否重复按键
        var isRepeat = ((lParam.ToInt64() >> 30) & 0x1) != 0;
        
        _inputManager.ProcessKeyDown(key, keyCode, modifiers, isRepeat);
    }
    
    private void OnKeyUp(IntPtr wParam, IntPtr lParam)
    {
        var keyCode = wParam.ToInt32();
        var key = (Key)keyCode;
        var modifiers = GetKeyboardModifiers();
        
        _inputManager.ProcessKeyUp(key, keyCode, modifiers);
    }
    
    private void OnChar(IntPtr wParam, IntPtr lParam)
    {
        // IME 组合期间忽略 WM_CHAR，避免双重输入
        if (_isImeComposing)
            return;
        
        var charCode = wParam.ToInt32();
        
        // 忽略控制字符
        if (charCode < 32)
            return;
        
        var text = char.ConvertFromUtf32(charCode);
        _inputManager.ProcessTextInput(text);
    }
    
    private KeyModifiers GetKeyboardModifiers()
    {
        var modifiers = KeyModifiers.None;
        
        if ((NativeMethods.GetKeyState(NativeMethods.VK_SHIFT) & 0x8000) != 0)
            modifiers |= KeyModifiers.Shift;
        if ((NativeMethods.GetKeyState(NativeMethods.VK_CONTROL) & 0x8000) != 0)
            modifiers |= KeyModifiers.Control;
        if ((NativeMethods.GetKeyState(NativeMethods.VK_MENU) & 0x8000) != 0)
            modifiers |= KeyModifiers.Alt;
        
        return modifiers;
    }
    
    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        
        // 取消 IME 事件订阅
        _imeContext.CompositionStarted -= OnImeCompositionStarted;
        _imeContext.CompositionChanged -= OnImeCompositionChanged;
        _imeContext.CompositionEnded -= OnImeCompositionEnded;
        _imeContext.ResultReceived -= OnImeResultReceived;
        
        _imeContext.Dispose();
    }
}

internal static class NativeMethods
{
    public const uint WM_LBUTTONDOWN = 0x0201;
    public const uint WM_LBUTTONUP = 0x0202;
    public const uint WM_RBUTTONDOWN = 0x0204;
    public const uint WM_RBUTTONUP = 0x0205;
    public const uint WM_MBUTTONDOWN = 0x0207;
    public const uint WM_MBUTTONUP = 0x0208;
    public const uint WM_XBUTTONDOWN = 0x020B;
    public const uint WM_XBUTTONUP = 0x020C;
    public const uint WM_MOUSEMOVE = 0x0200;
    public const uint WM_MOUSEWHEEL = 0x020A;
    public const uint WM_MOUSEHWHEEL = 0x020E;
    public const uint WM_TOUCH = 0x0240;
    
    // 键盘消息
    public const uint WM_KEYDOWN = 0x0100;
    public const uint WM_KEYUP = 0x0101;
    public const uint WM_CHAR = 0x0102;
    public const uint WM_SYSKEYDOWN = 0x0104;
    public const uint WM_SYSKEYUP = 0x0105;
    
    // IME 消息
    public const uint WM_IME_STARTCOMPOSITION = 0x010D;
    public const uint WM_IME_ENDCOMPOSITION = 0x010E;
    public const uint WM_IME_COMPOSITION = 0x010F;
    public const uint WM_IME_CHAR = 0x0286;
    public const uint WM_IME_NOTIFY = 0x0282;
    public const uint WM_IME_SETCONTEXT = 0x0281;
    
    public const int MK_CONTROL = 0x0008;
    public const int MK_SHIFT = 0x0004;
    
    public const int VK_LBUTTON = 0x01;
    public const int VK_RBUTTON = 0x02;
    public const int VK_MBUTTON = 0x04;
    public const int VK_MENU = 0x12;
    public const int VK_SHIFT = 0x10;
    public const int VK_CONTROL = 0x11;
    
    public const int TOUCHEVENTF_DOWN = 0x0001;
    public const int TOUCHEVENTF_UP = 0x0002;
    public const int TOUCHEVENTF_MOVE = 0x0004;
    
    [DllImport("user32.dll")]
    public static extern short GetKeyState(int nVirtKey);
    
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetTouchInputInfo(IntPtr hTouchInput, uint cInputs, [Out] TOUCHINPUT[] pInputs, int cbSize);
    
    [DllImport("user32.dll")]
    public static extern void CloseTouchInputHandle(IntPtr hTouchInput);
    
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);
    
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int x;
        public int y;
    }
    
    public static int GET_X_LPARAM(IntPtr lParam) => (short)(lParam.ToInt64() & 0xFFFF);
    public static int GET_Y_LPARAM(IntPtr lParam) => (short)((lParam.ToInt64() >> 16) & 0xFFFF);
    public static int HIWORD(IntPtr wParam) => (short)((wParam.ToInt64() >> 16) & 0xFFFF);
    public static int GET_WHEEL_DELTA_WPARAM(IntPtr wParam) => (short)((wParam.ToInt64() >> 16) & 0xFFFF);
    
    [StructLayout(LayoutKind.Sequential)]
    public struct TOUCHINPUT
    {
        public int x;
        public int y;
        public int dwID;
        public int dwFlags;
        public int dwMask;
        public int dwTime;
        public IntPtr dwExtraInfo;
        public int cxContact;
        public int cyContact;
    }
}