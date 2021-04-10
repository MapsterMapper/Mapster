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

        private static string? GetSegments(string? ns, string? baseNs)
        {
            if (ns == null || string.IsNullOrEmpty(baseNs) || baseNs == ns)
                return null;
            return ns.StartsWith(baseNs + ".") ? ns.Substring(baseNs.Length + 1) : ns;
        }

        private static string? CreateNamespace(string? ns, string? segment, string? typeNs)
        {
            if (ns == null)
                return typeNs;
            return segment == null ? ns : $"{ns}.{segment}";
        }

        private static string GetOutput(string baseOutput, string? segment, string typeName)
        {
            var fullBasePath = Path.GetFullPath(baseOutput);
            return segment == null 
                ? Path.Combine(fullBasePath, typeName + ".g.cs") 
                : Path.Combine(fullBasePath, segment.Replace('.', '/'), typeName + ".g.cs");
        }

        private static void WriteFile(string code, string path)
        {
            var dir = Path.GetDirectoryName(path);
            if (dir != null)
                Directory.CreateDirectory(dir);
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

                var segments = GetSegments(type.Namespace, opt.BaseNamespace);
                var definitions = new TypeDefinitions
                {
                    Implements = new[] {type},
                    Namespace = CreateNamespace(opt.Namespace, segments, type.Namespace),
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
                var path = GetOutput(opt.Output, segments, definitions.TypeName);
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
            var codeGenConfig = new CodeGenerationConfig();
            codeGenConfig.Scan(assembly);

            var types = assembly.GetTypes().ToHashSet();
            foreach (var builder in codeGenConfig.AdaptAttributeBuilders)
            {
                foreach (var setting in builder.TypeSettings)
                {
                    types.Add(setting.Key);
                }
            }
            foreach (var type in types)
            {
                var builders = type.GetAdaptAttributeBuilders(codeGenConfig)
                    .Where(it => !string.IsNullOrEmpty(it.Attribute.Name) && it.Attribute.Name != "[name]")
                    .ToList();
                if (builders.Count == 0)
                    continue;

                Console.WriteLine($"Processing: {type.FullName}");
                foreach (var builder in builders)
                {
                    CreateModel(opt, type, builder);
                }
            }
        }

        private static byte? GetTypeNullableContext(Type type)
        {
            var nilCtxAttr = type.GetCustomAttributesData()
                .FirstOrDefault(it => it.AttributeType.Name == "NullableContextAttribute");
            return nilCtxAttr?.ConstructorArguments.Count == 1 && nilCtxAttr.ConstructorArguments[0].Value is byte b
                ? (byte?) b
                : null;
        }
        private static void CreateModel(ModelOptions opt, Type type, AdaptAttributeBuilder builder)
        {
            var segments = GetSegments(type.Namespace, opt.BaseNamespace);
            var attr = builder.Attribute;
            var definitions = new TypeDefinitions
            {
                Namespace = CreateNamespace(opt.Namespace, segments, type.Namespace),
                TypeName = attr.Name!.Replace("[name]", type.Name),
                PrintFullTypeName = opt.PrintFullTypeName,
                IsRecordType = opt.IsRecordType,
                NullableContext = GetTypeNullableContext(type),
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

            var propSettings = builder.TypeSettings.GetValueOrDefault(type);
            var isReadOnly = isAdaptTo && attr.MapToConstructor;
            var isNullable = !isAdaptTo && attr.IgnoreNullValues;
            foreach (var member in properties)
            {
                var setting = propSettings?.GetValueOrDefault(member.Name);
                if (setting?.Ignore == true)
                    continue;
                
                var adaptMember = member.GetCustomAttribute<AdaptMemberAttribute>();
                if (!isTwoWays && adaptMember?.Side != null && adaptMember.Side != side)
                    adaptMember = null;
                var propType = setting?.MapFunc?.ReturnType ?? 
                               setting?.TargetPropertyType ??
                               GetPropertyType(member, getPropType(member), attr.GetType(), opt.Namespace, builder);
                var nilAttr = member.GetCustomAttributesData()
                    .FirstOrDefault(it => it.AttributeType.Name == "NullableAttribute");
                var nilAttrArg = nilAttr?.ConstructorArguments.Count == 1 ? nilAttr.ConstructorArguments[0].Value : null;
                translator.Properties.Add(new PropertyDefinitions
                {
                    Name = setting?.TargetPropertyName ?? adaptMember?.Name ?? member.Name,
                    Type = isNullable ? propType.MakeNullable() : propType,
                    IsReadOnly = isReadOnly,
                    NullableContext = nilAttrArg is byte b ? (byte?)b : null,
                    Nullable = nilAttrArg is byte[] bytes ? bytes : null,
                });
            }

            var code = translator.ToString();
            var path = GetOutput(opt.Output, segments, definitions.TypeName);
            WriteFile(code, path);

            static Type getPropType(MemberInfo mem)
            {
                return mem is PropertyInfo p ? p.PropertyType : ((FieldInfo) mem).FieldType;
            }
        }

        private static readonly Dictionary<string, MockType> _mockTypes = new Dictionary<string, MockType>();
        private static Type GetPropertyType(MemberInfo member, Type propType, Type attrType, string? ns, AdaptAttributeBuilder builder)
        {
            var navAttr = member.SafeGetCustomAttributes()
                .OfType<PropertyTypeAttribute>()
                .FirstOrDefault(it => it.ForAttributes?.Contains(attrType) != false);
            if (navAttr != null)
                return navAttr.Type;

            if (propType.IsCollection() && propType.IsCollectionCompatible() && propType.IsGenericType && propType.GetGenericArguments().Length == 1)
            {
                var elementType = propType.GetGenericArguments()[0];
                var newType = GetPropertyType(member, elementType, attrType, ns, builder);
                if (elementType == newType)
                    return propType;
                var generic = propType.GetGenericTypeDefinition();
                return generic.MakeGenericType(newType);
            }

            var alterType = builder.AlterTypes
                .Select(fn => fn(propType))
                .FirstOrDefault(it => it != null);
            if (alterType != null)
                return alterType;

            var propTypeAttrs = propType.SafeGetCustomAttributes();
            navAttr = propTypeAttrs.OfType<PropertyTypeAttribute>()
                .FirstOrDefault(it => it.ForAttributes?.Contains(attrType) != false);
            if (navAttr != null)
                return navAttr.Type;

            var adaptAttr = builder.TypeSettings.ContainsKey(propType)
                ? (BaseAdaptAttribute?) builder.Attribute
                : propTypeAttrs.OfType<BaseAdaptAttribute>()
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

        private static Type? GetFromType(Type type, BaseAdaptAttribute attr, HashSet<Type> types)
        {
            if (!(attr is AdaptFromAttribute) && !(attr is AdaptTwoWaysAttribute)) 
                return null;

            var fromType = attr.Type;
            if (fromType == null && attr.Name != null)
            {
                var name = attr.Name.Replace("[name]", type.Name);
                fromType = types.FirstOrDefault(it => it.Name == name);
            }

            return fromType;
        }

        private static Type? GetToType(Type type, BaseAdaptAttribute attr, HashSet<Type> types)
        {
            if (!(attr is AdaptToAttribute)) 
                return null;

            var toType = attr.Type;
            if (toType == null && attr.Name != null)
            {
                var name = attr.Name.Replace("[name]", type.Name);
                toType = types.FirstOrDefault(it => it.Name == name);
            }

            return toType;
        }

        private static void ApplySettings(TypeAdapterSetter setter, BaseAdaptAttribute attr, Dictionary<string, PropertySetting> settings)
        {
            setter.ApplyAdaptAttribute(attr);
            foreach (var (name, setting) in settings)
            {
                if (setting.MapFunc != null)
                {
                    setter.Settings.Resolvers.Add(new InvokerModel
                    {
                        DestinationMemberName = setting.TargetPropertyName ?? name,
                        SourceMemberName = name,
                        Invoker = setting.MapFunc,
                    });
                }
                else if (setting.TargetPropertyName != null)
                {
                    setter.Map(setting.TargetPropertyName, name);
                }
            }
        }

        private static void GenerateExtensions(ExtensionOptions opt)
        {
            using var dynamicContext = new AssemblyResolver(Path.GetFullPath(opt.Assembly));
            var assembly = dynamicContext.Assembly;
            var config = TypeAdapterConfig.GlobalSettings;
            config.SelfContainedCodeGeneration = true;
            config.Scan(assembly);
            var codeGenConfig = new CodeGenerationConfig();
            codeGenConfig.Scan(assembly);

            var assemblies = new HashSet<Assembly> {assembly};
            foreach (var builder in codeGenConfig.AdaptAttributeBuilders)
            {
                foreach (var setting in builder.TypeSettings)
                {
                    assemblies.Add(setting.Key.Assembly);
                }
            }
            var types = assemblies.SelectMany(it => it.GetTypes()).ToHashSet();
            var configDict = new Dictionary<BaseAdaptAttribute, TypeAdapterConfig>();
            foreach (var builder in codeGenConfig.AdaptAttributeBuilders)
            {
                var attr = builder.Attribute;
                var cloned = config.Clone();
                foreach (var (type, settings) in builder.TypeSettings)
                {
                    var fromType = GetFromType(type, attr, types);
                    if (fromType != null)
                        ApplySettings(cloned.ForType(fromType, type), attr, settings);

                    var toType = GetToType(type, attr, types);
                    if (toType != null)
                        ApplySettings(cloned.ForType(type, toType), attr, settings);
                }

                configDict[attr] = cloned;
            }

            foreach (var type in types)
            {
                var mapperAttr = type.GetGenerateMapperAttributes(codeGenConfig).FirstOrDefault();
                var ruleMaps = config.RuleMap
                    .Where(it => it.Key.Source == type &&
                                 it.Value.Settings.GenerateMapper is MapType)
                    .ToList();
                if (mapperAttr == null && ruleMaps.Count == 0)
                    continue;

                mapperAttr ??= new GenerateMapperAttribute();
                var set = mapperAttr.ForAttributes?.ToHashSet();
                var builders = type.GetAdaptAttributeBuilders(codeGenConfig)
                    .Where(it => set?.Contains(it.GetType()) != false)
                    .ToList();
                if (builders.Count == 0 && ruleMaps.Count == 0)
                    continue;

                Console.WriteLine($"Processing: {type.FullName}");

                var segments = GetSegments(type.Namespace, opt.BaseNamespace);
                var definitions = new TypeDefinitions
                {
                    IsStatic = true,
                    Namespace = CreateNamespace(opt.Namespace, segments, type.Namespace),
                    TypeName = mapperAttr.Name.Replace("[name]", type.Name),
                    IsInternal = mapperAttr.IsInternal,
                    PrintFullTypeName = opt.PrintFullTypeName,
                };
                var translator = new ExpressionTranslator(definitions);

                foreach (var builder in builders)
                {
                    var attr = builder.Attribute;
                    var cloned = configDict.GetValueOrDefault(attr) ?? config;
                    var fromType = GetFromType(type, attr, types);
                    if (fromType != null)
                    {
                        var tuple = new TypeTuple(fromType, type);
                        var mapType = attr.MapType == 0 ? MapType.Map | MapType.MapToTarget : attr.MapType;
                        GenerateExtensionMethods(mapType, cloned, tuple, translator, type, mapperAttr.IsHelperClass);
                    }

                    var toType = GetToType(type, attr, types);
                    if (toType != null && (!(attr is AdaptTwoWaysAttribute) || type != toType))
                    {
                        var tuple = new TypeTuple(type, toType);
                        var mapType = attr.MapType == 0
                            ? MapType.Map | MapType.MapToTarget
                            : attr.MapType;
                        GenerateExtensionMethods(mapType, cloned, tuple, translator, type, mapperAttr.IsHelperClass);
                    }
                }

                foreach (var (tuple, rule) in ruleMaps)
                {
                    var mapType = (MapType) rule.Settings.GenerateMapper!;
                    GenerateExtensionMethods(mapType, config, tuple, translator, type, mapperAttr.IsHelperClass);
                }

                var code = translator.ToString();
                var path = GetOutput(opt.Output, segments, definitions.TypeName);
                WriteFile(code, path);
            }
        }

        private static void GenerateExtensionMethods(MapType mapType, TypeAdapterConfig config, TypeTuple tuple,
            ExpressionTranslator translator, Type entityType, bool isHelperClass)
        {
            //add type name to prevent duplication
            translator.Translate(entityType);
            var destName = translator.Translate(tuple.Destination);
            destName = destName.Split('.').Last();

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