using System;
using Eclipse.Generator;
using Xunit;

namespace Eclipse.Demo;

public class GeneratorTests
{
    [Fact]
    public void Placeholder_Test()
    {
        // TODO: 添加实际的测试用例
        Assert.True(true);
    }

    [Fact]
    public void MarkupParser_ParseControl()
    {
        var parser = new EclipseMarkupParser("<Label Text=@title FontSize=\"24\" />");
        var nodes = parser.Parse();

        Assert.Single(nodes);
        var control = Assert.IsType<ControlNode>(nodes[0]);
        Assert.Equal("Label", control.TagName);
        Assert.Equal(2, control.Attributes.Count);

        var textAttr = control.Attributes[0];
        Assert.Equal("Text", textAttr.Name);
        Assert.Equal("title", textAttr.Value);
        Assert.True(textAttr.IsBinding);

        var fontSizeAttr = control.Attributes[1];
        Assert.Equal("FontSize", fontSizeAttr.Name);
        Assert.Equal("\"24\"", fontSizeAttr.Value);  // 字符串字面量现在带引号
        Assert.False(fontSizeAttr.IsBinding);
    }

    [Fact]
    public void MarkupParser_ParseNestedControls()
    {
        var parser = new EclipseMarkupParser("<StackLayout><Label Text=\"Hello\" /></StackLayout>");
        var nodes = parser.Parse();

        Assert.Single(nodes);
        var stack = Assert.IsType<ControlNode>(nodes[0]);
        Assert.Equal("StackLayout", stack.TagName);
        Assert.Single(stack.Children);

        var label = Assert.IsType<ControlNode>(stack.Children[0]);
        Assert.Equal("Label", label.TagName);
        Assert.Equal("Text", label.Attributes[0].Name);
        Assert.Equal("\"Hello\"", label.Attributes[0].Value);  // 字符串字面量现在带引号
    }

    [Fact]
    public void MarkupParser_ParseIfStatement()
    {
        var parser = new EclipseMarkupParser("@if (counter > 5) { <Label Text=\"High\" /> }");
        var nodes = parser.Parse();

        Assert.Single(nodes);
        var ifNode = Assert.IsType<IfNode>(nodes[0]);
        Assert.Equal("counter > 5", ifNode.Condition);
        Assert.Single(ifNode.ThenBranch);

        var label = Assert.IsType<ControlNode>(ifNode.ThenBranch[0]);
        Assert.Equal("Label", label.TagName);
    }

    // ==================== 语法检查测试 ====================

    [Fact]
    public void SyntaxCheck_UnclosedStringLiteral()
    {
        var parser = new EclipseMarkupParser("<Label Text=\"Hello />");
        var ex = Assert.Throws<FormatException>(() => parser.Parse());
        Assert.Contains("Unclosed string literal", ex.Message);
    }

    [Fact]
    public void SyntaxCheck_UnclosedIfCondition()
    {
        var parser = new EclipseMarkupParser("@if (counter > 5 { <Label /> }");
        var ex = Assert.Throws<FormatException>(() => parser.Parse());
        Assert.Contains("Unclosed if condition", ex.Message);
    }

    [Fact]
    public void SyntaxCheck_UnclosedIfBlock()
    {
        var parser = new EclipseMarkupParser("@if (counter > 5) { <Label />");
        var ex = Assert.Throws<FormatException>(() => parser.Parse());
        Assert.Contains("Unclosed if block", ex.Message);
    }

    [Fact]
    public void SyntaxCheck_UnclosedForeachCondition()
    {
        var parser = new EclipseMarkupParser("@foreach (var item in items { <Label /> }");
        var ex = Assert.Throws<FormatException>(() => parser.Parse());
        Assert.Contains("Unclosed foreach condition", ex.Message);
    }

    [Fact]
    public void SyntaxCheck_UnclosedForeachBlock()
    {
        var parser = new EclipseMarkupParser("@foreach (var item in items) { <Label />");
        var ex = Assert.Throws<FormatException>(() => parser.Parse());
        Assert.Contains("Unclosed foreach block", ex.Message);
    }

    [Fact]
    public void SyntaxCheck_ForeachMissingInKeyword()
    {
        var parser = new EclipseMarkupParser("@foreach (var item items) { <Label /> }");
        var ex = Assert.Throws<FormatException>(() => parser.Parse());
        Assert.Contains("expected 'var item in collection'", ex.Message);
    }

    [Fact]
    public void SyntaxCheck_UnclosedExpression()
    {
        var parser = new EclipseMarkupParser("<Label Text=@(counter + 5 />");
        var ex = Assert.Throws<FormatException>(() => parser.Parse());
        Assert.Contains("Unclosed expression", ex.Message);
    }

    [Fact]
    public void SyntaxCheck_EmptyTagName()
    {
        var parser = new EclipseMarkupParser("<>");
        var ex = Assert.Throws<FormatException>(() => parser.Parse());
        Assert.Contains("Empty or invalid tag name", ex.Message);
    }

    [Fact]
    public void SyntaxCheck_MismatchedEndTag()
    {
        var parser = new EclipseMarkupParser("<StackLayout><Label /></HStack>");
        var ex = Assert.Throws<FormatException>(() => parser.Parse());
        Assert.Contains("Mismatched end tag", ex.Message);
    }

    [Fact]
    public void SyntaxCheck_UnclosedTag()
    {
        var parser = new EclipseMarkupParser("<Label Text=\"Hello\"");
        var ex = Assert.Throws<FormatException>(() => parser.Parse());
        Assert.Contains("Unclosed tag", ex.Message);
    }
}