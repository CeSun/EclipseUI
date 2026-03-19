using SkiaSharp;
using EclipseUI.Core;
using EclipseUI.Rendering;

namespace EclipseUI.Controls;

/// <summary>
/// 开关元素 - iOS 风格
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
    public float FontSize { get; set; } = iOSTheme.FontSizeBody;
    public SKColor OnColor { get; set; } = iOSTheme.SystemGreen;
    public SKColor OffColor { get; set; } = iOSTheme.SystemGray5;
    
    public bool IsHovered { get; set; }
    public bool IsPressed { get; set; }
    
    // iOS 标准开关尺寸
    private const float SwitchWidth = 51f;
    private const float SwitchHeight = 31f;
    private const float ThumbSize = 27f;
    private const float ThumbMargin = 2f;
    
    private float _animationProgress;
    
    public Func<bool, Task>? OnToggled { get; set; }
    public Action<EclipseElement, SKPoint>? OnClick { get; set; }
    
    public override SKSize Measure(SKCanvas canvas, float availableWidth, float availableHeight)
    {
        float contentWidth = SwitchWidth;
        float contentHeight = SwitchHeight;
        
        // 如果有文本，增加宽度
        if (!string.IsNullOrEmpty(OnContent) || !string.IsNullOrEmpty(OffContent))
        {
            string text = !string.IsNullOrEmpty(OnContent) ? OnContent : OffContent;
            contentWidth += TextRenderer.MeasureText(text, FontSize) + 12;
        }
        
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
        float trackRight = X + SwitchWidth;
        
        var trackRect = new SKRect(trackLeft, centerY - SwitchHeight / 2, trackRight, centerY + SwitchHeight / 2);
        
        // 轨道背景色：开启绿色，关闭白色
        var trackColor = _animationProgress > 0.5f ? OnColor : SKColors.White;
        using var trackPaint = new SKPaint { Color = trackColor, IsAntialias = true };
        canvas.DrawRoundRect(trackRect, SwitchHeight / 2, SwitchHeight / 2, trackPaint);
        
        // 始终绘制 1px 灰色描边
        using var borderPaint = new SKPaint 
        { 
            Color = iOSTheme.SystemGray3, 
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1f
        };
        var borderRect = new SKRect(trackLeft + 0.5f, centerY - SwitchHeight / 2 + 0.5f, trackRight - 0.5f, centerY + SwitchHeight / 2 - 0.5f);
        canvas.DrawRoundRect(borderRect, SwitchHeight / 2 - 0.5f, SwitchHeight / 2 - 0.5f, borderPaint);
        
        // 计算滑块位置
        float thumbTravel = SwitchWidth - ThumbSize - ThumbMargin * 2;
        float thumbX = trackLeft + ThumbMargin + thumbTravel * _animationProgress;
        float thumbY = centerY;
        
        // 绘制滑块阴影
        using var shadowPaint = new SKPaint 
        { 
            Color = new SKColor(0, 0, 0, 40),
            IsAntialias = true,
            MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 3)
        };
        canvas.DrawCircle(thumbX + ThumbSize / 2, thumbY + 2, ThumbSize / 2, shadowPaint);
        
        // 绘制白色滑块
        using var thumbPaint = new SKPaint { Color = SKColors.White, IsAntialias = true };
        canvas.DrawCircle(thumbX + ThumbSize / 2, thumbY, ThumbSize / 2, thumbPaint);
        
        // 绘制文本标签（可选）
        if (!string.IsNullOrEmpty(OnContent) || !string.IsNullOrEmpty(OffContent))
        {
            float textX = trackRight + 12;
            float textY = centerY + FontSize / 3;
            
            string displayText = IsOn ? OnContent : OffContent;
            if (!string.IsNullOrEmpty(displayText))
            {
                using var renderContext = new SkiaRenderContext(canvas);
                var textColor = new Color(iOSTheme.LabelPrimary.Red, iOSTheme.LabelPrimary.Green, 
                    iOSTheme.LabelPrimary.Blue, iOSTheme.LabelPrimary.Alpha);
                TextRenderer.DrawText(renderContext, displayText, textX, textY, FontSize, textColor);
            }
        }
    }
    
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
