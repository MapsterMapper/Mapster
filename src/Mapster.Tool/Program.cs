using CommandLine;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Mapster.Tool.Tests, PublicKey=0024000004800000940000000602000000240000525341310004000001000100bd523e79e4decc052a3501363d71ecc123b9ce4bd5a8c949e81bc482d8b6822366ed6aead5ebace01aae3ade49e116fde094af03c34cdbc2ebcb89346ca510fac6246b240b71968ab7f9a24de44d680dc93307f9e8a2b00bec7c523db9696679b56725d622cfb01f4eb2604333a0a0e9f580cd6f5c3d5034b3e66f52d818e9a5")]
namespace Mapster.Tool
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default
                .ParseArguments<MapperOptions, ModelOptions, ExtensionOptions>(args)
                .WithParsed<MapperOptions>(GenerateMappers)
                .WithParsed<ModelOptions>(GenerateModels)
                .WithParsed<ExtensionOptions>(GenerateExtensions);
        }

        private static void GenerateExtensions(ExtensionOptions options)
        {
            Generators.GenerateExtensions(options);
        }

        private static void GenerateModels(ModelOptions options)
        {
            Generators.GenerateModels(options);
        }

        private static void GenerateMappers(MapperOptions options)
        {
            Generators.GenerateMappers(options);
        }
    }
}
