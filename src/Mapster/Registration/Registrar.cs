using System;
using System.Linq;
using System.Reflection;

namespace Mapster.Registration
{
    public static class Registrar
    {
        public static Action<string> OutputWriter { get; set; }

        public static void Register(this IRegistry registry)
        {
            if (OutputWriter != null)
            {
                OutputWriter(registry.Name);
            }

            registry.Apply();
        }

        public static void RegisterFromAssemblyContaining<T>()
        {
            RegisterFromAssemblyContaining(typeof(T));
        }

        public static void RegisterFromAssemblyContaining(this Type type)
        {
            RegisterFromAssembly(type.Assembly);
        }

        public static void RegisterFromAssembly(this Assembly assembly)
        {
            var registryTypes = assembly.GetExportedTypes()
                .Where(t => !t.IsInterface && typeof (IRegistry).IsAssignableFrom(t));

            foreach (var regType in registryTypes)
            {
                var registry = (IRegistry)Activator.CreateInstance(regType);

                Register(registry);
            }
        } 
    }
}