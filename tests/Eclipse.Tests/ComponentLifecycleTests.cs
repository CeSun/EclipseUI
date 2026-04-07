using Eclipse.Core;
using Eclipse.Core.Abstractions;
using Eclipse.Input;
using Xunit;

namespace Eclipse.Tests;

/// <summary>
/// 组件生命周期测试
/// </summary>
public class ComponentLifecycleTests
{
    // === 初始化和挂载测试 ===

    [Fact]
    public void ComponentBase_OnInitialized_ShouldBeCalledOnce()
    {
        // Arrange
        var component = new LifecycleTestComponent();

        // Act
        component.OnInitialized();
        component.OnInitialized(); // 再次调用

        // Assert
        Assert.Equal(1, component.InitializeCount);
    }

    [Fact]
    public void ComponentBase_OnMounted_ShouldBeCalledOnce()
    {
        // Arrange
        var component = new LifecycleTestComponent();

        // Act
        component.OnMounted();
        component.OnMounted(); // 再次调用

        // Assert
        Assert.Equal(1, component.MountCount);
    }

    [Fact]
    public void ComponentBase_OnUnmounted_ShouldResetMountFlag()
    {
        // Arrange
        var component = new LifecycleTestComponent();
        component.OnMounted();

        // Act
        component.OnUnmounted();

        // Assert
        Assert.Equal(1, component.UnmountCount);
    }

    // === 状态变更测试 ===

    [Fact]
    public void ComponentBase_MarkDirty_ShouldSetIsDirty()
    {
        // Arrange
        var component = new LifecycleTestComponent();
        component.ClearDirty(); // 先清除脏标记

        // Act
        component.MarkDirty();

        // Assert
        Assert.True(component.IsDirty);
    }

    [Fact]
    public void ComponentBase_ClearDirty_ShouldClearIsDirty()
    {
        // Arrange
        var component = new LifecycleTestComponent();
        component.MarkDirty();

        // Act
        component.ClearDirty();

        // Assert
        Assert.False(component.IsDirty);
    }

    [Fact]
    public void ComponentBase_StateHasChanged_ShouldMarkDirtyAndRaiseEvent()
    {
        // Arrange
        var component = new LifecycleTestComponent();
        var eventRaised = false;
        component.StateChanged += (s, e) => eventRaised = true;

        // Act
        component.TriggerStateHasChanged();

        // Assert
        Assert.True(component.IsDirty);
        Assert.True(eventRaised);
    }

    // === 子组件管理测试 ===

    [Fact]
    public void ComponentBase_AddChild_ShouldSetParentAndAddToList()
    {
        // Arrange
        var parent = new LifecycleTestComponent();
        var child = new LifecycleTestComponent();

        // Act
        parent.AddChild(child);

        // Assert
        Assert.Single(parent.Children);
        Assert.Equal(parent, child.Parent);
    }

    [Fact]
    public void ComponentBase_RemoveChild_ShouldClearParentAndRemoveFromList()
    {
        // Arrange
        var parent = new LifecycleTestComponent();
        var child = new LifecycleTestComponent();
        parent.AddChild(child);

        // Act
        parent.RemoveChild(child);

        // Assert
        Assert.Empty(parent.Children);
        Assert.Null(child.Parent);
    }

    [Fact]
    public void ComponentBase_ClearChildren_ShouldRemoveAll()
    {
        // Arrange
        var parent = new LifecycleTestComponent();
        parent.AddChild(new LifecycleTestComponent());
        parent.AddChild(new LifecycleTestComponent());
        parent.AddChild(new LifecycleTestComponent());

        // Act
        parent.ClearChildren();

        // Assert
        Assert.Empty(parent.Children);
    }

    // === Dispose 测试 ===

    [Fact]
    public void ComponentBase_Dispose_ShouldCallOnUnmountedAndClearChildren()
    {
        // Arrange
        var parent = new LifecycleTestComponent();
        var child = new LifecycleTestComponent();
        parent.AddChild(child);

        // Act
        parent.Dispose();

        // Assert
        Assert.True(parent.IsDisposed);
        Assert.Equal(1, parent.UnmountCount);
        Assert.Empty(parent.Children);
    }

    [Fact]
    public void ComponentBase_Dispose_ShouldNotExecuteTwice()
    {
        // Arrange
        var component = new LifecycleTestComponent();

        // Act
        component.Dispose();
        component.Dispose(); // 再次调用

        // Assert
        Assert.True(component.IsDisposed);
        Assert.Equal(1, component.UnmountCount); // 只调用一次
    }

    // === Rebuild 测试 ===

    [Fact]
    public void ComponentBase_Rebuild_ShouldOnlyExecuteWhenDirty()
    {
        // Arrange
        var component = new RebuildTestComponent();
        component.ClearDirty();

        // Act
        component.Rebuild();

        // Assert
        Assert.Equal(0, component.BuildCount); // 未执行，因为不是脏的
    }

    [Fact]
    public void ComponentBase_Rebuild_ShouldExecuteWhenDirty()
    {
        // Arrange
        var component = new RebuildTestComponent();
        component.MarkDirty();

        // Act
        component.Rebuild();

        // Assert
        Assert.Equal(1, component.BuildCount);
        Assert.False(component.IsDirty); // 清除脏标记
    }

    [Fact]
    public void ComponentBase_ForceRebuild_ShouldExecuteRegardlessOfDirty()
    {
        // Arrange
        var component = new RebuildTestComponent();
        component.ClearDirty();

        // Act
        component.ForceRebuild();

        // Assert
        Assert.Equal(1, component.BuildCount); // 强制执行
    }

    // === UpdateBounds 测试 ===

    [Fact]
    public void ComponentBase_UpdateBounds_ShouldUpdateBoundsProperty()
    {
        // Arrange
        var component = new LifecycleTestComponent();
        var newBounds = new Rect(10, 20, 100, 50);

        // Act
        component.UpdateBounds(newBounds);

        // Assert
        Assert.Equal(newBounds, component.Bounds);
    }

    // === 状态冒泡测试 ===

    [Fact]
    public void ComponentBase_StateChanged_ShouldBubbleToParent()
    {
        // Arrange
        var parent = new LifecycleTestComponent();
        var child = new LifecycleTestComponent();
        parent.AddChild(child);
        
        var parentEventRaised = false;
        parent.StateChanged += (s, e) => parentEventRaised = true;

        // Act
        child.TriggerStateHasChanged();

        // Assert
        Assert.True(child.IsDirty);
        Assert.True(parentEventRaised); // 事件冒泡到父组件
    }

    // === 测试组件 ===

    internal class LifecycleTestComponent : ComponentBase
    {
        public int InitializeCount { get; private set; }
        public int MountCount { get; private set; }
        public int UnmountCount { get; private set; }
        public bool IsDisposed { get; private set; }

        public override void OnInitialized()
        {
            // 先检查是否已初始化，再调用基类
            if (!_isInitializedTest)
            {
                base.OnInitialized();
                InitializeCount++;
                _isInitializedTest = true;
            }
        }

        public override void OnMounted()
        {
            // 先检查是否已挂载，再调用基类
            if (!_isMountedTest)
            {
                base.OnMounted();
                MountCount++;
                _isMountedTest = true;
            }
        }

        public override void OnUnmounted()
        {
            base.OnUnmounted();
            UnmountCount++;
            IsDisposed = true;
            _isMountedTest = false;
        }

        private bool _isInitializedTest;
        private bool _isMountedTest;

        public void TriggerStateHasChanged() => StateHasChanged();

        public override void Build(IBuildContext context) { }
    }

    internal class RebuildTestComponent : ComponentBase
    {
        public int BuildCount { get; private set; }

        public override void Build(IBuildContext context)
        {
            BuildCount++;
        }
    }
}