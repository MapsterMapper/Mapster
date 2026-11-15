#if !NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using CommandLine;
using ExpressionDebugger;
using FluentAssertions;

namespace Mapster.Tool.Tests;

public class WhenGeneratingWithGlobalPrefix
{
    public class OuterModel
    {
        public class NestedModel
        {
            public int Value { get; set; }
        }
    }

    public class SourceModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<OuterModel.NestedModel> Items { get; set; } = new();
    }

    public class DestModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<OuterModel.NestedModel> Items { get; set; } = new();
    }

    [Fact]
    public void ExpressionTranslator_UsesGlobalPrefix_ForTypesAndGenerics_WhenEnabled()
    {
        var definitions = new TypeDefinitions
        {
            Namespace = "Prefix.Mapster.Tool.Tests",
            TypeName = "SampleMapper",
            PrintFullTypeName = true,
            UseGlobalPrefix = true,
        };

        var translator = new ExpressionTranslator(definitions);
        Expression<Func<SourceModel, DestModel>> expr = src => new DestModel
        {
            Id = src.Id,
            Name = src.Name,
            Items = new List<OuterModel.NestedModel>(src.Items),
        };

        translator.VisitLambda(expr, ExpressionTranslator.LambdaType.PublicLambda, "Map");
        var code = translator.ToString();

        code.Should().Contain("global::Mapster.Tool.Tests.WhenGeneratingWithGlobalPrefix.SourceModel");
        code.Should().Contain("global::Mapster.Tool.Tests.WhenGeneratingWithGlobalPrefix.DestModel");
        code.Should().Contain("global::System.Collections.Generic.List<global::Mapster.Tool.Tests.WhenGeneratingWithGlobalPrefix.OuterModel.NestedModel>");
    }

    [Fact]
    public void ExpressionTranslator_UseGlobalPrefixWithoutPrintFullTypeName_ImpliesFullTypeNames()
    {
        var definitions = new TypeDefinitions
        {
            Namespace = "Prefix.Mapster.Tool.Tests",
            TypeName = "SampleMapper",
            UseGlobalPrefix = true,
        };

        var translator = new ExpressionTranslator(definitions);
        Expression<Func<SourceModel, DestModel>> expr = src => new DestModel
        {
            Id = src.Id,
            Name = src.Name,
        };

        translator.VisitLambda(expr, ExpressionTranslator.LambdaType.PublicLambda, "Map");
        var code = translator.ToString();

        code.Should().Contain("global::Mapster.Tool.Tests.WhenGeneratingWithGlobalPrefix.SourceModel");
        code.Should().Contain("global::Mapster.Tool.Tests.WhenGeneratingWithGlobalPrefix.DestModel");
        code.Should().NotContain("using Mapster.Tool.Tests;");
    }

    [Fact]
    public void ExpressionTranslator_PrintFullTypeNameWithoutGlobalPrefix_DoesNotEmitGlobalPrefix()
    {
        var definitions = new TypeDefinitions
        {
            Namespace = "Prefix.Mapster.Tool.Tests",
            TypeName = "SampleMapper",
            PrintFullTypeName = true,
            UseGlobalPrefix = false,
        };

        var translator = new ExpressionTranslator(definitions);
        Expression<Func<SourceModel, DestModel>> expr = src => new DestModel
        {
            Id = src.Id,
            Name = src.Name,
        };

        translator.VisitLambda(expr, ExpressionTranslator.LambdaType.PublicLambda, "Map");
        var code = translator.ToString();

        code.Should().Contain("Mapster.Tool.Tests.WhenGeneratingWithGlobalPrefix.SourceModel");
        code.Should().NotContain("global::");
    }

    [Theory]
    [InlineData(new[] { "mapper", "-a", "App.dll", "-g" }, true)]
    [InlineData(new[] { "mapper", "-a", "App.dll", "--useGlobalPrefix" }, true)]
    [InlineData(new[] { "mapper", "-a", "App.dll", "-p" }, false)]
    public void CliOptions_ParseUseGlobalPrefix_ForMapperExtensionAndModel(string[] args, bool expected)
    {
        var mapperResult = Parser.Default.ParseArguments<MapperOptions, ExtensionOptions, ModelOptions>(args);
        mapperResult.Tag.Should().Be(ParserResultType.Parsed);
        var parsed = ((Parsed<object>)mapperResult).Value.Should().BeOfType<MapperOptions>().Subject;
        parsed.UseGlobalPrefix.Should().Be(expected);

        var extArgs = new string[args.Length];
        args.CopyTo(extArgs, 0);
        extArgs[0] = "extension";
        var extResult = Parser.Default.ParseArguments<MapperOptions, ExtensionOptions, ModelOptions>(extArgs);
        extResult.Tag.Should().Be(ParserResultType.Parsed);
        ((Parsed<object>)extResult).Value.Should().BeOfType<ExtensionOptions>().Subject.UseGlobalPrefix.Should().Be(expected);

        var modelArgs = new string[args.Length];
        args.CopyTo(modelArgs, 0);
        modelArgs[0] = "model";
        var modelResult = Parser.Default.ParseArguments<MapperOptions, ExtensionOptions, ModelOptions>(modelArgs);
        modelResult.Tag.Should().Be(ParserResultType.Parsed);
        ((Parsed<object>)modelResult).Value.Should().BeOfType<ModelOptions>().Subject.UseGlobalPrefix.Should().Be(expected);
    }
}
#endif
