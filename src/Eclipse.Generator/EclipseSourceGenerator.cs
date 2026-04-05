using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
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
                .Where(file => file.Path.EndsWith(".eui", StringComparison.OrdinalIgnoreCase));

            // 解析 EUI 文件（增量计算，带缓存）
            var parsedFiles = euiFiles
                .Select((file, cancellationToken) => new ParsedEuiFile
                {
                    AdditionalText = file,
                    Path = file.Path,
                    ClassName = Path.GetFileNameWithoutExtension(file.Path),
                    Content = file.GetText(cancellationToken)?.ToString() ?? string.Empty,
                    Parsed = ParseEui(file.GetText(cancellationToken)?.ToString() ?? string.Empty)
                })
                .WithComparer(new ParsedEuiFileComparer());

            // 结合 Compilation、配置和解析后的文件
            var withCompilation = parsedFiles.Combine(context.CompilationProvider);
            var withConfig = withCompilation.Combine(context.AnalyzerConfigOptionsProvider);
            
            context.RegisterSourceOutput(withConfig, (spc, source) =>
            {
                var ((file, compilation), optionsProvider) = source;
                
                // 类型查找缓存（在 compilation 级别共享）
                var typeCache = new TypeLookupCache(compilation);
                
                GenerateSource(spc, optionsProvider, typeCache, file);
            });
        }

        private void GenerateSource(SourceProductionContext context, 
            AnalyzerConfigOptionsProvider optionsProvider, 
            TypeLookupCache typeCache, 
            ParsedEuiFile file)
        {
            var className = file.ClassName;
            var parsed = file.Parsed;
            
            // 优先使用 @namespace 指令，否则从 MSBuild 配置获取
            string @namespace = parsed.Namespace ?? InferNamespace(file.Path, file.AdditionalText, optionsProvider);
            if (string.IsNullOrEmpty(@namespace))
            {
                @namespace = "Eclipse.Generated";
            }
            
            // 一次性解析 markup，同时收集类型信息
            List<MarkupNode> nodes;
            HashSet<string> controlTypes;
            
            if (!string.IsNullOrWhiteSpace(parsed.Markup))
            {
                var parseResult = ParseMarkupAndCollectTypes(parsed.Markup, file.Path, context);
                if (parseResult == null)
                {
                    // 解析失败，诊断已报告
                    return;
                }
                nodes = parseResult.Value.Nodes;
                controlTypes = parseResult.Value.ControlTypes;
            }
            else
            {
                nodes = new List<MarkupNode>();
                controlTypes = new HashSet<string>();
            }
            
            // 获取这些类型的属性信息
            var propertyTypes = GetPropertyTypes(typeCache, controlTypes, parsed.Usings);
            
            var generatedCode = GenerateComponentCode(@namespace, className, parsed, nodes, propertyTypes);
            var hintName = $"{className}.eui.g.cs";
            context.AddSource(hintName, SourceText.From(generatedCode, Encoding.UTF8));
        }

        /// <summary>
        /// 一次性解析 markup 并收集控件类型（避免重复解析）
        /// </summary>
        private (List<MarkupNode> Nodes, HashSet<string> ControlTypes)? ParseMarkupAndCollectTypes(
            string markup, string filePath, SourceProductionContext context)
        {
            try
            {
                var parser = new EclipseMarkupParser(markup);
                var nodes = parser.Parse();
                var controlTypes = new HashSet<string>();
                
                CollectControlTypesFromNodes(nodes, controlTypes);
                
                return (nodes, controlTypes);
            }
            catch (Exception ex)
            {
                // 报告诊断错误
                var descriptor = new DiagnosticDescriptor(
                    "ECGEN002",
                    "EUI markup parse error",
                    "Failed to parse markup in '{0}': {1}",
                    "Eclipse",
                    DiagnosticSeverity.Error,
                    true);
                
                context.ReportDiagnostic(Diagnostic.Create(descriptor, Location.None, filePath, ex.Message));
                return null;
            }
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
        /// 使用缓存的类型查找获取属性类型信息
        /// </summary>
        private Dictionary<(string Control, string Property), PropertyTypeInfo> GetPropertyTypes(
            TypeLookupCache cache, HashSet<string> controlTypes, List<string> usings)
        {
            var result = new Dictionary<(string, string), PropertyTypeInfo>();
            
            foreach (var typeName in controlTypes)
            {
                var typeSymbol = cache.FindTypeSymbol(typeName, usings);
                if (typeSymbol == null) continue;
                
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

        private string InferNamespace(string filePath, AdditionalText additionalText, AnalyzerConfigOptionsProvider optionsProvider)
        {
            var options = optionsProvider.GetOptions(additionalText);
            
            string rootNamespace = "";
            if (options.TryGetValue("build_property.rootnamespace", out var rn) && !string.IsNullOrEmpty(rn))
            {
                rootNamespace = rn;
            }
            
            string projectDir = "";
            if (options.TryGetValue("build_property.projectdir", out var pd) && !string.IsNullOrEmpty(pd))
            {
                projectDir = pd;
            }
            
            var fileDir = Path.GetDirectoryName(filePath) ?? "";
            string relativeNs = "";
            
            if (!string.IsNullOrEmpty(projectDir))
            {
                var relativePath = GetRelativePath(projectDir, fileDir);
                if (!string.IsNullOrEmpty(relativePath))
                {
                    relativeNs = relativePath
                        .Replace('/', '.')
                        .Replace('\\', '.')
                        .Trim('.');
                }
            }
            
            if (!string.IsNullOrEmpty(rootNamespace) && !string.IsNullOrEmpty(relativeNs))
            {
                return $"{rootNamespace}.{relativeNs}";
            }
            else if (!string.IsNullOrEmpty(rootNamespace))
            {
                return rootNamespace;
            }
            else if (!string.IsNullOrEmpty(relativeNs))
            {
                return relativeNs;
            }
            
            return "Eclipse.Generated";
        }

        private string GetRelativePath(string basePath, string targetPath)
        {
            if (string.IsNullOrEmpty(basePath) || string.IsNullOrEmpty(targetPath))
                return "";
            
            try
            {
                var baseUri = new Uri(basePath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar);
                var targetUri = new Uri(targetPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar);
                
                if (!baseUri.IsBaseOf(targetUri))
                    return "";
                
                var result = Uri.UnescapeDataString(baseUri.MakeRelativeUri(targetUri).ToString()
                    .Replace('/', Path.DirectorySeparatorChar))
                    .TrimEnd(Path.DirectorySeparatorChar);
                
                return result ?? "";
            }
            catch
            {
                return "";
            }
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
                else if (trimmed.StartsWith("@namespace "))
                    result.Namespace = trimmed.Substring(11).Trim();
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
                    trimmed.StartsWith("@namespace ") ||
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
            List<MarkupNode> nodes, Dictionary<(string Control, string Property), PropertyTypeInfo> propertyTypes)
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
            var seq = 0;
            GenerateNodes(nodes, sb, ref indent, WriteLine, ref seq, propertyTypes);
            indent--;
            WriteLine("}");
            indent--;
            WriteLine("}");
            indent--;
            WriteLine("}");
            return sb.ToString();
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
            
            WriteLine($"using (context.BeginComponent<{control.TagName}>({controlId}, out var {varName}))");
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
        
        private string ConvertLiteralValue(string value, PropertyTypeInfo? typeInfo)
        {
            if (!value.StartsWith("\"") || !value.EndsWith("\"") || value.Length < 2)
                return value;
            
            var innerValue = value.Substring(1, value.Length - 2);
            
            if (typeInfo == null)
                return value;
            
            if (typeInfo.IsNumeric)
            {
                if (double.TryParse(innerValue, out _))
                    return innerValue;
            }
            
            if (typeInfo.IsBoolean)
            {
                if (innerValue.Equals("true", StringComparison.OrdinalIgnoreCase))
                    return "true";
                if (innerValue.Equals("false", StringComparison.OrdinalIgnoreCase))
                    return "false";
            }
            
            if (typeInfo.IsEnum && !string.IsNullOrEmpty(typeInfo.EnumTypeName))
            {
                if (!string.IsNullOrEmpty(innerValue) && char.IsLetter(innerValue[0]))
                    return $"{typeInfo.EnumTypeName}.{innerValue}";
            }
            
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

        // === 增量生成缓存支持 ===

        private class ParsedEuiFile
        {
            public AdditionalText AdditionalText { get; set; } = null!;
            public string Path { get; set; } = "";
            public string ClassName { get; set; } = "";
            public string Content { get; set; } = "";
            public ParsedEui Parsed { get; set; } = new();
        }

        private class ParsedEuiFileComparer : IEqualityComparer<ParsedEuiFile>
        {
            public bool Equals(ParsedEuiFile? x, ParsedEuiFile? y)
            {
                if (x == null || y == null) return x == y;
                return x.Path == y.Path && x.Content == y.Content;
            }

            public int GetHashCode(ParsedEuiFile obj)
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + (obj.Path?.GetHashCode() ?? 0);
                    hash = hash * 31 + (obj.Content?.GetHashCode() ?? 0);
                    return hash;
                }
            }
        }

        /// <summary>
        /// 类型查找缓存（compilation 级别共享）
        /// </summary>
        private class TypeLookupCache
        {
            private readonly Compilation _compilation;
            private readonly Dictionary<(string TypeName, string UsingList), INamedTypeSymbol?> _cache = new();

            public TypeLookupCache(Compilation compilation)
            {
                _compilation = compilation;
            }

            public INamedTypeSymbol? FindTypeSymbol(string typeName, List<string> usings)
            {
                var cacheKey = (typeName, string.Join("|", usings));
                
                if (_cache.TryGetValue(cacheKey, out var cached))
                    return cached;
                
                var result = FindTypeSymbolInternal(typeName, usings);
                _cache[cacheKey] = result;
                return result;
            }

            private INamedTypeSymbol? FindTypeSymbolInternal(string typeName, List<string> usings)
            {
                if (typeName.Contains('.'))
                {
                    var symbol = _compilation.GetTypeByMetadataName(typeName);
                    if (symbol != null) return symbol;
                }
                
                foreach (var ns in usings)
                {
                    var fullName = $"{ns}.{typeName}";
                    var symbol = _compilation.GetTypeByMetadataName(fullName);
                    if (symbol != null) return symbol;
                }
                
                var globalSymbol = _compilation.GetTypeByMetadataName(typeName);
                if (globalSymbol != null) return globalSymbol;
                
                foreach (var reference in _compilation.References)
                {
                    var assembly = _compilation.GetAssemblyOrModuleSymbol(reference) as IAssemblySymbol;
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
            public string? Namespace { get; set; }
            public List<(string Type, string Name)> Injections { get; } = new();
            public string? BaseClass { get; set; }
            public List<string> Attributes { get; } = new();
            public string CodeBlock { get; set; } = string.Empty;
            public string Markup { get; set; } = string.Empty;
        }
    }
}