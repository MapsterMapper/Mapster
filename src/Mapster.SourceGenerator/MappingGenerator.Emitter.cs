using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;

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

        // Generate the Emit Dictionary.
        public Dictionary<string, string> Emit(IEnumerable<PromisedTypeGenerating> generatedTypeInfos)
        {
            var dict = new Dictionary<string, string>();
            // ReSharper disable once PossibleMultipleEnumeration
            foreach (var toBeGeneratedTypeInfo in generatedTypeInfos)
            {
                var generatedTypeInfo = GenerateType(toBeGeneratedTypeInfo);
                var generatedMapperInfo = GenerateMapper(generatedTypeInfo);
                dict.Add($"{generatedTypeInfo.GeneratedTypeName}.g.cs", generatedTypeInfo.SourceText);
                dict.Add($"{generatedMapperInfo.ParentTypeGenerating.GeneratedTypeName}.Mapper.g.cs", generatedMapperInfo.SourceText);
            }
            // ReSharper disable once PossibleMultipleEnumeration
            foreach (var group in generatedTypeInfos.GroupBy(_ => _.SourceTypeSymbol, SymbolEqualityComparer.Default))
            {
                var typeSymbol = group.Key as INamedTypeSymbol;
                var generatedExtensionInfo = GenerateExtensions(typeSymbol!, group.Select(_ => _.GeneratedTypeName).ToList());
                dict.Add($"{generatedExtensionInfo.ThisTypeSymbol.Name}.Extensions.g.cs", generatedExtensionInfo.SourceText);
            }
            return dict;
        }

        private PromisedTypeGenerating GenerateType(PromisedTypeGenerating promisedTypeGenerating)
        {
            var sw = new StringWriter();
            var writer = new IndentedTextWriter(sw);
            string namespaceName = promisedTypeGenerating.SourceTypeSymbol.ContainingNamespace.ToDisplayString();

            #region HEADER
            writer.WriteLine("using System;");
            writer.WriteLine($"namespace {namespaceName}");
            writer.WriteLine("{");
            writer.Indent += 1;
            writer.WriteLine($"public partial class {promisedTypeGenerating.GeneratedTypeName}");
            writer.WriteLine("{");
            writer.Indent += 1;
            #endregion
            foreach (var property in promisedTypeGenerating.GeneratedProperties)
            {
                // Pre process the attribute on the property
                var propertyType = string.Empty;
                foreach (var attributeData in property.PropertySymbol.GetAttributes())
                {
                    // If ignore, pass
                    if (attributeData.AttributeClass!.ToDisplayString() == AdaptIgnoreAttribute)
                    {
                        goto propEnd;
                    }

                    // Get type from attribute
                    if (attributeData.AttributeClass!.ToDisplayString() == PropertyTypeAttribute)
                    {
                        propertyType = attributeData.NamedArguments.FirstOrDefault(kvp => kvp.Key == "Type").Value.ToString();
                    }
                }
                var propertySymbol = property.PropertySymbol;
                var propertySymbolName = propertySymbol.Name;
                var propertySymbolType = string.IsNullOrEmpty(propertyType) ? propertySymbol.Type.ToString() : propertyType;
                writer.WriteLine($"public {propertySymbolType} {propertySymbolName} {{ get; set; }}");

            propEnd:
                writer.WriteLine();
            }
            writer.WriteLine($"public {promisedTypeGenerating.SourceTypeSymbol} MapTo{promisedTypeGenerating.SourceTypeSymbol.Name}() => {promisedTypeGenerating.GeneratedTypeName}.Mapper.Map{promisedTypeGenerating.GeneratedTypeName}To{promisedTypeGenerating.SourceTypeSymbol.Name}(this);");
            writer.WriteLine();
            writer.Indent -= 1;
            writer.WriteLine("}");
            writer.Indent -= 1;
            writer.WriteLine("}");
            promisedTypeGenerating.SourceText = sw.ToString();
            return promisedTypeGenerating;

        }

        // Generate Mapper
        private GeneratedMapperInfo GenerateMapper(PromisedTypeGenerating promisedTypeGenerating)
        {
            var sw = new StringWriter();
            var writer = new IndentedTextWriter(sw);
            string namespaceName = promisedTypeGenerating.SourceTypeSymbol.ContainingNamespace.ToDisplayString();
            # region HEADER
            writer.WriteLine("using System;");
            writer.WriteLine($"namespace {namespaceName}");
            writer.WriteLine("{");
            writer.Indent += 1;
            writer.WriteLine($"public partial class {promisedTypeGenerating.GeneratedTypeName}");
            writer.WriteLine("{");
            writer.Indent += 1;
            writer.WriteLine("public class Mapper");
            writer.WriteLine("{");
            writer.Indent += 1;
            #endregion

            // Generate method name
            string forwardMethodName =
                $"Map{promisedTypeGenerating.SourceTypeSymbol.Name}To{promisedTypeGenerating.GeneratedTypeName}";
            string backwardMethodName =
                $"Map{promisedTypeGenerating.GeneratedTypeName}To{promisedTypeGenerating.SourceTypeSymbol.Name}";

            #region METHOD_BODY
            writer.WriteLine(
                "[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
            writer.WriteLine(
                $"public static {promisedTypeGenerating.GeneratedTypeName} {forwardMethodName}({promisedTypeGenerating.SourceTypeSymbol.ToDisplayString()} obj)");
            writer.WriteLine("{");
            writer.Indent += 1;
            writer.WriteLine($"var target = new {promisedTypeGenerating.GeneratedTypeName}();");
            foreach (var property in promisedTypeGenerating.GeneratedProperties)
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
                $"public static {promisedTypeGenerating.SourceTypeSymbol.ToDisplayString()} {backwardMethodName}({promisedTypeGenerating.GeneratedTypeName} obj)");
            writer.WriteLine("{");
            writer.Indent += 1;
            writer.WriteLine($"var target = new {promisedTypeGenerating.SourceTypeSymbol.ToDisplayString()}();");
            foreach (var property in promisedTypeGenerating.GeneratedProperties)
            {
                var propertySymbol = property.PropertySymbol;
                writer.WriteLine($"target.{propertySymbol.Name} = obj.{propertySymbol.Name};");
            }
            writer.WriteLine("return target;");
            writer.Indent -= 1;
            writer.WriteLine("}");
            #endregion

            #region FOOTER
            writer.Indent -= 1;
            writer.WriteLine("}");
            writer.Indent -= 1;
            writer.WriteLine("}");
            writer.Indent -= 1;
            writer.WriteLine("}");
            #endregion
            return new GeneratedMapperInfo()
            { ParentTypeGenerating = promisedTypeGenerating, SourceText = sw.ToString(), ForwardMappingMethodName = forwardMethodName, BackwardMappingMethodName = backwardMethodName };
        }

        private GeneratedExtensionInfo GenerateExtensions(INamedTypeSymbol thisType, IEnumerable<string> targetTypes)
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
