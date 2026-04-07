using Eclipse.Core;
using Eclipse.Core.Abstractions;
using Xunit;

namespace Eclipse.Tests;

/// <summary>
/// BuildContext 单元测试
/// </summary>
public class BuildContextTests
{
    [Fact]
    public void BeginComponent_ShouldCreateComponentAndAddToTree()
    {
        // Arrange
        var context = new BuildContext();
        var id = new ComponentId(1);

        // Act
        using (context.BeginComponent<TestComponent>(id, out var component))
        {
            component.Value = "Test";
        }

        // Assert
        Assert.NotNull(context.RootComponent);
        Assert.IsType<TestComponent>(context.RootComponent);
        Assert.Equal("Test", ((TestComponent)context.RootComponent).Value);
    }

    [Fact]
    public void BeginChildContent_ShouldAddChildrenToParent()
    {
        // Arrange
        var context = new BuildContext();

        // Act
        using (context.BeginComponent<TestContainer>(new ComponentId(1), out var container))
        {
            using (context.BeginChildContent())
            {
                using (context.BeginComponent<TestComponent>(new ComponentId(2), out var child1))
                {
                    child1.Value = "Child1";
                }
                using (context.BeginComponent<TestComponent>(new ComponentId(3), out var child2))
                {
                    child2.Value = "Child2";
                }
            }
        }

        // Assert
        var root = (TestContainer)context.RootComponent!;
        Assert.Equal(2, root.Children.Count);
    }

    [Fact]
    public void BeginComponent_WithoutChildContent_ShouldNotAddChildren()
    {
        // Arrange
        var context = new BuildContext();

        // Act - 不使用 BeginChildContent
        using (context.BeginComponent<TestContainer>(new ComponentId(1), out var container))
        {
            // 直接创建子组件，但不调用 BeginChildContent
            // 子组件会成为根组件而不是容器的子组件
        }

        // Assert
        // 由于没有调用 BeginChildContent，容器没有子组件
        var root = (TestContainer)context.RootComponent!;
        Assert.Empty(root.Children);
    }
}

/// <summary>
/// 测试用简单组件
/// </summary>
internal class TestComponent : ComponentBase
{
    public string? Value { get; set; }
    public override void Build(IBuildContext context) { }
}

/// <summary>
/// 测试用容器组件
/// </summary>
internal class TestContainer : ComponentBase
{
    public override void Build(IBuildContext context) { }
}