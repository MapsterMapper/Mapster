using ExpressionDebugger;
using Mapster.Diagnostics;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;

namespace Mapster
{
    public static class TypeAdapterConfigExtensions
    {
        static Dictionary<string, int> registeredFilename;

        [Conditional("DEBUG")]
        public static void EnableDebugging(this TypeAdapterConfig config, string sourceCodePath = null)
        {
            if (sourceCodePath == null)
                sourceCodePath = GetDefaultSourceCodePath();

            if (registeredFilename == null)
                registeredFilename = new Dictionary<string, int>();
            config.Compiler = lambda =>
            {
                var filename = lambda.Parameters[0].Type.Name + "-" + lambda.ReturnType.Name;
                var key = filename;
                lock (registeredFilename)
                {
                    if (!registeredFilename.TryGetValue(key, out var num))
                        registeredFilename[key] = 0;
                    else
                        filename += "-" + num;
                    registeredFilename[key]++;
                }
                using (var injector = new DebugInfoInjectorEx(Path.Combine(sourceCodePath, filename + ".cs")))
                {
                    return injector.Compile(lambda, _an);
                }
            };
        }

        private static string GetDefaultSourceCodePath()
        {
            var dir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Mapster");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return dir;
        }

        static AssemblyName _an = GetAssemblyName();
        private static AssemblyName GetAssemblyName()
        {
            StrongNameKeyPair kp;
            // Getting this from a resource would be a good idea.
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Mapster.Diagnostics.mock.keys"))
            using (var mem = new MemoryStream())
            {
                stream.CopyTo(mem);
                mem.Position = 0;
                kp = new StrongNameKeyPair(mem.ToArray());
            }
            var name = "Mapster.Dynamic";
            return new AssemblyName(name) { KeyPair = kp };
        }
    }
}
