using System;
using System.Collections.Generic;
using System.Text;

namespace Eclipse.Generator;

public abstract class MarkupNode { }

public class ControlNode : MarkupNode
{
    public string TagName { get; set; } = string.Empty;
    public List<AttributeNode> Attributes { get; } = new();
    public List<MarkupNode> Children { get; } = new();
}

public class AttributeNode
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool IsBinding { get; set; }
    public bool IsEvent { get; set; }
}

public class TextNode : MarkupNode
{
    public string Text { get; set; } = string.Empty;
}

public class ExpressionNode : MarkupNode
{
    public string Expression { get; set; } = string.Empty;
}

public class IfNode : MarkupNode
{
    public string Condition { get; set; } = string.Empty;
    public List<MarkupNode> ThenBranch { get; set; } = new();
    public List<MarkupNode>? ElseBranch { get; set; }
}

public class ForeachNode : MarkupNode
{
    public string ItemVar { get; set; } = "item";
    public string Collection { get; set; } = string.Empty;
    public List<MarkupNode> Body { get; set; } = new();
}

public class EclipseMarkupParser
{
    private readonly string _source;
    private int _position;

    public EclipseMarkupParser(string source)
    {
        _source = source;
        _position = 0;
    }

    public List<MarkupNode> Parse()
    {
        var nodes = new List<MarkupNode>();
        
        while (_position < _source.Length)
        {
            SkipWhitespace();
            
            if (_position >= _source.Length) break;
            
            var node = ParseNode();
            if (node != null)
                nodes.Add(node);
        }
        
        return nodes;
    }

    private MarkupNode? ParseNode()
    {
        if (Peek() == '<')
        {
            return ParseControl();
        }
        else if (Peek() == '@')
        {
            return ParseRazorExpression();
        }
        else
        {
            return ParseText();
        }
    }

    private ControlNode? ParseControl()
    {
        var startPos = _position;
        if (Read() != '<') return null;
        
        var tagName = ReadIdentifier();
        
        // #9: 空标签名检查
        if (string.IsNullOrEmpty(tagName))
        {
            throw new FormatException($"Empty or invalid tag name at position {startPos}");
        }
        
        var control = new ControlNode { TagName = tagName };
        
        while (true)
        {
            SkipWhitespace();
            
            if (Peek() == '/' && Peek(1) == '>')
            {
                Read(); Read();
                break;
            }
            else if (Peek() == '>')
            {
                Read();
                
                while (!IsAtEnd())
                {
                    if (Peek() == '<' && Peek(1) == '/')
                    {
                        Read(); Read();
                        var endTagName = ReadIdentifier();
                        
                        // 检查结束标签是否匹配当前控件
                        if (endTagName != tagName)
                        {
                            throw new FormatException($"Mismatched end tag: expected '</{tagName}>' but found '</{endTagName}>' at position {_position}");
                        }
                        
                        // 检查结束标签是否完整闭合
                        if (Peek() != '>')
                        {
                            throw new FormatException($"Invalid end tag '</{endTagName}' at position {_position}, expected '>'");
                        }
                        Read();
                        break;
                    }
                    
                    var child = ParseNode();
                    if (child != null)
                        control.Children.Add(child);
                }
                break;
            }
            
            var attrName = ReadIdentifier();
            if (string.IsNullOrEmpty(attrName))
            {
                throw new FormatException($"Unclosed tag '<{tagName}>' at position {_position}, expected '/>' or '>' or attribute");
            }
            
            SkipWhitespace();
            
            if (Read() != '=')
            {
                throw new FormatException($"Invalid attribute format in '<{tagName}>' at position {_position}, expected '=' after '{attrName}'");
            }
            
            SkipWhitespace();
            
            var attrValue = ReadAttributeValue();
            
            control.Attributes.Add(new AttributeNode
            {
                Name = attrName,
                Value = attrValue.text,
                IsBinding = attrValue.isBinding,
                IsEvent = attrName.StartsWith("On")
            });
        }
        
        return control;
    }

    private MarkupNode? ParseRazorExpression()
    {
        var startPos = _position;
        if (Read() != '@') return null;
        
        if (char.IsLetter(Peek()))
        {
            var keyword = ReadIdentifier();
            
            if (keyword == "if")
            {
                return ParseIfStatement(startPos);
            }
            else if (keyword == "foreach")
            {
                return ParseForeachStatement(startPos);
            }
            else
            {
                // 未知的 @ 关键字
                throw new FormatException($"Unknown directive '@{keyword}' at position {startPos}");
            }
        }
        
        if (Peek() == '(')
        {
            return ParseParenthesizedExpression(startPos);
        }
        else
        {
            var expr = ReadIdentifier();
            if (string.IsNullOrEmpty(expr))
            {
                throw new FormatException($"Invalid expression at position {startPos}");
            }
            return new ExpressionNode { Expression = expr };
        }
    }

    private IfNode ParseIfStatement(int startPos)
    {
        var ifNode = new IfNode();
        
        SkipWhitespace();
        
        // #1: 检查条件括号是否存在
        if (Peek() != '(')
        {
            throw new FormatException($"Missing condition parentheses for '@if' at position {_position}, expected '('");
        }
        
        Read(); // 读 '('
        var (condition, foundClose) = ReadUntilWithCheck(')');
        ifNode.Condition = condition;
        
        // #1: 检查条件括号是否闭合
        if (!foundClose)
        {
            throw new FormatException($"Unclosed if condition at position {startPos}, expected ')'");
        }
        Read(); // 读 ')'
        
        SkipWhitespace();
        
        // #2: 检查块花括号是否存在
        if (Peek() != '{')
        {
            throw new FormatException($"Missing block braces for '@if' at position {_position}, expected '{{'");
        }
        
        Read(); // 读 '{'
        var (body, foundBraceClose) = ReadBlockWithCheck();
        
        // #2: 检查块花括号是否闭合
        if (!foundBraceClose)
        {
            throw new FormatException($"Unclosed if block at position {startPos}, expected '}}'");
        }
        
        ifNode.ThenBranch = new EclipseMarkupParser(body).Parse();
        
        SkipWhitespace();
        
        if (Match("@else"))
        {
            _position += 5; // 跳过 "@else"
            SkipWhitespace();
            
            if (Peek() != '{')
            {
                throw new FormatException($"Missing block braces for '@else' at position {_position}, expected '{{'");
            }
            
            Read(); // 读 '{'
            var (elseBody, elseFoundClose) = ReadBlockWithCheck();
            
            if (!elseFoundClose)
            {
                throw new FormatException($"Unclosed else block at position {_position}, expected '}}'");
            }
            
            ifNode.ElseBranch = new EclipseMarkupParser(elseBody).Parse();
        }
        
        return ifNode;
    }

    private ForeachNode ParseForeachStatement(int startPos)
    {
        var foreachNode = new ForeachNode();
        
        SkipWhitespace();
        
        // #3: 检查条件括号是否存在
        if (Peek() != '(')
        {
            throw new FormatException($"Missing condition parentheses for '@foreach' at position {_position}, expected '('");
        }
        
        Read(); // 读 '('
        var (content, foundClose) = ReadUntilWithCheck(')');
        
        // #3: 检查条件括号是否闭合
        if (!foundClose)
        {
            throw new FormatException($"Unclosed foreach condition at position {startPos}, expected ')'");
        }
        Read(); // 读 ')'
        
        // #5: 检查是否包含 'in' 关键字
        if (!content.Contains(" in "))
        {
            throw new FormatException($"Invalid foreach syntax at position {startPos}: expected 'var item in collection' format");
        }
        
        var parts = content.Split(new[] { " in " }, StringSplitOptions.None);
        if (parts.Length != 2)
        {
            throw new FormatException($"Invalid foreach syntax at position {startPos}: expected 'var item in collection' format");
        }
        
        var varPart = parts[0].Trim();
        if (varPart.StartsWith("var "))
            foreachNode.ItemVar = varPart.Substring(4).Trim();
        else if (varPart.StartsWith("var"))
        {
            // "var" 后面没有空格
            throw new FormatException($"Invalid foreach syntax at position {startPos}: expected space after 'var'");
        }
        else
            foreachNode.ItemVar = varPart;
        
        // 检查集合表达式是否为空
        foreachNode.Collection = parts[1].Trim();
        if (string.IsNullOrEmpty(foreachNode.Collection))
        {
            throw new FormatException($"Empty collection expression in '@foreach' at position {startPos}");
        }
        
        SkipWhitespace();
        
        // #4: 检查块花括号是否存在
        if (Peek() != '{')
        {
            throw new FormatException($"Missing block braces for '@foreach' at position {_position}, expected '{{'");
        }
        
        Read(); // 读 '{'
        var (body, foundBraceClose) = ReadBlockWithCheck();
        
        // #4: 检查块花括号是否闭合
        if (!foundBraceClose)
        {
            throw new FormatException($"Unclosed foreach block at position {startPos}, expected '}}'");
        }
        
        foreachNode.Body = new EclipseMarkupParser(body).Parse();
        
        return foreachNode;
    }

    private ExpressionNode ParseParenthesizedExpression(int startPos)
    {
        Read(); // 读 '('
        var (expr, foundClose) = ReadUntilWithCheck(')');
        
        // #6: 检查括号是否闭合
        if (!foundClose)
        {
            throw new FormatException($"Unclosed expression at position {startPos}, expected ')'");
        }
        Read(); // 读 ')'
        
        if (string.IsNullOrWhiteSpace(expr))
        {
            throw new FormatException($"Empty expression '@()' at position {startPos}");
        }
        
        return new ExpressionNode { Expression = expr };
    }

    private TextNode ParseText()
    {
        var text = new StringBuilder();
        
        while (!IsAtEnd() && Peek() != '<' && Peek() != '@')
        {
            text.Append(Read());
        }
        
        return new TextNode { Text = text.ToString() };
    }

    private string ReadIdentifier()
    {
        var sb = new StringBuilder();
        
        while (!IsAtEnd() && (char.IsLetterOrDigit(Peek()) || Peek() == '_'))
        {
            sb.Append(Read());
        }
        
        return sb.ToString();
    }

    private (string text, bool isBinding) ReadAttributeValue()
    {
        SkipWhitespace();
        var quote = Peek();
        var startPos = _position;
        
        if (quote == '"')
        {
            Read();
            var sb = new StringBuilder();
            var isBinding = false;
            
            while (!IsAtEnd() && Peek() != '"')
            {
                if (Peek() == '@')
                {
                    Read();
                    isBinding = true;
                    if (Peek() == '(')
                    {
                        Read();
                        sb.Append('(');
                        var (expr, foundClose) = ReadUntilWithCheck(')');
                        sb.Append(expr);
                        if (!foundClose)
                        {
                            throw new FormatException($"Unclosed binding expression at position {startPos}, expected ')'");
                        }
                        Read();
                        sb.Append(')');
                    }
                    else
                    {
                        sb.Append(ReadIdentifier());
                    }
                }
                else if (Peek() == '$' && Peek(1) == '"')
                {
                    Read(); Read();
                    sb.Append("$\"");
                    var depth = 1;
                    while (!IsAtEnd() && depth > 0)
                    {
                        var ch = Read();
                        sb.Append(ch);
                        if (ch == '"') depth--;
                        else if (ch == '{') 
                        {
                            // 插值字符串中的表达式
                            while (!IsAtEnd() && Peek() != '}')
                            {
                                sb.Append(Read());
                            }
                            if (Peek() == '}')
                            {
                                sb.Append(Read());
                            }
                            else
                            {
                                throw new FormatException($"Unclosed interpolation expression at position {startPos}, expected '}}'");
                            }
                        }
                    }
                    // #7: 检查插值字符串是否闭合
                    if (depth > 0)
                    {
                        throw new FormatException($"Unclosed interpolated string at position {startPos}, expected '\"'");
                    }
                }
                else
                {
                    sb.Append(Read());
                }
            }
            
            // #7: 检查字符串字面量是否闭合
            if (IsAtEnd())
            {
                throw new FormatException($"Unclosed string literal at position {startPos}, expected '\"'");
            }
            Read(); // 读闭合的 '"'
            
            if (isBinding)
                return (sb.ToString(), true);
            else
                return ("\"" + EscapeStringLiteral(sb.ToString()) + "\"", false);
        }
        else if (quote == '\'')
        {
            Read();
            var sb = new StringBuilder("'");
            while (!IsAtEnd() && Peek() != '\'')
            {
                sb.Append(Read());
            }
            
            // #7: 检查单引号字符串是否闭合
            if (IsAtEnd())
            {
                throw new FormatException($"Unclosed character literal at position {startPos}, expected \"'\"");
            }
            Read();
            sb.Append("'");
            return (sb.ToString(), false);
        }
        else if (quote == '@')
        {
            Read();
            return ReadAtExpression(startPos);
        }
        else if (char.IsDigit(quote) || quote == '-' || quote == '+')
        {
            var sb = new StringBuilder();
            
            if (quote == '-' || quote == '+')
            {
                sb.Append(Read());
            }
            
            while (!IsAtEnd() && (char.IsDigit(Peek()) || Peek() == '.'))
            {
                sb.Append(Read());
            }
            
            // 检查数字格式是否有效
            var numStr = sb.ToString();
            if (numStr == "-" || numStr == "+")
            {
                throw new FormatException($"Incomplete number literal at position {startPos}");
            }
            
            return (sb.ToString(), false);
        }
        else if (char.IsLetter(quote) || quote == '_')
        {
            var sb = new StringBuilder();
            sb.Append(ReadIdentifier());
            
            if (string.IsNullOrEmpty(sb.ToString()))
            {
                throw new FormatException($"Invalid attribute value at position {startPos}");
            }
            
            // 支持枚举/常量的连接 (如 Color + Red)
            while (!IsAtEnd() && Peek() == '+')
            {
                sb.Append(Read());
                var nextIdent = ReadIdentifier();
                if (string.IsNullOrEmpty(nextIdent))
                {
                    throw new FormatException($"Invalid expression after '+' at position {_position}");
                }
                sb.Append(nextIdent);
            }
            
            return (sb.ToString(), false);
        }
        else if (quote == 't' && Match("true"))
        {
            _position += 4;
            return ("true", false);
        }
        else if (quote == 'f' && Match("false"))
        {
            _position += 5;
            return ("false", false);
        }
        else if (quote == 'n' && Match("null"))
        {
            _position += 4;
            return ("null", false);
        }
        
        throw new FormatException($"Invalid attribute value starting with '{quote}' at position {startPos}");
    }
    
    private (string text, bool isBinding) ReadAtExpression(int startPos)
    {
        var next = Peek();
        
        if (next == '$' && Peek(1) == '"')
        {
            return ReadInterpolatedString(startPos);
        }
        else if (next == '(')
        {
            var exprNode = ParseParenthesizedExpression(_position);
            return (exprNode.Expression, true);
        }
        else if (char.IsLetter(next) || next == '_')
        {
            var name = ReadIdentifier();
            
            if (string.IsNullOrEmpty(name))
            {
                throw new FormatException($"Invalid binding expression at position {startPos}");
            }
            
            while (!IsAtEnd() && Peek() == '.')
            {
                Read();
                var member = ReadIdentifier();
                if (string.IsNullOrEmpty(member))
                {
                    throw new FormatException($"Invalid member access at position {_position}, expected identifier after '.'");
                }
                name += "." + member;
            }
            
            return (name, true);
        }
        else
        {
            throw new FormatException($"Invalid expression starting with '@{next}' at position {startPos}");
        }
    }
    
    private (string text, bool isBinding) ReadInterpolatedString(int startPos)
    {
        Read(); Read();
        
        var sb = new StringBuilder("$\"");
        var depth = 1;
        
        while (!IsAtEnd() && depth > 0)
        {
            var ch = Peek();
            
            if (ch == '"')
            {
                Read();
                sb.Append('"');
                depth--;
            }
            else if (ch == '{')
            {
                sb.Append(Read());
                var braceDepth = 1;
                while (!IsAtEnd() && braceDepth > 0)
                {
                    var c = Read();
                    sb.Append(c);
                    if (c == '{') braceDepth++;
                    else if (c == '}') braceDepth--;
                }
                if (braceDepth > 0)
                {
                    throw new FormatException($"Unclosed interpolation in interpolated string at position {startPos}, expected '}}'");
                }
            }
            else
            {
                sb.Append(Read());
            }
        }
        
        // 检查插值字符串是否闭合
        if (depth > 0)
        {
            throw new FormatException($"Unclosed interpolated string at position {startPos}, expected '\"'");
        }
        
        return (sb.ToString(), true);
    }

    /// <summary>
    /// 读取块内容，返回内容和是否找到闭合花括号
    /// </summary>
    private (string content, bool foundClose) ReadBlockWithCheck()
    {
        var sb = new StringBuilder();
        var depth = 1;
        
        while (!IsAtEnd() && depth > 0)
        {
            var ch = Read();
            if (ch == '{') depth++;
            else if (ch == '}') depth--;
            
            if (depth > 0)
                sb.Append(ch);
        }
        
        return (sb.ToString(), depth == 0);
    }

    /// <summary>
    /// 读取直到指定字符，返回内容和是否找到该字符
    /// </summary>
    private (string content, bool foundClose) ReadUntilWithCheck(char end)
    {
        var sb = new StringBuilder();
        var depth = 1;
        
        while (!IsAtEnd())
        {
            var ch = Peek();
            if (ch == '(') depth++;
            else if (ch == end)
            {
                depth--;
                if (depth == 0)
                    break;
            }
            sb.Append(Read());
        }
        
        return (sb.ToString(), depth == 0);
    }

    private void SkipWhitespace()
    {
        while (!IsAtEnd() && char.IsWhiteSpace(Peek()))
            Read();
    }

    private bool Match(string text)
    {
        if (_position + text.Length > _source.Length)
            return false;
        
        return _source.Substring(_position, text.Length) == text;
    }

    private char Peek(int offset = 0)
    {
        var pos = _position + offset;
        return pos < _source.Length ? _source[pos] : '\0';
    }

    private char Read()
    {
        return _position < _source.Length ? _source[_position++] : '\0';
    }

    private bool IsAtEnd() => _position >= _source.Length;

    private string EscapeStringLiteral(string text)
    {
        return text
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"");
    }
}