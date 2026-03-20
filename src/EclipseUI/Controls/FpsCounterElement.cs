using EclipseUI.Core;
using SkiaSharp;
using System.Diagnostics;

namespace EclipseUI.Controls;

/// <summary>
/// 帧率计数器元素 - 渲染帧率显示
/// </summary>
public class FpsCounterElement : EclipseElement
{
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private long _lastFrameTime = 0;
    private float _fps = 0f;
    
    public string Color { get; set; } = "#FF0000";
    public int FontSize { get; set; } = 14;
    
    public override void Render(SKCanvas canvas)
    {
        if (!IsVisible || canvas == null) return;
        
        // 计算帧率
        var currentTime = _stopwatch.ElapsedMilliseconds;
        if (currentTime - _lastFrameTime > 0)
        {
            var frameTime = currentTime - _lastFrameTime;
            _fps = 1000f / frameTime;
            _lastFrameTime = currentTime;
        }
        
        // 绘制 FPS 文本（使用基类的 X, Y 属性）
        using var paint = new SKPaint
        {
            Color = SKColor.Parse(Color),
            TextSize = FontSize,
            IsAntialias = true
        };
        
        var fpsText = $"FPS: {_fps:F1}";
        canvas.DrawText(fpsText, X, Y + FontSize, paint);
    }
    
    public override SKSize Measure(SKCanvas canvas, float availableWidth, float availableHeight)
    {
        // FPS 计数器不需要复杂的测量，返回固定大小
        return new SKSize(100, FontSize + 4);
    }
    
    // FpsCounterElement 不需要重写 Arrange，使用基类的默认实现
}