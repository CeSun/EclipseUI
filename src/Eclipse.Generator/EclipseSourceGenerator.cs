using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using Eclipse.Core.Abstractions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Eclipse.Generator
{
    [Generator]
    public class EclipseSourceGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var eclFiles = context.AdditionalTextsProvider
                .Where(file => file.Path.EndsWith(".eui", StringComparison.OrdinalIgnoreCase))
                .Select((file, cancellationToken) => (
                    Path: file.Path,
                    Content: file.GetText(cancellationToken)?.ToString() ?? string.Empty
                ));

            context.RegisterSourceOutput(eclFiles, GenerateSource);
        }

        private void GenerateSource(SourceProductionContext context, (string Path, string Content) eclFile)
        {
            var (path, content) = eclFile;
            
            try
            {
                var className = Path.GetFileNameWithoutExtension(path);
                var @namespace = InferNamespace(path);
                var parsed = ParseEcl(content);
                var generatedCode = GenerateComponentCode(@namespace, className, parsed);
                var hintName = $"{className}.ecl.g.cs";
                context.AddSource(hintName, SourceText.From(generatedCode, Encoding.UTF8));
            }
            catch (Exception ex)
            {
                var descriptor = new DiagnosticDescriptor(
                    "ECGEN001",
                    "Eclipse component generation failed",
                    "Failed to generate component for '{0}': {1}",
                    "Eclipse",
                    DiagnosticSeverity.Error,
                    true);

                context.ReportDiagnostic(Diagnostic.Create(descriptor, Location.None, path, ex.Message));
            }
        }

        private string InferNamespace(string filePath)
        {
            var dir = Path.GetDirectoryName(filePath) ?? "";
            var segments = dir.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            var startIndex = Array.FindIndex(segments, s => 
                s.Equals("Components", StringComparison.OrdinalIgnoreCase) ||
                s.Equals("Pages", StringComparison.OrdinalIgnoreCase) ||
                s.Equals("Shared", StringComparison.OrdinalIgnoreCase) ||
                s.Equals("Views", StringComparison.OrdinalIgnoreCase));
            
            if (startIndex >= 0 && startIndex < segments.Length - 1)
            {
                return string.Join(".", segments.Skip(startIndex));
            }
            
            return "Eclipse.Generated";
        }

        private ParsedEcl ParseEcl(string content)
        {
            var result = new ParsedEcl();
            ExtractDirectives(content, result);
            result.CodeBlock = ExtractCodeBlock(content);
            result.Markup = ExtractMarkup(content);
            return result;
        }

        private void ExtractDirectives(string content, ParsedEcl result)
        {
            var lines = content.Split('\n');
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("@using "))
                    result.Usings.Add(trimmed.Substring(7).Trim());
                else if (trimmed.StartsWith("@inject "))
                {
                    var inject = trimmed.Substring(8).Trim();
                    var parts = inject.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        var type = string.Join(" ", parts.Take(parts.Length - 1));
                        var name = parts.Last();
                        result.Injections.Add((type, name));
                    }
                }
                else if (trimmed.StartsWith("@inherits "))
                    result.BaseClass = trimmed.Substring(9).Trim();
                else if (trimmed.StartsWith("@attribute "))
                    result.Attributes.Add(trimmed.Substring(11).Trim());
            }
        }

        private string ExtractCodeBlock(string content)
        {
            var codeIndex = content.IndexOf("@code");
            if (codeIndex < 0) return string.Empty;
            
            var blockStart = content.IndexOf('{', codeIndex);
            if (blockStart < 0)
            {
                throw new FormatException($"Missing block braces for '@code' at position {codeIndex}, expected '{{'");
            }
            
            var depth = 1;
            var i = blockStart + 1;
            while (i < content.Length && depth > 0)
            {
                if (content[i] == '{') depth++;
                else if (content[i] == '}') depth--;
                i++;
            }
            
            // #8: 检查 @code 块是否闭合
            if (depth > 0)
            {
                throw new FormatException($"Unclosed @code block at position {codeIndex}, expected '}}'");
            }
            
            return content.Substring(blockStart + 1, i - blockStart - 2).Trim();
        }

        private string ExtractMarkup(string content)
        {
            // 先找到 @code 块的位置
            var codeIndex = content.IndexOf("@code");
            
            if (codeIndex < 0)
            {
                // 没有 @code 块，整个内容（去掉 directives）就是 markup
                return RemoveDirectives(content);
            }
            
            // 找到 @code 块的结束位置
            var blockStart = content.IndexOf('{', codeIndex);
            if (blockStart < 0)
            {
                // @code 后面没有 {，这种情况取 @code 之前的内容
                return RemoveDirectives(content.Substring(0, codeIndex));
            }
            
            var depth = 1;
            var i = blockStart + 1;
            while (i < content.Length && depth > 0)
            {
                if (content[i] == '{') depth++;
                else if (content[i] == '}') depth--;
                i++;
            }
            
            // @code 块结束位置是 i
            // 提取 @code 块之前和之后的内容
            var beforeCode = content.Substring(0, codeIndex);
            var afterCode = content.Substring(i);
            
            // 合并并去掉 directives
            var combined = beforeCode + afterCode;
            return RemoveDirectives(combined);
        }
        
        private string RemoveDirectives(string content)
        {
            var lines = content.Split('\n');
            var markupLines = new List<string>();
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                    continue;
                if (trimmed.StartsWith("@using ") ||
                    trimmed.StartsWith("@inject ") ||
                    trimmed.StartsWith("@inherits ") ||
                    trimmed.StartsWith("@attribute ") ||
                    trimmed.StartsWith("@layout "))
                    continue;
                markupLines.Add(line);
            }
            return string.Join("\n", markupLines).Trim();
        }

        private string GenerateComponentCode(string @namespace, string className, ParsedEcl parsed)
        {
            var sb = new StringBuilder();
            var indent = 0;
            void WriteLine(string line = "") => sb.AppendLine(new string(' ', indent * 4) + line);
            
            sb.AppendLine("// <auto-generated />");
            sb.AppendLine($"// Generated from {className}.ecl");
            sb.AppendLine();
            WriteLine("using System;");
            WriteLine("using Eclipse.Core;");
            WriteLine("using Eclipse.Core.Abstractions;");
            foreach (var @using in parsed.Usings)
                WriteLine($"using {@using};");
            sb.AppendLine();
            WriteLine($"namespace {@namespace}");
            WriteLine("{");
            indent++;
            foreach (var attr in parsed.Attributes)
                WriteLine($"[{attr}]");
            var baseClass = !string.IsNullOrEmpty(parsed.BaseClass) ? parsed.BaseClass : "ComponentBase";
            WriteLine($"public partial class {className} : {baseClass}");
            WriteLine("{");
            indent++;
            foreach (var (type, name) in parsed.Injections)
            {
                WriteLine("[Inject]");
                WriteLine($"public {type} {name} {{ get; set; }} = null!;");
                WriteLine();
            }
            if (!string.IsNullOrEmpty(parsed.CodeBlock))
            {
                foreach (var line in parsed.CodeBlock.Split('\n'))
                    WriteLine(line);
                WriteLine();
            }
            WriteLine("public override void Render(IRenderContext context)");
            WriteLine("{");
            indent++;
            GenerateRenderBody(parsed.Markup, sb, ref indent, WriteLine);
            indent--;
            WriteLine("}");
            indent--;
            WriteLine("}");
            indent--;
            WriteLine("}");
            return sb.ToString();
        }

        private void GenerateRenderBody(string markup, StringBuilder sb, ref int indent, Action<string> WriteLine)
        {
            if (string.IsNullOrWhiteSpace(markup))
            {
                WriteLine("// Empty markup");
                return;
            }
            var parser = new EclipseMarkupParser(markup);
            var nodes = parser.Parse();
            var seq = 0;
            GenerateNodes(nodes, sb, ref indent, WriteLine, ref seq);
        }

        private void GenerateNodes(List<MarkupNode> nodes, StringBuilder sb, ref int indent, Action<string> WriteLine, ref int seq)
        {
            foreach (var node in nodes)
            {
                switch (node)
                {
                    case ControlNode control:
                        GenerateControl(control, sb, ref indent, WriteLine, ref seq);
                        break;
                    case TextNode text:
                        if (!string.IsNullOrWhiteSpace(text.Text))
                            WriteLine($"context.SetText(\"{EscapeString(text.Text)}\");");
                        break;
                    case ExpressionNode expr:
                        // 顶层表达式作为文本输出
                        WriteLine($"context.SetText({expr.Expression}?.ToString());");
                        break;
                    case IfNode ifNode:
                        GenerateIf(ifNode, sb, ref indent, WriteLine, ref seq);
                        break;
                    case ForeachNode foreachNode:
                        GenerateForeach(foreachNode, sb, ref indent, WriteLine, ref seq);
                        break;
                }
            }
        }

        private void GenerateControl(ControlNode control, StringBuilder sb, ref int indent, Action<string> WriteLine, ref int seq)
        {
            var controlId = $"new ComponentId({++seq})";
            var varName = $"__{control.TagName.ToLower()}_{seq}";
            
            WriteLine($"using (context.BeginComponent<{control.TagName}>({controlId}, out var {varName}))");
            WriteLine("{");
            indent++;
            
            foreach (var attr in control.Attributes)
            {
                if (attr.IsEvent)
                {
                    // 事件绑定：OnClick=@Method
                    WriteLine($"{varName}.{attr.Name} += {attr.Value};");
                }
                else
                {
                    // 属性赋值（包括绑定和字面量）
                    WriteLine($"{varName}.{attr.Name} = {attr.Value};");
                }
            }
            
            if (control.Children.Count > 0)
            {
                WriteLine("");
                WriteLine("using (context.BeginChildContent())");
                WriteLine("{");
                indent++;
                GenerateNodes(control.Children, sb, ref indent, WriteLine, ref seq);
                indent--;
                WriteLine("}");
            }
            indent--;
            WriteLine("}");
        }

        private void GenerateIf(IfNode ifNode, StringBuilder sb, ref int indent, Action<string> WriteLine, ref int seq)
        {
            WriteLine($"if ({ifNode.Condition})");
            WriteLine("{");
            indent++;
            GenerateNodes(ifNode.ThenBranch, sb, ref indent, WriteLine, ref seq);
            indent--;
            WriteLine("}");
            if (ifNode.ElseBranch != null && ifNode.ElseBranch.Count > 0)
            {
                WriteLine("else");
                WriteLine("{");
                indent++;
                GenerateNodes(ifNode.ElseBranch, sb, ref indent, WriteLine, ref seq);
                indent--;
                WriteLine("}");
            }
        }

        private void GenerateForeach(ForeachNode foreachNode, StringBuilder sb, ref int indent, Action<string> WriteLine, ref int seq)
        {
            WriteLine($"foreach (var {foreachNode.ItemVar} in {foreachNode.Collection})");
            WriteLine("{");
            indent++;
            GenerateNodes(foreachNode.Body, sb, ref indent, WriteLine, ref seq);
            indent--;
            WriteLine("}");
        }

        private string EscapeString(string text)
        {
            return text
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }

        private class ParsedEcl
        {
            public List<string> Usings { get; } = new();
            public List<(string Type, string Name)> Injections { get; } = new();
            public string? BaseClass { get; set; }
            public List<string> Attributes { get; } = new();
            public string CodeBlock { get; set; } = string.Empty;
            public string Markup { get; set; } = string.Empty;
        }
    }
}