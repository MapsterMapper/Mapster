using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace ExpressionDebugger
{
    public class ExpressionCompiler
    {
        public List<ExpressionTranslator> Translators { get; } = new List<ExpressionTranslator>();

        private readonly ExpressionCompilationOptions? _options;
        public ExpressionCompiler(ExpressionCompilationOptions? options = null)
        {
            _options = options;
        }

        private readonly List<SyntaxTree> _codes = new List<SyntaxTree>();
        public void AddFile(string code, string filename)
        {
            var buffer = Encoding.UTF8.GetBytes(code);

            var path = filename;
            if (_options?.EmitFile == true)
            {
                var root = _options?.RootPath 
                           ?? Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "GeneratedSources");
                Directory.CreateDirectory(root);
                path = Path.Combine(root, filename);
                using var fs = new FileStream(path, FileMode.Create);
                fs.Write(buffer, 0, buffer.Length);
            }

            var sourceText = SourceText.From(buffer, buffer.Length, Encoding.UTF8, canBeEmbedded: true);

            var syntaxTree = CSharpSyntaxTree.ParseText(
                sourceText,
                new CSharpParseOptions(),
                path);

            var syntaxRootNode = syntaxTree.GetRoot() as CSharpSyntaxNode;
            var encoded = CSharpSyntaxTree.Create(syntaxRootNode, null, path, Encoding.UTF8);
            _codes.Add(encoded);
        }

        public void AddFile(LambdaExpression node, ExpressionDefinitions? definitions = null)
        {
            definitions ??= _options?.DefaultDefinitions ?? new ExpressionDefinitions {IsStatic = true};
            definitions.TypeName ??= "Program";

            var translator = ExpressionTranslator.Create(node, definitions);
            var script = translator.ToString();
            Translators.Add(translator);

            this.AddFile(script, Path.ChangeExtension(Path.GetRandomFileName(), ".cs"));
        }

        public Assembly CreateAssembly()
        {
            var references = new HashSet<Assembly>();
            references.UnionWith(from t in Translators
                                 from n in t.TypeNames
                                 select n.Key.Assembly);

            if (_options?.References != null)
                references.UnionWith(_options.References);
            references.Add(typeof(object).Assembly);

#if NETSTANDARD2_0 || NET6_0_OR_GREATER
            references.Add(Assembly.Load(new AssemblyName("netstandard")));
            references.Add(Assembly.Load(new AssemblyName("System.Runtime")));
            references.Add(Assembly.Load(new AssemblyName("System.Collections")));
#endif

            var assemblyName = Path.GetRandomFileName();
            var symbolsName = Path.ChangeExtension(assemblyName, "pdb");

            var metadataReferences = references.Select(it => MetadataReference.CreateFromFile(it.Location));
            var isRelease = _options?.IsRelease ?? !Debugger.IsAttached;
            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName,
                _codes,
                metadataReferences,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, usings: new[] { "System" })
                    .WithOptimizationLevel(isRelease ? OptimizationLevel.Release : OptimizationLevel.Debug)
                    .WithPlatform(Platform.AnyCpu)
            );

            using var assemblyStream = new MemoryStream();
            using var symbolsStream = new MemoryStream();
            var emitOptions = new EmitOptions(
                debugInformationFormat: DebugInformationFormat.PortablePdb,
                pdbFilePath: symbolsName);

            var embeddedTexts = _codes.Select(it => EmbeddedText.FromSource(it.FilePath, it.GetText()));

            EmitResult result = compilation.Emit(
                peStream: assemblyStream,
                pdbStream: symbolsStream,
                embeddedTexts: embeddedTexts,
                options: emitOptions);

            if (!result.Success)
            {
                var errors = new List<string>();

                IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                    diagnostic.IsWarningAsError ||
                    diagnostic.Severity == DiagnosticSeverity.Error);

                foreach (Diagnostic diagnostic in failures)
                    errors.Add($"{diagnostic.Id}: {diagnostic.GetMessage()}");

                throw new InvalidOperationException(string.Join("\n", errors));
            }

            assemblyStream.Seek(0, SeekOrigin.Begin);
            symbolsStream.Seek(0, SeekOrigin.Begin);

#if NETSTANDARD2_0 || NET6_0_OR_GREATER
            return System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromStream(assemblyStream, symbolsStream);
#else
                return Assembly.Load(assemblyStream.ToArray(), symbolsStream.ToArray());
#endif
        }

    }
}
