using System;

namespace Eclipse.Input;

/// <summary>
/// 键码
/// </summary>
public enum Key
{
    None = 0,
    
    // 功能键
    Back = 8,
    Tab = 9,
    Enter = 13,
    Escape = 27,
    Space = 32,
    Delete = 46,
    
    // 方向键
    Left = 37,
    Up = 38,
    Right = 39,
    Down = 40,
    
    // 数字键
    D0 = 48, D1, D2, D3, D4, D5, D6, D7, D8, D9,
    
    // 字母键
    A = 65, B, C, D, E, F, G, H, I, J, K, L, M,
    N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
    
    // 小键盘数字
    NumPad0 = 96, NumPad1, NumPad2, NumPad3, NumPad4,
    NumPad5, NumPad6, NumPad7, NumPad8, NumPad9,
    
    // 功能键 F1-F12
    F1 = 112, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,
    
    // 其他
    Home = 36,
    End = 35,
    PageUp = 33,
    PageDown = 34,
    Insert = 45,
    
    // 符号键
    OemPlus = 187,
    OemMinus = 189,
    OemPeriod = 190,
    OemComma = 188,
    OemQuestion = 191,
    OemSemicolon = 186,
    OemQuotes = 222,
    OemOpenBrackets = 219,
    OemCloseBrackets = 221,
    OemBackslash = 220
}

/// <summary>
/// 键盘事件参数
/// </summary>
public class KeyEventArgs : RoutedEventArgs
{
    /// <summary>
    /// 按下的键
    /// </summary>
    public Key Key { get; init; }
    
    /// <summary>
    /// 原始键码
    /// </summary>
    public int KeyCode { get; init; }
    
    /// <summary>
    /// 键盘修饰键
    /// </summary>
    public KeyModifiers Modifiers { get; init; }
    
    /// <summary>
    /// 是否重复按下
    /// </summary>
    public bool IsRepeat { get; init; }
    
    public KeyEventArgs() { }
    
    public KeyEventArgs(Key key, int keyCode, KeyModifiers modifiers = KeyModifiers.None)
    {
        Key = key;
        KeyCode = keyCode;
        Modifiers = modifiers;
    }
    
    /// <summary>
    /// 是否按下指定修饰键
    /// </summary>
    public bool HasModifier(KeyModifiers modifier) => (Modifiers & modifier) != 0;
}

/// <summary>
/// 文本输入事件参数
/// </summary>
public class TextInputEventArgs : RoutedEventArgs
{
    /// <summary>
    /// 输入的文本
    /// </summary>
    public string Text { get; init; } = string.Empty;
    
    public TextInputEventArgs() { }
    
    public TextInputEventArgs(string text)
    {
        Text = text;
    }
}