using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Mapster.Tool
{
    //
    // Summary:
    //     Used for loading an assembly and its dependencies in an isolated assembly load context but deferring the resolution of
    //     a subset of those assemblies to an already existing Assembly Load Context (likely the AssemblyLoadContext.Default
    //     context that is used by the runtime by default at startup)
    public class DeferredDependencyAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver resolver;
        private readonly ImmutableHashSet<string> deferredDependencyAssemblyNames;
        private readonly AssemblyLoadContext deferToContext;

        public DeferredDependencyAssemblyLoadContext(
            string assemblyPath,
            AssemblyLoadContext deferToContext,
            params AssemblyName[] deferredDependencyAssemblyNames
        )
        {
            // set up a resolver for the dependencies of this non-deferred assembly
            resolver = new AssemblyDependencyResolver(assemblyPath);

            // store all of the assembly simple names that should be deferred w/
            // the sharing assembly context loader (and not resolved exclusively in this loader)
            this.deferredDependencyAssemblyNames = deferredDependencyAssemblyNames
                .Select(an => an.Name!)
                .Where(n => n != null)
                .ToImmutableHashSet();

            // store a reference to the assembly load context that assembly resolution will be deferred
            // to when on the deferredDependencyAssemblyNames list
            this.deferToContext = deferToContext;

            // load the non-deferred assembly in this context to start
            Load(GetAssemblyName(assemblyPath));
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            if (assemblyName.Name == null)
            {
                return null;
            }

            // if the assembly to be loaded is also set to be deferrred (based on constructor)
            // then first attempt to load it from the sharing assembly load context
            if (deferredDependencyAssemblyNames.Contains(assemblyName.Name))
            {
                return deferToContext.LoadFromAssemblyName(assemblyName);
            }

            // all other loaded assemblies should be considered dependencies of the
            // non-deferred dependency loaded in the constructor and should be loaded
            // from its path (the AssemblyDepedencyResolver resolves dependency paths)
            string? assemblyPath = resolver.ResolveAssemblyToPath(assemblyName);
            if (assemblyPath == null)
            {
                return null;
            }

            return LoadFromAssemblyPath(assemblyPath);
        }

        public static Assembly LoadAssemblyFrom(
            string assemblyPath,
            AssemblyLoadContext deferToContext,
            params AssemblyName[] deferredDependencyAssemblyNames
        )
        {
            DeferredDependencyAssemblyLoadContext loadContext =
                new DeferredDependencyAssemblyLoadContext(
                    assemblyPath,
                    deferToContext,
                    deferredDependencyAssemblyNames
                );
            return loadContext.LoadFromAssemblyName(
                new AssemblyName(Path.GetFileNameWithoutExtension(assemblyPath))
            );
        }
    }
}
