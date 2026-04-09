using System;
using System.Runtime.InteropServices;

namespace Eclipse.Core.Abstractions;

/// <summary>
/// 输入适配器接口 - 抽象平台输入处理
/// </summary>
public interface IInputAdapter : IDisposable
{
    /// <summary>
    /// 处理平台消息
    /// </summary>
    void ProcessMessage(uint msg, IntPtr wParam, IntPtr lParam);
    
    /// <summary>
    /// 更新 IME 组合窗口位置
    /// </summary>
    void UpdateCompositionWindowPosition(double x, double y, float scale);
    
    /// <summary>
    /// 获取 IME 上下文
    /// </summary>
    IImeContext? ImeContext { get; }
}

/// <summary>
/// IME 上下文接口
/// </summary>
public interface IImeContext
{
    /// <summary>
    /// 组合开始事件
    /// </summary>
    event EventHandler? CompositionStarted;
    
    /// <summary>
    /// 组合内容变化事件
    /// </summary>
    event EventHandler<CompositionChangedEventArgs>? CompositionChanged;
    
    /// <summary>
    /// 组合结束事件
    /// </summary>
    event EventHandler? CompositionEnded;
    
    /// <summary>
    /// 结果文本事件
    /// </summary>
    event EventHandler<ResultEventArgs>? ResultReceived;
}

/// <summary>
/// IME 组合变化事件参数
/// </summary>
public class CompositionChangedEventArgs : EventArgs
{
    public string CompositionText { get; }
    public int CursorPosition { get; }
    
    public CompositionChangedEventArgs(string text, int cursorPosition)
    {
        CompositionText = text;
        CursorPosition = cursorPosition;
    }
}

/// <summary>
/// IME 结果事件参数
/// </summary>
public class ResultEventArgs : EventArgs
{
    public string Result { get; }
    
    public ResultEventArgs(string result)
    {
        Result = result;
    }
}