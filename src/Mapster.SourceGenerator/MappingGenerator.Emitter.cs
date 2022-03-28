using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mapster.SourceGenerator;

internal partial class MappingGenerator
{
    internal class Emitter
    {
        private static readonly string s_generatedCodeAttribute =
            $"global::System.CodeDom.Compiler.GeneratedCodeAttribute(" +
            $"\"{typeof(Emitter).Assembly.GetName().Name}\", " +
            $"\"{typeof(Emitter).Assembly.GetName().Version}\")";

        private static readonly string s_editorBrowsableAttribute =
            "global::System.ComponentModel.EditorBrowsableAttribute(" +
            "global::System.ComponentModel.EditorBrowsableState.Never)";

        public Dictionary<string, string> Emit(IEnumerable<GeneratedTypeInfo> generatedTypeInfos)
        {
            var dict = new Dictionary<string, string>();
            foreach (var toBeGeneratedTypeInfo in generatedTypeInfos)
            {
                var generatedTypeInfo = GenerateType(toBeGeneratedTypeInfo);
                var generatedMapperInfo = GenerateMapper(generatedTypeInfo);
                dict.Add($"{generatedTypeInfo.GeneratedTypeName}.g.cs", generatedTypeInfo.SourceText);
                dict.Add($"{generatedMapperInfo.ParentTypeInfo.GeneratedTypeName}.Mapper.g.cs", generatedMapperInfo.SourceText);
            }
            foreach (var group in generatedTypeInfos.GroupBy(_ => _.SourceTypeSymbol, SymbolEqualityComparer.Default))
            {
                var typeSymbol = group.Key as INamedTypeSymbol;
                var generatedExtensionInfo =  GenerateExtensions(typeSymbol,group.Select(_=>_.GeneratedTypeName).ToList());
                dict.Add($"{generatedExtensionInfo.ThisTypeSymbol.Name}.Extensions.g.cs",generatedExtensionInfo.SourceText);
            }
            return dict;
        }

        private GeneratedTypeInfo GenerateType(GeneratedTypeInfo generatedTypeInfo)
        {
            var sw = new StringWriter();
            var writer = new IndentedTextWriter(sw);
            string namespaceName = generatedTypeInfo.SourceTypeSymbol.ContainingNamespace.ToDisplayString();
            writer.WriteLine("using System;");
            writer.WriteLine($"namespace {namespaceName}");
            writer.WriteLine("{");
            writer.Indent += 1;
            writer.WriteLine($"public partial class {generatedTypeInfo.GeneratedTypeName}");
            writer.WriteLine("{");
            writer.Indent += 1;
            foreach (var property in generatedTypeInfo.GeneratedProperties)
            {
                var propertySymbol = property.PropertySymbol;
                string propertySymbolName = propertySymbol.Name;
                ITypeSymbol propertySymbolType = propertySymbol.Type;
                writer.WriteLine($"public {propertySymbolType} {propertySymbolName} {{ get; set; }}");
                writer.WriteLine();
            }
            writer.WriteLine($"public {generatedTypeInfo.SourceTypeSymbol} MapTo{generatedTypeInfo.SourceTypeSymbol.Name}() => {generatedTypeInfo.GeneratedTypeName}.Mapper.Map{generatedTypeInfo.GeneratedTypeName}To{generatedTypeInfo.SourceTypeSymbol.Name}(this);");
            writer.WriteLine();
            writer.Indent -= 1;
            writer.WriteLine("}");
            writer.Indent -= 1;
            writer.WriteLine("}");
            generatedTypeInfo.SourceText = sw.ToString();
            return generatedTypeInfo;

        }

        private GeneratedMapperInfo GenerateMapper(GeneratedTypeInfo generatedTypeInfo)
        {
            var sw = new StringWriter();
            var writer = new IndentedTextWriter(sw);
            string namespaceName = generatedTypeInfo.SourceTypeSymbol.ContainingNamespace.ToDisplayString();
            writer.WriteLine("using System;");
            writer.WriteLine($"namespace {namespaceName}");
            writer.WriteLine("{");
            writer.Indent += 1;
            writer.WriteLine($"public partial class {generatedTypeInfo.GeneratedTypeName}");
            writer.WriteLine("{");
            writer.Indent += 1;
            writer.WriteLine("public class Mapper");
            writer.WriteLine("{");
            writer.Indent += 1;
            string forwardMethodName =
                $"Map{generatedTypeInfo.SourceTypeSymbol.Name}To{generatedTypeInfo.GeneratedTypeName}";
            string backwardMethodName =
                $"Map{generatedTypeInfo.GeneratedTypeName}To{generatedTypeInfo.SourceTypeSymbol.Name}";

            writer.WriteLine(
                "[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
            writer.WriteLine(
                $"public static {generatedTypeInfo.GeneratedTypeName} {forwardMethodName}({generatedTypeInfo.SourceTypeSymbol.ToDisplayString()} obj)");
            writer.WriteLine("{");
            writer.Indent += 1;
            writer.WriteLine($"var target = new {generatedTypeInfo.GeneratedTypeName}();");
            foreach (var property in generatedTypeInfo.GeneratedProperties)
            {
                var propertySymbol = property.PropertySymbol;
                writer.WriteLine($"target.{propertySymbol.Name} = obj.{propertySymbol.Name};");
            }
            writer.WriteLine("return target;");
            writer.Indent -= 1;
            writer.WriteLine("}");

            writer.WriteLine();

            writer.WriteLine(
                "[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
            writer.WriteLine(
                $"public static {generatedTypeInfo.SourceTypeSymbol.ToDisplayString()} {backwardMethodName}({generatedTypeInfo.GeneratedTypeName} obj)");
            writer.WriteLine("{");
            writer.Indent += 1;
            writer.WriteLine($"var target = new {generatedTypeInfo.SourceTypeSymbol.ToDisplayString()}();");
            foreach (var property in generatedTypeInfo.GeneratedProperties)
            {
                var propertySymbol = property.PropertySymbol;
                writer.WriteLine($"target.{propertySymbol.Name} = obj.{propertySymbol.Name};");
            }
            writer.WriteLine("return target;");
            writer.Indent -= 1;
            writer.WriteLine("}");

            writer.Indent -= 1;
            writer.WriteLine("}");
            writer.Indent -= 1;
            writer.WriteLine("}");
            writer.Indent -= 1;
            writer.WriteLine("}");
            return new GeneratedMapperInfo()
            { ParentTypeInfo = generatedTypeInfo, SourceText = sw.ToString(), ForwardMappingMethodName = forwardMethodName, BackwardMappingMethodName = backwardMethodName };
        }

        private GeneratedExtensionInfo GenerateExtensions(INamedTypeSymbol thisType,IEnumerable<string> targetTypes)
        {
            var sw = new StringWriter();
            var writer = new IndentedTextWriter(sw);
            var className = $"{thisType.Name}Extensions";
            string namespaceName = thisType.ContainingNamespace.ToDisplayString();
            writer.WriteLine("using System;");
            writer.WriteLine($"namespace {namespaceName}");
            writer.WriteLine("{");
            writer.Indent += 1;
            writer.WriteLine($"public static class {className}");
            writer.WriteLine("{");
            writer.Indent += 1;
            foreach (var targetType in targetTypes)
            {
                writer.WriteLine($"public static {targetType} MapTo{targetType}(this {thisType} obj) => {targetType}.Mapper.Map{thisType.Name}To{targetType}(obj);");
                writer.WriteLine();
            }
            writer.Indent -= 1;
            writer.WriteLine("}");
            writer.Indent -= 1;
            writer.WriteLine("}");
            return new GeneratedExtensionInfo()
            {
                SourceText = sw.ToString(),
                ThisTypeSymbol = thisType
            };
        }
    }
}
