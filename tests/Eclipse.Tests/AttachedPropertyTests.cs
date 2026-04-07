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
        Assert.Equal("Grid.Row", Grid.RowProperty.Name);
        Assert.Equal(0, Grid.RowProperty.DefaultValue);
    }

    [Fact]
    public void Grid_Column_ShouldHaveCorrectNameAndDefault()
    {
        // Assert
        Assert.Equal("Grid.Column", Grid.ColumnProperty.Name);
        Assert.Equal(0, Grid.ColumnProperty.DefaultValue);
    }

    [Fact]
    public void Grid_RowSpan_ShouldHaveCorrectNameAndDefault()
    {
        // Assert
        Assert.Equal("Grid.RowSpan", Grid.RowSpanProperty.Name);
        Assert.Equal(1, Grid.RowSpanProperty.DefaultValue);
    }

    [Fact]
    public void Grid_ColumnSpan_ShouldHaveCorrectNameAndDefault()
    {
        // Assert
        Assert.Equal("Grid.ColumnSpan", Grid.ColumnSpanProperty.Name);
        Assert.Equal(1, Grid.ColumnSpanProperty.DefaultValue);
    }

    [Fact]
    public void Grid_SetAndGetRow_ShouldWork()
    {
        // Arrange
        var component = new TestComponent();

        // Act
        component.Set(Grid.RowProperty, 3);

        // Assert
        Assert.Equal(3, component.Get(Grid.RowProperty));
    }

    [Fact]
    public void Grid_SetAndGetColumn_ShouldWork()
    {
        // Arrange
        var component = new TestComponent();

        // Act
        component.Set(Grid.ColumnProperty, 2);

        // Assert
        Assert.Equal(2, component.Get(Grid.ColumnProperty));
    }
    
    [Fact]
    public void Grid_GetSetHelpers_ShouldWork()
    {
        // Arrange
        var component = new TestComponent();

        // Act
        Grid.SetRow(component, 3);
        Grid.SetColumn(component, 2);

        // Assert
        Assert.Equal(3, Grid.GetRow(component));
        Assert.Equal(2, Grid.GetColumn(component));
    }

    // === Canvas 附加属性测试 ===

    [Fact]
    public void Canvas_Left_ShouldHaveCorrectNameAndDefault()
    {
        // Assert
        Assert.Equal("Canvas.Left", Canvas.LeftProperty.Name);
        Assert.Equal(0.0, Canvas.LeftProperty.DefaultValue);
    }

    [Fact]
    public void Canvas_Top_ShouldHaveCorrectNameAndDefault()
    {
        // Assert
        Assert.Equal("Canvas.Top", Canvas.TopProperty.Name);
        Assert.Equal(0.0, Canvas.TopProperty.DefaultValue);
    }

    [Fact]
    public void Canvas_Right_ShouldHaveCorrectNameAndDefault()
    {
        // Assert
        Assert.Equal("Canvas.Right", Canvas.RightProperty.Name);
        Assert.Equal(0.0, Canvas.RightProperty.DefaultValue);
    }

    [Fact]
    public void Canvas_Bottom_ShouldHaveCorrectNameAndDefault()
    {
        // Assert
        Assert.Equal("Canvas.Bottom", Canvas.BottomProperty.Name);
        Assert.Equal(0.0, Canvas.BottomProperty.DefaultValue);
    }

    [Fact]
    public void Canvas_ZIndex_ShouldHaveCorrectNameAndDefault()
    {
        // Assert
        Assert.Equal("Canvas.ZIndex", Canvas.ZIndexProperty.Name);
        Assert.Equal(0, Canvas.ZIndexProperty.DefaultValue);
    }

    [Fact]
    public void Canvas_SetAndGetPosition_ShouldWork()
    {
        // Arrange
        var component = new TestComponent();

        // Act
        component.Set(Canvas.LeftProperty, 50.0);
        component.Set(Canvas.TopProperty, 100.0);

        // Assert
        Assert.Equal(50.0, component.Get(Canvas.LeftProperty));
        Assert.Equal(100.0, component.Get(Canvas.TopProperty));
    }
    
    [Fact]
    public void Canvas_GetSetHelpers_ShouldWork()
    {
        // Arrange
        var component = new TestComponent();

        // Act
        Canvas.SetLeft(component, 50.0);
        Canvas.SetTop(component, 100.0);

        // Assert
        Assert.Equal(50.0, Canvas.GetLeft(component));
        Assert.Equal(100.0, Canvas.GetTop(component));
    }

    // === 多组件独立性测试 ===

    [Fact]
    public void AttachedProperty_DifferentComponents_ShouldHaveIndependentValues()
    {
        // Arrange
        var comp1 = new TestComponent();
        var comp2 = new TestComponent();

        // Act
        comp1.Set(Grid.RowProperty, 1);
        comp2.Set(Grid.RowProperty, 2);

        // Assert
        Assert.Equal(1, comp1.Get(Grid.RowProperty));
        Assert.Equal(2, comp2.Get(Grid.RowProperty));
    }

    [Fact]
    public void AttachedProperty_SetOnChild_ShouldNotAffectParent()
    {
        // Arrange
        var parent = new TestComponent();
        var child = new TestComponent();
        parent.AddChild(child);

        // Act
        parent.Set(Grid.RowProperty, 0);
        child.Set(Grid.RowProperty, 1);

        // Assert
        Assert.Equal(0, parent.Get(Grid.RowProperty));
        Assert.Equal(1, child.Get(Grid.RowProperty));
    }

    // === 边界值测试 ===

    [Fact]
    public void AttachedProperty_SetNegativeValue_ShouldWork()
    {
        // Arrange
        var component = new TestComponent();

        // Act - Canvas 支持负坐标
        component.Set(Canvas.LeftProperty, -10.0);

        // Assert
        Assert.Equal(-10.0, component.Get(Canvas.LeftProperty));
    }

    [Fact]
    public void AttachedProperty_SetLargeValue_ShouldWork()
    {
        // Arrange
        var component = new TestComponent();

        // Act
        component.Set(Grid.RowProperty, 1000);

        // Assert
        Assert.Equal(1000, component.Get(Grid.RowProperty));
    }

    // === 测试组件 ===

    internal class TestComponent : Eclipse.Core.ComponentBase
    {
        public override bool IsVisible => true;
        public override void Build(Eclipse.Core.Abstractions.IBuildContext context) { }
    }
}