using System.Reflection;

namespace Mapster.Tool.Tests.Helpers
{
    internal static class ConfigHelpers
    {
        internal static MapperOptions optMappers => new MapperOptions() { Assembly = Assembly.GetExecutingAssembly().Location, Output = Path.GetTempPath() };
        internal static ModelOptions optModels = new ModelOptions() { Assembly = Assembly.GetExecutingAssembly().Location, Output = Path.GetTempPath() };
        internal static ExtensionOptions optExtentions = new ExtensionOptions() { Assembly = Assembly.GetExecutingAssembly().Location, Output = Path.GetTempPath() };
    }
}
