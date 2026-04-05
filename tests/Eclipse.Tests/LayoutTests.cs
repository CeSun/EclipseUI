using Eclipse.Controls;
using Eclipse.Input;
using Eclipse.Rendering;
using Xunit;

namespace Eclipse.Tests;

/// <summary>
/// 布局系统测试 - Measure/Arrange 机制
/// </summary>
public class LayoutTests
{
    /// <summary>
    /// 测试用的 DrawingContext
    /// </summary>
    private class TestDrawingContext : IDrawingContext
    {
        public double Scale => 1.0;
        public double Width => 800;
        public double Height => 600;
        
        public void Clear(string? color = null) { }
        
        public double MeasureText(string text, double fontSize, string? fontFamily = null)
        {
            // 简化实现：每个字符宽度�?fontSize * 0.6
            return text.Length * fontSize * 0.6;
        }
        
        public void DrawRectangle(Rect bounds, string? fillColor, string? strokeColor = null, double strokeWidth = 0, double cornerRadius = 0) { }
        public void DrawRoundRect(Rect bounds, string fillColor, double cornerRadius) { }
        public void DrawText(string text, double x, double y, double fontSize, string? fontFamily = null, string? fontWeight = null, string? color = null) { }
        public void DrawImage(string imageKey, Rect bounds, Stretch stretch) { }
        public string? LoadImage(string source) => null;
        public Size GetImageSize(string imageKey) => Size.Zero;
    }
    
    private readonly TestDrawingContext _context = new();
    
    // === StackLayout Measure 测试 ===
    
    [Fact]
    public void StackLayout_Measure_ShouldReturnCorrectSize_ForEmptyStack()
    {
        // Arrange
        var stack = new StackLayout { Padding = 10 };
        
        // Act
        var size = stack.Measure(Size.Empty, _context);
        
        // Assert - �?Stack 只有 Padding
        Assert.Equal(20, size.Width);  // Padding * 2
        Assert.Equal(20, size.Height);
    }
    
    [Fact]
    public void StackLayout_Measure_ShouldReturnCorrectSize_ForVerticalStack()
    {
        // Arrange
        var stack = new StackLayout 
        { 
            Orientation = Orientation.Vertical,
            Spacing = 10,
            Padding = 5
        };
        
        var label1 = new Label { Text = "Hello", FontSize = 14 };
        var label2 = new Label { Text = "World", FontSize = 14 };
        
        stack.AddChild(label1);
        stack.AddChild(label2);
        
        // Act
        var size = stack.Measure(Size.Empty, _context);
        
        // Assert
        // 每个 Label 高度�?14 * 1.3 = 18.2
        // 总高�?= 18.2 + 18.2 + 10 (spacing) + 10 (padding * 2)
        var expectedHeight = 18.2 * 2 + 10 + 10;
        Assert.Equal(expectedHeight, size.Height, 1);
    }
    
    [Fact]
    public void StackLayout_Measure_ShouldReturnCorrectSize_ForHorizontalStack()
    {
        // Arrange
        var stack = new StackLayout 
        { 
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            Padding = 5
        };
        
        var label1 = new Label { Text = "Hello", FontSize = 14 };
        var label2 = new Label { Text = "World", FontSize = 14 };
        
        stack.AddChild(label1);
        stack.AddChild(label2);
        
        // Act
        var size = stack.Measure(Size.Empty, _context);
        
        // Assert
        // 每个字符宽度�?14 * 0.6 = 8.4
        // Hello = 5 chars = 42, World = 5 chars = 42
        // 总宽�?= 42 + 42 + 10 (spacing) + 10 (padding * 2)
        var expectedWidth = 42 + 42 + 10 + 10;
        Assert.Equal(expectedWidth, size.Width, 1);
    }
    
    [Fact]
    public void HStack_ShouldHaveHorizontalOrientation()
    {
        // Arrange & Act
        var hStack = new HStack();
        
        // Assert
        Assert.Equal(Orientation.Horizontal, hStack.Orientation);
    }
    
    // === Label Measure 测试 ===
    
    [Fact]
    public void Label_Measure_ShouldReturnZero_ForEmptyText()
    {
        // Arrange
        var label = new Label();
        
        // Act
        var size = label.Measure(Size.Empty, _context);
        
        // Assert
        Assert.Equal(0, size.Width);
        Assert.Equal(0, size.Height);
    }
    
    [Fact]
    public void Label_Measure_ShouldReturnCorrectSize_ForText()
    {
        // Arrange
        var label = new Label { Text = "Hello", FontSize = 16 };
        
        // Act
        var size = label.Measure(Size.Empty, _context);
        
        // Assert
        // Width = 5 chars * 16 * 0.6 = 48
        // Height = 16 * 1.3 = 20.8
        Assert.Equal(48, size.Width, 1);
        Assert.Equal(20.8, size.Height, 1);
    }
    
    [Fact]
    public void Label_Measure_ShouldScaleWithFontSize()
    {
        // Arrange
        var label14 = new Label { Text = "Test", FontSize = 14 };
        var label28 = new Label { Text = "Test", FontSize = 28 };
        
        // Act
        var size14 = label14.Measure(Size.Empty, _context);
        var size28 = label28.Measure(Size.Empty, _context);
        
        // Assert - 字体大小翻倍，尺寸也翻�?        Assert.Equal(size28.Width, size14.Width * 2, 1);
        Assert.Equal(size28.Height, size14.Height * 2, 1);
    }
    
    // === Button Measure 测试 ===
    
    [Fact]
    public void Button_Measure_ShouldReturnDefaultSize_ForEmptyText()
    {
        // Arrange
        var button = new Button();
        
        // Act
        var size = button.Measure(Size.Empty, _context);
        
        // Assert - 默认按钮尺寸
        Assert.Equal(80, size.Width);
        Assert.Equal(44, size.Height);
    }
    
    [Fact]
    public void Button_Measure_ShouldIncludePadding_ForText()
    {
        // Arrange
        var button = new Button { Text = "Click", FontSize = 14 };
        
        // Act
        var size = button.Measure(Size.Empty, _context);
        
        // Assert
        // Text width = 5 chars * 14 * 0.6 = 42
        // Button width = 42 + 40 (padding) = 82
        Assert.Equal(82, size.Width, 1);
        Assert.Equal(44, size.Height);
    }
    
    // === Grid Measure/Arrange 测试 ===
    
    [Fact]
    public void Grid_RowCount_ShouldDefaultToOne()
    {
        // Arrange & Act
        var grid = new Grid();
        
        // Assert
        Assert.Equal(1, grid.RowCount);
    }
    
    [Fact]
    public void Grid_ColumnCount_ShouldDefaultToOne()
    {
        // Arrange & Act
        var grid = new Grid();
        
        // Assert
        Assert.Equal(1, grid.ColumnCount);
    }
    
    [Fact]
    public void Grid_SetRowDefinitions_ShouldUpdateRowCount()
    {
        // Arrange
        var grid = new Grid();
        
        // Act
        grid.SetRowDefinitions(GridLength.Auto, GridLength.Star(1), GridLength.Absolute(50));
        
        // Assert
        Assert.Equal(3, grid.RowCount);
    }
    
    [Fact]
    public void Grid_SetColumnDefinitions_ShouldUpdateColumnCount()
    {
        // Arrange
        var grid = new Grid();
        
        // Act
        grid.SetColumnDefinitions(GridLength.Star(1), GridLength.Star(2));
        
        // Assert
        Assert.Equal(2, grid.ColumnCount);
    }
    
    [Fact]
    public void GridLength_Absolute_ShouldHaveCorrectType()
    {
        // Arrange & Act
        var length = GridLength.Absolute(100);
        
        // Assert
        Assert.True(length.IsAbsolute);
        Assert.False(length.IsAuto);
        Assert.False(length.IsStar);
        Assert.Equal(100, length.Value);
    }
    
    [Fact]
    public void GridLength_Auto_ShouldHaveCorrectType()
    {
        // Arrange & Act
        var length = GridLength.Auto;
        
        // Assert
        Assert.True(length.IsAuto);
        Assert.False(length.IsAbsolute);
        Assert.False(length.IsStar);
    }
    
    [Fact]
    public void GridLength_Star_ShouldHaveCorrectType()
    {
        // Arrange & Act
        var length = GridLength.Star(2);
        
        // Assert
        Assert.True(length.IsStar);
        Assert.False(length.IsAbsolute);
        Assert.False(length.IsAuto);
        Assert.Equal(2, length.Value);
    }
    
    [Fact]
    public void GridLength_ImplicitConversion_ShouldCreateAbsolute()
    {
        // Arrange & Act
        GridLength length = 100;
        
        // Assert
        Assert.True(length.IsAbsolute);
        Assert.Equal(100, length.Value);
    }
    
    // === ScrollView 测试 ===
    
    [Fact]
    public void ScrollView_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var scrollView = new ScrollView();
        
        // Assert
        Assert.True(scrollView.VerticalScrollBarVisible);
        Assert.False(scrollView.HorizontalScrollBarVisible);
        Assert.Equal(0, scrollView.ScrollX);
        Assert.Equal(0, scrollView.ScrollY);
        Assert.Equal(10, scrollView.ScrollBarWidth);
    }
    
    [Fact]
    public void ScrollView_ScrollTo_ShouldUpdateScrollPosition()
    {
        // Arrange
        var scrollView = new ScrollView();
        scrollView.UpdateBounds(new Rect(0, 0, 200, 200));
        
        // Act
        scrollView.ScrollTo(50, 100);
        
        // Assert
        Assert.Equal(50, scrollView.ScrollX);
        Assert.Equal(100, scrollView.ScrollY);
    }
    
    [Fact]
    public void ScrollView_ScrollToTop_ShouldSetScrollYToZero()
    {
        // Arrange
        var scrollView = new ScrollView();
        scrollView.UpdateBounds(new Rect(0, 0, 200, 200));
        scrollView.ScrollTo(50, 100);
        
        // Act
        scrollView.ScrollToTop();
        
        // Assert
        Assert.Equal(0, scrollView.ScrollY);
    }
    
    // === Container 测试 ===
    
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
    
    // === Rect 测试 ===
    
    [Fact]
    public void Rect_Contains_ShouldReturnTrue_ForPointInside()
    {
        // Arrange
        var rect = new Rect(10, 10, 100, 100);
        var point = new Point(50, 50);
        
        // Act
        var contains = rect.Contains(point);
        
        // Assert
        Assert.True(contains);
    }
    
    [Fact]
    public void Rect_Contains_ShouldReturnFalse_ForPointOutside()
    {
        // Arrange
        var rect = new Rect(10, 10, 100, 100);
        var pointOutside1 = new Point(5, 50);
        var pointOutside2 = new Point(150, 50);
        var pointOutside3 = new Point(50, 200);
        
        // Assert
        Assert.False(rect.Contains(pointOutside1));
        Assert.False(rect.Contains(pointOutside2));
        Assert.False(rect.Contains(pointOutside3));
    }
    
    [Fact]
    public void Rect_Properties_ShouldReturnCorrectValues()
    {
        // Arrange
        var rect = new Rect(10, 20, 100, 50);
        
        // Assert
        Assert.Equal(10, rect.Left);
        Assert.Equal(20, rect.Top);
        Assert.Equal(110, rect.Right);
        Assert.Equal(70, rect.Bottom);
    }
    
    [Fact]
    public void Rect_IsEmpty_ShouldReturnTrue_ForZeroSize()
    {
        // Arrange
        var emptyRect = new Rect(0, 0, 0, 0);
        var nonEmptyRect = new Rect(0, 0, 1, 1);
        
        // Assert
        Assert.True(emptyRect.IsEmpty);
        Assert.False(nonEmptyRect.IsEmpty);
    }
    
    // === Point 测试 ===
    
    [Fact]
    public void Point_Operators_ShouldWorkCorrectly()
    {
        // Arrange
        var p1 = new Point(10, 20);
        var p2 = new Point(5, 10);
        
        // Act & Assert
        var sum = p1 + p2;
        Assert.Equal(15, sum.X);
        Assert.Equal(30, sum.Y);
        
        var diff = p1 - p2;
        Assert.Equal(5, diff.X);
        Assert.Equal(10, diff.Y);
        
        var scaled = p1 * 2;
        Assert.Equal(20, scaled.X);
        Assert.Equal(40, scaled.Y);
    }
    
    [Fact]
    public void Point_Length_ShouldReturnCorrectValue()
    {
        // Arrange
        var p = new Point(3, 4);
        
        // Act
        var length = p.Length;
        
        // Assert - 3-4-5 triangle
        Assert.Equal(5, length);
    }
    
    // === Size 测试 ===
    
    [Fact]
    public void Size_Zero_ShouldReturnZeroSize()
    {
        // Arrange & Act
        var size = Size.Zero;
        
        // Assert
        Assert.Equal(0, size.Width);
        Assert.Equal(0, size.Height);
    }
    
    [Fact]
    public void Size_IsEmpty_ShouldReturnTrue_ForEmpty()
    {
        // Arrange
        var emptySize = Size.Empty;
        var normalSize = new Size(100, 100);
        
        // Assert
        Assert.True(emptySize.IsEmpty);
        Assert.False(normalSize.IsEmpty);
    }
    
    // === CheckBox Measure 测试 ===
    
    [Fact]
    public void CheckBox_Measure_ShouldReturnCorrectSize_WithoutLabel()
    {
        // Arrange
        var checkBox = new CheckBox { Size = 20 };
        
        // Act
        var size = checkBox.Measure(Size.Empty, _context);
        
        // Assert
        Assert.Equal(20, size.Width);
        Assert.Equal(20, size.Height);
    }
    
    [Fact]
    public void CheckBox_Measure_ShouldIncludeLabelWidth()
    {
        // Arrange
        var checkBox = new CheckBox { Size = 20, Label = "Option" };
        
        // Act
        var size = checkBox.Measure(Size.Empty, _context);
        
        // Assert - 应包�?Label 宽度
        Assert.True(size.Width > 20);
    }
    
    // === TextInput Measure 测试 ===
    
    [Fact]
    public void TextInput_Measure_ShouldReturnDefaultSize_ForEmptyText()
    {
        // Arrange
        var input = new TextInput { FontSize = 14 };
        
        // Act
        var size = input.Measure(Size.Empty, _context);
        
        // Assert
        Assert.Equal(200, size.Width); // 默认宽度
        // 高度 = fontSize * 1.5 + padding * 2
    }
    
    [Fact]
    public void TextInput_Measure_ShouldExpand_ForText()
    {
        // Arrange
        var input = new TextInput { Text = "Hello World", FontSize = 14 };
        
        // Act
        var size = input.Measure(Size.Empty, _context);
        
        // Assert - 宽度应该基于文本内容
        Assert.True(size.Width > 200);
    }
    
    // === InteractiveControl 测试 ===
    
    [Fact]
    public void InteractiveControl_IsInputEnabled_ShouldDependOnIsEnabled()
    {
        // Arrange
        var button = new Button();
        
        // Act & Assert
        Assert.True(button.IsInputEnabled);
        
        button.IsEnabled = false;
        Assert.False(button.IsInputEnabled);
    }
    
    [Fact]
    public void InteractiveControl_UpdateBounds_ShouldSetBounds()
    {
        // Arrange
        var button = new Button();
        var newBounds = new Rect(10, 10, 100, 50);
        
        // Act
        button.UpdateBounds(newBounds);
        
        // Assert
        Assert.Equal(newBounds, button.Bounds);
    }
}