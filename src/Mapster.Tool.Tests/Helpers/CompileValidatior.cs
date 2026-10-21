using Mapster.Tool.Tests;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System.Linq.Expressions;

public class CompileValidator
{
    public static bool TryCompile(string code, out string errors)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code);

        string basePath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;

        var references = new[]
        {
            MetadataReference.CreateFromFile(Path.Combine(basePath, "System.Private.CoreLib.dll")),
            MetadataReference.CreateFromFile(Path.Combine(basePath, "System.Runtime.dll")),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location), 
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(TestBase).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Expression).Assembly.Location)
        };

        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: "Generated_" + Guid.NewGuid().ToString("N"), 
            syntaxTrees: new[] { syntaxTree },
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        using (var ms = new MemoryStream())
        {
            EmitResult result = compilation.Emit(ms);

            if (result.Success)
            {
                errors = string.Empty;
                return true;
            }
            else
            {
                errors = string.Join(Environment.NewLine, result.Diagnostics
                    .Where(d => d.Severity == DiagnosticSeverity.Error || d.Severity == DiagnosticSeverity.Warning)
                    .Select(d => $"{d.Id}: {d.GetMessage()}"));
                return false;
            }
        }
    }
}
