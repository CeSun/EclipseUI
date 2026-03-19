using SkiaSharp;
using EclipseUI.Core;
using EclipseUI.Rendering;

namespace EclipseUI.Controls;

/// <summary>
/// 开关元素
/// </summary>
public class ToggleSwitchElement : EclipseElement
{
    private bool _isOn;
    
    public bool IsOn 
    { 
        get => _isOn;
        set
        {
            _isOn = value;
            _animationProgress = value ? 1 : 0;
        }
    }
    
    public string OnContent { get; set; } = "";
    public string OffContent { get; set; } = "";
    public float FontSize { get; set; } = 12;
    public SKColor OnColor { get; set; } = SKColor.Parse("#4CAF50");
    public SKColor OffColor { get; set; } = SKColors.Gray;
    
    public bool IsHovered { get; set; }
    public bool IsPressed { get; set; }
    
    /// <summary>
    /// 开关轨道高度
    /// </summary>
    private const float TrackHeight = 24;
    
    /// <summary>
    /// 开关滑块半径
    /// </summary>
    private const float ThumbRadius = 10;
    
    /// <summary>
    /// 开关动画进度（0-1）
    /// </summary>
    private float _animationProgress;
    
    /// <summary>
    /// 切换回调
    /// </summary>
    public Func<bool, Task>? OnToggled { get; set; }
    
    /// <summary>
    /// 点击事件
    /// </summary>
    public Action<EclipseElement, SKPoint>? OnClick { get; set; }
    
    public override SKSize Measure(SKCanvas canvas, float availableWidth, float availableHeight)
    {
        // 开关宽度 = 2 * 高度 + 文本空间
        float trackWidth = TrackHeight * 2 + 8;
        float contentWidth = trackWidth;
        float contentHeight = Math.Max(TrackHeight, FontSize + 4);
        
        float finalWidth = RequestedWidth ?? contentWidth;
        float finalHeight = RequestedHeight ?? contentHeight;
        
        if (MinWidth.HasValue) finalWidth = Math.Max(finalWidth, MinWidth.Value);
        if (MinHeight.HasValue) finalHeight = Math.Max(finalHeight, MinHeight.Value);
        if (MaxWidth.HasValue) finalWidth = Math.Min(finalWidth, MaxWidth.Value);
        if (MaxHeight.HasValue) finalHeight = Math.Min(finalHeight, MaxHeight.Value);
        
        return new SKSize(finalWidth, finalHeight);
    }
    
    public override void Render(SKCanvas canvas)
    {
        if (!IsVisible) return;
        
        canvas.Save();
        
        try
        {
            RenderContent(canvas);
        }
        finally
        {
            canvas.Restore();
        }
    }
    
    protected override void RenderContent(SKCanvas canvas)
    {
        float centerY = Y + Height / 2;
        float trackLeft = X;
        float trackRight = X + TrackHeight * 2 + 8;
        float trackCenter = (trackLeft + trackRight) / 2;
        
        // 计算滑块位置（带动画）
        float thumbOffset = (trackRight - trackLeft - ThumbRadius * 2) * _animationProgress;
        float thumbCenterX = trackLeft + ThumbRadius + thumbOffset;
        
        // 绘制轨道背景
        var trackColor = InterpolateColor(OffColor, OnColor, _animationProgress);
        using var trackPaint = new SKPaint { Color = trackColor, IsAntialias = true };
        var trackRect = new SKRect(trackLeft, centerY - TrackHeight / 2, trackRight, centerY + TrackHeight / 2);
        canvas.DrawRoundRect(trackRect, TrackHeight / 2, TrackHeight / 2, trackPaint);
        
        // 绘制滑块阴影
        using var shadowPaint = new SKPaint 
        { 
            Color = SKColor.Parse("#40000000"), 
            IsAntialias = true,
            MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 2)
        };
        canvas.DrawCircle(thumbCenterX, centerY + 1, ThumbRadius, shadowPaint);
        
        // 绘制滑块
        using var thumbPaint = new SKPaint { Color = SKColors.White, IsAntialias = true };
        canvas.DrawCircle(thumbCenterX, centerY, ThumbRadius, thumbPaint);
        
        // 绘制文本（可选）
        if (!string.IsNullOrEmpty(OnContent) || !string.IsNullOrEmpty(OffContent))
        {
            float textX = trackRight + 8;
            float textY = Y + (Height + FontSize) / 2;
            
            string displayText = IsOn ? OnContent : OffContent;
            if (!string.IsNullOrEmpty(displayText))
            {
                using var renderContext = new SkiaRenderContext(canvas);
                TextRenderer.DrawText(
                    renderContext,
                    displayText,
                    textX,
                    textY,
                    FontSize,
                    new Color(0, 0, 0, 255)
                );
            }
        }
    }
    
    /// <summary>
    /// 颜色插值
    /// </summary>
    private SKColor InterpolateColor(SKColor from, SKColor to, float progress)
    {
        byte r = (byte)(from.Red + (to.Red - from.Red) * progress);
        byte g = (byte)(from.Green + (to.Green - from.Green) * progress);
        byte b = (byte)(from.Blue + (to.Blue - from.Blue) * progress);
        byte a = (byte)(from.Alpha + (to.Alpha - from.Alpha) * progress);
        return new SKColor(r, g, b, a);
    }
    
    public override bool HandleClick(SKPoint point)
    {
        if (!IsVisible) return false;
        
        var rect = new SKRect(X, Y, X + Width, Y + Height);
        if (!rect.Contains(point)) return false;
        
        // 切换状态
        IsOn = !IsOn;
        _animationProgress = IsOn ? 1 : 0;
        
        OnToggled?.Invoke(IsOn);
        OnClick?.Invoke(this, point);
        
        return true;
    }
    
    public override bool HandleMouseDown(float x, float y)
    {
        var rect = new SKRect(X, Y, X + Width, Y + Height);
        if (rect.Contains(new SKPoint(x, y)))
        {
            IsPressed = true;
            return true;
        }
        return false;
    }
    
    public override bool HandleMouseMove(float x, float y)
    {
        var rect = new SKRect(X, Y, X + Width, Y + Height);
        var wasHovered = IsHovered;
        IsHovered = rect.Contains(new SKPoint(x, y));
        return IsHovered != wasHovered;
    }
    
    public override void HandleMouseUp()
    {
        IsPressed = false;
    }
    
    public override void HandleMouseLeave()
    {
        IsHovered = false;
        IsPressed = false;
    }
}
