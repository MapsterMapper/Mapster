using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Mapster.SourceGenerator;

[Generator]
internal partial class MappingGenerator : IIncrementalGenerator
{
    private const string attributeText = @"
using System;
namespace Mapster.SourceGenerator
{
    [AttributeUsage(AttributeTargets.Class
                    | AttributeTargets.Struct
                    | AttributeTargets.Interface, AllowMultiple = true)]
    public class AdaptFromAttribute : Attribute
    {
        public AdaptFromAttribute(Type type)
        {
            Type = type;
        }

        public AdaptFromAttribute(string name)
        {
            Name = name;
        }

        public Type? Type { get; }
        public string? Name { get; }
    }

    [AttributeUsage(AttributeTargets.Class
                    | AttributeTargets.Struct
                    | AttributeTargets.Interface, AllowMultiple = true)]
    public class AdaptToAttribute : Attribute
    {
        public AdaptToAttribute(Type type)
        {
            Type = type;
        }

        public AdaptToAttribute(string name)
        {
            Name = name;
        }
        public Type? Type { get; }
        public string? Name { get; }
    }

    [AttributeUsage(AttributeTargets.Class
                    | AttributeTargets.Struct
                    | AttributeTargets.Interface, AllowMultiple = true)]
    public class AdaptTwoWaysAttribute : Attribute
    {
        public AdaptTwoWaysAttribute(Type type)
        {
            Type = type;
        }

        public AdaptTwoWaysAttribute(string name)
        {
            Name = name;
        }

        public Type? Type { get; }
        public string? Name { get; }
    }
}
";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(i => i.AddSource("AdaptAttribute", attributeText));

        IncrementalValuesProvider<GeneratedTypeInfo> attributeDeclarations = context
            .SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
            .Where(static m => m is not null)!;

        IncrementalValueProvider<(Compilation, ImmutableArray<GeneratedTypeInfo>)> compilationAndClasses
            = context.CompilationProvider.Combine(attributeDeclarations.Collect());
        context.RegisterSourceOutput(compilationAndClasses,
            static (spc, source) => Execute(source.Item1, source.Item2, spc));

    }

    private static void Execute(Compilation compilation, ImmutableArray<GeneratedTypeInfo> generatedModels, SourceProductionContext context)
    {
        var emitter = new Emitter();
        var sources =emitter.Emit(generatedModels);
        foreach (var source in sources)
        {
            context.AddSource(source.Key,source.Value);
        }
    }
}
