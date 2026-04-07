using Eclipse.Core;
using Eclipse.Controls;
using Xunit;

namespace Eclipse.Tests;

/// <summary>
/// 附加属性测试
/// </summary>
public class AttachedPropertyTests
{
    // === AttachedProperty 基础测试 ===

    [Fact]
    public void AttachedProperty_Create_ShouldHaveCorrectNameAndDefaultValue()
    {
        // Arrange & Act
        var prop = new AttachedProperty<double>("Test.Value", 10.0);

        // Assert
        Assert.Equal("Test.Value", prop.Name);
        Assert.Equal(10.0, prop.DefaultValue);
    }

    [Fact]
    public void AttachedProperty_GetDefaultValue_ShouldReturnDefault()
    {
        // Arrange
        var prop = new AttachedProperty<int>("Grid.Row", 0);
        var component = new TestComponent();

        // Act
        var value = component.Get(prop);

        // Assert
        Assert.Equal(0, value);
    }

    [Fact]
    public void AttachedProperty_SetAndGet_ShouldWorkCorrectly()
    {
        // Arrange
        var prop = new AttachedProperty<int>("Grid.Row", 0);
        var component = new TestComponent();

        // Act
        component.Set(prop, 2);
        var value = component.Get(prop);

        // Assert
        Assert.Equal(2, value);
    }

    [Fact]
    public void AttachedProperty_SetMultipleProperties_ShouldStoreAll()
    {
        // Arrange
        var rowProp = new AttachedProperty<int>("Grid.Row", 0);
        var colProp = new AttachedProperty<int>("Grid.Column", 0);
        var rowSpanProp = new AttachedProperty<int>("Grid.RowSpan", 1);
        var component = new TestComponent();

        // Act
        component.Set(rowProp, 1);
        component.Set(colProp, 2);
        component.Set(rowSpanProp, 3);

        // Assert
        Assert.Equal(1, component.Get(rowProp));
        Assert.Equal(2, component.Get(colProp));
        Assert.Equal(3, component.Get(rowSpanProp));
    }

    // === Grid 附加属性测试 ===

    [Fact]
    public void Grid_Row_ShouldHaveCorrectNameAndDefault()
    {
        // Assert
        Assert.Equal("Grid.Row", Grid.Row.Name);
        Assert.Equal(0, Grid.Row.DefaultValue);
    }

    [Fact]
    public void Grid_Column_ShouldHaveCorrectNameAndDefault()
    {
        // Assert
        Assert.Equal("Grid.Column", Grid.Column.Name);
        Assert.Equal(0, Grid.Column.DefaultValue);
    }

    [Fact]
    public void Grid_RowSpan_ShouldHaveCorrectNameAndDefault()
    {
        // Assert
        Assert.Equal("Grid.RowSpan", Grid.RowSpan.Name);
        Assert.Equal(1, Grid.RowSpan.DefaultValue);
    }

    [Fact]
    public void Grid_ColumnSpan_ShouldHaveCorrectNameAndDefault()
    {
        // Assert
        Assert.Equal("Grid.ColumnSpan", Grid.ColumnSpan.Name);
        Assert.Equal(1, Grid.ColumnSpan.DefaultValue);
    }

    [Fact]
    public void Grid_SetAndGetRow_ShouldWork()
    {
        // Arrange
        var component = new TestComponent();

        // Act
        component.Set(Grid.Row, 3);

        // Assert
        Assert.Equal(3, component.Get(Grid.Row));
    }

    [Fact]
    public void Grid_SetAndGetColumn_ShouldWork()
    {
        // Arrange
        var component = new TestComponent();

        // Act
        component.Set(Grid.Column, 2);

        // Assert
        Assert.Equal(2, component.Get(Grid.Column));
    }

    // === Canvas 附加属性测试 ===

    [Fact]
    public void Canvas_Left_ShouldHaveCorrectNameAndDefault()
    {
        // Assert
        Assert.Equal("Canvas.Left", Canvas.Left.Name);
        Assert.Equal(0.0, Canvas.Left.DefaultValue);
    }

    [Fact]
    public void Canvas_Top_ShouldHaveCorrectNameAndDefault()
    {
        // Assert
        Assert.Equal("Canvas.Top", Canvas.Top.Name);
        Assert.Equal(0.0, Canvas.Top.DefaultValue);
    }

    [Fact]
    public void Canvas_Right_ShouldHaveCorrectNameAndDefault()
    {
        // Assert
        Assert.Equal("Canvas.Right", Canvas.Right.Name);
        Assert.Equal(0.0, Canvas.Right.DefaultValue);
    }

    [Fact]
    public void Canvas_Bottom_ShouldHaveCorrectNameAndDefault()
    {
        // Assert
        Assert.Equal("Canvas.Bottom", Canvas.Bottom.Name);
        Assert.Equal(0.0, Canvas.Bottom.DefaultValue);
    }

    [Fact]
    public void Canvas_ZIndex_ShouldHaveCorrectNameAndDefault()
    {
        // Assert
        Assert.Equal("Canvas.ZIndex", Canvas.ZIndex.Name);
        Assert.Equal(0, Canvas.ZIndex.DefaultValue);
    }

    [Fact]
    public void Canvas_SetAndGetPosition_ShouldWork()
    {
        // Arrange
        var component = new TestComponent();

        // Act
        component.Set(Canvas.Left, 50.0);
        component.Set(Canvas.Top, 100.0);

        // Assert
        Assert.Equal(50.0, component.Get(Canvas.Left));
        Assert.Equal(100.0, component.Get(Canvas.Top));
    }

    // === 多组件独立性测试 ===

    [Fact]
    public void AttachedProperty_DifferentComponents_ShouldHaveIndependentValues()
    {
        // Arrange
        var comp1 = new TestComponent();
        var comp2 = new TestComponent();

        // Act
        comp1.Set(Grid.Row, 1);
        comp2.Set(Grid.Row, 2);

        // Assert
        Assert.Equal(1, comp1.Get(Grid.Row));
        Assert.Equal(2, comp2.Get(Grid.Row));
    }

    [Fact]
    public void AttachedProperty_SetOnChild_ShouldNotAffectParent()
    {
        // Arrange
        var parent = new TestComponent();
        var child = new TestComponent();
        parent.AddChild(child);

        // Act
        parent.Set(Grid.Row, 0);
        child.Set(Grid.Row, 1);

        // Assert
        Assert.Equal(0, parent.Get(Grid.Row));
        Assert.Equal(1, child.Get(Grid.Row));
    }

    // === 边界值测试 ===

    [Fact]
    public void AttachedProperty_SetNegativeValue_ShouldWork()
    {
        // Arrange
        var component = new TestComponent();

        // Act - Canvas 支持负坐标
        component.Set(Canvas.Left, -10.0);

        // Assert
        Assert.Equal(-10.0, component.Get(Canvas.Left));
    }

    [Fact]
    public void AttachedProperty_SetLargeValue_ShouldWork()
    {
        // Arrange
        var component = new TestComponent();

        // Act
        component.Set(Grid.Row, 1000);

        // Assert
        Assert.Equal(1000, component.Get(Grid.Row));
    }

    // === 测试组件 ===

    internal class TestComponent : Eclipse.Core.ComponentBase
    {
        public override void Build(Eclipse.Core.Abstractions.IBuildContext context) { }
    }
}