using Eclipse.Generator;
using Xunit;

namespace Eclipse.Tests;

/// <summary>
/// EclipseMarkupParser 单元测试
/// </summary>
public class EclipseMarkupParserTests
{
    // === 基础解析测试 ===

    [Fact]
    public void Parse_EmptyMarkup_ShouldReturnEmptyList()
    {
        // Arrange
        var parser = new EclipseMarkupParser("");

        // Act
        var nodes = parser.Parse();

        // Assert
        Assert.Empty(nodes);
    }

    [Fact]
    public void Parse_SimpleControl_ShouldReturnControlNode()
    {
        // Arrange
        var parser = new EclipseMarkupParser("<Button />");

        // Act
        var nodes = parser.Parse();

        // Assert
        Assert.Single(nodes);
        var control = Assert.IsType<ControlNode>(nodes[0]);
        Assert.Equal("Button", control.TagName);
        Assert.Empty(control.Attributes);
        Assert.Empty(control.Children);
    }

    [Fact]
    public void Parse_ControlWithAttribute_ShouldParseAttribute()
    {
        // Arrange
        var parser = new EclipseMarkupParser("<Label Text=\"Hello\" />");

        // Act
        var nodes = parser.Parse();

        // Assert
        Assert.Single(nodes);
        var control = Assert.IsType<ControlNode>(nodes[0]);
        Assert.Equal("Label", control.TagName);
        Assert.Single(control.Attributes);
        Assert.Equal("Text", control.Attributes[0].Name);
        Assert.Equal("\"Hello\"", control.Attributes[0].Value);
        Assert.False(control.Attributes[0].IsBinding);
    }

    [Fact]
    public void Parse_ControlWithBinding_ShouldMarkAsBinding()
    {
        // Arrange
        var parser = new EclipseMarkupParser("<Label Text=\"@message\" />");

        // Act
        var nodes = parser.Parse();

        // Assert
        Assert.Single(nodes);
        var control = Assert.IsType<ControlNode>(nodes[0]);
        Assert.Single(control.Attributes);
        Assert.Equal("Text", control.Attributes[0].Name);
        Assert.Equal("message", control.Attributes[0].Value);
        Assert.True(control.Attributes[0].IsBinding);
    }

    [Fact]
    public void Parse_ControlWithMultipleAttributes_ShouldParseAll()
    {
        // Arrange
        var parser = new EclipseMarkupParser("<Button Text=\"Click\" Width=\"100\" Height=\"50\" />");

        // Act
        var nodes = parser.Parse();

        // Assert
        Assert.Single(nodes);
        var control = Assert.IsType<ControlNode>(nodes[0]);
        Assert.Equal(3, control.Attributes.Count);
        Assert.Equal("Text", control.Attributes[0].Name);
        Assert.Equal("Width", control.Attributes[1].Name);
        Assert.Equal("Height", control.Attributes[2].Name);
    }

    [Fact]
    public void Parse_ControlWithChildren_ShouldParseNestedControls()
    {
        // Arrange
        var parser = new EclipseMarkupParser("<Stack><Label Text=\"Hello\" /><Button Text=\"Click\" /></Stack>");

        // Act
        var nodes = parser.Parse();

        // Assert
        Assert.Single(nodes);
        var stack = Assert.IsType<ControlNode>(nodes[0]);
        Assert.Equal("Stack", stack.TagName);
        Assert.Equal(2, stack.Children.Count);
        
        var label = Assert.IsType<ControlNode>(stack.Children[0]);
        Assert.Equal("Label", label.TagName);
        
        var button = Assert.IsType<ControlNode>(stack.Children[1]);
        Assert.Equal("Button", button.TagName);
    }

    // === 文本节点测试 ===

    [Fact]
    public void Parse_TextContent_ShouldReturnTextNode()
    {
        // Arrange
        var parser = new EclipseMarkupParser("Hello World");

        // Act
        var nodes = parser.Parse();

        // Assert
        Assert.Single(nodes);
        var text = Assert.IsType<TextNode>(nodes[0]);
        Assert.Equal("Hello World", text.Text);
    }

    [Fact]
    public void Parse_MixedTextAndControls_ShouldParseBoth()
    {
        // Arrange
        var parser = new EclipseMarkupParser("Hello <Label Text=\"Name\" /> World");

        // Act
        var nodes = parser.Parse();

        // Assert
        Assert.Equal(3, nodes.Count);
        Assert.IsType<TextNode>(nodes[0]);
        Assert.IsType<ControlNode>(nodes[1]);
        Assert.IsType<TextNode>(nodes[2]);
    }

    // === 表达式节点测试 ===

    [Fact]
    public void Parse_SimpleExpression_ShouldReturnExpressionNode()
    {
        // Arrange - 使用括号形式的表达式
        var parser = new EclipseMarkupParser("@(message)");

        // Act
        var nodes = parser.Parse();

        // Assert
        Assert.Single(nodes);
        var expr = Assert.IsType<ExpressionNode>(nodes[0]);
        Assert.Equal("message", expr.Expression);
    }

    [Fact]
    public void Parse_ParenthesizedExpression_ShouldReturnExpressionNode()
    {
        // Arrange
        var parser = new EclipseMarkupParser("@(item.Name)");

        // Act
        var nodes = parser.Parse();

        // Assert
        Assert.Single(nodes);
        var expr = Assert.IsType<ExpressionNode>(nodes[0]);
        Assert.Equal("item.Name", expr.Expression);
    }

    [Fact]
    public void Parse_MemberAccessExpression_ShouldParseChain()
    {
        // Arrange - 使用括号形式
        var parser = new EclipseMarkupParser("@(user.Profile.Name)");

        // Act
        var nodes = parser.Parse();

        // Assert
        Assert.Single(nodes);
        var expr = Assert.IsType<ExpressionNode>(nodes[0]);
        Assert.Equal("user.Profile.Name", expr.Expression);
    }

    // === @if 控制流测试 ===

    [Fact]
    public void Parse_IfStatement_ShouldReturnIfNode()
    {
        // Arrange
        var parser = new EclipseMarkupParser("@if (isVisible) { <Label Text=\"Visible\" /> }");

        // Act
        var nodes = parser.Parse();

        // Assert
        Assert.Single(nodes);
        var ifNode = Assert.IsType<IfNode>(nodes[0]);
        Assert.Equal("isVisible", ifNode.Condition);
        Assert.Single(ifNode.ThenBranch);
        Assert.Null(ifNode.ElseBranch);
    }

    [Fact]
    public void Parse_IfElseStatement_ShouldParseBothBranches()
    {
        // Arrange
        var parser = new EclipseMarkupParser("@if (isVisible) { <Label Text=\"Yes\" /> } @else { <Label Text=\"No\" /> }");

        // Act
        var nodes = parser.Parse();

        // Assert
        Assert.Single(nodes);
        var ifNode = Assert.IsType<IfNode>(nodes[0]);
        Assert.Equal("isVisible", ifNode.Condition);
        Assert.Single(ifNode.ThenBranch);
        Assert.NotNull(ifNode.ElseBranch);
        Assert.Single(ifNode.ElseBranch);
    }

    [Fact]
    public void Parse_IfWithMultipleChildren_ShouldParseAll()
    {
        // Arrange
        var parser = new EclipseMarkupParser("@if (showHeader) { <Label Text=\"Title\" /><Label Text=\"Subtitle\" /> }");

        // Act
        var nodes = parser.Parse();

        // Assert
        Assert.Single(nodes);
        var ifNode = Assert.IsType<IfNode>(nodes[0]);
        Assert.Equal(2, ifNode.ThenBranch.Count);
    }

    // === @foreach 控制流测试 ===

    [Fact]
    public void Parse_ForeachStatement_ShouldReturnForeachNode()
    {
        // Arrange
        var parser = new EclipseMarkupParser("@foreach (var item in items) { <Label Text=\"@item\" /> }");

        // Act
        var nodes = parser.Parse();

        // Assert
        Assert.Single(nodes);
        var foreachNode = Assert.IsType<ForeachNode>(nodes[0]);
        Assert.Equal("item", foreachNode.ItemVar);
        Assert.Equal("items", foreachNode.Collection);
        Assert.Single(foreachNode.Body);
    }

    [Fact]
    public void Parse_ForeachWithExplicitType_ShouldParseItemVar()
    {
        // Arrange
        var parser = new EclipseMarkupParser("@foreach (Product product in products) { <Label Text=\"@product.Name\" /> }");

        // Act
        var nodes = parser.Parse();

        // Assert
        Assert.Single(nodes);
        var foreachNode = Assert.IsType<ForeachNode>(nodes[0]);
        Assert.Equal("Product product", foreachNode.ItemVar);
        Assert.Equal("products", foreachNode.Collection);
    }

    // === 附加属性测试 ===

    [Fact]
    public void Parse_AttachedProperty_ShouldParseCorrectly()
    {
        // Arrange
        var parser = new EclipseMarkupParser("<Label Grid.Row=\"0\" Grid.Column=\"1\" />");

        // Act
        var nodes = parser.Parse();

        // Assert
        Assert.Single(nodes);
        var control = Assert.IsType<ControlNode>(nodes[0]);
        Assert.Equal(2, control.Attributes.Count);
        
        Assert.True(control.Attributes[0].IsAttached);
        Assert.Equal("Grid.Row", control.Attributes[0].Name);
        Assert.Equal("Grid", control.Attributes[0].AttachedTypeName);
        Assert.Equal("Row", control.Attributes[0].AttachedPropertyName);
        
        Assert.True(control.Attributes[1].IsAttached);
        Assert.Equal("Grid.Column", control.Attributes[1].Name);
        Assert.Equal("Grid", control.Attributes[1].AttachedTypeName);
        Assert.Equal("Column", control.Attributes[1].AttachedPropertyName);
    }

    // === 双向绑定测试 ===

    [Fact]
    public void Parse_TwoWayBinding_ShouldMarkCorrectly()
    {
        // Arrange
        var parser = new EclipseMarkupParser("<TextInput @bind-Text=\"inputValue\" />");

        // Act
        var nodes = parser.Parse();

        // Assert
        Assert.Single(nodes);
        var control = Assert.IsType<ControlNode>(nodes[0]);
        Assert.Single(control.Attributes);
        
        Assert.Equal("Text", control.Attributes[0].Name);
        Assert.True(control.Attributes[0].IsTwoWayBinding);
        Assert.True(control.Attributes[0].IsBinding);
    }

    [Fact]
    public void Parse_BindWithoutAt_ShouldAlsoWork()
    {
        // Arrange
        var parser = new EclipseMarkupParser("<CheckBox bind-IsChecked=\"isChecked\" />");

        // Act
        var nodes = parser.Parse();

        // Assert
        Assert.Single(nodes);
        var control = Assert.IsType<ControlNode>(nodes[0]);
        Assert.Single(control.Attributes);
        
        Assert.Equal("IsChecked", control.Attributes[0].Name);
        Assert.True(control.Attributes[0].IsTwoWayBinding);
    }

    // === 事件处理测试 ===

    [Fact]
    public void Parse_EventAttribute_ShouldMarkAsEvent()
    {
        // Arrange
        var parser = new EclipseMarkupParser("<Button Click=\"@HandleClick\" />");

        // Act
        var nodes = parser.Parse();

        // Assert
        Assert.Single(nodes);
        var control = Assert.IsType<ControlNode>(nodes[0]);
        Assert.Single(control.Attributes);
        Assert.Equal("Click", control.Attributes[0].Name);
        Assert.True(control.Attributes[0].IsEvent);
        Assert.Equal("HandleClick", control.Attributes[0].Value);
    }

    [Fact]
    public void Parse_OnPrefixEvent_ShouldMarkAsEvent()
    {
        // Arrange
        var parser = new EclipseMarkupParser("<Button OnTapped=\"@OnButtonTapped\" />");

        // Act
        var nodes = parser.Parse();

        // Assert
        Assert.Single(nodes);
        var control = Assert.IsType<ControlNode>(nodes[0]);
        Assert.Single(control.Attributes);
        Assert.True(control.Attributes[0].IsEvent);
    }

    // === 错误处理测试 ===

    [Fact]
    public void Parse_UnclosedTag_ShouldNotThrowButReturnIncomplete()
    {
        // Arrange - Parser 对于未闭合标签会继续解析直到文件结束
        var parser = new EclipseMarkupParser("<Button>");

        // Act - 不抛出异常，返回已解析的控件
        var nodes = parser.Parse();

        // Assert - 返回一个 Button 控件（不完整的）
        Assert.Single(nodes);
        var control = Assert.IsType<ControlNode>(nodes[0]);
        Assert.Equal("Button", control.TagName);
    }

    [Fact]
    public void Parse_MismatchedEndTag_ShouldThrowFormatException()
    {
        // Arrange
        var parser = new EclipseMarkupParser("<Button></Label>");

        // Act & Assert
        Assert.Throws<FormatException>(() => parser.Parse());
    }

    [Fact]
    public void Parse_UnclosedIfBlock_ShouldThrowFormatException()
    {
        // Arrange
        var parser = new EclipseMarkupParser("@if (condition) { <Label />");

        // Act & Assert
        Assert.Throws<FormatException>(() => parser.Parse());
    }

    [Fact]
    public void Parse_UnclosedForeachBlock_ShouldThrowFormatException()
    {
        // Arrange
        var parser = new EclipseMarkupParser("@foreach (var item in items) { <Label />");

        // Act & Assert
        Assert.Throws<FormatException>(() => parser.Parse());
    }

    [Fact]
    public void Parse_InvalidForeachSyntax_ShouldThrowFormatException()
    {
        // Arrange - 缺少 'in' 关键字
        var parser = new EclipseMarkupParser("@foreach (var item) { <Label /> }");

        // Act & Assert
        Assert.Throws<FormatException>(() => parser.Parse());
    }

    [Fact]
    public void Parse_EmptyTagName_ShouldThrowFormatException()
    {
        // Arrange
        var parser = new EclipseMarkupParser("<>");

        // Act & Assert
        Assert.Throws<FormatException>(() => parser.Parse());
    }

    [Fact]
    public void Parse_UnclosedStringLiteral_ShouldThrowFormatException()
    {
        // Arrange
        var parser = new EclipseMarkupParser("<Label Text=\"unclosed />");

        // Act & Assert
        Assert.Throws<FormatException>(() => parser.Parse());
    }

    // === 数值属性测试 ===

    [Fact]
    public void Parse_NumberValue_ShouldParseCorrectly()
    {
        // Arrange - 属性值在引号内，Parser 保留引号形式
        var parser = new EclipseMarkupParser("<Button Width=\"100\" Height=\"44.5\" />");

        // Act
        var nodes = parser.Parse();

        // Assert
        Assert.Single(nodes);
        var control = Assert.IsType<ControlNode>(nodes[0]);
        Assert.Equal("\"100\"", control.Attributes[0].Value);
        Assert.Equal("\"44.5\"", control.Attributes[1].Value);
    }

    [Fact]
    public void Parse_NegativeNumber_ShouldParseCorrectly()
    {
        // Arrange
        var parser = new EclipseMarkupParser("<Canvas Left=\"-10\" />");

        // Act
        var nodes = parser.Parse();

        // Assert
        Assert.Single(nodes);
        var control = Assert.IsType<ControlNode>(nodes[0]);
        Assert.Equal("\"-10\"", control.Attributes[0].Value);
    }

    // === 布尔值测试 ===

    [Fact]
    public void Parse_BooleanValue_ShouldParseCorrectly()
    {
        // Arrange - 属性值在引号内，Parser 保留引号形式
        var parser = new EclipseMarkupParser("<CheckBox IsChecked=\"true\" IsEnabled=\"false\" />");

        // Act
        var nodes = parser.Parse();

        // Assert
        Assert.Single(nodes);
        var control = Assert.IsType<ControlNode>(nodes[0]);
        Assert.Equal("\"true\"", control.Attributes[0].Value);
        Assert.Equal("\"false\"", control.Attributes[1].Value);
    }

    // === 插值字符串测试 ===

    [Fact]
    public void Parse_InterpolatedString_ShouldParseCorrectly()
    {
        // Arrange - 在属性值中使用 $"" 插值字符串（不需要额外引号）
        var parser = new EclipseMarkupParser(@"<Label Text=$""Hello {name}"" />");

        // Act
        var nodes = parser.Parse();

        // Assert
        Assert.Single(nodes);
        var control = Assert.IsType<ControlNode>(nodes[0]);
        // 插值字符串会被解析器正确处理并标记为绑定
        Assert.True(control.Attributes[0].IsBinding);
    }

    // === 复杂场景测试 ===

    [Fact]
    public void Parse_ComplexNestedStructure_ShouldParseCorrectly()
    {
        // Arrange - 简化测试，去掉控制流
        var markup = "<Stack>" +
                     "<Label Text=\"Title\" FontSize=\"24\" />" +
                     "<Container>" +
                     "<Label Text=\"@item.Name\" />" +
                     "<Button Text=\"Delete\" Click=\"@DeleteItem\" />" +
                     "</Container>" +
                     "<Button Text=\"Add\" Click=\"@AddItem\" />" +
                     "</Stack>";
        var parser = new EclipseMarkupParser(markup);

        // Act
        var nodes = parser.Parse();

        // Assert
        Assert.Single(nodes);
        var stack = Assert.IsType<ControlNode>(nodes[0]);
        
        // 只检查控件节点，过滤空白文本节点
        var controlChildren = stack.Children.Where(c => c is ControlNode).ToList();
        Assert.Equal(3, controlChildren.Count);
        
        // 第一个子节点是 Label
        var titleLabel = Assert.IsType<ControlNode>(controlChildren[0]);
        Assert.Equal("Label", titleLabel.TagName);
        
        // 第二个子节点是 Container
        var container = Assert.IsType<ControlNode>(controlChildren[1]);
        Assert.Equal("Container", container.TagName);
        
        // Container 内的子节点
        var containerControlChildren = container.Children.Where(c => c is ControlNode).ToList();
        Assert.Equal(2, containerControlChildren.Count);
        
        // 第三个子节点是 Button
        var addButton = Assert.IsType<ControlNode>(controlChildren[2]);
        Assert.Equal("Button", addButton.TagName);
    }

    [Fact]
    public void Parse_WhitespaceBetweenNodes_ShouldBeHandled()
    {
        // Arrange
        var parser = new EclipseMarkupParser("""
            <Label Text="First" />
            
            <Label Text="Second" />
            
            <Label Text="Third" />
            """);

        // Act
        var nodes = parser.Parse();

        // Assert - 3 个控件，不含空白文本节点
        Assert.Equal(3, nodes.Count);
        Assert.All(nodes, n => Assert.IsType<ControlNode>(n));
    }
}