using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace Mapster.Tool
{
    [Verb("model", HelpText = "Generate models")]
    public class ModelOptions
    {
        [Option('a', "assembly", Required = true, HelpText = "Assembly to scan")]
        public string Assembly { get; set; }

        [Option('o', "output", Required = false, Default = "Models", HelpText = "Output directory.")]
        public string Output { get; set; }

        [Option('n', "namespace", Required = false, HelpText = "Namespace for models")]
        public string? Namespace { get; set; }
        
        [Option('p', "printFullTypeName", Required = false, HelpText = "Set true to print full type name")]
        public bool PrintFullTypeName { get; set; }

        [Option('r', "isRecordType", Required = false, HelpText = "Generate record type")]
        public bool IsRecordType { get; set; }
        
        [Option('b', "baseNamespace", Required = false, HelpText = "Provide base namespace to generate nested output & namespace")]
        public string? BaseNamespace { get; set; }

        [Usage(ApplicationAlias = "dotnet mapster model")]
        public static IEnumerable<Example> Examples =>
            new List<Example>
            {
                new Example("Generate models", new MapperOptions
                {
                    Assembly = "/Path/To/YourAssembly.dll",
                    Output = "Models"
                })
            };
    }
}