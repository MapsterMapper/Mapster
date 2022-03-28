using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mapster.SourceGenerator
{
    internal class GeneratedTypeInfo
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
