using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using CommandLine;
using ExpressionDebugger;
using Mapster.Models;

namespace Mapster.Tool
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<MapperOptions, ModelOptions, ExtensionOptions>(args)
                .WithParsed<MapperOptions>(GenerateMappers)
                .WithParsed<ModelOptions>(GenerateModels)
                .WithParsed<ExtensionOptions>(GenerateExtensions);
        }

        private static void WriteFile(string code, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            if (File.Exists(path))
            {
                var old = File.ReadAllText(path);
                if (old == code)
                    return;
            }
            File.WriteAllText(path, code);
        }

        private static void GenerateMappers(MapperOptions opt)
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
                    Namespace = opt.Namespace ?? type.Namespace,
                    TypeName = attr.Name ?? GetImplName(type.Name),
                    IsInternal = attr.IsInternal,
                    PrintFullTypeName = opt.PrintFullTypeName,
                };
                var translator = new ExpressionTranslator(definitions);
                var interfaces = type.GetAllInterfaces();
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
                        translator.VisitLambda(expr, ExpressionTranslator.LambdaType.PublicLambda,
                            prop.Name);
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
                        var expr = config.CreateMapExpression(tuple,
                            methodArgs.Length == 1 ? MapType.Map : MapType.MapToTarget);
                        translator.VisitLambda(expr, ExpressionTranslator.LambdaType.PublicMethod,
                            method.Name);
                    }
                }

                var code = translator.ToString();
                var path = Path.Combine(Path.GetFullPath(opt.Output), definitions.TypeName + ".g.cs");
                WriteFile(code, path);
            }
        }

        private static string GetImplName(string name)
        {
            if (name.Length >= 2 && name[0] == 'I' && name[1] >= 'A' && name[1] <= 'Z')
                return name.Substring(1);
            return name + "Impl";
        }

        private static void GenerateModels(ModelOptions opt)
        {
            using var dynamicContext = new AssemblyResolver(Path.GetFullPath(opt.Assembly));
            var assembly = dynamicContext.Assembly;

            foreach (var type in assembly.GetTypes())
            {
                var attrs = type.SafeGetCustomAttributes()
                    .OfType<BaseAdaptAttribute>()
                    .Where(it => !string.IsNullOrEmpty(it.Name) && it.Name != "[name]")
                    .ToList();
                if (attrs.Count == 0)
                    continue;

                Console.WriteLine($"Processing: {type.FullName}");
                foreach (var attr in attrs)
                {
                    CreateModel(opt, type, attr);
                }
            }
        }

        private static void CreateModel(ModelOptions opt, Type type, BaseAdaptAttribute attr)
        {
            var definitions = new TypeDefinitions
            {
                Namespace = opt.Namespace ?? type.Namespace,
                TypeName = attr.Name!.Replace("[name]", type.Name),
                PrintFullTypeName = opt.PrintFullTypeName,
            };
            var translator = new ExpressionTranslator(definitions);
            var isAdaptTo = attr is AdaptToAttribute;
            var isTwoWays = attr is AdaptTwoWaysAttribute;
            var side = isAdaptTo ? MemberSide.Source : MemberSide.Destination;
            var properties = type.GetFieldsAndProperties().Where(it =>
                !it.SafeGetCustomAttributes().OfType<AdaptIgnoreAttribute>()
                    .Any(it2 => isTwoWays || it2.Side == null || it2.Side == side));

            if (attr.IgnoreAttributes != null)
            {
                properties = properties.Where(it =>
                    !it.SafeGetCustomAttributes()
                        .Select(it2 => it2.GetType())
                        .Intersect(attr.IgnoreAttributes)
                        .Any());
            }

            if (attr.IgnoreNoAttributes != null)
            {
                properties = properties.Where(it =>
                    it.SafeGetCustomAttributes()
                        .Select(it2 => it2.GetType())
                        .Intersect(attr.IgnoreNoAttributes)
                        .Any());
            }

            if (attr.IgnoreNamespaces != null)
            {
                foreach (var ns in attr.IgnoreNamespaces)
                {
                    properties = properties.Where(it => getPropType(it).Namespace?.StartsWith(ns) != true);
                }
            }
            var isReadOnly = isAdaptTo && attr.MapToConstructor;
            var isNullable = !isAdaptTo && attr.IgnoreNullValues;
            foreach (var member in properties)
            {
                var adaptMember = member.GetCustomAttribute<AdaptMemberAttribute>();
                var propType = GetPropertyType(member, getPropType(member), attr.GetType(), opt.Namespace);
                translator.Properties.Add(new PropertyDefinitions
                {
                    Name = adaptMember?.Name ?? member.Name,
                    Type = isNullable ? propType.MakeNullable() : propType,
                    IsReadOnly = isReadOnly
                });
            }

            var code = translator.ToString();
            var path = Path.Combine(Path.GetFullPath(opt.Output), definitions.TypeName + ".g.cs");
            WriteFile(code, path);

            static Type getPropType(MemberInfo mem)
            {
                return mem is PropertyInfo p ? p.PropertyType : ((FieldInfo) mem).FieldType;
            }
        }

        private static readonly Dictionary<string, MockType> _mockTypes = new Dictionary<string, MockType>();
        private static Type GetPropertyType(MemberInfo member, Type propType, Type attrType, string? ns)
        {
            var navAttr = member.SafeGetCustomAttributes()
                .OfType<PropertyTypeAttribute>()
                .FirstOrDefault(it => it.ForAttributes?.Contains(attrType) != false);
            if (navAttr != null)
                return navAttr.Type;

            if (propType.IsCollection() && propType.IsCollectionCompatible() && propType.IsGenericType && propType.GetGenericArguments().Length == 1)
            {
                var elementType = propType.GetGenericArguments()[0];
                var newType = GetPropertyType(member, elementType, attrType, ns);
                if (elementType == newType)
                    return propType;
                var generic = propType.GetGenericTypeDefinition();
                return generic.MakeGenericType(newType);
            }

            var propTypeAttrs = propType.SafeGetCustomAttributes();
            navAttr = propTypeAttrs.OfType<PropertyTypeAttribute>()
                .FirstOrDefault(it => it.ForAttributes?.Contains(attrType) != false);
            if (navAttr != null)
                return navAttr.Type;
            var adaptAttr = propTypeAttrs.OfType<BaseAdaptAttribute>()
                .FirstOrDefault(it => it.GetType() == attrType);
            if (adaptAttr == null)
                return propType;
            if (adaptAttr.Type != null)
                return adaptAttr.Type;

            var name = adaptAttr.Name!.Replace("[name]", propType.Name);
            if (!_mockTypes.TryGetValue(name, out var mockType))
            {
                mockType = new MockType(ns ?? propType.Namespace!, name, propType.Assembly);
                _mockTypes[name] = mockType;
            }
            return mockType;
        }

        private static void GenerateExtensions(ExtensionOptions opt)
        {
            using var dynamicContext = new AssemblyResolver(Path.GetFullPath(opt.Assembly));
            var assembly = dynamicContext.Assembly;
            var config = TypeAdapterConfig.GlobalSettings;
            config.SelfContainedCodeGeneration = true;
            config.Scan(assembly);

            var types = assembly.GetTypes();
            foreach (var type in types)
            {
                var attrs = type.SafeGetCustomAttributes();
                var mapperAttr = attrs.OfType<GenerateMapperAttribute>()
                    .FirstOrDefault();
                var ruleMaps = config.RuleMap
                    .Where(it => it.Key.Source == type &&
                                 it.Value.Settings.GenerateMapper is MapType)
                    .ToList();
                if (mapperAttr == null && ruleMaps.Count == 0)
                    continue;

                mapperAttr ??= new GenerateMapperAttribute();
                var set = mapperAttr.ForAttributes?.ToHashSet();
                var adaptAttrs = attrs
                    .OfType<BaseAdaptAttribute>()
                    .Where(it => set?.Contains(it.GetType()) != false)
                    .ToList();
                if (adaptAttrs.Count == 0 && ruleMaps.Count == 0)
                    continue;

                Console.WriteLine($"Processing: {type.FullName}");

                var definitions = new TypeDefinitions
                {
                    IsStatic = true,
                    Namespace = opt.Namespace ?? type.Namespace,
                    TypeName = mapperAttr.Name.Replace("[name]", type.Name),
                    IsInternal = mapperAttr.IsInternal,
                    PrintFullTypeName = opt.PrintFullTypeName,
                };
                var translator = new ExpressionTranslator(definitions);

                foreach (var attr in adaptAttrs)
                {
                    if (attr is AdaptFromAttribute || attr is AdaptTwoWaysAttribute)
                    {
                        var fromType = attr.Type;
                        if (fromType == null && attr.Name != null)
                        {
                            var name = attr.Name.Replace("[name]", type.Name);
                            fromType = Array.Find(types, it => it.Name == name);
                        }

                        if (fromType == null)
                            continue;

                        var tuple = new TypeTuple(fromType, type);
                        var mapType = attr.MapType == 0 ? MapType.Map | MapType.MapToTarget : attr.MapType;
                        GenerateExtensionMethods(mapType, config, tuple, translator, type, mapperAttr.IsHelperClass);
                    }

                    if (attr is AdaptToAttribute)
                    {
                        var toType = attr.Type;
                        if (toType == null && attr.Name != null)
                        {
                            var name = attr.Name.Replace("[name]", type.Name);
                            toType = Array.Find(types, it => it.Name == name);
                        }

                        if (toType == null)
                            continue;

                        if (attr is AdaptTwoWaysAttribute && type == toType)
                            continue;

                        var tuple = new TypeTuple(type, toType);
                        var mapType = attr.MapType == 0 ? MapType.Map | MapType.MapToTarget | MapType.Projection : attr.MapType;
                        GenerateExtensionMethods(mapType, config, tuple, translator, type, mapperAttr.IsHelperClass);
                    }
                }

                foreach (var (tuple, rule) in ruleMaps)
                {
                    var mapType = (MapType) rule.Settings.GenerateMapper!;
                    GenerateExtensionMethods(mapType, config, tuple, translator, type, mapperAttr.IsHelperClass);
                }

                var code = translator.ToString();
                var path = Path.Combine(Path.GetFullPath(opt.Output), definitions.TypeName + ".g.cs");
                WriteFile(code, path);
            }
        }

        private static void GenerateExtensionMethods(MapType mapType, TypeAdapterConfig config, TypeTuple tuple,
            ExpressionTranslator translator, Type entityType, bool isHelperClass)
        {
            //add type name to prevent duplication
            translator.Translate(entityType);
            var destName = translator.Translate(tuple.Destination);

            var name = tuple.Destination.Name == entityType.Name
                ? destName
                : destName.Replace(entityType.Name, "");
            if ((mapType & MapType.Map) > 0)
            {
                var expr = config.CreateMapExpression(tuple, MapType.Map);
                translator.VisitLambda(expr, isHelperClass ? ExpressionTranslator.LambdaType.PublicMethod : ExpressionTranslator.LambdaType.ExtensionMethod,
                    "AdaptTo" + name);
            }

            if ((mapType & MapType.MapToTarget) > 0)
            {
                var expr2 = config.CreateMapExpression(tuple, MapType.MapToTarget);
                translator.VisitLambda(expr2, isHelperClass ? ExpressionTranslator.LambdaType.PublicMethod : ExpressionTranslator.LambdaType.ExtensionMethod,
                    "AdaptTo");
            }

            if ((mapType & MapType.Projection) > 0)
            {
                var proj = config.CreateMapExpression(tuple, MapType.Projection);
                translator.VisitLambda(proj, ExpressionTranslator.LambdaType.PublicLambda,
                    "ProjectTo" + name);
            }
        }
    }
}