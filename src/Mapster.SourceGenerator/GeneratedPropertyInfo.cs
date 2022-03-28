using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mapster.SourceGenerator
{
    internal class GeneratedPropertyInfo
    {
        public PropertyDeclarationSyntax PropertyDeclarationSyntax { get; set; }
        public IPropertySymbol PropertySymbol { get; set; }
        public INamedTypeSymbol PropertyTypeSymbol { get; set; }
    }
}
