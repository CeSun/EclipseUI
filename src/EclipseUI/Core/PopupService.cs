using SkiaSharp;

namespace EclipseUI.Core;

/// <summary>
/// Popup 服务 - 管理所有弹出层
/// </summary>
public class PopupService
{
    private readonly List<PopupInfo> _popups = new();
    
    /// <summary>
    /// 当前活动的 Popup 列表
    /// </summary>
    public IReadOnlyList<PopupInfo> ActivePopups => _popups;
    
    /// <summary>
    /// 显示 Popup
    /// </summary>
    public void Show(PopupInfo popup)
    {
        if (!_popups.Contains(popup))
        {
            _popups.Add(popup);
        }
    }
    
    /// <summary>
    /// 关闭 Popup
    /// </summary>
    public void Close(PopupInfo popup)
    {
        _popups.Remove(popup);
    }
    
    /// <summary>
    /// 关闭所有 Popup
    /// </summary>
    public void CloseAll()
    {
        foreach (var popup in _popups.ToList())
        {
            popup.OnClose?.Invoke();
        }
        _popups.Clear();
    }
    
    /// <summary>
    /// 渲染所有 Popup（在最上层）
    /// </summary>
    public void Render(SKCanvas canvas)
    {
        foreach (var popup in _popups)
        {
            canvas.Save();
            try
            {
                popup.RenderAction?.Invoke(canvas);
            }
            finally
            {
                canvas.Restore();
            }
        }
    }
    
    /// <summary>
    /// 处理点击事件（返回 true 表示已处理）
    /// </summary>
    public bool HandleClick(SKPoint point)
    {
        // 从后往前遍历（后添加的在上层）
        for (int i = _popups.Count - 1; i >= 0; i--)
        {
            var popup = _popups[i];
            
            // 检查是否点击在 Popup 区域内
            if (popup.Bounds.Contains(point))
            {
                // 让 Popup 处理点击
                if (popup.HandleClick?.Invoke(point) == true)
                {
                    return true;
                }
            }
            else if (popup.CloseOnClickOutside)
            {
                // 点击外部，关闭 Popup
                popup.OnClose?.Invoke();
                _popups.RemoveAt(i);
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 处理鼠标移动事件
    /// </summary>
    public bool HandleMouseMove(float x, float y)
    {
        var point = new SKPoint(x, y);
        bool handled = false;
        
        foreach (var popup in _popups)
        {
            if (popup.HandleMouseMove?.Invoke(point) == true)
            {
                handled = true;
            }
        }
        
        return handled;
    }
    
    /// <summary>
    /// 处理鼠标滚轮事件
    /// </summary>
    public bool HandleMouseWheel(float deltaY)
    {
        // 从后往前遍历
        for (int i = _popups.Count - 1; i >= 0; i--)
        {
            var popup = _popups[i];
            if (popup.HandleMouseWheel?.Invoke(deltaY) == true)
            {
                return true;
            }
        }
        
        return false;
    }
}

/// <summary>
/// Popup 信息
/// </summary>
public class PopupInfo
{
    /// <summary>
    /// Popup 的边界区域
    /// </summary>
    public SKRect Bounds { get; set; }
    
    /// <summary>
    /// 渲染动作
    /// </summary>
    public Action<SKCanvas>? RenderAction { get; set; }
    
    /// <summary>
    /// 点击处理（返回 true 表示已处理）
    /// </summary>
    public Func<SKPoint, bool>? HandleClick { get; set; }
    
    /// <summary>
    /// 鼠标移动处理
    /// </summary>
    public Func<SKPoint, bool>? HandleMouseMove { get; set; }
    
    /// <summary>
    /// 鼠标滚轮处理
    /// </summary>
    public Func<float, bool>? HandleMouseWheel { get; set; }
    
    /// <summary>
    /// 关闭回调
    /// </summary>
    public Action? OnClose { get; set; }
    
    /// <summary>
    /// 点击外部时是否关闭
    /// </summary>
    public bool CloseOnClickOutside { get; set; } = true;
    
    /// <summary>
    /// 所属元素（用于标识）
    /// </summary>
    public EclipseElement? Owner { get; set; }
}
