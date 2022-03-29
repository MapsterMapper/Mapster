using Microsoft.CodeAnalysis;

namespace Mapster.SourceGenerator
{
    internal class GeneratedExtensionInfo
    {
        public string SourceText { get; set; }
        public INamedTypeSymbol ThisTypeSymbol { get; set; }
    }
}
