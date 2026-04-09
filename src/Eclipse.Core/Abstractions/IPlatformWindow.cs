using System;

namespace Eclipse.Core.Abstractions;

/// <summary>
/// 平台窗口接口 - 抽象窗口操作
/// </summary>
public interface IPlatformWindow : IDisposable
{
    /// <summary>
    /// 窗口标题
    /// </summary>
    string Title { get; set; }
    
    /// <summary>
    /// 窗口宽度
    /// </summary>
    int Width { get; }
    
    /// <summary>
    /// 窗口高度
    /// </summary>
    int Height { get; }
    
    /// <summary>
    /// 窗口句柄（平台特定）
    /// </summary>
    IntPtr Handle { get; }
    
    /// <summary>
    /// 根内容组件
    /// </summary>
    IComponent? Content { get; set; }
    
    /// <summary>
    /// 显示窗口
    /// </summary>
    void Show();
    
    /// <summary>
    /// 显示模态窗口（阻塞）
    /// </summary>
    void ShowDialog();
    
    /// <summary>
    /// 关闭窗口
    /// </summary>
    void Close();
    
    /// <summary>
    /// 使窗口无效（触发重绘）
    /// </summary>
    void Invalidate();
    
    /// <summary>
    /// 更新 IME 组合窗口位置
    /// </summary>
    void UpdateImePosition(double x, double y);
}