using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mapster.SourceGenerator
{
    internal class PromisedTypeGenerating
    {
        public INamedTypeSymbol SourceTypeSymbol { get; set; }
        public TypeDeclarationSyntax SourceTypeDeclarationSyntax { get; set; }
        public string GeneratedTypeName { get; set; }

        public string GeneratedModelPattern { get; set; }
        public string SourceText { get; set; }

        public AttributeSyntax AttributeSyntax { get; set; }

        public List<GeneratedPropertyInfo> GeneratedProperties { get; set; }
    }
}
