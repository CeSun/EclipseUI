using Eclipse.Input;
using Xunit;

namespace Eclipse.Tests;

/// <summary>
/// FocusManager 单元测试
/// </summary>
public class FocusManagerTests
{
    /// <summary>
    /// 测试用的输入元素
    /// </summary>
    private class TestInputElement : InputElementBase
    {
        private Rect _bounds = new Rect(0, 0, 100, 100);
        private bool _isVisible = true;
        
        public override bool IsVisible => _isVisible;
        public override Rect Bounds => _bounds;
        
        public void SetBounds(Rect bounds) => _bounds = bounds;
        public void SetVisible(bool visible) => _isVisible = visible;
        
        public override void Build(Eclipse.Core.Abstractions.IBuildContext context) { }
        
        public List<string> FocusLog { get; } = new();
        
        public TestInputElement()
        {
            // 记录焦点变化
        }
        
        protected override void OnGotFocus()
        {
            base.OnGotFocus();
            FocusLog.Add("GotFocus");
        }
        
        protected override void OnLostFocus()
        {
            base.OnLostFocus();
            FocusLog.Add("LostFocus");
        }
    }
    
    // === 基础焦点测试 ===
    
    [Fact]
    public void SetFocus_ShouldSetFocusedElement()
    {
        // Arrange
        var focusManager = new FocusManager();
        var element = new TestInputElement { IsFocusable = true };
        
        // Act
        var result = focusManager.SetFocus(element);
        
        // Assert
        Assert.True(result);
        Assert.Equal(element, focusManager.FocusedElement);
        Assert.True(element.IsFocused);
    }
    
    [Fact]
    public void SetFocus_ShouldReturnFalse_WhenElementNotFocusable()
    {
        // Arrange
        var focusManager = new FocusManager();
        var element = new TestInputElement { IsFocusable = false };
        
        // Act
        var result = focusManager.SetFocus(element);
        
        // Assert
        Assert.False(result);
        Assert.Null(focusManager.FocusedElement);
    }
    
    [Fact]
    public void SetFocus_ShouldReturnFalse_WhenElementNotVisible()
    {
        // Arrange
        var focusManager = new FocusManager();
        var element = new TestInputElement { IsFocusable = true };
        element.SetVisible(false);
        
        // Act
        var result = focusManager.SetFocus(element);
        
        // Assert
        Assert.False(result);
        Assert.Null(focusManager.FocusedElement);
    }
    
    [Fact]
    public void SetFocus_ShouldReturnTrue_WhenSettingSameElement()
    {
        // Arrange
        var focusManager = new FocusManager();
        var element = new TestInputElement { IsFocusable = true };
        focusManager.SetFocus(element);
        
        // Act - 再次设置相同元素
        var result = focusManager.SetFocus(element);
        
        // Assert
        Assert.True(result);
        Assert.Single(element.FocusLog); // 只触发一次 GotFocus
    }
    
    // === 焦点切换测试 ===
    
    [Fact]
    public void SetFocus_ShouldRemoveFocusFromPreviousElement()
    {
        // Arrange
        var focusManager = new FocusManager();
        var element1 = new TestInputElement { IsFocusable = true };
        var element2 = new TestInputElement { IsFocusable = true };
        
        focusManager.SetFocus(element1);
        
        // Act - 切换焦点
        focusManager.SetFocus(element2);
        
        // Assert
        Assert.False(element1.IsFocused);
        Assert.True(element2.IsFocused);
        Assert.Contains("LostFocus", element1.FocusLog);
        Assert.Contains("GotFocus", element2.FocusLog);
    }
    
    [Fact]
    public void ClearFocus_ShouldRemoveFocusFromCurrentElement()
    {
        // Arrange
        var focusManager = new FocusManager();
        var element = new TestInputElement { IsFocusable = true };
        focusManager.SetFocus(element);
        
        // Act
        focusManager.ClearFocus();
        
        // Assert
        Assert.Null(focusManager.FocusedElement);
        Assert.False(element.IsFocused);
        Assert.Contains("LostFocus", element.FocusLog);
    }
    
    // === 焦点事件测试 ===
    
    [Fact]
    public void FocusChanged_ShouldRaiseEvent()
    {
        // Arrange
        var focusManager = new FocusManager();
        var element = new TestInputElement { IsFocusable = true };
        
        FocusChangedEventArgs? eventArgs = null;
        focusManager.FocusChanged += (s, e) => eventArgs = e;
        
        // Act
        focusManager.SetFocus(element);
        
        // Assert
        Assert.NotNull(eventArgs);
        Assert.Null(eventArgs.OldFocus);
        Assert.Equal(element, eventArgs.NewFocus);
    }
    
    [Fact]
    public void FocusChanged_ShouldIncludeOldAndNewFocus()
    {
        // Arrange
        var focusManager = new FocusManager();
        var element1 = new TestInputElement { IsFocusable = true };
        var element2 = new TestInputElement { IsFocusable = true };
        
        focusManager.SetFocus(element1);
        
        FocusChangedEventArgs? eventArgs = null;
        focusManager.FocusChanged += (s, e) => eventArgs = e;
        
        // Act
        focusManager.SetFocus(element2);
        
        // Assert
        Assert.NotNull(eventArgs);
        Assert.Equal(element1, eventArgs.OldFocus);
        Assert.Equal(element2, eventArgs.NewFocus);
    }
    
    // === 焦点范围测试 ===
    
    [Fact]
    public void SetFocusScope_ShouldSetScope()
    {
        // Arrange
        var focusManager = new FocusManager();
        var scope = new TestInputElement { IsFocusable = true };
        
        // Act
        focusManager.SetFocusScope(scope);
        
        // Assert
        Assert.Equal(scope, focusManager.FocusScope);
    }
    
    [Fact]
    public void GetFirstFocusable_ShouldReturnFirstFocusableElement()
    {
        // Arrange
        var focusManager = new FocusManager();
        var parent = new TestInputElement { IsFocusable = false };
        var child1 = new TestInputElement { IsFocusable = false };
        var child2 = new TestInputElement { IsFocusable = true };
        
        parent.AddChild(child1);
        parent.AddChild(child2);
        
        // Act
        var result = focusManager.GetFirstFocusable(parent);
        
        // Assert
        Assert.Equal(child2, result);
    }
    
    [Fact]
    public void GetFirstFocusable_ShouldReturnNull_WhenNoFocusableElement()
    {
        // Arrange
        var focusManager = new FocusManager();
        var parent = new TestInputElement { IsFocusable = false };
        var child1 = new TestInputElement { IsFocusable = false };
        var child2 = new TestInputElement { IsFocusable = false };
        
        parent.AddChild(child1);
        parent.AddChild(child2);
        
        // Act
        var result = focusManager.GetFirstFocusable(parent);
        
        // Assert
        Assert.Null(result);
    }
    
    // === 焦点移动测试 ===
    
    [Fact]
    public void MoveFocusForward_ShouldMoveToNextFocusableElement()
    {
        // Arrange
        var focusManager = new FocusManager();
        var parent = new TestInputElement { IsFocusable = true };
        var child1 = new TestInputElement { IsFocusable = true };
        var child2 = new TestInputElement { IsFocusable = true };
        
        parent.AddChild(child1);
        parent.AddChild(child2);
        
        focusManager.SetFocusScope(parent);
        focusManager.SetFocus(parent);
        
        // Act
        focusManager.MoveFocusForward();
        
        // Assert
        Assert.Equal(child1, focusManager.FocusedElement);
    }
    
    [Fact]
    public void MoveFocusBackward_ShouldMoveToPreviousFocusableElement()
    {
        // Arrange
        var focusManager = new FocusManager();
        var parent = new TestInputElement { IsFocusable = true };
        var child1 = new TestInputElement { IsFocusable = true };
        var child2 = new TestInputElement { IsFocusable = true };
        
        parent.AddChild(child1);
        parent.AddChild(child2);
        
        focusManager.SetFocusScope(parent);
        focusManager.SetFocus(child2);
        
        // Act
        focusManager.MoveFocusBackward();
        
        // Assert
        Assert.Equal(child1, focusManager.FocusedElement);
    }
    
    [Fact]
    public void MoveFocusForward_ShouldReturnFalse_WhenNoFocusedElement()
    {
        // Arrange
        var focusManager = new FocusManager();
        
        // Act
        var result = focusManager.MoveFocusForward();
        
        // Assert
        Assert.False(result);
    }
    
    [Fact]
    public void MoveFocusForward_ShouldWrapAround_WhenAtEnd()
    {
        // Arrange
        var focusManager = new FocusManager();
        var parent = new TestInputElement { IsFocusable = true };
        var child1 = new TestInputElement { IsFocusable = true };
        
        parent.AddChild(child1);
        
        focusManager.SetFocusScope(parent);
        focusManager.SetFocus(child1);
        
        // Act - 移动到下一个（应该回到 parent）
        focusManager.MoveFocusForward();
        
        // Assert
        Assert.Equal(parent, focusManager.FocusedElement);
    }
    
    // === Element.Focus() 测试 ===
    
    [Fact]
    public void ElementFocus_ShouldReturnTrue_WhenFocusable()
    {
        // Arrange
        var element = new TestInputElement { IsFocusable = true };
        
        // Act
        var result = element.Focus();
        
        // Assert
        Assert.True(result);
        Assert.True(element.IsFocused);
        Assert.Contains("GotFocus", element.FocusLog);
    }
    
    [Fact]
    public void ElementFocus_ShouldReturnFalse_WhenNotFocusable()
    {
        // Arrange
        var element = new TestInputElement { IsFocusable = false };
        
        // Act
        var result = element.Focus();
        
        // Assert
        Assert.False(result);
        Assert.False(element.IsFocused);
    }
    
    [Fact]
    public void ElementFocus_ShouldReturnFalse_WhenNotVisible()
    {
        // Arrange
        var element = new TestInputElement { IsFocusable = true };
        element.SetVisible(false);
        
        // Act
        var result = element.Focus();
        
        // Assert
        Assert.False(result);
    }
}