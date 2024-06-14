using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace Mapster.Tool
{
    public class IsolatedAssemblyContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver resolver;

        public IsolatedAssemblyContext(string assemblyPath)
        {
            resolver = new AssemblyDependencyResolver(assemblyPath);
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            string assemblyPath = resolver.ResolveAssemblyToPath(assemblyName);
            if (assemblyPath != null)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }

            return null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            string libraryPath = resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            if (libraryPath != null)
            {
                return LoadUnmanagedDllFromPath(libraryPath);
            }

            return IntPtr.Zero;
        }

        public static Assembly LoadAssemblyFrom(string assemblyPath)
        {
            IsolatedAssemblyContext loadContext = new IsolatedAssemblyContext(assemblyPath);
            return loadContext.LoadFromAssemblyName(
                new AssemblyName(Path.GetFileNameWithoutExtension(assemblyPath))
            );
        }
    }
}
