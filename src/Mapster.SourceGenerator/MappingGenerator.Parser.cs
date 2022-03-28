using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mapster.SourceGenerator;

internal partial class MappingGenerator
{
    private const string AdaptFromAttribute = "Mapster.SourceGenerator.AdaptFromAttribute";
    private const string AdaptToAttribute = "Mapster.SourceGenerator.AdaptToAttribute";
    private const string AdaptTwoWaysAttribute = "Mapster.SourceGenerator.AdaptTwoWaysAttribute";

    public static bool IsSyntaxTargetForGeneration(SyntaxNode node)
    {
        var attributeSyntax = node as AttributeSyntax;
        return attributeSyntax?.ArgumentList != null && attributeSyntax.ArgumentList.Arguments.Count > 0;
    }

    public static GeneratedTypeInfo? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var attributeSyntax = context.Node as AttributeSyntax;
        if (attributeSyntax.Parent is AttributeListSyntax attributeListSyntax)
        {
            if (attributeListSyntax.Parent is TypeDeclarationSyntax typeDeclarationSyntax)
            {
                var typeSymbol = context.SemanticModel.GetDeclaredSymbol(typeDeclarationSyntax) as INamedTypeSymbol;
                var properties = new List<GeneratedPropertyInfo>();
                foreach (var propertyDeclarationSyntax in typeDeclarationSyntax.Members.OfType<PropertyDeclarationSyntax>())
                {
                    var propertySymbol = context.SemanticModel.GetDeclaredSymbol(propertyDeclarationSyntax) as IPropertySymbol;
                    properties.Add(new GeneratedPropertyInfo()
                    {
                        PropertyDeclarationSyntax = propertyDeclarationSyntax,
                        PropertySymbol = propertySymbol
                    });
                }
                
                var attributeSymbol = context.SemanticModel.GetTypeInfo(attributeSyntax).Type as INamedTypeSymbol;
                var attributeText = attributeSyntax.ArgumentList.Arguments.FirstOrDefault()?.Expression.ToString()
                    .Trim('"');
                if (attributeText is not null)
                {
                    var generatedTypeName = attributeText.Replace("[name]", typeSymbol.Name);
                    switch(attributeSymbol.ToDisplayString()) 
                    {
                        case AdaptFromAttribute:
                        case AdaptToAttribute:
                        case AdaptTwoWaysAttribute:
                            break;
                        default:
                            return null;
                    };
                    return new GeneratedTypeInfo
                    {
                        AttributeSyntax = attributeSyntax,
                        GeneratedTypeName = generatedTypeName,
                        SourceTypeDeclarationSyntax = typeDeclarationSyntax,
                        SourceTypeSymbol = typeSymbol,
                        GeneratedModelPattern = attributeText,
                        GeneratedProperties = properties
                    };
                }
            }
        }

        return null;
    }
}