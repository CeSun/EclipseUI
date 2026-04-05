using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Eclipse.Generator
{
    [Generator]
    public class EclipseSourceGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // 获取 EUI 文件
            var euiFiles = context.AdditionalTextsProvider
                .Where(file => file.Path.EndsWith(".eui", StringComparison.OrdinalIgnoreCase))
                .Select((file, cancellationToken) => (
                    Path: file.Path,
                    Content: file.GetText(cancellationToken)?.ToString() ?? string.Empty
                ));

            // 结合 Compilation 和 EUI 文件
            var compilationAndFiles = context.CompilationProvider.Combine(euiFiles.Collect());

            context.RegisterSourceOutput(compilationAndFiles, (spc, source) =>
            {
                var (compilation, files) = source;
                foreach (var file in files)
                {
                    GenerateSource(spc, compilation, file.Path, file.Content);
                }
            });
        }

        private void GenerateSource(SourceProductionContext context, Compilation compilation, string path, string content)
        {
            try
            {
                var className = Path.GetFileNameWithoutExtension(path);
                var @namespace = InferNamespace(path);
                var parsed = ParseEui(content);
                
                // 收集所有用到的控件类型
                var controlTypes = CollectControlTypes(parsed.Markup);
                
                // 获取这些类型的属性信息
                var propertyTypes = GetPropertyTypes(compilation, controlTypes);
                
                var generatedCode = GenerateComponentCode(@namespace, className, parsed, propertyTypes);
                var hintName = $"{className}.eui.g.cs";
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

        /// <summary>
        /// 从 markup 中收集所有用到的控件类型名
        /// </summary>
        private HashSet<string> CollectControlTypes(string markup)
        {
            var types = new HashSet<string>();
            if (string.IsNullOrWhiteSpace(markup)) return types;
            
            try
            {
                var parser = new EclipseMarkupParser(markup);
                var nodes = parser.Parse();
                CollectControlTypesFromNodes(nodes, types);
            }
            catch
            {
                // 忽略解析错误，会在后续处理
            }
            
            return types;
        }

        private void CollectControlTypesFromNodes(List<MarkupNode> nodes, HashSet<string> types)
        {
            foreach (var node in nodes)
            {
                if (node is ControlNode control)
                {
                    types.Add(control.TagName);
                    CollectControlTypesFromNodes(control.Children, types);
                }
                else if (node is IfNode ifNode)
                {
                    CollectControlTypesFromNodes(ifNode.ThenBranch, types);
                    if (ifNode.ElseBranch != null)
                        CollectControlTypesFromNodes(ifNode.ElseBranch, types);
                }
                else if (node is ForeachNode foreachNode)
                {
                    CollectControlTypesFromNodes(foreachNode.Body, types);
                }
            }
        }

        /// <summary>
        /// 使用 Roslyn 获取控件类型的属性类型信息
        /// </summary>
        private Dictionary<(string Control, string Property), PropertyTypeInfo> GetPropertyTypes(
            Compilation compilation, HashSet<string> controlTypes)
        {
            var result = new Dictionary<(string, string), PropertyTypeInfo>();
            
            foreach (var typeName in controlTypes)
            {
                // 尝试找到类型
                var typeSymbol = FindTypeSymbol(compilation, typeName);
                if (typeSymbol == null) continue;
                
                // 遍历所有属性
                foreach (var member in typeSymbol.GetMembers())
                {
                    if (member is IPropertySymbol property && !property.IsStatic && property.SetMethod != null)
                    {
                        var propType = property.Type;
                        var info = new PropertyTypeInfo
                        {
                            TypeName = propType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                            IsEnum = propType.TypeKind == TypeKind.Enum,
                            IsNumeric = IsNumericType(propType),
                            IsBoolean = propType.SpecialType == SpecialType.System_Boolean,
                            IsString = propType.SpecialType == SpecialType.System_String,
                            EnumTypeName = propType.TypeKind == TypeKind.Enum 
                                ? propType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat) 
                                : null
                        };
                        
                        result[(typeName, property.Name)] = info;
                    }
                }
            }
            
            return result;
        }

        private INamedTypeSymbol? FindTypeSymbol(Compilation compilation, string typeName)
        {
            // 尝试多种可能的完全限定名
            var possibleNames = new[]
            {
                $"Eclipse.Controls.{typeName}",
                $"Eclipse.Core.{typeName}",
                typeName
            };
            
            foreach (var name in possibleNames)
            {
                var symbol = compilation.GetTypeByMetadataName(name);
                if (symbol != null) return symbol;
            }
            
            // 全局搜索
            foreach (var reference in compilation.References)
            {
                var assembly = compilation.GetAssemblyOrModuleSymbol(reference) as IAssemblySymbol;
                if (assembly == null) continue;
                
                var type = FindTypeInAssembly(assembly, typeName);
                if (type != null) return type;
            }
            
            return null;
        }

        private INamedTypeSymbol? FindTypeInAssembly(IAssemblySymbol assembly, string typeName)
        {
            foreach (var ns in assembly.GlobalNamespace.GetNamespaceMembers())
            {
                var type = FindTypeInNamespace(ns, typeName);
                if (type != null) return type;
            }
            return null;
        }

        private INamedTypeSymbol? FindTypeInNamespace(INamespaceSymbol ns, string typeName)
        {
            foreach (var type in ns.GetTypeMembers())
            {
                if (type.Name == typeName) return type;
            }
            
            foreach (var childNs in ns.GetNamespaceMembers())
            {
                var found = FindTypeInNamespace(childNs, typeName);
                if (found != null) return found;
            }
            
            return null;
        }

        private bool IsNumericType(ITypeSymbol type)
        {
            return type.SpecialType switch
            {
                SpecialType.System_Byte => true,
                SpecialType.System_SByte => true,
                SpecialType.System_Int16 => true,
                SpecialType.System_UInt16 => true,
                SpecialType.System_Int32 => true,
                SpecialType.System_UInt32 => true,
                SpecialType.System_Int64 => true,
                SpecialType.System_UInt64 => true,
                SpecialType.System_Single => true,
                SpecialType.System_Double => true,
                SpecialType.System_Decimal => true,
                _ => false
            };
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
            
            if (startIndex >= 0)
            {
                return string.Join(".", segments.Skip(startIndex));
            }
            
            return "Eclipse.Generated";
        }

        private ParsedEui ParseEui(string content)
        {
            var result = new ParsedEui();
            ExtractDirectives(content, result);
            result.CodeBlock = ExtractCodeBlock(content);
            result.Markup = ExtractMarkup(content);
            return result;
        }

        private void ExtractDirectives(string content, ParsedEui result)
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
            
            if (depth > 0)
            {
                throw new FormatException($"Unclosed @code block at position {codeIndex}, expected '}}'");
            }
            
            return content.Substring(blockStart + 1, i - blockStart - 2).Trim();
        }

        private string ExtractMarkup(string content)
        {
            var codeIndex = content.IndexOf("@code");
            
            if (codeIndex < 0)
            {
                return RemoveDirectives(content);
            }
            
            var blockStart = content.IndexOf('{', codeIndex);
            if (blockStart < 0)
            {
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
            
            var beforeCode = content.Substring(0, codeIndex);
            var afterCode = content.Substring(i);
            
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

        private string GenerateComponentCode(string @namespace, string className, ParsedEui parsed, 
            Dictionary<(string Control, string Property), PropertyTypeInfo> propertyTypes)
        {
            var sb = new StringBuilder();
            var indent = 0;
            void WriteLine(string line = "") => sb.AppendLine(new string(' ', indent * 4) + line);
            
            sb.AppendLine("// <auto-generated />");
            sb.AppendLine($"// Generated from {className}.eui");
            sb.AppendLine("#nullable enable");
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
            WriteLine("public override void Build(IBuildContext context)");
            WriteLine("{");
            indent++;
            GenerateBuildBody(parsed.Markup, sb, ref indent, WriteLine, propertyTypes);
            indent--;
            WriteLine("}");
            indent--;
            WriteLine("}");
            indent--;
            WriteLine("}");
            return sb.ToString();
        }

        private void GenerateBuildBody(string markup, StringBuilder sb, ref int indent, Action<string> WriteLine,
            Dictionary<(string Control, string Property), PropertyTypeInfo> propertyTypes)
        {
            if (string.IsNullOrWhiteSpace(markup))
            {
                WriteLine("// Empty markup");
                return;
            }
            var parser = new EclipseMarkupParser(markup);
            var nodes = parser.Parse();
            var seq = 0;
            GenerateNodes(nodes, sb, ref indent, WriteLine, ref seq, propertyTypes);
        }

        private void GenerateNodes(List<MarkupNode> nodes, StringBuilder sb, ref int indent, Action<string> WriteLine, 
            ref int seq, Dictionary<(string Control, string Property), PropertyTypeInfo> propertyTypes)
        {
            foreach (var node in nodes)
            {
                switch (node)
                {
                    case ControlNode control:
                        GenerateControl(control, sb, ref indent, WriteLine, ref seq, propertyTypes);
                        break;
                    case TextNode text:
                        if (!string.IsNullOrWhiteSpace(text.Text))
                        {
                            var textVar = $"__textcontent_{++seq}";
                            WriteLine($"using (context.BeginComponent<TextContent>(new ComponentId({seq}), out var {textVar}))");
                            WriteLine("{");
                            WriteLine($"{textVar}.Text = \"{EscapeString(text.Text)}\";");
                            WriteLine("}");
                        }
                        break;
                    case ExpressionNode expr:
                        var exprVar = $"__textcontent_{++seq}";
                        WriteLine($"using (context.BeginComponent<TextContent>(new ComponentId({seq}), out var {exprVar}))");
                        WriteLine("{");
                        WriteLine($"{exprVar}.Text = {expr.Expression}?.ToString();");
                        WriteLine("}");
                        break;
                    case IfNode ifNode:
                        GenerateIf(ifNode, sb, ref indent, WriteLine, ref seq, propertyTypes);
                        break;
                    case ForeachNode foreachNode:
                        GenerateForeach(foreachNode, sb, ref indent, WriteLine, ref seq, propertyTypes);
                        break;
                }
            }
        }

        private void GenerateControl(ControlNode control, StringBuilder sb, ref int indent, Action<string> WriteLine, 
            ref int seq, Dictionary<(string Control, string Property), PropertyTypeInfo> propertyTypes)
        {
            var controlId = $"new ComponentId({++seq})";
            var varName = $"__{control.TagName.ToLower()}_{seq}";
            
            var typeName = GetFullTypeName(control.TagName);
            
            WriteLine($"using (context.BeginComponent<{typeName}>({controlId}, out var {varName}))");
            WriteLine("{");
            indent++;
            
            foreach (var attr in control.Attributes)
            {
                if (attr.IsEvent)
                {
                    WriteLine($"{varName}.{attr.Name} += {attr.Value};");
                }
                else if (attr.IsBinding)
                {
                    WriteLine($"{varName}.{attr.Name} = {attr.Value};");
                }
                else
                {
                    var propTypeInfo = propertyTypes.TryGetValue((control.TagName, attr.Name), out var info) 
                        ? info : null;
                    var value = ConvertLiteralValue(attr.Value, propTypeInfo);
                    WriteLine($"{varName}.{attr.Name} = {value};");
                }
            }
            
            if (control.Children.Count > 0)
            {
                WriteLine("");
                WriteLine("using (context.BeginChildContent())");
                WriteLine("{");
                indent++;
                GenerateNodes(control.Children, sb, ref indent, WriteLine, ref seq, propertyTypes);
                indent--;
                WriteLine("}");
            }
            indent--;
            WriteLine("}");
        }

        private void GenerateIf(IfNode ifNode, StringBuilder sb, ref int indent, Action<string> WriteLine, 
            ref int seq, Dictionary<(string Control, string Property), PropertyTypeInfo> propertyTypes)
        {
            WriteLine($"if ({ifNode.Condition})");
            WriteLine("{");
            indent++;
            GenerateNodes(ifNode.ThenBranch, sb, ref indent, WriteLine, ref seq, propertyTypes);
            indent--;
            WriteLine("}");
            if (ifNode.ElseBranch != null && ifNode.ElseBranch.Count > 0)
            {
                WriteLine("else");
                WriteLine("{");
                indent++;
                GenerateNodes(ifNode.ElseBranch, sb, ref indent, WriteLine, ref seq, propertyTypes);
                indent--;
                WriteLine("}");
            }
        }

        private void GenerateForeach(ForeachNode foreachNode, StringBuilder sb, ref int indent, Action<string> WriteLine, 
            ref int seq, Dictionary<(string Control, string Property), PropertyTypeInfo> propertyTypes)
        {
            WriteLine($"foreach (var {foreachNode.ItemVar} in {foreachNode.Collection})");
            WriteLine("{");
            indent++;
            GenerateNodes(foreachNode.Body, sb, ref indent, WriteLine, ref seq, propertyTypes);
            indent--;
            WriteLine("}");
        }
        
        private string GetFullTypeName(string tagName)
        {
            return tagName;
        }
        
        /// <summary>
        /// 根据属性类型信息转换字面量值
        /// </summary>
        private string ConvertLiteralValue(string value, PropertyTypeInfo? typeInfo)
        {
            // 如果不是字符串字面量，直接返回
            if (!value.StartsWith("\"") || !value.EndsWith("\"") || value.Length < 2)
                return value;
            
            var innerValue = value.Substring(1, value.Length - 2);
            
            if (typeInfo == null)
            {
                // 未知类型，保留字符串
                return value;
            }
            
            // 数字类型：去掉引号
            if (typeInfo.IsNumeric)
            {
                if (double.TryParse(innerValue, out _))
                    return innerValue;
            }
            
            // 布尔类型：去掉引号
            if (typeInfo.IsBoolean)
            {
                if (innerValue.Equals("true", StringComparison.OrdinalIgnoreCase))
                    return "true";
                if (innerValue.Equals("false", StringComparison.OrdinalIgnoreCase))
                    return "false";
            }
            
            // 枚举类型：添加枚举类型前缀
            if (typeInfo.IsEnum && !string.IsNullOrEmpty(typeInfo.EnumTypeName))
            {
                if (!string.IsNullOrEmpty(innerValue) && char.IsLetter(innerValue[0]))
                    return $"{typeInfo.EnumTypeName}.{innerValue}";
            }
            
            // 其他情况保留字符串字面量
            return value;
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

        private class PropertyTypeInfo
        {
            public string TypeName { get; set; } = "";
            public bool IsEnum { get; set; }
            public bool IsNumeric { get; set; }
            public bool IsBoolean { get; set; }
            public bool IsString { get; set; }
            public string? EnumTypeName { get; set; }
        }

        private class ParsedEui
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