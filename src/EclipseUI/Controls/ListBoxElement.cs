using SkiaSharp;
using EclipseUI.Core;
using EclipseUI.Rendering;


namespace EclipseUI.Controls;

/// <summary>
/// 列表框元素
/// </summary>
public class ListBoxElement : EclipseElement
{
    public IList<string>? ItemsSource { get; set; }
    public List<string>? ItemValues { get; set; }
    public int SelectedIndex { get; set; } = -1;
    public string? SelectedItem { get; set; }
    public float FontSize { get; set; } = 14;
    public SKColor TextColor { get; set; } = SKColors.Black;
    public float ItemHeight { get; set; } = 44;
    public Action<int, string>? OnItemSelected { get; set; }
    
    private int _hoverIndex = -1;
    private float _scrollOffset = 0;
    
    public override SKSize Measure(SKCanvas canvas, float availableWidth, float availableHeight)
    {
        float width = RequestedWidth ?? MinWidth ?? 200;
        float height = RequestedHeight ?? MinHeight ?? 200;
        
        if (MinWidth.HasValue) width = Math.Max(width, MinWidth.Value);
        if (MinHeight.HasValue) height = Math.Max(height, MinHeight.Value);
        
        return new SKSize(width, height);
    }
    
    public override void Render(SKCanvas canvas)
    {
        if (!IsVisible) return;
        
        // 创建 RenderContext
        var renderContext = new SkiaRenderContext(canvas);
        
        canvas.Save();
        try
        {
            // 背景
            var rect = new SKRect(X, Y, X + Width, Y + Height);
            using var bgPaint = new SKPaint
            {
                Color = BackgroundColor ?? SKColors.White,
                IsAntialias = true
            };
            canvas.DrawRoundRect(rect, iOSTheme.CornerRadiusMedium, iOSTheme.CornerRadiusMedium, bgPaint);
            
            // 边框
            using var borderPaint = new SKPaint
            {
                Color = iOSTheme.Separator,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1
            };
            canvas.DrawRoundRect(rect, iOSTheme.CornerRadiusMedium, iOSTheme.CornerRadiusMedium, borderPaint);
            
            // 裁剪内容区域
            canvas.ClipRoundRect(new SKRoundRect(rect, iOSTheme.CornerRadiusMedium));
            
            // 绘制列表项
            if (ItemsSource != null)
            {
                float itemY = Y - _scrollOffset;
                
                for (int i = 0; i < ItemsSource.Count; i++)
                {
                    if (itemY + ItemHeight < Y)
                    {
                        itemY += ItemHeight;
                        continue;
                    }
                    if (itemY > Y + Height) break;
                    
                    var itemRect = new SKRect(X, itemY, X + Width, itemY + ItemHeight);
                    
                    // 选中/悬停背景
                    if (i == SelectedIndex)
                    {
                        using var selectedPaint = new SKPaint
                        {
                            Color = iOSTheme.SystemBlue,
                            IsAntialias = true
                        };
                        canvas.DrawRect(itemRect, selectedPaint);
                    }
                    else if (i == _hoverIndex)
                    {
                        using var hoverPaint = new SKPaint
                        {
                            Color = iOSTheme.SystemGray6,
                            IsAntialias = true
                        };
                        canvas.DrawRect(itemRect, hoverPaint);
                    }
                    
                    // 文本 - 使用 TextRenderer 支持中文和 Emoji
                    var text = ItemsSource[i];
                    float textY = itemY + (ItemHeight + FontSize) / 2 - 2;
                    var textColor = i == SelectedIndex ? Color.White : new Color(TextColor.Red, TextColor.Green, TextColor.Blue, TextColor.Alpha);
                    TextRenderer.DrawText(renderContext, text, X + 16, textY, FontSize, textColor);
                    
                    // 分隔线
                    if (i < ItemsSource.Count - 1 && i != SelectedIndex && i + 1 != SelectedIndex)
                    {
                        using var separatorPaint = new SKPaint
                        {
                            Color = iOSTheme.Separator,
                            StrokeWidth = 0.5f
                        };
                        canvas.DrawLine(X + 16, itemY + ItemHeight, X + Width, itemY + ItemHeight, separatorPaint);
                    }
                    
                    itemY += ItemHeight;
                }
            }
        }
        finally
        {
            canvas.Restore();
        }
    }
    
    public override bool HandleMouseDown(float x, float y)
    {
        var rect = new SKRect(X, Y, X + Width, Y + Height);
        if (!rect.Contains(x, y)) return false;
        
        if (ItemsSource != null)
        {
            int index = (int)((y - Y + _scrollOffset) / ItemHeight);
            if (index >= 0 && index < ItemsSource.Count)
            {
                SelectedIndex = index;
                SelectedItem = ItemsSource[index];
                OnItemSelected?.Invoke(index, ItemsSource[index]);
                return true;
            }
        }
        
        return true;
    }
    
    public override bool HandleMouseMove(float x, float y)
    {
        var rect = new SKRect(X, Y, X + Width, Y + Height);
        if (!rect.Contains(x, y))
        {
            _hoverIndex = -1;
            return false;
        }
        
        if (ItemsSource != null)
        {
            int index = (int)((y - Y + _scrollOffset) / ItemHeight);
            if (index >= 0 && index < ItemsSource.Count)
            {
                _hoverIndex = index;
            }
            else
            {
                _hoverIndex = -1;
            }
        }
        
        return true;
    }
    
    public override void HandleMouseLeave()
    {
        _hoverIndex = -1;
        base.HandleMouseLeave();
    }
    
    public override bool HandleMouseWheel(float x, float y, float deltaY)
    {
        // 只有鼠标在 ListBox 区域内才处理滚轮
        var rect = new SKRect(X, Y, X + Width, Y + Height);
        if (!rect.Contains(x, y)) return false;
        
        if (ItemsSource == null) return false;
        
        float totalHeight = ItemsSource.Count * ItemHeight;
        float maxScroll = Math.Max(0, totalHeight - Height);
        
        _scrollOffset -= deltaY * 30;
        _scrollOffset = Math.Max(0, Math.Min(_scrollOffset, maxScroll));
        
        return true;
    }
}
