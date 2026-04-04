using Eclipse.Controls;
using Xunit;

namespace Eclipse.Tests;

/// <summary>
/// 控件单元测试
/// </summary>
public class ControlTests
{
    [Fact]
    public void StackLayout_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var stack = new StackLayout();

        // Assert
        Assert.Equal(Orientation.Vertical, stack.Orientation);
        Assert.Equal("0", stack.Spacing);
        Assert.Equal("0", stack.Padding);
        Assert.Null(stack.BackgroundColor);
    }

    [Fact]
    public void StackLayout_GetSpacing_ShouldParseValidString()
    {
        // Arrange
        var stack = new StackLayout { Spacing = "16.5" };

        // Act
        var spacing = stack.GetSpacing();

        // Assert
        Assert.Equal(16.5, spacing);
    }

    [Fact]
    public void StackLayout_GetSpacing_ShouldReturnZeroForInvalidString()
    {
        // Arrange
        var stack = new StackLayout { Spacing = "invalid" };

        // Act
        var spacing = stack.GetSpacing();

        // Assert
        Assert.Equal(0, spacing);
    }

    [Fact]
    public void HStack_ShouldHaveHorizontalOrientation()
    {
        // Arrange & Act
        var hStack = new HStack();

        // Assert
        Assert.Equal(Orientation.Horizontal, hStack.Orientation);
    }

    [Fact]
    public void Label_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var label = new Label();

        // Assert
        Assert.Equal("14", label.FontSize);
        Assert.Equal(TextAlignment.Left, label.TextAlignment);
        Assert.Null(label.Text);
        Assert.Null(label.Color);
        Assert.Null(label.FontWeight);
    }

    [Fact]
    public void Label_GetFontSize_ShouldParseValidString()
    {
        // Arrange
        var label = new Label { FontSize = "24" };

        // Act
        var size = label.GetFontSize();

        // Assert
        Assert.Equal(24, size);
    }

    [Fact]
    public void Button_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var button = new Button();

        // Assert
        Assert.Equal("#007AFF", button.BackgroundColor);
        Assert.Equal("White", button.TextColor);
        Assert.Equal("14", button.FontSize);
        Assert.Equal("4", button.CornerRadius);
        Assert.True(button.IsEnabled);
    }

    [Fact]
    public void Button_GetCornerRadius_ShouldParseValidString()
    {
        // Arrange
        var button = new Button { CornerRadius = "12.5" };

        // Act
        var radius = button.GetCornerRadius();

        // Assert
        Assert.Equal(12.5, radius);
    }

    [Fact]
    public void Button_OnClickEvent_CanSubscribe()
    {
        // Arrange
        var button = new Button();
        
        // Act - 订阅事件
        button.Click += (s, e) => { };
        
        // Assert - 无异常即为成功
        Assert.True(button.IsEnabled);
    }

    [Fact]
    public void TextInput_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var input = new TextInput();

        // Assert
        Assert.Equal(14, input.FontSize);
        Assert.Equal(4, input.CornerRadius);
        Assert.Equal(8, input.Padding);
        Assert.True(input.IsEnabled);
        Assert.False(input.IsPassword);
    }

    [Fact]
    public void CheckBox_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var checkBox = new CheckBox();

        // Assert
        Assert.False(checkBox.IsChecked);
        Assert.Equal(20, checkBox.Size);
        Assert.True(checkBox.IsEnabled);
    }

    [Fact]
    public void Image_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var image = new Image();

        // Assert
        Assert.Equal(-1, image.Width);
        Assert.Equal(-1, image.Height);
        Assert.Equal(Stretch.Uniform, image.Stretch);
    }

    [Fact]
    public void Container_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var container = new Container();

        // Assert
        Assert.Null(container.BackgroundColor);
        Assert.Equal(0, container.Padding);
        Assert.Equal(0, container.CornerRadius);
    }
}