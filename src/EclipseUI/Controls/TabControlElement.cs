using SkiaSharp;
using EclipseUI.Core;
using EclipseUI.Rendering;


namespace EclipseUI.Controls;

/// <summary>
/// 选项卡控件元素
/// </summary>
public class TabControlElement : EclipseElement
{
    public List<string> Headers { get; set; } = new();
    public int SelectedIndex { get; set; } = 0;
    public float HeaderHeight { get; set; } = 44;
    public float FontSize { get; set; } = 14;
    public Action<int>? OnTabSelected { get; set; }
    
    private int _hoverIndex = -1;
    
    // 缓存每个 Tab 内容的子元素索引
    private readonly Dictionary<int, List<EclipseElement>> _tabContents = new();
    
    public void SetTabContent(int tabIndex, List<EclipseElement> elements)
    {
        _tabContents[tabIndex] = elements;
    }
    
    public override SKSize Measure(SKCanvas canvas, float availableWidth, float availableHeight)
    {
        float width = RequestedWidth ?? availableWidth;
        float height = RequestedHeight ?? availableHeight;
        
        if (MinWidth.HasValue) width = Math.Max(width, MinWidth.Value);
        if (MinHeight.HasValue) height = Math.Max(height, MinHeight.Value);
        
        return new SKSize(width, height);
    }
    
    public override void Arrange(SKCanvas canvas, float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
        
        // 内容区域
        float contentY = y + HeaderHeight;
        float contentHeight = height - HeaderHeight;
        
        // 只排列当前选中 Tab 的子元素
        for (int i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            if (i == SelectedIndex)
            {
                child.IsVisible = true;
                var size = child.Measure(canvas, width, contentHeight);
                child.Arrange(canvas, x, contentY, width, contentHeight);
            }
            else
            {
                child.IsVisible = false;
            }
        }
    }
    
    public override void Render(SKCanvas canvas)
    {
        if (!IsVisible) return;
        
        canvas.Save();
        try
        {
            // 背景
            if (BackgroundColor.HasValue)
            {
                var rect = new SKRect(X, Y, X + Width, Y + Height);
                using var bgPaint = new SKPaint { Color = BackgroundColor.Value, IsAntialias = true };
                canvas.DrawRect(rect, bgPaint);
            }
            
            // 绘制 Tab 头部
            RenderHeaders(canvas);
            
            // 绘制内容区域
            var contentRect = new SKRect(X, Y + HeaderHeight, X + Width, Y + Height);
            canvas.ClipRect(contentRect);
            
            // 只渲染当前选中的 Tab 内容
            for (int i = 0; i < Children.Count; i++)
            {
                if (i == SelectedIndex && Children[i].IsVisible)
                {
                    Children[i].Render(canvas);
                }
            }
        }
        finally
        {
            canvas.Restore();
        }
    }
    
    private void RenderHeaders(SKCanvas canvas)
    {
        if (Headers.Count == 0) return;
        
        var renderContext = new SkiaRenderContext(canvas);
        float tabWidth = Width / Headers.Count;
        
        // 头部背景
        var headerRect = new SKRect(X, Y, X + Width, Y + HeaderHeight);
        using var headerBgPaint = new SKPaint
        {
            Color = iOSTheme.SystemGray6,
            IsAntialias = true
        };
        canvas.DrawRect(headerRect, headerBgPaint);
        
        for (int i = 0; i < Headers.Count; i++)
        {
            float tabX = X + i * tabWidth;
            var tabRect = new SKRect(tabX, Y, tabX + tabWidth, Y + HeaderHeight);
            
            // 选中/悬停背景
            if (i == SelectedIndex)
            {
                using var selectedPaint = new SKPaint
                {
                    Color = SKColors.White,
                    IsAntialias = true
                };
                canvas.DrawRect(tabRect, selectedPaint);
                
                // 底部指示条
                using var indicatorPaint = new SKPaint
                {
                    Color = iOSTheme.SystemBlue,
                    IsAntialias = true
                };
                canvas.DrawRect(new SKRect(tabX, Y + HeaderHeight - 3, tabX + tabWidth, Y + HeaderHeight), indicatorPaint);
            }
            else if (i == _hoverIndex)
            {
                using var hoverPaint = new SKPaint
                {
                    Color = iOSTheme.SystemGray5,
                    IsAntialias = true
                };
                canvas.DrawRect(tabRect, hoverPaint);
            }
            
            // 文本 - 使用 TextRenderer 支持中文和 Emoji
            var textColor = i == SelectedIndex 
                ? new Color(iOSTheme.SystemBlue.Red, iOSTheme.SystemBlue.Green, iOSTheme.SystemBlue.Blue, iOSTheme.SystemBlue.Alpha)
                : new Color(iOSTheme.LabelPrimary.Red, iOSTheme.LabelPrimary.Green, iOSTheme.LabelPrimary.Blue, iOSTheme.LabelPrimary.Alpha);
            
            float textY = Y + (HeaderHeight + FontSize) / 2 - 2;
            TextRenderer.DrawText(renderContext, Headers[i], tabX + tabWidth / 2, textY, FontSize, textColor, SKTextAlign.Center);
        }
        
        // 底部分隔线
        using var separatorPaint = new SKPaint
        {
            Color = iOSTheme.Separator,
            StrokeWidth = 0.5f
        };
        canvas.DrawLine(X, Y + HeaderHeight, X + Width, Y + HeaderHeight, separatorPaint);
    }
    
    public override bool HandleMouseDown(float x, float y)
    {
        // 检查是否点击了 Tab 头部
        if (y >= Y && y <= Y + HeaderHeight && Headers.Count > 0)
        {
            float tabWidth = Width / Headers.Count;
            int index = (int)((x - X) / tabWidth);
            if (index >= 0 && index < Headers.Count && index != SelectedIndex)
            {
                SelectedIndex = index;
                OnTabSelected?.Invoke(index);
                return true;
            }
        }
        
        // 传递给内容区域
        if (SelectedIndex >= 0 && SelectedIndex < Children.Count)
        {
            return Children[SelectedIndex].HandleMouseDown(x, y);
        }
        
        return false;
    }
    
    public override bool HandleMouseMove(float x, float y)
    {
        // 检查 Tab 头部悬停
        if (y >= Y && y <= Y + HeaderHeight && Headers.Count > 0)
        {
            float tabWidth = Width / Headers.Count;
            int index = (int)((x - X) / tabWidth);
            if (index >= 0 && index < Headers.Count)
            {
                _hoverIndex = index;
            }
            else
            {
                _hoverIndex = -1;
            }
            return true;
        }
        
        _hoverIndex = -1;
        
        // 传递给内容区域
        if (SelectedIndex >= 0 && SelectedIndex < Children.Count)
        {
            return Children[SelectedIndex].HandleMouseMove(x, y);
        }
        
        return false;
    }
    
    public override void HandleMouseLeave()
    {
        _hoverIndex = -1;
        base.HandleMouseLeave();
    }
    
    public override bool HandleMouseWheel(float deltaY)
    {
        if (SelectedIndex >= 0 && SelectedIndex < Children.Count)
        {
            return Children[SelectedIndex].HandleMouseWheel(deltaY);
        }
        return false;
    }
}
