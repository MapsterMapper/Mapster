using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

namespace Mapster.SourceGenerator;

[Generator]
internal partial class MappingGenerator : IIncrementalGenerator
{
    private const string AttributeText = @"
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

    public enum MemberSide
    {
        Source,
        Destination,
    }

    [AttributeUsage(AttributeTargets.Field
                    | AttributeTargets.Property)]
    public class AdaptIgnoreAttribute : Attribute
    {
        public MemberSide? Side { get; set; }

        public AdaptIgnoreAttribute() { }

        public AdaptIgnoreAttribute(MemberSide side)
        {
            this.Side = side;
        }
    }
}
";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(i => i.AddSource("AdaptAttribute", AttributeText));

        IncrementalValuesProvider<PromisedTypeGenerating> attributeDeclarations = context
            .SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
            .Where(static m => m is not null)!;

        IncrementalValueProvider<(Compilation, ImmutableArray<PromisedTypeGenerating>)> compilationAndClasses
            = context.CompilationProvider.Combine(attributeDeclarations.Collect());
        context.RegisterSourceOutput(compilationAndClasses,
            static (spc, source) => Execute(source.Item1, source.Item2, spc));

    }

    private static void Execute(Compilation compilation, ImmutableArray<PromisedTypeGenerating> generatedModels, SourceProductionContext context)
    {
        var emitter = new Emitter();
        var sources = emitter.Emit(generatedModels);
        foreach (var source in sources)
        {
            context.AddSource(source.Key, source.Value);
        }
    }
}
