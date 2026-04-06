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

            // 收集所有 EUI 文件的组件信息（用于识别自定义组件）
            var allComponentsInfo = parsedFiles.Collect().Select((files, _) =>
            {
                var components = new Dictionary<string, string>(); // ClassName -> Namespace
                foreach (var file in files)
                {
                    // 先解析 @namespace 指令
                    var ns = file.Parsed.Namespace;
                    if (string.IsNullOrEmpty(ns))
                    {
                        // 暂时用占位符，后续在 GenerateSource 中用 MSBuild 配置推断
                        ns = $"__pending__:{file.Path}";
                    }
                    components[file.ClassName] = ns;
                }
                return new AllComponentsInfo { Components = components };
            });

            // 结合 Compilation、配置、解析后的文件和所有组件信息
            var withCompilation = parsedFiles.Combine(context.CompilationProvider);
            var withConfig = withCompilation.Combine(context.AnalyzerConfigOptionsProvider);
            var withAllComponents = withConfig.Combine(allComponentsInfo);
            
            context.RegisterSourceOutput(withAllComponents, (spc, source) =>
            {
                var (((file, compilation), optionsProvider), allComponents) = source;
                
                // 推断命名空间（如果还没确定）
                string @namespace = file.Parsed.Namespace ?? InferNamespace(file.Path, file.AdditionalText, optionsProvider);
                
                // 更新组件信息中的命名空间
                if (allComponents.Components.TryGetValue(file.ClassName, out var cachedNs) && cachedNs.StartsWith("__pending__:"))
                {
                    allComponents.Components[file.ClassName] = @namespace;
                }
                
                // 类型查找缓存（在 compilation 级别共享）
                var typeCache = new TypeLookupCache(compilation, allComponents);
                
                GenerateSource(spc, optionsProvider, typeCache, file, @namespace, allComponents);
            });
        }

        // 存储所有组件信息
        private class AllComponentsInfo
        {
            public Dictionary<string, string> Components { get; set; } = new();
        }

        private void GenerateSource(SourceProductionContext context, 
            AnalyzerConfigOptionsProvider optionsProvider, 
            TypeLookupCache typeCache, 
            ParsedEuiFile file,
            string @namespace,
            AllComponentsInfo allComponents)
        {
            var className = file.ClassName;
            var parsed = file.Parsed;
            
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
            var propertyTypes = GetPropertyTypes(typeCache, controlTypes, parsed.Usings, file.Path, context);
            
            var generatedCode = GenerateComponentCode(@namespace, className, parsed, nodes, propertyTypes, typeCache, usings: parsed.Usings);
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
            TypeLookupCache cache, 
            HashSet<string> controlTypes, 
            List<string> usings,
            string filePath,
            SourceProductionContext context)
        {
            var result = new Dictionary<(string, string), PropertyTypeInfo>();
            
            foreach (var typeName in controlTypes)
            {
                // 检查是否是自定义组件
                if (cache.IsCustomComponent(typeName))
                {
                    // 自定义组件：尝试从 Compilation 中查找已有的 partial class
                    var componentNs = cache.GetCustomComponentNamespace(typeName);
                    if (!string.IsNullOrEmpty(componentNs))
                    {
                        var fullTypeName = $"{componentNs}.{typeName}";
                        var typeSymbol = cache.FindTypeSymbol(fullTypeName, usings);
                        
                        if (typeSymbol != null)
                        {
                            // 找到了 partial class，读取其属性
                            foreach (var prop in GetAllSettableProperties(typeSymbol))
                            {
                                var info = CreatePropertyTypeInfo(prop.Type);
                                result[(typeName, prop.Name)] = info;
                            }
                        }
                        // 没找到 partial class 时，组件没有额外属性，直接继承 ComponentBase
                    }
                    continue;
                }
                
                // 标准控件
                var controlTypeSymbol = cache.FindTypeSymbol(typeName, usings);
                if (controlTypeSymbol == null)
                {
                    // 报告类型未找到警告
                    var descriptor = new DiagnosticDescriptor(
                        "ECGEN003",
                        "EUI control type not found",
                        "Control type '{0}' not found in '{1}'. Make sure the type exists and the namespace is imported via @using.",
                        "Eclipse",
                        DiagnosticSeverity.Warning,
                        true);
                    
                    context.ReportDiagnostic(Diagnostic.Create(descriptor, Location.None, typeName, filePath));
                    continue;
                }
                
                foreach (var prop in GetAllSettableProperties(controlTypeSymbol))
                {
                    var info = CreatePropertyTypeInfo(prop.Type);
                    result[(typeName, prop.Name)] = info;
                }
            }
            
            return result;
        }
        
        private PropertyTypeInfo CreatePropertyTypeInfo(ITypeSymbol propType)
        {
            return new PropertyTypeInfo
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
        }

        /// <summary>
        /// 获取类型及其基类中所有可设置的属性（包括继承的属性）
        /// </summary>
        private IEnumerable<IPropertySymbol> GetAllSettableProperties(ITypeSymbol typeSymbol)
        {
            var current = typeSymbol;
            while (current != null && current.SpecialType != SpecialType.System_Object)
            {
                foreach (var member in current.GetMembers())
                {
                    if (member is IPropertySymbol property && !property.IsStatic && property.SetMethod != null)
                    {
                        yield return property;
                    }
                }
                current = current.BaseType;
            }
        }
        
        /// <summary>
        /// 推断附加属性的类型
        /// </summary>
        private PropertyTypeInfo? GetAttachedPropertyType(string? typeName, string? propertyName)
        {
            if (string.IsNullOrEmpty(typeName) || string.IsNullOrEmpty(propertyName))
                return null;
            
            // Grid 附加属性：Row, Column, RowSpan, ColumnSpan 都是 int
            if (typeName == "Grid" && 
                (propertyName == "Row" || propertyName == "Column" || propertyName == "RowSpan" || propertyName == "ColumnSpan"))
            {
                return new PropertyTypeInfo { TypeName = "int", IsNumeric = true };
            }
            
            // Canvas 附加属性：Left, Top, Right, Bottom, ZIndex 都是 double
            if (typeName == "Canvas" &&
                (propertyName == "Left" || propertyName == "Top" || propertyName == "Right" || propertyName == "Bottom" || propertyName == "ZIndex"))
            {
                return new PropertyTypeInfo { TypeName = "double", IsNumeric = true };
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
            List<MarkupNode> nodes, Dictionary<(string Control, string Property), PropertyTypeInfo> propertyTypes,
            TypeLookupCache typeCache, List<string> usings)
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
            
            // 添加自定义组件的命名空间（如果有使用）
            var customComponentNamespaces = new HashSet<string>();
            CollectCustomComponentNamespaces(nodes, typeCache, customComponentNamespaces);
            foreach (var customNs in customComponentNamespaces)
            {
                if (customNs != @namespace) // 不添加自己的命名空间
                    WriteLine($"using {customNs};");
            }
            
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

        /// <summary>
        /// 收集自定义组件的命名空间
        /// </summary>
        private void CollectCustomComponentNamespaces(List<MarkupNode> nodes, TypeLookupCache typeCache, HashSet<string> namespaces)
        {
            foreach (var node in nodes)
            {
                if (node is ControlNode control)
                {
                    if (typeCache.IsCustomComponent(control.TagName))
                    {
                        var ns = typeCache.GetCustomComponentNamespace(control.TagName);
                        if (!string.IsNullOrEmpty(ns))
                            namespaces.Add(ns);
                    }
                    CollectCustomComponentNamespaces(control.Children, typeCache, namespaces);
                }
                else if (node is IfNode ifNode)
                {
                    CollectCustomComponentNamespaces(ifNode.ThenBranch, typeCache, namespaces);
                    if (ifNode.ElseBranch != null)
                        CollectCustomComponentNamespaces(ifNode.ElseBranch, typeCache, namespaces);
                }
                else if (node is ForeachNode foreachNode)
                {
                    CollectCustomComponentNamespaces(foreachNode.Body, typeCache, namespaces);
                }
            }
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
                if (attr.IsAttached)
                {
                    // 附加属性：element.Set(Grid.Row, value)
                    // 需要根据附加属性名称推断类型
                    var attachedType = GetAttachedPropertyType(attr.AttachedTypeName, attr.AttachedPropertyName);
                    var value = attr.IsBinding ? attr.Value : ConvertLiteralValue(attr.Value, attachedType);
                    WriteLine($"{varName}.Set({attr.AttachedTypeName}.{attr.AttachedPropertyName}, {value});");
                }
                else if (attr.IsEvent)
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
            
            // 复杂类型转换
            switch (typeInfo.TypeName)
            {
                // Color 类型：支持多种格式
                // "#FF0000" → "Color.FromHex(\"#FF0000\")"
                // "Red" → "Colors.Red"
                case "Color":
                case "SkiaSharp.SKColor":
                    return ConvertColorValue(innerValue);
                
                // Thickness 类型：支持多种格式
                // "10" → "new Thickness(10)"
                // "10,20" → "new Thickness(10, 20)"
                // "10,20,30,40" → "new Thickness(10, 20, 30, 40)"
                case "Thickness":
                    return ConvertThicknessValue(innerValue);
                
                // Point 类型："10,20" → "new Point(10, 20)"
                case "Point":
                    return ConvertPointValue(innerValue);
                
                // Size 类型："100,50" → "new Size(100, 50)"
                case "Size":
                    return ConvertSizeValue(innerValue);
                
                // Rect 类型："10,20,100,50" → "new Rect(10, 20, 100, 50)"
                case "Rect":
                    return ConvertRectValue(innerValue);
                
                // Vector 类型："10,20" → "new Vector(10, 20)"
                case "Vector":
                    return ConvertVectorValue(innerValue);
                
                // TimeSpan 类型："00:01:30" → "TimeSpan.Parse(\"00:01:30\")"
                case "TimeSpan":
                    return $"TimeSpan.Parse(\"{innerValue}\")";
                
                // DateTime 类型："2024-01-15" → "DateTime.Parse(\"2024-01-15\")"
                case "DateTime":
                case "DateTimeOffset":
                    return $"{typeInfo.TypeName}.Parse(\"{innerValue}\")";
                
                // Guid 类型："..." → "Guid.Parse(\"...\")"
                case "Guid":
                    return $"Guid.Parse(\"{innerValue}\")";
                
                // Uri 类型："https://..." → "new Uri(\"https://...\")"
                case "Uri":
                    return $"new Uri(\"{innerValue}\")";
            }
            
            // 保留字符串字面量
            return value;
        }

        /// <summary>
        /// 转换颜色值
        /// </summary>
        private string ConvertColorValue(string value)
        {
            // Hex 格式: #RGB, #RRGGBB, #ARGB, #AARRGGBB
            if (value.StartsWith("#"))
            {
                return $"Color.FromHex(\"{value}\")";
            }
            
            // rgb/rgba 格式: rgb(255,0,0), rgba(255,0,0,0.5)
            if (value.StartsWith("rgb", StringComparison.OrdinalIgnoreCase))
            {
                return $"Color.Parse(\"{value}\")";
            }
            
            // 颜色名称: Red, Blue, Green 等
            if (char.IsLetter(value[0]))
            {
                // 尝试作为 Colors 静态属性
                return $"Colors.{value}";
            }
            
            // 回退：作为字符串解析
            return $"Color.Parse(\"{value}\")";
        }

        /// <summary>
        /// 转换 Thickness 值
        /// </summary>
        private string ConvertThicknessValue(string value)
        {
            var parts = value.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .ToArray();
            
            return parts.Length switch
            {
                1 => $"new Thickness({parts[0]})",
                2 => $"new Thickness({parts[0]}, {parts[1]})",
                4 => $"new Thickness({parts[0]}, {parts[1]}, {parts[2]}, {parts[3]})",
                _ => $"Thickness.Parse(\"{value}\")"
            };
        }

        /// <summary>
        /// 转换 Point 值
        /// </summary>
        private string ConvertPointValue(string value)
        {
            var parts = value.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .ToArray();
            
            if (parts.Length == 2)
            {
                return $"new Point({parts[0]}, {parts[1]})";
            }
            
            return $"Point.Parse(\"{value}\")";
        }

        /// <summary>
        /// 转换 Size 值
        /// </summary>
        private string ConvertSizeValue(string value)
        {
            var parts = value.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .ToArray();
            
            if (parts.Length == 2)
            {
                return $"new Size({parts[0]}, {parts[1]})";
            }
            
            return $"Size.Parse(\"{value}\")";
        }

        /// <summary>
        /// 转换 Rect 值
        /// </summary>
        private string ConvertRectValue(string value)
        {
            var parts = value.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .ToArray();
            
            if (parts.Length == 4)
            {
                return $"new Rect({parts[0]}, {parts[1]}, {parts[2]}, {parts[3]})";
            }
            
            return $"Rect.Parse(\"{value}\")";
        }

        /// <summary>
        /// 转换 Vector 值
        /// </summary>
        private string ConvertVectorValue(string value)
        {
            var parts = value.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .ToArray();
            
            if (parts.Length == 2)
            {
                return $"new Vector({parts[0]}, {parts[1]})";
            }
            
            return $"Vector.Parse(\"{value}\")";
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
            private readonly AllComponentsInfo? _allComponents;
            private readonly Dictionary<(string TypeName, string UsingList), INamedTypeSymbol?> _cache = new();
            // 自定义组件属性缓存（由于组件正在生成，没有编译好的类型符号）
            private readonly Dictionary<string, Dictionary<string, PropertyTypeInfo>> _customComponentProperties = new();

            public TypeLookupCache(Compilation compilation, AllComponentsInfo? allComponents = null)
            {
                _compilation = compilation;
                _allComponents = allComponents;
            }

            /// <summary>
            /// 检查是否是自定义组件
            /// </summary>
            public bool IsCustomComponent(string typeName)
            {
                if (_allComponents == null) return false;
                return _allComponents.Components.ContainsKey(typeName);
            }

            /// <summary>
            /// 获取自定义组件的命名空间
            /// </summary>
            public string? GetCustomComponentNamespace(string typeName)
            {
                if (_allComponents == null) return null;
                return _allComponents.Components.TryGetValue(typeName, out var ns) ? ns : null;
            }

            /// <summary>
            /// 注册自定义组件的属性（从 @code 块解析）
            /// </summary>
            public void RegisterCustomComponentProperties(string typeName, Dictionary<string, PropertyTypeInfo> properties)
            {
                _customComponentProperties[typeName] = properties;
            }

            /// <summary>
            /// 获取自定义组件的属性
            /// </summary>
            public Dictionary<string, PropertyTypeInfo>? GetCustomComponentProperties(string typeName)
            {
                return _customComponentProperties.TryGetValue(typeName, out var props) ? props : null;
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
                // 先检查自定义组件
                if (IsCustomComponent(typeName))
                {
                    // 自定义组件在编译时还没生成类型符号，返回 null
                    // 但 Generator 会知道它是自定义组件并正确生成代码
                    return null;
                }

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