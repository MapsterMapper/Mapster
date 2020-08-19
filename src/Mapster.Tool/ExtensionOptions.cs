using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace Mapster.Tool
{
    [Verb("extension", HelpText = "Generate extensions")]
    public class ExtensionOptions
    {
        [Option('a', "assembly", Required = true, HelpText = "Assembly to scan")]
        public string Assembly { get; set; }

        [Option('o', "output", Required = false, Default = "Models", HelpText = "Output directory.")]
        public string Output { get; set; }

        [Option('n', "namespace", Required = false, HelpText = "Namespace for extensions")]
        public string? Namespace { get; set; }

        [Usage(ApplicationAlias = "dotnet mapster extension")]
        public static IEnumerable<Example> Examples =>
            new List<Example>
            {
                new Example("Generate extensions", new MapperOptions
                {
                    Assembly = "/Path/To/YourAssembly.dll",
                    Output = "Models"
                })
            };
    }
}