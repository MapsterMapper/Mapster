using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Mapster.Tool
{
    internal static class Extensions
    {
        public static HashSet<Type> GetAllInterfaces(this Type interfaceType)
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
                    if (allInterfaces.Add(subInterface))
                        interfaceQueue.Enqueue(subInterface);
                }
            }
            return allInterfaces;
        }

        public static IEnumerable<MemberInfo> GetFieldsAndProperties(this Type type)
        {
            var bindingFlags = BindingFlags.Instance | BindingFlags.Public;
            
            if (type.GetTypeInfo().IsInterface)
            {
                var allInterfaces = GetAllInterfaces(type);
                return allInterfaces.SelectMany(GetPropertiesFunc);
            }

            return GetPropertiesFunc(type).Concat(GetFieldsFunc(type));

            IEnumerable<MemberInfo> GetPropertiesFunc(Type t) => t.GetProperties(bindingFlags)
                .Where(x => x.GetIndexParameters().Length == 0);

            IEnumerable<MemberInfo> GetFieldsFunc(Type t) => t.GetFields(bindingFlags);
        }

        public static bool IsCollection(this Type type)
        {
            return typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()) && type != typeof(string);
        }

        public static bool IsAssignableFromList(this Type type)
        {
            var elementType = type.ExtractCollectionType();
            var listType = typeof(List<>).MakeGenericType(elementType);
            return type.GetTypeInfo().IsAssignableFrom(listType.GetTypeInfo());
        }

        public static bool IsAssignableFromSet(this Type type)
        {
            var elementType = type.ExtractCollectionType();
            var setType = typeof(HashSet<>).MakeGenericType(elementType);
            return type.GetTypeInfo().IsAssignableFrom(setType.GetTypeInfo());
        }
        
        public static bool IsAssignableFromCollection(this Type type)
        {
            return type.IsAssignableFromList() || type.IsAssignableFromSet();
        }

        public static bool IsCollectionCompatible(this Type type)
        {
            var typeInfo = type.GetTypeInfo();
            if (typeInfo.IsInterface)
                return type.IsAssignableFromCollection();

            if (typeInfo.IsAbstract)
                return false;

            var elementType = type.ExtractCollectionType();
            if (typeof(ICollection<>).MakeGenericType(elementType).GetTypeInfo().IsAssignableFrom(typeInfo))
                return true;

            if (typeof(IList).GetTypeInfo().IsAssignableFrom(typeInfo))
                return true;

            return false;
        }

        public static Type ExtractCollectionType(this Type collectionType)
        {
            if (collectionType.IsArray)
                return collectionType.GetElementType()!;
            var enumerableType = collectionType.GetGenericEnumerableType();
            return enumerableType != null
                ? enumerableType.GetGenericArguments()[0]
                : typeof(object);
        }

        public static Type? GetInterface(this Type type, Predicate<Type> predicate)
        {
            if (predicate(type))
                return type;

            return Array.Find(type.GetInterfaces(), predicate);
        }

        public static Type? GetGenericEnumerableType(this Type type)
        {
            return type.GetInterface(IsGenericEnumerableType);
        }

        public static bool IsGenericEnumerableType(this Type type)
        {
            return type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>);
        }

        public static Attribute[] SafeGetCustomAttributes(this MemberInfo element)
        {
            try
            {
                return Attribute.GetCustomAttributes(element);
            }
            catch
            {
                return Array.Empty<Attribute>();
            }
        }

        public static IEnumerable<AdaptAttributeBuilder> GetAdaptAttributeBuilders(this Type type, CodeGenerationConfig config)
        {
            foreach (var attribute in type.SafeGetCustomAttributes())
            {
                if (attribute is BaseAdaptAttribute adaptAttr)
                    yield return new AdaptAttributeBuilder(adaptAttr);
            }

            foreach (var builder in config.AdaptAttributeBuilders)
            {
                if (builder.TypeSettings.ContainsKey(type))
                    yield return builder;
            }
        }

        public static IEnumerable<GenerateMapperAttribute> GetGenerateMapperAttributes(this Type type, CodeGenerationConfig config)
        {
            foreach (var attribute in type.SafeGetCustomAttributes())
            {
                if (attribute is GenerateMapperAttribute genMapperAttr)
                    yield return genMapperAttr;
            }

            foreach (var builder in config.GenerateMapperAttributeBuilders)
            {
                if (builder.Types.Contains(type))
                    yield return builder.Attribute;
            }
        }

        public static bool IsNullable(this Type type)
        {
            return type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        public static bool CanBeNull(this Type type)
        {
            return !type.GetTypeInfo().IsValueType || type.IsNullable();
        }

        public static Type MakeNullable(this Type type)
        {
            return type.CanBeNull() ? type : typeof(Nullable<>).MakeGenericType(type);
        }

        public static void Scan(this CodeGenerationConfig config, Assembly assembly)
        {
            var registers = assembly.GetTypes()
                .Where(x => typeof(ICodeGenerationRegister).GetTypeInfo().IsAssignableFrom(x.GetTypeInfo()) &&
                            x.GetTypeInfo().IsClass && !x.GetTypeInfo().IsAbstract)
                .Select(type => (ICodeGenerationRegister) Activator.CreateInstance(type)!);

            foreach (var register in registers)
            {
                register.Register(config);
            }
        }
    }
}
