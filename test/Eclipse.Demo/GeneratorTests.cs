using System;
using Eclipse.Core;
using Eclipse.Core.Abstractions;
using Eclipse.Demo.Controls;
using Eclipse.Generator;
using Xunit;

namespace Eclipse.Demo;

public class BuildContextTests
{
    [Fact]
    public void BuildContext_SimpleComponent()
    {
        var context = new BuildContext();
        
        using (context.BeginComponent<Label>(new ComponentId(1), out var label))
        {
            label.Text = "Hello World";
            label.FontSize = 16;
        }
        
        var root = context.RootComponent;
        Assert.NotNull(root);
        Assert.IsType<Label>(root);
        
        var labelRoot = (Label)root!;
        Assert.Equal("Hello World", labelRoot.Text);
        Assert.Equal(16, labelRoot.FontSize);
        Assert.Empty(labelRoot.Children);
    }
    
    [Fact]
    public void BuildContext_NestedComponentTree()
    {
        var context = new BuildContext();
        
        using (context.BeginComponent<StackLayout>(new ComponentId(1), out var layout))
        {
            layout.Spacing = 10;
            layout.Padding = 20;
            
            using (context.BeginChildContent())
            {
                using (context.BeginComponent<Label>(new ComponentId(2), out var label1))
                {
                    label1.Text = "Title";
                    label1.FontSize = 24;
                }
                
                using (context.BeginComponent<Button>(new ComponentId(3), out var button))
                {
                    button.Text = "Click Me";
                }
            }
        }
        
        var root = context.RootComponent;
        Assert.NotNull(root);
        Assert.IsType<StackLayout>(root);
        
        var stackLayout = (StackLayout)root!;
        Assert.Equal(10, stackLayout.Spacing);
        Assert.Equal(20, stackLayout.Padding);
        Assert.Equal(2, stackLayout.Children.Count);
        
        var child1 = Assert.IsType<Label>(stackLayout.Children[0]);
        Assert.Equal("Title", child1.Text);
        Assert.Equal(24, child1.FontSize);
        
        var child2 = Assert.IsType<Button>(stackLayout.Children[1]);
        Assert.Equal("Click Me", child2.Text);
    }
    
    [Fact]
    public void BuildContext_DepthTracking()
    {
        var context = new BuildContext();
        
        Assert.Equal(0, context.Depth);
        
        using (context.BeginComponent<StackLayout>(new ComponentId(1), out var _))
        {
            Assert.Equal(1, context.Depth);
            
            using (context.BeginChildContent())
            {
                using (context.BeginComponent<Label>(new ComponentId(2), out var _))
                {
                    Assert.Equal(2, context.Depth);
                }
                
                Assert.Equal(1, context.Depth);
            }
            
            Assert.Equal(1, context.Depth);
        }
        
        Assert.Equal(0, context.Depth);
    }
    
    [Fact]
    public void BuildContext_ComponentPathTracking()
    {
        var context = new BuildContext();
        
        Assert.Empty(context.ComponentPath);
        
        using (context.BeginComponent<StackLayout>(new ComponentId(100), out var _))
        {
            Assert.Single(context.ComponentPath);
            Assert.Equal(new ComponentId(100), context.ComponentPath[0]);
            
            using (context.BeginChildContent())
            {
                using (context.BeginComponent<Label>(new ComponentId(200), out var _))
                {
                    Assert.Equal(2, context.ComponentPath.Count);
                    Assert.Equal(new ComponentId(100), context.ComponentPath[0]);
                    Assert.Equal(new ComponentId(200), context.ComponentPath[1]);
                }
            }
        }
        
        Assert.Empty(context.ComponentPath);
    }
    
    [Fact]
    public void BuildContext_ParentChildRelationship()
    {
        var context = new BuildContext();
        
        using (context.BeginComponent<StackLayout>(new ComponentId(1), out var layout))
        {
            using (context.BeginChildContent())
            {
                using (context.BeginComponent<Label>(new ComponentId(2), out var label))
                {
                    label.Text = "Child";
                }
            }
        }
        
        var root = context.RootComponent!;
        var child = root.Children[0];
        
        // 验证父子关系
        Assert.Equal(root, child.Parent);
        Assert.Contains(child, root.Children);
    }
    
    [Fact]
    public void BuildContext_LifecycleCalled()
    {
        var context = new BuildContext();
        LifecycleTracker? tracker = null;
        
        using (context.BeginComponent<LifecycleTracker>(new ComponentId(1), out tracker))
        {
            // tracker is created and OnInitialized called
            Assert.True(tracker.OnInitializedCalled, "OnInitialized should be called in BeginComponent");
        }
        
        Assert.NotNull(tracker);
        Assert.True(tracker.OnInitializedCalled, "OnInitializedCalled");
        Assert.True(tracker.OnParametersSetCalled, "OnParametersSetCalled");
        Assert.True(tracker.OnMountedCalled, "OnMountedCalled");
    }
}

/// <summary>
/// 生命周期跟踪组件
/// </summary>
public class LifecycleTracker : ComponentBase
{
    public bool OnInitializedCalled { get; private set; }
    public bool OnParametersSetCalled { get; private set; }
    public bool OnMountedCalled { get; private set; }
    
    public override void OnInitialized()
    {
        base.OnInitialized();
        OnInitializedCalled = true;
    }
    
    public override void OnParametersSet()
    {
        base.OnParametersSet();
        OnParametersSetCalled = true;
    }
    
    public override void OnMounted()
    {
        base.OnMounted();
        OnMountedCalled = true;
    }
    
    public override void Render(IBuildContext context) { }
}

public class GeneratorTests
{
    [Fact]
    public void Placeholder_Test()
    {
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
        Assert.Equal("\"24\"", fontSizeAttr.Value);
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
        Assert.Equal("\"Hello\"", label.Attributes[0].Value);
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

    // ==================== 新语法测试 ====================

    [Fact]
    public void MarkupParser_InterpolatedString_WithoutAt()
    {
        // $"" 不需要 @
        var parser = new EclipseMarkupParser("<Label Text=$\"Hello {name}\" />");
        var nodes = parser.Parse();

        Assert.Single(nodes);
        var control = Assert.IsType<ControlNode>(nodes[0]);
        var textAttr = control.Attributes[0];
        Assert.Equal("Text", textAttr.Name);
        Assert.True(textAttr.IsBinding);
        Assert.Contains("Hello", textAttr.Value);
    }

    [Fact]
    public void MarkupParser_VerbatimString()
    {
        // @"" 多行字符串
        var parser = new EclipseMarkupParser(@"<Label Text=@""Line1
Line2"" />");
        var nodes = parser.Parse();

        Assert.Single(nodes);
        var control = Assert.IsType<ControlNode>(nodes[0]);
        var textAttr = control.Attributes[0];
        Assert.Equal("Text", textAttr.Name);
        Assert.False(textAttr.IsBinding);
    }

    [Fact]
    public void MarkupParser_VerbatimInterpolatedString()
    {
        // @$"" 多行内插字符串
        var parser = new EclipseMarkupParser(@"<Label Text=@$""Hello {name}
World"" />");
        var nodes = parser.Parse();

        Assert.Single(nodes);
        var control = Assert.IsType<ControlNode>(nodes[0]);
        var textAttr = control.Attributes[0];
        Assert.Equal("Text", textAttr.Name);
        Assert.True(textAttr.IsBinding);
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