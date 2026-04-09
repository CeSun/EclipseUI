using Eclipse.Core;
using Eclipse.Input;
using Xunit;

namespace Eclipse.Tests;

/// <summary>
/// InputManager 鍗曞厓娴嬭瘯
/// </summary>
public class InputManagerTests
{
    /// <summary>
    /// 娴嬭瘯鐢ㄧ殑杈撳叆鍏冪礌
    /// </summary>
    private class TestInputElement : ComponentBase
    {
        private Rect _bounds = new Rect(0, 0, 100, 100);
        
        public override bool IsVisible => true;
        public override Rect Bounds => _bounds;
        
        public void SetBounds(Rect bounds) => _bounds = bounds;
        
        public override void Build(Eclipse.Core.Abstractions.IBuildContext context) { }
        
        public List<string> EventLog { get; } = new();
        
        public TestInputElement()
        {
            PointerPressed += (s, e) => EventLog.Add($"PointerPressed:{e.Position}");
            PointerReleased += (s, e) => EventLog.Add($"PointerReleased:{e.Position}");
            PointerMoved += (s, e) => EventLog.Add($"PointerMoved:{e.Position}");
            PointerEntered += (s, e) => EventLog.Add($"PointerEntered");
            PointerExited += (s, e) => EventLog.Add($"PointerExited");
            Tapped += (s, e) => EventLog.Add($"Tapped:{e.Position}");
            KeyDown += (s, e) => EventLog.Add($"KeyDown:{e.Key}");
            KeyUp += (s, e) => EventLog.Add($"KeyUp:{e.Key}");
            TextInput += (s, e) => EventLog.Add($"TextInput:{e.Text}");
        }
    }
    
    // === 鎸囬拡浜嬩欢娴嬭瘯 ===
    
    [Fact]
    public void ProcessPointerPressed_ShouldRaisePointerPressedEvent()
    {
        // Arrange
        var inputManager = new InputManager(new FocusManager());
        var element = new TestInputElement { IsHitTestVisible = true };
        inputManager.RootElement = element;
        
        var pointer = Pointer.GetOrCreate(0, PointerType.Mouse);
        var position = new Point(50, 50);
        var properties = new PointerPointProperties { IsLeftButtonPressed = true };
        
        // Act
        inputManager.ProcessPointerPressed(pointer, position, properties);
        
        // Assert
        Assert.Contains("PointerPressed:(50.0, 50.0)", element.EventLog);
    }
    
    [Fact]
    public void ProcessPointerMoved_ShouldRaisePointerMovedEvent()
    {
        // Arrange
        var inputManager = new InputManager(new FocusManager());
        var element = new TestInputElement { IsHitTestVisible = true };
        inputManager.RootElement = element;
        
        var pointer = Pointer.GetOrCreate(0, PointerType.Mouse);
        var position = new Point(50, 50);
        
        // Act
        inputManager.ProcessPointerMoved(pointer, position);
        
        // Assert
        Assert.Contains("PointerMoved:(50.0, 50.0)", element.EventLog);
    }
    
    [Fact]
    public void ProcessPointerReleased_ShouldRaisePointerReleasedEvent()
    {
        // Arrange
        var inputManager = new InputManager(new FocusManager());
        var element = new TestInputElement { IsHitTestVisible = true };
        inputManager.RootElement = element;
        
        var pointer = Pointer.GetOrCreate(0, PointerType.Mouse);
        var pressPosition = new Point(50, 50);
        var properties = new PointerPointProperties { IsLeftButtonPressed = true };
        
        // 鍏堟寜涓?
        inputManager.ProcessPointerPressed(pointer, pressPosition, properties);
        
        // Act - 閲婃斁
        inputManager.ProcessPointerReleased(pointer, pressPosition, PointerButtons.Left);
        
        // Assert
        Assert.Contains("PointerReleased:(50.0, 50.0)", element.EventLog);
    }
    
    [Fact]
    public void ProcessPointerPressed_ShouldRaiseTapped_WhenQuickRelease()
    {
        // Arrange
        var inputManager = new InputManager(new FocusManager());
        var element = new TestInputElement { IsHitTestVisible = true };
        inputManager.RootElement = element;
        
        var pointer = Pointer.GetOrCreate(0, PointerType.Mouse);
        var position = new Point(50, 50);
        var properties = new PointerPointProperties { IsLeftButtonPressed = true };
        
        // Act - 鎸変笅骞跺揩閫熼噴鏀?
        inputManager.ProcessPointerPressed(pointer, position, properties);
        inputManager.ProcessPointerReleased(pointer, position, PointerButtons.Left);
        
        // Assert - Tapped 搴旇瑙﹀彂
        Assert.Contains("Tapped:(50.0, 50.0)", element.EventLog);
    }
    
    [Fact]
    public void ProcessPointerWheel_ShouldRaisePointerWheelEvent()
    {
        // Arrange
        var inputManager = new InputManager(new FocusManager());
        var element = new TestInputElement { IsHitTestVisible = true };
        inputManager.RootElement = element;
        
        var pointer = Pointer.GetOrCreate(0, PointerType.Mouse);
        var position = new Point(50, 50);
        var delta = new Vector(0, 120);
        
        bool wheelEventRaised = false;
        inputManager.PointerWheel += (s, e) => wheelEventRaised = true;
        
        // Act
        inputManager.ProcessPointerWheel(pointer, position, delta);
        
        // Assert
        Assert.True(wheelEventRaised);
    }
    
    // === Hit Testing 娴嬭瘯 ===
    
    [Fact]
    public void HitTest_ShouldFindElementAtPosition()
    {
        // Arrange
        var inputManager = new InputManager(new FocusManager());
        var parent = new TestInputElement { IsHitTestVisible = true };
        parent.SetBounds(new Rect(0, 0, 200, 200));
        
        var child = new TestInputElement { IsHitTestVisible = true };
        child.SetBounds(new Rect(50, 50, 100, 100));
        parent.AddChild(child);
        
        inputManager.RootElement = parent;
        
        var pointer = Pointer.GetOrCreate(0, PointerType.Mouse);
        var position = new Point(75, 75); // 鍦?child 鍐?
        
        // Act
        inputManager.ProcessPointerPressed(pointer, position, new PointerPointProperties { IsLeftButtonPressed = true });
        
        // Assert - child 搴旇鏀跺埌浜嬩欢
        Assert.Contains("PointerPressed:(75.0, 75.0)", child.EventLog);
        Assert.DoesNotContain("PointerPressed", parent.EventLog);
    }
    
    [Fact]
    public void HitTest_ShouldReturnNull_WhenElementNotVisible()
    {
        // Arrange
        var inputManager = new InputManager(new FocusManager());
        var element = new TestInputElement();
        element.IsHitTestVisible = false;
        element.SetBounds(new Rect(0, 0, 100, 100));
        
        inputManager.RootElement = element;
        
        var pointer = Pointer.GetOrCreate(0, PointerType.Mouse);
        var position = new Point(50, 50);
        
        // Act
        inputManager.ProcessPointerPressed(pointer, position, new PointerPointProperties { IsLeftButtonPressed = true });
        
        // Assert - 娌℃湁浜嬩欢瑙﹀彂
        Assert.Empty(element.EventLog);
    }
    
    [Fact]
    public void HitTest_ShouldReturnNull_WhenPointOutsideBounds()
    {
        // Arrange
        var inputManager = new InputManager(new FocusManager());
        var element = new TestInputElement { IsHitTestVisible = true };
        element.SetBounds(new Rect(0, 0, 100, 100));
        
        inputManager.RootElement = element;
        
        var pointer = Pointer.GetOrCreate(0, PointerType.Mouse);
        var position = new Point(200, 200); // 鍦?bounds 澶?
        
        // Act
        inputManager.ProcessPointerPressed(pointer, position, new PointerPointProperties { IsLeftButtonPressed = true });
        
        // Assert - 娌℃湁浜嬩欢瑙﹀彂
        Assert.Empty(element.EventLog);
    }
    
    // === 鎸囬拡鎹曡幏娴嬭瘯 ===
    
    [Fact]
    public void PointerCapture_ShouldSendAllEventsToCapturedElement()
    {
        // Arrange
        var inputManager = new InputManager(new FocusManager());
        var element = new TestInputElement { IsHitTestVisible = true };
        element.SetBounds(new Rect(0, 0, 100, 100));
        
        inputManager.RootElement = element;
        
        var pointer = Pointer.GetOrCreate(0, PointerType.Mouse);
        var position = new Point(50, 50);
        
        // Act - 鎹曡幏鎸囬拡
        element.CapturePointer(pointer);
        
        // 绉诲姩鍒?bounds 澶?
        inputManager.ProcessPointerMoved(pointer, new Point(200, 200));
        
        // Assert - 浜嬩欢浠嶇劧鍙戦€佸埌鎹曡幏鐨勫厓绱?
        Assert.Contains("PointerMoved:(200.0, 200.0)", element.EventLog);
    }
    
    // === 鎮仠鐘舵€佹祴璇?===
    
    [Fact]
    public void ProcessPointerMoved_ShouldRaisePointerEntered_WhenEnteringElement()
    {
        // Arrange
        var inputManager = new InputManager(new FocusManager());
        var element = new TestInputElement { IsHitTestVisible = true };
        element.SetBounds(new Rect(0, 0, 100, 100));
        
        inputManager.RootElement = element;
        
        var pointer = Pointer.GetOrCreate(0, PointerType.Mouse);
        
        // Act - 浠庡閮ㄧЩ鍔ㄥ埌鍏冪礌鍐?
        inputManager.ProcessPointerMoved(pointer, new Point(-10, -10)); // 澶栭儴
        inputManager.ProcessPointerMoved(pointer, new Point(50, 50));   // 杩涘叆
        
        // Assert
        Assert.Contains("PointerEntered", element.EventLog);
    }
    
    [Fact]
    public void ProcessPointerMoved_ShouldRaisePointerExited_WhenLeavingElement()
    {
        // Arrange
        var inputManager = new InputManager(new FocusManager());
        var element = new TestInputElement { IsHitTestVisible = true };
        element.SetBounds(new Rect(0, 0, 100, 100));
        
        inputManager.RootElement = element;
        
        var pointer = Pointer.GetOrCreate(0, PointerType.Mouse);
        
        // Act - 鍏堣繘鍏ュ啀绂诲紑
        inputManager.ProcessPointerMoved(pointer, new Point(50, 50));   // 杩涘叆
        inputManager.ProcessPointerMoved(pointer, new Point(200, 200)); // 绂诲紑
        
        // Assert
        Assert.Contains("PointerEntered", element.EventLog);
        Assert.Contains("PointerExited", element.EventLog);
    }
    
    // === 閿洏浜嬩欢娴嬭瘯 ===
    
    [Fact]
    public void ProcessKeyDown_ShouldRaiseKeyDownEvent()
    {
        // Arrange
        var inputManager = new InputManager(new FocusManager());
        var element = new TestInputElement { IsFocusable = true };
        inputManager.RootElement = element;
        inputManager.FocusManager.SetFocus(element);
        
        // Act
        inputManager.ProcessKeyDown(Key.A, 65);
        
        // Assert
        Assert.Contains("KeyDown:A", element.EventLog);
    }
    
    [Fact]
    public void ProcessKeyUp_ShouldRaiseKeyUpEvent()
    {
        // Arrange
        var inputManager = new InputManager(new FocusManager());
        var element = new TestInputElement { IsFocusable = true };
        inputManager.RootElement = element;
        inputManager.FocusManager.SetFocus(element);
        
        // Act
        inputManager.ProcessKeyUp(Key.Enter, 13);
        
        // Assert
        Assert.Contains("KeyUp:Enter", element.EventLog);
    }
    
    [Fact]
    public void ProcessTextInput_ShouldRaiseTextInputEvent()
    {
        // Arrange
        var inputManager = new InputManager(new FocusManager());
        var element = new TestInputElement { IsFocusable = true };
        inputManager.RootElement = element;
        inputManager.FocusManager.SetFocus(element);
        
        // Act
        inputManager.ProcessTextInput("Hello");
        
        // Assert
        Assert.Contains("TextInput:Hello", element.EventLog);
    }
    
    [Fact]
    public void ProcessKeyDown_ShouldUseRootElement_WhenNoFocusedElement()
    {
        // Arrange
        var inputManager = new InputManager(new FocusManager());
        var element = new TestInputElement { IsFocusable = true };
        inputManager.RootElement = element;
        // 涓嶈缃劍鐐?
        
        // Act
        inputManager.ProcessKeyDown(Key.Space, 32);
        
        // Assert - 浜嬩欢搴旇鍙戦€佸埌 RootElement
        Assert.Contains("KeyDown:Space", element.EventLog);
    }
    
    // === 閿洏淇グ閿祴璇?===
    
    [Fact]
    public void ProcessKeyDown_ShouldIncludeModifiers()
    {
        // Arrange
        var inputManager = new InputManager(new FocusManager());
        var element = new TestInputElement { IsFocusable = true };
        inputManager.RootElement = element;
        inputManager.FocusManager.SetFocus(element);
        
        KeyEventArgs? receivedArgs = null;
        element.KeyDown += (s, e) => receivedArgs = e;
        
        // Act
        inputManager.ProcessKeyDown(Key.A, 65, KeyModifiers.Shift | KeyModifiers.Control);
        
        // Assert
        Assert.NotNull(receivedArgs);
        Assert.True(receivedArgs.HasModifier(KeyModifiers.Shift));
        Assert.True(receivedArgs.HasModifier(KeyModifiers.Control));
    }
    
    // === Touch 鎸囬拡娴嬭瘯 ===
    
    [Fact]
    public void TouchPointer_ShouldBeRemovedAfterRelease()
    {
        // Arrange
        var inputManager = new InputManager(new FocusManager());
        var element = new TestInputElement { IsHitTestVisible = true };
        inputManager.RootElement = element;
        
        var pointer = Pointer.GetOrCreate(1, PointerType.Touch);
        var position = new Point(50, 50);
        var properties = new PointerPointProperties { IsLeftButtonPressed = true };
        
        // Act
        inputManager.ProcessPointerPressed(pointer, position, properties);
        inputManager.ProcessPointerReleased(pointer, position, PointerButtons.Left);
        
        // Assert - 瑙︽懜鎸囬拡搴旇琚Щ闄?
        Assert.False(pointer.IsCaptured);
    }
    
    // === 浜嬩欢浼犳挱娴嬭瘯 ===
    
    [Fact]
    public void PreviewPointerPressed_ShouldRaiseBeforePointerPressed()
    {
        // Arrange
        var inputManager = new InputManager(new FocusManager());
        var element = new TestInputElement { IsHitTestVisible = true };
        inputManager.RootElement = element;
        
        var eventOrder = new List<string>();
        element.PreviewPointerPressed += (s, e) => eventOrder.Add("Preview");
        element.PointerPressed += (s, e) => eventOrder.Add("Bubble");
        
        var pointer = Pointer.GetOrCreate(0, PointerType.Mouse);
        var position = new Point(50, 50);
        
        // Act
        inputManager.ProcessPointerPressed(pointer, position, new PointerPointProperties { IsLeftButtonPressed = true });
        
        // Assert - Preview 搴旇鍏堣Е鍙?
        Assert.Equal(new[] { "Preview", "Bubble" }, eventOrder);
    }
    
    [Fact]
    public void PreviewEvent_ShouldStopPropagation_WhenHandled()
    {
        // Arrange
        var inputManager = new InputManager(new FocusManager());
        var element = new TestInputElement { IsHitTestVisible = true };
        inputManager.RootElement = element;
        
        var eventOrder = new List<string>();
        element.PreviewPointerPressed += (s, e) => 
        {
            eventOrder.Add("Preview");
            e.Handled = true;
        };
        element.PointerPressed += (s, e) => eventOrder.Add("Bubble");
        
        var pointer = Pointer.GetOrCreate(0, PointerType.Mouse);
        var position = new Point(50, 50);
        
        // Act
        inputManager.ProcessPointerPressed(pointer, position, new PointerPointProperties { IsLeftButtonPressed = true });
        
        // Assert - Bubble 搴旇琚樆姝?
        Assert.Equal(new[] { "Preview" }, eventOrder);
    }
}
