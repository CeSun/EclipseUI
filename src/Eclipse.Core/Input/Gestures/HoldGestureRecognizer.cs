using System;

namespace Eclipse.Input.Gestures;

/// <summary>
/// 长按识别器
/// </summary>
public class HoldGestureRecognizer : GestureRecognizer
{
    private Pointer? _trackedPointer;
    private Point _startPosition;
    private DateTime _startTime;
    private bool _holding;
    
    /// <summary>
    /// 长按阈值 (毫秒)
    /// </summary>
    public int HoldDuration { get; set; } = 500;
    
    /// <summary>
    /// 移动阈值 (像素)
    /// </summary>
    public double MoveThreshold { get; set; } = 10;
    
    /// <summary>
    /// 长按开始事件
    /// </summary>
    public event EventHandler<HoldingRoutedEventArgs>? HoldingStarted;
    
    /// <summary>
    /// 长按完成事件
    /// </summary>
    public event EventHandler<HoldingRoutedEventArgs>? HoldingCompleted;
    
    /// <summary>
    /// 长按取消事件
    /// </summary>
    public event EventHandler<HoldingRoutedEventArgs>? HoldingCanceled;
    
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (_trackedPointer != null)
            return;
        
        _trackedPointer = e.Pointer;
        _startPosition = e.Position;
        _startTime = DateTime.UtcNow;
        _holding = false;
        
        CapturePointer(e.Pointer);
    }
    
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (_trackedPointer != e.Pointer)
            return;
        
        var distance = (e.Position - _startPosition).Length;
        
        // 超出移动阈值，取消长按
        if (distance > MoveThreshold)
        {
            CancelHolding();
        }
    }
    
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (_trackedPointer != e.Pointer)
            return;
        
        if (_holding)
        {
            // 长按完成
            var args = new HoldingRoutedEventArgs(HoldingState.Completed, e.Pointer, e.Position);
            args.RoutedEvent = GestureEvents.HoldingEvent;
            Target?.RaiseEvent(args);
            
            HoldingCompleted?.Invoke(this, args);
        }
        
        Reset();
    }
    
    protected override void OnPointerCanceled(PointerEventArgs e)
    {
        if (_trackedPointer == e.Pointer)
        {
            CancelHolding();
            Reset();
        }
    }
    
    /// <summary>
    /// 更新状态 (需要定时调用)
    /// </summary>
    public void Update()
    {
        if (_trackedPointer == null || _holding)
            return;
        
        var elapsed = (DateTime.UtcNow - _startTime).TotalMilliseconds;
        
        if (elapsed >= HoldDuration)
        {
            _holding = true;
            
            var args = new HoldingRoutedEventArgs(HoldingState.Started, _trackedPointer, _startPosition);
            args.RoutedEvent = GestureEvents.HoldingEvent;
            Target?.RaiseEvent(args);
            
            HoldingStarted?.Invoke(this, args);
        }
    }
    
    private void CancelHolding()
    {
        if (!_holding && _trackedPointer != null)
        {
            var args = new HoldingRoutedEventArgs(HoldingState.Canceled, _trackedPointer, _startPosition);
            args.RoutedEvent = GestureEvents.HoldingEvent;
            Target?.RaiseEvent(args);
            
            HoldingCanceled?.Invoke(this, args);
        }
        
        Reset();
    }
    
    private void Reset()
    {
        if (_trackedPointer != null)
        {
            ReleasePointerCapture(_trackedPointer);
        }
        
        _trackedPointer = null;
        _holding = false;
    }
}