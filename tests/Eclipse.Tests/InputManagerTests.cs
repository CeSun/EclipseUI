using Eclipse.Input;
using Xunit;

namespace Eclipse.Tests;

/// <summary>
/// InputManager 单元测试
/// </summary>
public class InputManagerTests
{
    /// <summary>
    /// 测试用的输入元素
    /// </summary>
    private class TestInputElement : InputElementBase
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
    
    // === 指针事件测试 ===
    
    [Fact]
    public void ProcessPointerPressed_ShouldRaisePointerPressedEvent()
    {
        // Arrange
        var inputManager = new InputManager();
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
        var inputManager = new InputManager();
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
        var inputManager = new InputManager();
        var element = new TestInputElement { IsHitTestVisible = true };
        inputManager.RootElement = element;
        
        var pointer = Pointer.GetOrCreate(0, PointerType.Mouse);
        var pressPosition = new Point(50, 50);
        var properties = new PointerPointProperties { IsLeftButtonPressed = true };
        
        // 先按下
        inputManager.ProcessPointerPressed(pointer, pressPosition, properties);
        
        // Act - 释放
        inputManager.ProcessPointerReleased(pointer, pressPosition, PointerButtons.Left);
        
        // Assert
        Assert.Contains("PointerReleased:(50.0, 50.0)", element.EventLog);
    }
    
    [Fact]
    public void ProcessPointerPressed_ShouldRaiseTapped_WhenQuickRelease()
    {
        // Arrange
        var inputManager = new InputManager();
        var element = new TestInputElement { IsHitTestVisible = true };
        inputManager.RootElement = element;
        
        var pointer = Pointer.GetOrCreate(0, PointerType.Mouse);
        var position = new Point(50, 50);
        var properties = new PointerPointProperties { IsLeftButtonPressed = true };
        
        // Act - 按下并快速释放
        inputManager.ProcessPointerPressed(pointer, position, properties);
        inputManager.ProcessPointerReleased(pointer, position, PointerButtons.Left);
        
        // Assert - Tapped 应该触发
        Assert.Contains("Tapped:(50.0, 50.0)", element.EventLog);
    }
    
    [Fact]
    public void ProcessPointerWheel_ShouldRaisePointerWheelEvent()
    {
        // Arrange
        var inputManager = new InputManager();
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
    
    // === Hit Testing 测试 ===
    
    [Fact]
    public void HitTest_ShouldFindElementAtPosition()
    {
        // Arrange
        var inputManager = new InputManager();
        var parent = new TestInputElement { IsHitTestVisible = true };
        parent.SetBounds(new Rect(0, 0, 200, 200));
        
        var child = new TestInputElement { IsHitTestVisible = true };
        child.SetBounds(new Rect(50, 50, 100, 100));
        parent.AddChild(child);
        
        inputManager.RootElement = parent;
        
        var pointer = Pointer.GetOrCreate(0, PointerType.Mouse);
        var position = new Point(75, 75); // 在 child 内
        
        // Act
        inputManager.ProcessPointerPressed(pointer, position, new PointerPointProperties { IsLeftButtonPressed = true });
        
        // Assert - child 应该收到事件
        Assert.Contains("PointerPressed:(75.0, 75.0)", child.EventLog);
        Assert.DoesNotContain("PointerPressed", parent.EventLog);
    }
    
    [Fact]
    public void HitTest_ShouldReturnNull_WhenElementNotVisible()
    {
        // Arrange
        var inputManager = new InputManager();
        var element = new TestInputElement();
        element.IsHitTestVisible = false;
        element.SetBounds(new Rect(0, 0, 100, 100));
        
        inputManager.RootElement = element;
        
        var pointer = Pointer.GetOrCreate(0, PointerType.Mouse);
        var position = new Point(50, 50);
        
        // Act
        inputManager.ProcessPointerPressed(pointer, position, new PointerPointProperties { IsLeftButtonPressed = true });
        
        // Assert - 没有事件触发
        Assert.Empty(element.EventLog);
    }
    
    [Fact]
    public void HitTest_ShouldReturnNull_WhenPointOutsideBounds()
    {
        // Arrange
        var inputManager = new InputManager();
        var element = new TestInputElement { IsHitTestVisible = true };
        element.SetBounds(new Rect(0, 0, 100, 100));
        
        inputManager.RootElement = element;
        
        var pointer = Pointer.GetOrCreate(0, PointerType.Mouse);
        var position = new Point(200, 200); // 在 bounds 外
        
        // Act
        inputManager.ProcessPointerPressed(pointer, position, new PointerPointProperties { IsLeftButtonPressed = true });
        
        // Assert - 没有事件触发
        Assert.Empty(element.EventLog);
    }
    
    // === 指针捕获测试 ===
    
    [Fact]
    public void PointerCapture_ShouldSendAllEventsToCapturedElement()
    {
        // Arrange
        var inputManager = new InputManager();
        var element = new TestInputElement { IsHitTestVisible = true };
        element.SetBounds(new Rect(0, 0, 100, 100));
        
        inputManager.RootElement = element;
        
        var pointer = Pointer.GetOrCreate(0, PointerType.Mouse);
        var position = new Point(50, 50);
        
        // Act - 捕获指针
        element.CapturePointer(pointer);
        
        // 移动到 bounds 外
        inputManager.ProcessPointerMoved(pointer, new Point(200, 200));
        
        // Assert - 事件仍然发送到捕获的元素
        Assert.Contains("PointerMoved:(200.0, 200.0)", element.EventLog);
    }
    
    // === 悬停状态测试 ===
    
    [Fact]
    public void ProcessPointerMoved_ShouldRaisePointerEntered_WhenEnteringElement()
    {
        // Arrange
        var inputManager = new InputManager();
        var element = new TestInputElement { IsHitTestVisible = true };
        element.SetBounds(new Rect(0, 0, 100, 100));
        
        inputManager.RootElement = element;
        
        var pointer = Pointer.GetOrCreate(0, PointerType.Mouse);
        
        // Act - 从外部移动到元素内
        inputManager.ProcessPointerMoved(pointer, new Point(-10, -10)); // 外部
        inputManager.ProcessPointerMoved(pointer, new Point(50, 50));   // 进入
        
        // Assert
        Assert.Contains("PointerEntered", element.EventLog);
    }
    
    [Fact]
    public void ProcessPointerMoved_ShouldRaisePointerExited_WhenLeavingElement()
    {
        // Arrange
        var inputManager = new InputManager();
        var element = new TestInputElement { IsHitTestVisible = true };
        element.SetBounds(new Rect(0, 0, 100, 100));
        
        inputManager.RootElement = element;
        
        var pointer = Pointer.GetOrCreate(0, PointerType.Mouse);
        
        // Act - 先进入再离开
        inputManager.ProcessPointerMoved(pointer, new Point(50, 50));   // 进入
        inputManager.ProcessPointerMoved(pointer, new Point(200, 200)); // 离开
        
        // Assert
        Assert.Contains("PointerEntered", element.EventLog);
        Assert.Contains("PointerExited", element.EventLog);
    }
    
    // === 键盘事件测试 ===
    
    [Fact]
    public void ProcessKeyDown_ShouldRaiseKeyDownEvent()
    {
        // Arrange
        var inputManager = new InputManager();
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
        var inputManager = new InputManager();
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
        var inputManager = new InputManager();
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
        var inputManager = new InputManager();
        var element = new TestInputElement { IsFocusable = true };
        inputManager.RootElement = element;
        // 不设置焦点
        
        // Act
        inputManager.ProcessKeyDown(Key.Space, 32);
        
        // Assert - 事件应该发送到 RootElement
        Assert.Contains("KeyDown:Space", element.EventLog);
    }
    
    // === 键盘修饰键测试 ===
    
    [Fact]
    public void ProcessKeyDown_ShouldIncludeModifiers()
    {
        // Arrange
        var inputManager = new InputManager();
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
    
    // === Touch 指针测试 ===
    
    [Fact]
    public void TouchPointer_ShouldBeRemovedAfterRelease()
    {
        // Arrange
        var inputManager = new InputManager();
        var element = new TestInputElement { IsHitTestVisible = true };
        inputManager.RootElement = element;
        
        var pointer = Pointer.GetOrCreate(1, PointerType.Touch);
        var position = new Point(50, 50);
        var properties = new PointerPointProperties { IsLeftButtonPressed = true };
        
        // Act
        inputManager.ProcessPointerPressed(pointer, position, properties);
        inputManager.ProcessPointerReleased(pointer, position, PointerButtons.Left);
        
        // Assert - 触摸指针应该被移除
        Assert.False(pointer.IsCaptured);
    }
    
    // === 事件传播测试 ===
    
    [Fact]
    public void PreviewPointerPressed_ShouldRaiseBeforePointerPressed()
    {
        // Arrange
        var inputManager = new InputManager();
        var element = new TestInputElement { IsHitTestVisible = true };
        inputManager.RootElement = element;
        
        var eventOrder = new List<string>();
        element.PreviewPointerPressed += (s, e) => eventOrder.Add("Preview");
        element.PointerPressed += (s, e) => eventOrder.Add("Bubble");
        
        var pointer = Pointer.GetOrCreate(0, PointerType.Mouse);
        var position = new Point(50, 50);
        
        // Act
        inputManager.ProcessPointerPressed(pointer, position, new PointerPointProperties { IsLeftButtonPressed = true });
        
        // Assert - Preview 应该先触发
        Assert.Equal(new[] { "Preview", "Bubble" }, eventOrder);
    }
    
    [Fact]
    public void PreviewEvent_ShouldStopPropagation_WhenHandled()
    {
        // Arrange
        var inputManager = new InputManager();
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
        
        // Assert - Bubble 应该被阻止
        Assert.Equal(new[] { "Preview" }, eventOrder);
    }
}