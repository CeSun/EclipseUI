using System;
using System.IO;
using System.Text;
using Eclipse.Core;
using Eclipse.Core.Abstractions;

namespace Eclipse.Compiler
{
    public class EclipseCompiler
    {
        public string Namespace { get; set; } = "Eclipse.Generated";
        public string? ClassName { get; set; }
        
        public string Compile(string source, string? fileName = null)
        {
            var className = ClassName ?? Path.GetFileNameWithoutExtension(fileName ?? "Component.ecl");
            return GenerateComponent(source, className);
        }
        
        private string GenerateComponent(string source, string className)
        {
            var sb = new StringBuilder();
            var indent = 0;
            
            void WriteLine(string line = "")
            {
                sb.AppendLine(new string(' ', indent * 4) + line);
            }
            
            WriteLine("using System;");
            WriteLine("using Eclipse.Core;");
            WriteLine("using Eclipse.Core.Abstractions;");
            WriteLine();
            
            WriteLine($"namespace {Namespace}");
            WriteLine("{");
            indent++;
            
            WriteLine($"public partial class {className} : ComponentBase");
            WriteLine("{");
            indent++;
            
            ParseAndGenerate(source, sb, ref indent, WriteLine);
            
            WriteLine("public override void Render(IRenderContext context)");
            WriteLine("{");
            indent++;
            WriteLine("// TODO: Implement rendering");
            indent--;
            WriteLine("}");
            
            indent--;
            WriteLine("}");
            
            indent--;
            WriteLine("}");
            
            return sb.ToString();
        }
        
        private void ParseAndGenerate(string source, StringBuilder sb, ref int indent, Action<string> WriteLine)
        {
            var codeIndex = source.IndexOf("@code");
            if (codeIndex >= 0)
            {
                var blockStart = source.IndexOf('{', codeIndex);
                if (blockStart >= 0)
                {
                    var depth = 1;
                    var i = blockStart + 1;
                    while (i < source.Length && depth > 0)
                    {
                        if (source[i] == '{') depth++;
                        else if (source[i] == '}') depth--;
                        i++;
                    }
                    
                    var codeContent = source.Substring(blockStart + 1, i - blockStart - 2);
                    WriteLine("// Fields and methods");
                    foreach (var line in codeContent.Split('\n'))
                    {
                        WriteLine(line.TrimStart());
                    }
                    WriteLine("");
                }
            }
        }
    }
}
