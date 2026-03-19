using SkiaSharp;
using EclipseUI.Core;
using EclipseUI.Rendering;

namespace EclipseUI.Controls;

/// <summary>
/// 滑块元素 - iOS 风格
/// </summary>
public class SliderElement : EclipseElement
{
    public double Value { get; set; }
    public double Minimum { get; set; } = 0;
    public double Maximum { get; set; } = 100;
    public double TickFrequency { get; set; } = 1;
    public bool ShowTicks { get; set; }
    public bool IsSnapToTick { get; set; }
    public Orientation Orientation { get; set; } = Orientation.Horizontal;
    public float FontSize { get; set; } = iOSTheme.FontSizeBody;
    public SKColor TextColor { get; set; } = iOSTheme.LabelPrimary;
    public SKColor TrackColor { get; set; } = iOSTheme.SystemGray5;
    public SKColor ThumbColor { get; set; } = SKColors.White;
    public SKColor MinTrackColor { get; set; } = iOSTheme.SystemBlue;
    
    public bool IsHovered { get; set; }
    public bool IsDragging { get; set; }
    
    // iOS 风格尺寸
    private const float TrackThickness = 4;
    private const float ThumbSize = 28;
    private const float TickSize = 6;
    
    public Func<double, Task>? OnValueChanged { get; set; }
    public Action<EclipseElement, SKPoint>? OnClick { get; set; }
    
    public override SKSize Measure(SKCanvas canvas, float availableWidth, float availableHeight)
    {
        float contentWidth, contentHeight;
        
        if (Orientation == Orientation.Horizontal)
        {
            // 如果有指定宽度则使用，否则使用可用宽度或默认值
            if (RequestedWidth.HasValue)
            {
                contentWidth = RequestedWidth.Value;
            }
            else if (availableWidth > 0 && availableWidth < float.MaxValue)
            {
                contentWidth = availableWidth;
            }
            else
            {
                contentWidth = 200;
            }
            contentHeight = ThumbSize + 16;
        }
        else
        {
            contentWidth = ThumbSize + 16;
            // 如果有指定高度则使用，否则使用可用高度或默认值
            if (RequestedHeight.HasValue)
            {
                contentHeight = RequestedHeight.Value;
            }
            else if (availableHeight > 0 && availableHeight < float.MaxValue)
            {
                contentHeight = availableHeight;
            }
            else
            {
                contentHeight = 200;
            }
        }
        
        float finalWidth = contentWidth;
        float finalHeight = contentHeight;
        
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
            canvas.Restore();;
        }
    }
    
    protected override void RenderContent(SKCanvas canvas)
    {
        if (Orientation == Orientation.Horizontal)
            RenderHorizontal(canvas);
        else
            RenderVertical(canvas);
    }
    
    private void RenderHorizontal(SKCanvas canvas)
    {
        float trackY = Y + Height / 2;
        float trackLeft = X + ThumbSize / 2;
        float trackRight = X + Width - ThumbSize / 2;
        float trackLength = trackRight - trackLeft;
        
        double normalizedValue = (Value - Minimum) / (Maximum - Minimum);
        float thumbX = trackLeft + (float)normalizedValue * trackLength;
        
        // 绘制轨道背景（灰色）
        using var trackPaint = new SKPaint { Color = TrackColor, IsAntialias = true };
        var trackRect = new SKRect(trackLeft, trackY - TrackThickness / 2, trackRight, trackY + TrackThickness / 2);
        canvas.DrawRoundRect(trackRect, TrackThickness / 2, TrackThickness / 2, trackPaint);
        
        // 绘制已填充的轨道（蓝色）
        using var filledPaint = new SKPaint { Color = MinTrackColor, IsAntialias = true };
        var filledRect = new SKRect(trackLeft, trackY - TrackThickness / 2, thumbX, trackY + TrackThickness / 2);
        canvas.DrawRoundRect(filledRect, TrackThickness / 2, TrackThickness / 2, filledPaint);
        
        // 绘制刻度
        if (ShowTicks && TickFrequency > 0)
        {
            DrawHorizontalTicks(canvas, trackLeft, trackY, trackLength);
        }
        
        // iOS 风格滑块：白色圆形 + 柔和阴影
        // 绘制阴影
        using var shadowPaint = new SKPaint 
        { 
            Color = new SKColor(0, 0, 0, 40),
            IsAntialias = true,
            MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 3)
        };
        canvas.DrawCircle(thumbX, trackY + 1.5f, ThumbSize / 2 + 1, shadowPaint);
        
        // 绘制白色滑块
        using var thumbPaint = new SKPaint { Color = SKColors.White, IsAntialias = true };
        canvas.DrawCircle(thumbX, trackY, ThumbSize / 2, thumbPaint);
        
        // 绘制滑块边框（非常淡的灰色）
        using var borderPaint = new SKPaint 
        { 
            Color = new SKColor(0, 0, 0, 20),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 0.5f
        };
        canvas.DrawCircle(thumbX, trackY, ThumbSize / 2, borderPaint);
    }
    
    private void DrawHorizontalTicks(SKCanvas canvas, float trackLeft, float trackY, float trackLength)
    {
        using var tickPaint = new SKPaint { Color = iOSTheme.SystemGray3, IsAntialias = true, StrokeWidth = 1 };
        
        double range = Maximum - Minimum;
        int tickCount = (int)(range / TickFrequency) + 1;
        float tickSpacing = trackLength / (tickCount - 1);
        
        for (int i = 0; i < tickCount; i++)
        {
            float tickX = trackLeft + i * tickSpacing;
            canvas.DrawLine(tickX, trackY + TrackThickness / 2 + 4, tickX, trackY + TrackThickness / 2 + 4 + TickSize, tickPaint);
        }
    }
    
    private void RenderVertical(SKCanvas canvas)
    {
        float trackX = X + Width / 2;
        float trackTop = Y + ThumbSize / 2;
        float trackBottom = Y + Height - ThumbSize / 2;
        float trackLength = trackBottom - trackTop;
        
        double normalizedValue = (Value - Minimum) / (Maximum - Minimum);
        float thumbY = trackBottom - (float)normalizedValue * trackLength;
        
        // 绘制轨道背景
        using var trackPaint = new SKPaint { Color = TrackColor, IsAntialias = true };
        var trackRect = new SKRect(trackX - TrackThickness / 2, trackTop, trackX + TrackThickness / 2, trackBottom);
        canvas.DrawRoundRect(trackRect, TrackThickness / 2, TrackThickness / 2, trackPaint);
        
        // 绘制已填充的轨道
        using var filledPaint = new SKPaint { Color = MinTrackColor, IsAntialias = true };
        var filledRect = new SKRect(trackX - TrackThickness / 2, thumbY, trackX + TrackThickness / 2, trackBottom);
        canvas.DrawRoundRect(filledRect, TrackThickness / 2, TrackThickness / 2, filledPaint);
        
        // 绘制阴影
        using var shadowPaint = new SKPaint 
        { 
            Color = new SKColor(0, 0, 0, 40),
            IsAntialias = true,
            MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 3)
        };
        canvas.DrawCircle(trackX, thumbY + 1.5f, ThumbSize / 2 + 1, shadowPaint);
        
        // 绘制白色滑块
        using var thumbPaint = new SKPaint { Color = SKColors.White, IsAntialias = true };
        canvas.DrawCircle(trackX, thumbY, ThumbSize / 2, thumbPaint);
        
        // 绘制滑块边框
        using var borderPaint = new SKPaint 
        { 
            Color = new SKColor(0, 0, 0, 20),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 0.5f
        };
        canvas.DrawCircle(trackX, thumbY, ThumbSize / 2, borderPaint);
    }
    
    private double GetValueFromPosition(float x, float y)
    {
        if (Orientation == Orientation.Horizontal)
        {
            float trackLeft = X + ThumbSize / 2;
            float trackRight = X + Width - ThumbSize / 2;
            float trackLength = trackRight - trackLeft;
            float posX = Math.Max(trackLeft, Math.Min(x, trackRight));
            double normalized = (posX - trackLeft) / trackLength;
            double value = Minimum + normalized * (Maximum - Minimum);
            
            if (IsSnapToTick && TickFrequency > 0)
                value = Math.Round(value / TickFrequency) * TickFrequency;
            
            return Math.Max(Minimum, Math.Min(Maximum, value));
        }
        else
        {
            float trackTop = Y + ThumbSize / 2;
            float trackBottom = Y + Height - ThumbSize / 2;
            float trackLength = trackBottom - trackTop;
            float posY = Math.Max(trackTop, Math.Min(y, trackBottom));
            double normalized = (trackBottom - posY) / trackLength;
            double value = Minimum + normalized * (Maximum - Minimum);
            
            if (IsSnapToTick && TickFrequency > 0)
                value = Math.Round(value / TickFrequency) * TickFrequency;
            
            return Math.Max(Minimum, Math.Min(Maximum, value));
        }
    }
    
    public override bool HandleMouseDown(float x, float y)
    {
        float clickRadius = ThumbSize / 2 + 5;
        
        if (Orientation == Orientation.Horizontal)
        {
            float trackY = Y + Height / 2;
            double range = Maximum - Minimum;
            if (range == 0) return false;
            
            double normalizedValue = (Value - Minimum) / range;
            float trackLeft = X + ThumbSize / 2;
            float trackRight = X + Width - ThumbSize / 2;
            float thumbX = trackLeft + (float)normalizedValue * (trackRight - trackLeft);
            
            var thumbRect = new SKRect(thumbX - clickRadius, trackY - clickRadius, thumbX + clickRadius, trackY + clickRadius);
            if (thumbRect.Contains(new SKPoint(x, y)))
            {
                IsDragging = true;
                UpdateValue(x, y);
                return true;
            }
        }
        else
        {
            float trackX = X + Width / 2;
            double range = Maximum - Minimum;
            if (range == 0) return false;
            
            double normalizedValue = (Value - Minimum) / range;
            float trackTop = Y + ThumbSize / 2;
            float trackBottom = Y + Height - ThumbSize / 2;
            float thumbY = trackBottom - (float)normalizedValue * (trackBottom - trackTop);
            
            var thumbRect = new SKRect(trackX - clickRadius, thumbY - clickRadius, trackX + clickRadius, thumbY + clickRadius);
            if (thumbRect.Contains(new SKPoint(x, y)))
            {
                IsDragging = true;
                UpdateValue(x, y);
                return true;
            }
        }
        
        return false;
    }
    
    public override bool HandleMouseMove(float x, float y)
    {
        var rect = new SKRect(X, Y, X + Width, Y + Height);
        var wasHovered = IsHovered;
        IsHovered = rect.Contains(new SKPoint(x, y));
        
        if (IsDragging)
        {
            UpdateValue(x, y);
            return true;
        }
        
        return IsHovered != wasHovered;
    }
    
    private void UpdateValue(float x, float y)
    {
        double newValue = GetValueFromPosition(x, y);
        if (Math.Abs(newValue - Value) > 0.001)
        {
            Value = newValue;
            OnValueChanged?.Invoke(Value);
        }
    }
    
    public override void HandleMouseUp()
    {
        IsDragging = false;
    }
    
    public override void HandleMouseLeave()
    {
        IsHovered = false;
        IsDragging = false;
    }
}
