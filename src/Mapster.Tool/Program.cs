using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using CommandLine;
using CommandLine.Text;
using ExpressionDebugger;
using Mapster.Models;

namespace Mapster.Tool
{
    class Program
    {
        public class Options
        {
            [Option('a', "assembly", Required = true, HelpText = "Assembly to scan")]
            public string Assembly { get; set; }

            [Option('o', "output", Required = false, Default = "Mappers", HelpText = "Output directory.")]
            public string Output { get; set; }
            
            [Usage(ApplicationAlias = "dotnet mapster")]
            public static IEnumerable<Example> Examples =>
                new List<Example> {
                    new Example("Generate mapping", new Options { Assembly = "/Path/To/YourAssembly.dll", Output = "Mappers"})
                };
        }

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(opt =>
                {
                    using var dynamicContext = new AssemblyResolver(Path.GetFullPath(opt.Assembly));
                    var assembly = dynamicContext.Assembly;
                    var config = TypeAdapterConfig.GlobalSettings;
                    config.SelfContainedCodeGeneration = true;
                    config.Scan(assembly);

                    foreach (var type in assembly.GetTypes())
                    {
                        if (!type.IsInterface)
                            continue;
                        var attr = type.GetCustomAttribute<MapperAttribute>();
                        if (attr == null)
                            continue;
                        
                        Console.WriteLine($"Processing: {type.FullName}");
                        
                        var definitions = new TypeDefinitions
                        {
                            Implements = new[] {type},
                            Namespace = type.Namespace,
                            TypeName = attr.Name ?? GetName(type.Name)
                        };
                        var translator = new ExpressionTranslator(definitions);
                        var interfaces = GetAllInterfaces(type);
                        foreach (var @interface in interfaces)
                        {
                            foreach (var prop in @interface.GetProperties())
                            {
                                if (!prop.PropertyType.IsGenericType)
                                    continue;
                                if (prop.PropertyType.GetGenericTypeDefinition() != typeof(Expression<>))
                                    continue;
                                var propArgs = prop.PropertyType.GetGenericArguments()[0];
                                if (!propArgs.IsGenericType)
                                    continue;
                                if (propArgs.GetGenericTypeDefinition() != typeof(Func<,>))
                                    continue;
                                var funcArgs = propArgs.GetGenericArguments();
                                var tuple = new TypeTuple(funcArgs[0], funcArgs[1]);
                                var expr = config.CreateMapExpression(tuple, MapType.Projection);
                                translator.VisitLambda(expr, ExpressionTranslator.LambdaType.PublicLambda, prop.Name);
                            }
                        }
                        foreach (var @interface in interfaces)
                        {
                            foreach (var method in @interface.GetMethods())
                            {
                                if (method.IsGenericMethod)
                                    continue;
                                if (method.ReturnType == typeof(void))
                                    continue;
                                var methodArgs = method.GetParameters();
                                if (methodArgs.Length < 1 || methodArgs.Length > 2)
                                    continue;
                                var tuple = new TypeTuple(methodArgs[0].ParameterType, method.ReturnType);
                                var expr = config.CreateMapExpression(tuple, methodArgs.Length == 1 ? MapType.Map : MapType.MapToTarget);
                                translator.VisitLambda(expr, ExpressionTranslator.LambdaType.PublicMethod, method.Name);
                            }
                        }

                        var code = translator.ToString();
                        var path = Path.Combine(Path.GetFullPath(opt.Output), definitions.TypeName + ".cs");
                        Directory.CreateDirectory(Path.GetDirectoryName(path));
                        File.WriteAllText(path, code);
                    }
                });
        }

        private static string GetName(string name)
        {
            if (name.Length >= 2 && name[0] == 'I' && name[1] >= 'A' && name[1] <= 'Z')
                return name.Substring(1);
            return name + "Impl";
        }

        private static HashSet<Type> GetAllInterfaces(Type interfaceType)
        {
            var allInterfaces = new HashSet<Type>();
            var interfaceQueue = new Queue<Type>();
            allInterfaces.Add(interfaceType);
            interfaceQueue.Enqueue(interfaceType);
            while (interfaceQueue.Count > 0)
            {
                var currentInterface = interfaceQueue.Dequeue();
                foreach (var subInterface in currentInterface.GetInterfaces())
                {
                    if (allInterfaces.Contains(subInterface))
                        continue;
                    allInterfaces.Add(subInterface);
                    interfaceQueue.Enqueue(subInterface);
                }
            }
            return allInterfaces;
        }
    }
}