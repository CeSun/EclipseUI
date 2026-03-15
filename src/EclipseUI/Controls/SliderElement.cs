using SkiaSharp;
using EclipseUI.Core;
using EclipseUI.Rendering;

namespace EclipseUI.Controls;

/// <summary>
/// 滑块元素
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
    public float FontSize { get; set; } = 12;
    public SKColor TextColor { get; set; } = SKColors.Black;
    public SKColor TrackColor { get; set; } = SKColor.Parse("#E0E0E0");
    public SKColor ThumbColor { get; set; } = SKColors.Blue;
    
    public bool IsHovered { get; set; }
    public bool IsDragging { get; set; }
    
    /// <summary>
    /// 轨道厚度
    /// </summary>
    private const float TrackThickness = 4;
    
    /// <summary>
    /// 滑块半径
    /// </summary>
    private const float ThumbRadius = 10;
    
    /// <summary>
    /// 刻度标记大小
    /// </summary>
    private const float TickSize = 6;
    
    /// <summary>
    /// 值变更回调
    /// </summary>
    public Func<double, Task>? OnValueChanged { get; set; }
    
    /// <summary>
    /// 点击事件
    /// </summary>
    public Action<EclipseElement, SKPoint>? OnClick { get; set; }
    
    public override SKSize Measure(SKCanvas canvas, float availableWidth, float availableHeight)
    {
        float contentWidth, contentHeight;
        
        if (Orientation == Orientation.Horizontal)
        {
            contentWidth = RequestedWidth ?? 200;
            contentHeight = Math.Max(ThumbRadius * 2 + 4, FontSize + 20);
        }
        else
        {
            contentWidth = Math.Max(ThumbRadius * 2 + 4, FontSize + 20);
            contentHeight = RequestedHeight ?? 200;
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
            canvas.Restore();
        }
    }
    
    protected override void RenderContent(SKCanvas canvas)
    {
        if (Orientation == Orientation.Horizontal)
        {
            RenderHorizontal(canvas);
        }
        else
        {
            RenderVertical(canvas);
        }
    }
    
    /// <summary>
    /// 绘制水平滑块
    /// </summary>
    private void RenderHorizontal(SKCanvas canvas)
    {
        float trackY = Y + Height / 2;
        float trackLeft = X + ThumbRadius;
        float trackRight = X + Width - ThumbRadius;
        float trackLength = trackRight - trackLeft;
        
        // 计算滑块位置
        double normalizedValue = (Value - Minimum) / (Maximum - Minimum);
        float thumbX = trackLeft + (float)normalizedValue * trackLength;
        
        // 绘制轨道背景
        using var trackPaint = new SKPaint { Color = TrackColor, IsAntialias = true };
        var trackRect = new SKRect(trackLeft, trackY - TrackThickness / 2, trackRight, trackY + TrackThickness / 2);
        canvas.DrawRoundRect(trackRect, TrackThickness / 2, TrackThickness / 2, trackPaint);
        
        // 绘制已填充的轨道
        using var filledPaint = new SKPaint { Color = ThumbColor, IsAntialias = true };
        var filledRect = new SKRect(trackLeft, trackY - TrackThickness / 2, thumbX, trackY + TrackThickness / 2);
        canvas.DrawRoundRect(filledRect, TrackThickness / 2, TrackThickness / 2, filledPaint);
        
        // 绘制刻度
        if (ShowTicks && TickFrequency > 0)
        {
            DrawHorizontalTicks(canvas, trackLeft, trackY, trackLength);
        }
        
        // 绘制滑块阴影
        using var shadowPaint = new SKPaint 
        { 
            Color = SKColor.Parse("#40000000"), 
            IsAntialias = true,
            MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 2)
        };
        canvas.DrawCircle(thumbX, trackY + 2, ThumbRadius, shadowPaint);
        
        // 绘制滑块
        using var thumbPaint = new SKPaint { Color = IsDragging || IsHovered ? SKColors.LightBlue : ThumbColor, IsAntialias = true };
        canvas.DrawCircle(thumbX, trackY, ThumbRadius, thumbPaint);
        
        // 绘制值文本
        string valueText = Value.ToString("F0");
        float textX = X + Width / 2;
        float textY = Y + Height - 4;
        
        using var renderContext = new SkiaRenderContext(canvas);
        TextRenderer.DrawText(
            renderContext,
            valueText,
            textX,
            textY,
            FontSize,
            new Color(TextColor.Red, TextColor.Green, TextColor.Blue, TextColor.Alpha),
            SKTextAlign.Center
        );
    }
    
    /// <summary>
    /// 绘制水刻度
    /// </summary>
    private void DrawHorizontalTicks(SKCanvas canvas, float trackLeft, float trackY, float trackLength)
    {
        using var tickPaint = new SKPaint { Color = SKColors.Gray, IsAntialias = true, StrokeWidth = 1 };
        
        double range = Maximum - Minimum;
        int tickCount = (int)(range / TickFrequency) + 1;
        float tickSpacing = trackLength / (tickCount - 1);
        
        for (int i = 0; i < tickCount; i++)
        {
            float tickX = trackLeft + i * tickSpacing;
            canvas.DrawLine(tickX, trackY + TrackThickness / 2 + 2, tickX, trackY + TrackThickness / 2 + 2 + TickSize, tickPaint);
        }
    }
    
    /// <summary>
    /// 绘制垂直滑块
    /// </summary>
    private void RenderVertical(SKCanvas canvas)
    {
        float trackX = X + Width / 2;
        float trackTop = Y + ThumbRadius;
        float trackBottom = Y + Height - ThumbRadius;
        float trackLength = trackBottom - trackTop;
        
        // 计算滑块位置（垂直方向从上到下）
        double normalizedValue = (Value - Minimum) / (Maximum - Minimum);
        float thumbY = trackBottom - (float)normalizedValue * trackLength;
        
        // 绘制轨道背景
        using var trackPaint = new SKPaint { Color = TrackColor, IsAntialias = true };
        var trackRect = new SKRect(trackX - TrackThickness / 2, trackTop, trackX + TrackThickness / 2, trackBottom);
        canvas.DrawRoundRect(trackRect, TrackThickness / 2, TrackThickness / 2, trackPaint);
        
        // 绘制已填充的轨道
        using var filledPaint = new SKPaint { Color = ThumbColor, IsAntialias = true };
        var filledRect = new SKRect(trackX - TrackThickness / 2, thumbY, trackX + TrackThickness / 2, trackBottom);
        canvas.DrawRoundRect(filledRect, TrackThickness / 2, TrackThickness / 2, filledPaint);
        
        // 绘制滑块阴影
        using var shadowPaint = new SKPaint 
        { 
            Color = SKColor.Parse("#40000000"), 
            IsAntialias = true,
            MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 2)
        };
        canvas.DrawCircle(trackX, thumbY + 2, ThumbRadius, shadowPaint);
        
        // 绘制滑块
        using var thumbPaint = new SKPaint { Color = IsDragging || IsHovered ? SKColors.LightBlue : ThumbColor, IsAntialias = true };
        canvas.DrawCircle(trackX, thumbY, ThumbRadius, thumbPaint);
        
        // 绘制值文本
        string valueText = Value.ToString("F0");
        float textX = X + Width / 2;
        float textY = Y - 8;
        
        using var renderContext = new SkiaRenderContext(canvas);
        TextRenderer.DrawText(
            renderContext,
            valueText,
            textX,
            textY,
            FontSize,
            new Color(TextColor.Red, TextColor.Green, TextColor.Blue, TextColor.Alpha),
            SKTextAlign.Center
        );
    }
    
    /// <summary>
    /// 根据位置计算值
    /// </summary>
    private double GetValueFromPosition(float x, float y)
    {
        if (Orientation == Orientation.Horizontal)
        {
            float trackLeft = X + ThumbRadius;
            float trackRight = X + Width - ThumbRadius;
            float trackLength = trackRight - trackLeft;
            float posX = Math.Max(trackLeft, Math.Min(x, trackRight));
            double normalized = (posX - trackLeft) / trackLength;
            double value = Minimum + normalized * (Maximum - Minimum);
            
            if (IsSnapToTick && TickFrequency > 0)
            {
                value = Math.Round(value / TickFrequency) * TickFrequency;
            }
            
            return Math.Max(Minimum, Math.Min(Maximum, value));
        }
        else
        {
            float trackTop = Y + ThumbRadius;
            float trackBottom = Y + Height - ThumbRadius;
            float trackLength = trackBottom - trackTop;
            float posY = Math.Max(trackTop, Math.Min(y, trackBottom));
            double normalized = (trackBottom - posY) / trackLength;
            double value = Minimum + normalized * (Maximum - Minimum);
            
            if (IsSnapToTick && TickFrequency > 0)
            {
                value = Math.Round(value / TickFrequency) * TickFrequency;
            }
            
            return Math.Max(Minimum, Math.Min(Maximum, value));
        }
    }
    
    public override bool HandleMouseDown(float x, float y)
    {
        // 检查是否点击在滑块上
        if (Orientation == Orientation.Horizontal)
        {
            float trackY = Y + Height / 2;
            double normalizedValue = (Value - Minimum) / (Maximum - Minimum);
            float trackLeft = X + ThumbRadius;
            float trackRight = X + Width - ThumbRadius;
            float thumbX = trackLeft + (float)normalizedValue * (trackRight - trackLeft);
            
            var thumbRect = new SKRect(thumbX - ThumbRadius, trackY - ThumbRadius, thumbX + ThumbRadius, trackY + ThumbRadius);
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
            double normalizedValue = (Value - Minimum) / (Maximum - Minimum);
            float trackTop = Y + ThumbRadius;
            float trackBottom = Y + Height - ThumbRadius;
            float thumbY = trackBottom - (float)normalizedValue * (trackBottom - trackTop);
            
            var thumbRect = new SKRect(trackX - ThumbRadius, thumbY - ThumbRadius, trackX + ThumbRadius, thumbY + ThumbRadius);
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
    
    public override void HandleMouseUp()
    {
        IsDragging = false;
    }
    
    public override void HandleMouseLeave()
    {
        IsHovered = false;
        IsDragging = false;
    }
    
    /// <summary>
    /// 更新值
    /// </summary>
    private void UpdateValue(float x, float y)
    {
        double newValue = GetValueFromPosition(x, y);
        if (Math.Abs(newValue - Value) > 0.001)
        {
            Value = newValue;
            OnValueChanged?.Invoke(Value);
        }
    }
}
