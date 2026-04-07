using Eclipse.Core;
using Eclipse.Input;
using Xunit;
using InputPointer = Eclipse.Input.Pointer;

namespace Eclipse.Tests;

/// <summary>
/// EventRouter 单元测试
/// </summary>
public class EventRouterTests
{
    /// <summary>
    /// 测试用的输入元素
    /// </summary>
    private class TestInputElement : ComponentBase
    {
        private Rect _bounds = new Rect(0, 0, 100, 100);
        
        public override bool IsVisible => true;
        public override Rect Bounds => _bounds;
        
        public void SetBounds(Rect bounds) => _bounds = bounds;
        
        public override void Build(Eclipse.Core.Abstractions.IBuildContext context) { }
        
        public List<(string Event, object? Source)> EventLog { get; } = new();
        
        public TestInputElement(string name = "")
        {
            Name = name;
            
            PointerPressed += (s, e) => EventLog.Add(("PointerPressed", e.Source));
            PreviewPointerPressed += (s, e) => EventLog.Add(("PreviewPointerPressed", e.Source));
            PointerMoved += (s, e) => EventLog.Add(("PointerMoved", e.Source));
            PointerReleased += (s, e) => EventLog.Add(("PointerReleased", e.Source));
            KeyDown += (s, e) => EventLog.Add(("KeyDown", e.Source));
            KeyUp += (s, e) => EventLog.Add(("KeyUp", e.Source));
            PointerEntered += (s, e) => EventLog.Add(("PointerEntered", e.Source));
            PointerExited += (s, e) => EventLog.Add(("PointerExited", e.Source));
            Tapped += (s, e) => EventLog.Add(("Tapped", e.Source));
        }
        
        public string Name { get; set; } = "";
        
        public override string ToString() => Name;
    }
    
    // 辅助方法：使用反射设�?RoutedEvent
    private static void SetRoutedEvent(RoutedEventArgs args, RoutedEvent routedEvent)
    {
        var prop = typeof(RoutedEventArgs).GetProperty("RoutedEvent");
        prop?.SetValue(args, routedEvent);
    }
    
    // === Bubble 路由测试 ===
    
    [Fact]
    public void BubbleEvent_ShouldPropagateFromSourceToRoot()
    {
        // Arrange
        var root = new TestInputElement("Root") { IsHitTestVisible = true };
        var parent = new TestInputElement("Parent") { IsHitTestVisible = true };
        var child = new TestInputElement("Child") { IsHitTestVisible = true };
        
        root.AddChild(parent);
        parent.AddChild(child);
        
        var args = new PointerPressedEventArgs(InputPointer.Mouse, new Point(50, 50));
        SetRoutedEvent(args, ComponentBase.PointerPressedEvent);
        
        // Act
        child.RaiseEvent(args);
        
        // Assert - 事件应该�?Child -> Parent -> Root 传播
        Assert.Equal(3, child.EventLog.Count + parent.EventLog.Count + root.EventLog.Count);
        Assert.Contains(("PointerPressed", child), child.EventLog);
        Assert.Contains(("PointerPressed", parent), parent.EventLog);
        Assert.Contains(("PointerPressed", root), root.EventLog);
    }
    
    [Fact]
    public void BubbleEvent_ShouldStopPropagation_WhenHandled()
    {
        // Arrange
        var root = new TestInputElement("Root") { IsHitTestVisible = true };
        var parent = new TestInputElement("Parent") { IsHitTestVisible = true };
        var child = new TestInputElement("Child") { IsHitTestVisible = true };
        
        root.AddChild(parent);
        parent.AddChild(child);
        
        // Parent 处理事件并设�?Handled
        parent.PointerPressed += (s, e) => e.Handled = true;
        
        var args = new PointerPressedEventArgs(InputPointer.Mouse, new Point(50, 50));
        SetRoutedEvent(args, ComponentBase.PointerPressedEvent);
        
        // Act
        child.RaiseEvent(args);
        
        // Assert - 事件应该停止�?Parent
        Assert.Contains(("PointerPressed", child), child.EventLog);
        Assert.Contains(("PointerPressed", parent), parent.EventLog);
        Assert.DoesNotContain(("PointerPressed", root), root.EventLog);
    }
    
    // === Tunnel 路由测试 ===
    
    [Fact]
    public void TunnelEvent_ShouldPropagateFromRootToSource()
    {
        // Arrange
        var root = new TestInputElement("Root") { IsHitTestVisible = true };
        var parent = new TestInputElement("Parent") { IsHitTestVisible = true };
        var child = new TestInputElement("Child") { IsHitTestVisible = true };
        
        root.AddChild(parent);
        parent.AddChild(child);
        
        var args = new PointerPressedEventArgs(InputPointer.Mouse, new Point(50, 50));
        SetRoutedEvent(args, ComponentBase.PreviewPointerPressedEvent);
        
        // Act
        child.RaiseEvent(args);
        
        // Assert - 事件应该�?Root -> Parent -> Child 传播
        Assert.Contains(("PreviewPointerPressed", root), root.EventLog);
        Assert.Contains(("PreviewPointerPressed", parent), parent.EventLog);
        Assert.Contains(("PreviewPointerPressed", child), child.EventLog);
    }
    
    [Fact]
    public void TunnelEvent_ShouldStopPropagation_WhenHandled()
    {
        // Arrange
        var root = new TestInputElement("Root") { IsHitTestVisible = true };
        var parent = new TestInputElement("Parent") { IsHitTestVisible = true };
        var child = new TestInputElement("Child") { IsHitTestVisible = true };
        
        root.AddChild(parent);
        parent.AddChild(child);
        
        // Parent 处理 Preview 事件并设�?Handled
        parent.PreviewPointerPressed += (s, e) => e.Handled = true;
        
        var args = new PointerPressedEventArgs(InputPointer.Mouse, new Point(50, 50));
        SetRoutedEvent(args, ComponentBase.PreviewPointerPressedEvent);
        
        // Act
        child.RaiseEvent(args);
        
        // Assert - 事件应该停止�?Parent，不会到�?Child
        Assert.Contains(("PreviewPointerPressed", root), root.EventLog);
        Assert.Contains(("PreviewPointerPressed", parent), parent.EventLog);
        Assert.DoesNotContain(("PreviewPointerPressed", child), child.EventLog);
    }
    
    // === Direct 路由测试 ===
    
    [Fact]
    public void DirectEvent_ShouldOnlyRaiseOnSourceElement()
    {
        // Arrange
        var root = new TestInputElement("Root") { IsHitTestVisible = true };
        var parent = new TestInputElement("Parent") { IsHitTestVisible = true };
        var child = new TestInputElement("Child") { IsHitTestVisible = true };
        
        root.AddChild(parent);
        parent.AddChild(child);
        
        var args = new PointerEventArgs(InputPointer.Mouse, new Point(50, 50));
        SetRoutedEvent(args, ComponentBase.PointerEnteredEvent);
        
        // Act
        child.RaiseEvent(args);
        
        // Assert - 只有 Child 收到事件
        Assert.Contains(("PointerEntered", child), child.EventLog);
        Assert.DoesNotContain(("PointerEntered", parent), parent.EventLog);
        Assert.DoesNotContain(("PointerEntered", root), root.EventLog);
    }
    
    // === RoutedEvent 注册测试 ===
    
    [Fact]
    public void RoutedEvent_ShouldHaveCorrectProperties()
    {
        // Assert
        Assert.Equal("PointerPressed", ComponentBase.PointerPressedEvent.Name);
        Assert.Equal(RoutingStrategy.Bubble, ComponentBase.PointerPressedEvent.RoutingStrategy);
        Assert.Equal(typeof(ComponentBase), ComponentBase.PointerPressedEvent.OwnerType);
    }
    
    [Fact]
    public void RoutedEvent_TunnelEvent_ShouldHaveCorrectStrategy()
    {
        // Assert
        Assert.Equal(RoutingStrategy.Tunnel, ComponentBase.PreviewPointerPressedEvent.RoutingStrategy);
    }
    
    [Fact]
    public void RoutedEvent_DirectEvent_ShouldHaveCorrectStrategy()
    {
        // Assert
        Assert.Equal(RoutingStrategy.Direct, ComponentBase.PointerEnteredEvent.RoutingStrategy);
    }
    
    // === Source �?OriginalSource 测试 ===
    
    [Fact]
    public void RoutedEventArgs_ShouldSetOriginalSource()
    {
        // Arrange
        var child = new TestInputElement("Child") { IsHitTestVisible = true };
        
        RoutedEventArgs? receivedArgs = null;
        child.PointerPressed += (s, e) => receivedArgs = e;
        
        var args = new PointerPressedEventArgs(InputPointer.Mouse, new Point(50, 50));
        SetRoutedEvent(args, ComponentBase.PointerPressedEvent);
        
        // Act
        child.RaiseEvent(args);
        
        // Assert
        Assert.NotNull(receivedArgs);
        Assert.Equal(child, receivedArgs.OriginalSource);
    }
    
    // === 键盘事件路由测试 ===
    
    [Fact]
    public void KeyDown_ShouldRouteAsBubble()
    {
        // Arrange
        var root = new TestInputElement("Root") { IsHitTestVisible = true };
        var child = new TestInputElement("Child") { IsHitTestVisible = true };
        
        root.AddChild(child);
        
        var args = new KeyEventArgs(Key.Enter, 13);
        SetRoutedEvent(args, ComponentBase.KeyDownEvent);
        
        // Act
        child.RaiseEvent(args);
        
        // Assert - KeyDown �?Bubble 事件
        Assert.Contains(("KeyDown", child), child.EventLog);
        Assert.Contains(("KeyDown", root), root.EventLog);
    }
    
    // === 添加/移除处理器测�?===
    
    [Fact]
    public void AddHandler_ShouldReceiveEvent()
    {
        // Arrange
        var element = new TestInputElement { IsHitTestVisible = true };
        
        int callCount = 0;
        element.AddHandler(ComponentBase.PointerPressedEvent, new EventHandler<PointerPressedEventArgs>((s, e) => callCount++));
        
        var args = new PointerPressedEventArgs(InputPointer.Mouse, new Point(50, 50));
        SetRoutedEvent(args, ComponentBase.PointerPressedEvent);
        
        // Act
        element.RaiseEvent(args);
        
        // Assert
        Assert.Equal(1, callCount);
    }
    
    [Fact]
    public void RemoveHandler_ShouldNotReceiveEvent()
    {
        // Arrange
        var element = new TestInputElement { IsHitTestVisible = true };
        
        int callCount = 0;
        var handler = new EventHandler<PointerPressedEventArgs>((s, e) => callCount++);
        
        element.AddHandler(ComponentBase.PointerPressedEvent, handler);
        element.RemoveHandler(ComponentBase.PointerPressedEvent, handler);
        
        var args = new PointerPressedEventArgs(InputPointer.Mouse, new Point(50, 50));
        SetRoutedEvent(args, ComponentBase.PointerPressedEvent);
        
        // Act
        element.RaiseEvent(args);
        
        // Assert
        Assert.Equal(0, callCount);
    }
    
    [Fact]
    public void MultipleHandlers_ShouldAllReceiveEvent()
    {
        // Arrange
        var element = new TestInputElement { IsHitTestVisible = true };
        
        var calls = new List<int>();
        element.AddHandler(ComponentBase.PointerPressedEvent, new EventHandler<PointerPressedEventArgs>((s, e) => calls.Add(1)));
        element.AddHandler(ComponentBase.PointerPressedEvent, new EventHandler<PointerPressedEventArgs>((s, e) => calls.Add(2)));
        element.AddHandler(ComponentBase.PointerPressedEvent, new EventHandler<PointerPressedEventArgs>((s, e) => calls.Add(3)));
        
        var args = new PointerPressedEventArgs(InputPointer.Mouse, new Point(50, 50));
        SetRoutedEvent(args, ComponentBase.PointerPressedEvent);
        
        // Act
        element.RaiseEvent(args);
        
        // Assert
        Assert.Equal(new[] { 1, 2, 3 }, calls);
    }
    
    [Fact]
    public void MultipleHandlers_ShouldStopWhenHandled()
    {
        // Arrange
        var element = new TestInputElement { IsHitTestVisible = true };
        
        var calls = new List<int>();
        element.AddHandler(ComponentBase.PointerPressedEvent, new EventHandler<PointerPressedEventArgs>((s, e) => 
        {
            calls.Add(1);
            e.Handled = true;
        }));
        element.AddHandler(ComponentBase.PointerPressedEvent, new EventHandler<PointerPressedEventArgs>((s, e) => calls.Add(2)));
        
        var args = new PointerPressedEventArgs(InputPointer.Mouse, new Point(50, 50));
        SetRoutedEvent(args, ComponentBase.PointerPressedEvent);
        
        // Act
        element.RaiseEvent(args);
        
        // Assert - 第二个处理器不应该被调用
        Assert.Equal(new[] { 1 }, calls);
    }
}