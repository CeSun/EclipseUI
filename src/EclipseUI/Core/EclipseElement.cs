using SkiaSharp;

namespace EclipseUI.Core;

/// <summary>
/// EclipseUI 鍏冪礌鍩虹被 - 鎵€锟?UI 鍏冪礌鐨勬娊璞″熀锟?/// </summary>
public class EclipseElement
{
    /// <summary>
    /// 鐖跺厓锟?    /// </summary>
    public EclipseElement? Parent { get; internal set; }
    
    /// <summary>
    /// 瀛愬厓绱犲垪锟?    /// </summary>
    public List<EclipseElement> Children { get; } = new();
    
    /// <summary>
    /// 鍏冪礌鏄惁鍙
    /// </summary>
    public bool IsVisible { get; set; } = true;
    
    /// <summary>
    /// 鍏冪礌鐨勪綅缃紙鐩稿浜庣埗鍏冪礌锟?    /// </summary>
    public float X { get; set; }
    
    /// <summary>
    /// 鍏冪礌鐨勪綅缃紙鐩稿浜庣埗鍏冪礌锟?    /// </summary>
    public float Y { get; set; }
    
    /// <summary>
    /// 鍏冪礌鐨勫锟?    /// </summary>
    public float Width { get; set; }
    
    /// <summary>
    /// 鍏冪礌鐨勯珮锟?    /// </summary>
    public float Height { get; set; }
    /// <summary>
    /// 用户请求的宽度（可选，null 表示自动）
    /// </summary>
    public float? RequestedWidth { get; set; }
    
    /// <summary>
    /// 用户请求的高度（可选，null 表示自动）
    /// </summary>
    public float? RequestedHeight { get; set; }    
    /// <summary>
    /// 最小宽度（可选）
    /// </summary>
    public float? MinWidth { get; set; }
    
    /// <summary>
    /// 最小高度（可选）
    /// </summary>
    public float? MinHeight { get; set; }
    
    /// <summary>
    /// 最大宽度（可选）
    /// </summary>
    public float? MaxWidth { get; set; }
    
    /// <summary>
    /// 最大高度（可选）
    /// </summary>
    public float? MaxHeight { get; set; }
    
    /// <summary>
    /// 水平对齐方式
    /// </summary>
    public HorizontalAlignment HorizontalAlignment { get; set; } = HorizontalAlignment.Stretch;
    
    /// <summary>
    /// 垂直对齐方式
    /// </summary>
    public VerticalAlignment VerticalAlignment { get; set; } = VerticalAlignment.Stretch;
    
    /// <summary>
    /// 宸﹁竟锟?    /// </summary>
    public float MarginLeft { get; set; }
    
    /// <summary>
    /// 涓婅竟锟?    /// </summary>
    public float MarginTop { get; set; }
    
    /// <summary>
    /// 鍙宠竟锟?    /// </summary>
    public float MarginRight { get; set; }
    
    /// <summary>
    /// 涓嬭竟锟?    /// </summary>
    public float MarginBottom { get; set; }
    
    /// <summary>
    /// 宸﹀唴杈硅窛
    /// </summary>
    public float PaddingLeft { get; set; }
    
    /// <summary>
    /// 涓婂唴杈硅窛
    /// </summary>
    public float PaddingTop { get; set; }
    
    /// <summary>
    /// 鍙冲唴杈硅窛
    /// </summary>
    public float PaddingRight { get; set; }
    
    /// <summary>
    /// 涓嬪唴杈硅窛
    /// </summary>
    public float PaddingBottom { get; set; }
    
    /// <summary>
    /// 鑳屾櫙棰滆壊
    /// </summary>
    public SKColor? BackgroundColor { get; set; }
    
    /// <summary>
    /// 鍏冪礌 ID锛堢敤浜庝簨浠跺鐞嗭級
    /// </summary>
    public string? Id { get; set; }
    
    /// <summary>
    /// CSS 绫诲悕锛堢敤浜庢牱寮忥級
    /// </summary>
    public string? ClassName { get; set; }
    
    /// <summary>
    /// 鐐瑰嚮浜嬩欢
    /// </summary>
    public Action<EclipseElement, SKPoint>? OnClick { get; set; }
    
    /// <summary>
    /// 鑾峰彇瀹為檯缁樺埗鍖哄煙锛堣€冭檻鍐呰竟璺濓級
    /// </summary>
    public SKRect ContentRect => new(
        X + PaddingLeft,
        Y + PaddingTop,
        X + Width - PaddingRight,
        Y + Height - PaddingBottom
    );
    
    /// <summary>
    /// 鑾峰彇鍖呭惈杈硅窛鐨勫妗嗗尯锟?    /// </summary>
    public SKRect OuterRect => new(
        X - MarginLeft,
        Y - MarginTop,
        X + Width + MarginRight,
        Y + Height + MarginBottom
    );
    
    /// <summary>
    /// 娴嬮噺鍏冪礌鎵€闇€鐨勬渶灏忓昂锟?    /// </summary>
    public virtual SKSize Measure(SKCanvas canvas, float availableWidth, float availableHeight)
    {
        // 濡傛灉鏈夌敤鎴疯缃殑 RequestedWidth/Height锛屼紭鍏堜娇鐢?
        if (RequestedWidth.HasValue && RequestedHeight.HasValue)
        {
            return new SKSize(RequestedWidth.Value + PaddingLeft + PaddingRight, 
                              RequestedHeight.Value + PaddingTop + PaddingBottom);
        }
        
        if (Children.Count > 0)
        {
            float maxWidth = 0;
            float maxHeight = 0;
            
            foreach (var child in Children)
            {
                var childSize = child.Measure(canvas, availableWidth, availableHeight);
                maxWidth = Math.Max(maxWidth, childSize.Width);
                maxHeight = Math.Max(maxHeight, childSize.Height);
            }
            
            // 濡傛灉鍙缃簡 RequestedWidth 鎴?RequestedHeight 涓殑涓€涓?
            float finalWidth = RequestedWidth ?? maxWidth;
            float finalHeight = RequestedHeight ?? maxHeight;
            
            return new SKSize(finalWidth + PaddingLeft + PaddingRight, 
                              finalHeight + PaddingTop + PaddingBottom);
        }
        
        return new SKSize(RequestedWidth ?? Width, RequestedHeight ?? Height);
    }
    
    /// <summary>
    /// 鎺掑垪鍏冪礌鍙婂叾瀛愬厓锟?    /// </summary>
    public virtual void Arrange(SKCanvas canvas, float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
        ArrangeChildren(canvas);
    }
    
    /// <summary>
    /// 鎺掑垪瀛愬厓锟?    /// </summary>
    protected virtual void ArrangeChildren(SKCanvas canvas)
    {
        foreach (var child in Children)
        {
            child.Arrange(canvas, X + PaddingLeft, Y + PaddingTop, 
                Width - PaddingLeft - PaddingRight, 
                Height - PaddingTop - PaddingBottom);
        }
    }
    
    /// <summary>
    /// 缁樺埗鍏冪礌鍙婂叾瀛愬厓锟?    /// </summary>
    public virtual void Render(SKCanvas canvas)
    {
        if (!IsVisible) return;
        
        canvas.Save();
        
        try
        {
            if (BackgroundColor.HasValue)
            {
                var rect = new SKRect(X, Y, X + Width, Y + Height);
                using var paint = new SKPaint { Color = BackgroundColor.Value, IsAntialias = true };
                canvas.DrawRect(rect, paint);
            }
            
            RenderContent(canvas);
            RenderChildren(canvas);
        }
        finally
        {
            canvas.Restore();
        }
    }
    
    /// <summary>
    /// 缁樺埗鍏冪礌鍐呭锛堢敱瀛愮被瀹炵幇锟?    /// </summary>
    protected virtual void RenderContent(SKCanvas canvas) { }
    
    /// <summary>
    /// 缁樺埗瀛愬厓锟?    /// </summary>
    protected void RenderChildren(SKCanvas canvas)
    {
        foreach (var child in Children)
        {
            child.Render(canvas);
        }
    }
    
    /// <summary>
    /// 澶勭悊鐐瑰嚮浜嬩欢
    /// </summary>
    public virtual bool HandleClick(SKPoint point)
    {
        if (!IsVisible) return false;
        
        var rect = new SKRect(X, Y, X + Width, Y + Height);
        if (!rect.Contains(point)) return false;
        
        for (int i = Children.Count - 1; i >= 0; i--)
        {
            if (Children[i].HandleClick(point)) return true;
        }
        
        OnClick?.Invoke(this, point);
        return true;
    }
    
    /// <summary>
    /// 娣诲姞瀛愬厓锟?    /// </summary>
    public void AddChild(EclipseElement child)
    {
        child.Parent = this;
        Children.Add(child);
    }
    
    /// <summary>
    /// 绉婚櫎瀛愬厓锟?    /// </summary>
    public void RemoveChild(EclipseElement child)
    {
        child.Parent = null;
        Children.Remove(child);
    }
    
    /// <summary>
    /// 娓呴櫎鎵€鏈夊瓙鍏冪礌
    /// </summary>
    public void ClearChildren()
    {
        foreach (var child in Children) child.Parent = null;
        Children.Clear();
    }
    
    /// <summary>
    /// 闄勫姞灞炴€у€煎瓨鍌?
    /// </summary>
    private readonly Dictionary<int, object?> _attachedProperties = new();
    
    /// <summary>
    /// 璁剧疆闄勫姞灞炴€у€?
    /// </summary>
    public void SetValue(int propertyKey, object? value)
    {
        _attachedProperties[propertyKey] = value;
    }
    
    /// <summary>
    /// 鑾峰彇闄勫姞灞炴€у€?
    /// </summary>
    public T GetValue<T>(int propertyKey, T defaultValue)
    {
        if (_attachedProperties.TryGetValue(propertyKey, out var value))
            return (T?)(value ?? defaultValue);
        return defaultValue;
    }
}

/// <summary>
/// 榧犳爣浜嬩欢鍙傛暟
/// </summary>
public class MouseEventArgs
{
    public float ClientX { get; set; }
    public float ClientY { get; set; }
}
