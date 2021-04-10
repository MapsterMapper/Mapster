using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;

namespace Mapster.Tool
{
    // https://www.codeproject.com/Articles/1194332/Resolving-Assemblies-in-NET-Core
    internal sealed class AssemblyResolver : IDisposable
    {
        private readonly ICompilationAssemblyResolver _assemblyResolver;
        private readonly DependencyContext _dependencyContext;
        private readonly AssemblyLoadContext _loadContext;

        public AssemblyResolver(string path)
        {
            Assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
            _dependencyContext = DependencyContext.Load(Assembly);

            _assemblyResolver = new CompositeCompilationAssemblyResolver
            (new ICompilationAssemblyResolver[]
            {
                new AppBaseCompilationAssemblyResolver(Path.GetDirectoryName(path)),
                new ReferenceAssemblyPathResolver(),
                new PackageCompilationAssemblyResolver(),
            });

            _loadContext = AssemblyLoadContext.GetLoadContext(Assembly)!;
            _loadContext.Resolving += OnResolving;
        }

        public Assembly Assembly { get; }

        public void Dispose()
        {
            _loadContext.Resolving -= OnResolving;
        }

        private Assembly? OnResolving(AssemblyLoadContext context, AssemblyName name)
        {
            //hack for loaded assemblies
            if (name.Name == "Mapster")
                return typeof(TypeAdapterConfig).Assembly;
            if (name.Name == "Mapster.Core")
                return typeof(MapperAttribute).Assembly;
            if (name.Name == "System.Text.Json")
                return typeof(JsonIgnoreAttribute).Assembly;

            var (library, assetPath) = (from lib in _dependencyContext.RuntimeLibraries
                from grp in lib.RuntimeAssemblyGroups
                where grp.Runtime == string.Empty
                from path in grp.AssetPaths
                where string.Equals(GetAssemblyName(path), name.Name, StringComparison.OrdinalIgnoreCase)
                select (lib, path)).FirstOrDefault();

            if (library == null)
            {
                Console.WriteLine("Cannot find library: " + name.Name);
                return null;
            }
            
            try
            {
                var wrapped = new CompilationLibrary(
                    library.Type,
                    library.Name,
                    library.Version,
                    library.Hash,
                    new[] {assetPath},
                    library.Dependencies,
                    library.Serviceable);

                var assemblies = new List<string>();
                _assemblyResolver.TryResolveAssemblyPaths(wrapped, assemblies);

                if (assemblies.Count == 0)
                {
                    Console.WriteLine($"Cannot find assembly path: {name.Name} (type={library.Type}, version={library.Version})");
                    return null;
                }

                return _loadContext.LoadFromAssemblyPath(assemblies[0]);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cannot find assembly path: {name.Name} (type={library.Type}, version={library.Version})");
                Console.WriteLine("exception: " + ex.Message);
                return null;
            }
        }

        private const string NativeImageSufix = ".ni";
        private static string GetAssemblyName(string assetPath)
        {
            var name = Path.GetFileNameWithoutExtension(assetPath);
            if (name == null)
            {
                throw new ArgumentException($"Provided path has empty file name '{assetPath}'", nameof(assetPath));
            }

            if (name.EndsWith(NativeImageSufix))
            {
                name = name.Substring(0, name.Length - NativeImageSufix.Length);
            }

            return name;
        }
    }
}