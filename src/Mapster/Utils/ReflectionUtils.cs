using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Mapster.Models;
using Mapster.Utils;

// ReSharper disable once CheckNamespace
namespace Mapster
{
    internal static class ReflectionUtils
    {
        // Primitive types with their conversion methods from System.Convert class.
        private static readonly Dictionary<Type, string> _primitiveTypes = new Dictionary<Type, string>() {
            { typeof(bool), "ToBoolean" },
            { typeof(short), "ToInt16" },
            { typeof(int), "ToInt32" },
            { typeof(long), "ToInt64" },
            { typeof(float), "ToSingle" },
            { typeof(double), "ToDouble" },
            { typeof(decimal), "ToDecimal" },
            { typeof(ushort), "ToUInt16" },
            { typeof(uint), "ToUInt32" },
            { typeof(ulong), "ToUInt64" },
            { typeof(byte), "ToByte" },
            { typeof(sbyte), "ToSByte" },
            { typeof(DateTime), "ToDateTime" }
        };

#if NET40
        public static Type GetTypeInfo(this Type type)
        {
            return type;
        }
#endif

        public static bool IsNullable(this Type type)
        {
            return type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        public static bool CanBeNull(this Type type)
        {
            return !type.GetTypeInfo().IsValueType || type.IsNullable();
        }

        public static bool IsPoco(this Type type)
        {
            //not nullable
            if (type.IsNullable())
                return false;

            //not primitives
            if (type.IsConvertible())
                return false;

            return type.GetFieldsAndProperties().Any(it => (it.SetterModifier & (AccessModifier.Public | AccessModifier.NonPublic)) != 0);
        }

        public static IEnumerable<IMemberModelEx> GetFieldsAndProperties(this Type type, bool includeNonPublic = false)
        {
            var bindingFlags = BindingFlags.Instance | BindingFlags.Public;
            if (includeNonPublic)
                bindingFlags |= BindingFlags.NonPublic;
            
            if (type.GetTypeInfo().IsInterface)
            {
                var allInterfaces = GetAllInterfaces(type);
                return allInterfaces.SelectMany(GetPropertiesFunc);
            }

            return GetPropertiesFunc(type).Concat(GetFieldsFunc(type));

            IEnumerable<IMemberModelEx> GetPropertiesFunc(Type t) => t.GetProperties(bindingFlags)
                .Where(x => x.GetIndexParameters().Length == 0)
                .Select(CreateModel);

            IEnumerable<IMemberModelEx> GetFieldsFunc(Type t) => t.GetFields(bindingFlags)
                .Select(CreateModel);
        }

        // GetProperties(), GetFields(), GetMethods() do not return properties/methods from parent interfaces,
        // so we need to process every one of them separately.
        public static IEnumerable<Type> GetAllInterfaces(this Type interfaceType)
        {
            var allInterfaces = new HashSet<Type>();
            var interfaceQueue = new Queue<Type>();

            allInterfaces.Add(interfaceType);
            yield return interfaceType;

            interfaceQueue.Enqueue(interfaceType);
            while (interfaceQueue.Count > 0)
            {
                var currentInterface = interfaceQueue.Dequeue();
                foreach (var subInterface in currentInterface.GetInterfaces())
                {
                    if (allInterfaces.Contains(subInterface))
                        continue;

                    allInterfaces.Add(subInterface);
                    yield return subInterface;
                    interfaceQueue.Enqueue(subInterface);
                }
            }
        }

        public static bool IsCollection(this Type type)
        {
            return typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()) && type != typeof(string);
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

        public static bool IsGenericEnumerableType(this Type type)
        {
            return type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>);
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

        public static Expression? CreateConvertMethod(Type srcType, Type destType, Expression source)
        {
            if (!_primitiveTypes.TryGetValue(destType, out var name))
                return null;

            var method = typeof(Convert).GetMethod(name, new[] { srcType });
            if (method != null)
                return Expression.Call(method, source);

            method = typeof(Convert).GetMethod(name, new[] { typeof(object) });
            return Expression.Convert(Expression.Call(method!, Expression.Convert(source, typeof(object))), destType);
        }

        public static Type UnwrapNullable(this Type type)
        {
            return type.IsNullable() ? type.GetGenericArguments()[0] : type;
        }

        public static bool IsRecordType(this Type type)
        {
            //not nullable
            if (type.IsNullable())
                return false;

            //not primitives
            if (type.IsConvertible())
                return false;

            var props = type.GetFieldsAndProperties().ToList();

            //interface, all props must be readonly
            if (type.GetTypeInfo().IsInterface && 
                props.All(p => p.SetterModifier != AccessModifier.Public))
                return true;

            //1 constructor
            var ctors = type.GetConstructors().ToList();
            if (ctors.Count != 1)
                return false;

            //ctor must not empty
            var ctorParams = ctors[0].GetParameters();
            if (ctorParams.Length == 0)
                return false;

            //all parameters should match getter
            return props.All(prop =>
            {
                var name = prop.Name.ToPascalCase();
                return ctorParams.Any(p => p.ParameterType == prop.Type && p.Name?.ToPascalCase() == name);
            });
        }

        public static bool IsConvertible(this Type type)
        {
            return typeof(IConvertible).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo());
        }

        public static IMemberModelEx CreateModel(this PropertyInfo propertyInfo)
        {
            return new PropertyModel(propertyInfo);
        }

        public static IMemberModelEx CreateModel(this FieldInfo propertyInfo)
        {
            return new FieldModel(propertyInfo);
        }

        public static IMemberModelEx CreateModel(this ParameterInfo propertyInfo)
        {
            return new ParameterModel(propertyInfo);
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

        public static Type? GetDictionaryType(this Type destinationType)
        {
#if !NET40
            if (destinationType.GetTypeInfo().IsInterface
                && destinationType.GetTypeInfo().IsGenericType
                && destinationType.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>))
            {
                return destinationType;
            }
#endif
            return destinationType.GetInterface(type => 
                type.GetTypeInfo().IsGenericType && 
                type.GetGenericTypeDefinition() == typeof(IDictionary<,>) );
        }

        public static AccessModifier GetAccessModifier(this FieldInfo memberInfo)
        {
            if (memberInfo.IsFamilyOrAssembly)
                return AccessModifier.ProtectedInternal;
            if (memberInfo.IsFamily)
                return AccessModifier.Protected;
            if (memberInfo.IsAssembly)
                return AccessModifier.Internal;
            if (memberInfo.IsPublic)
                return AccessModifier.Public;
            return AccessModifier.Private;
        }

        public static AccessModifier GetAccessModifier(this MethodBase methodBase)
        {
            if (methodBase.IsFamilyOrAssembly)
                return AccessModifier.ProtectedInternal;
            if (methodBase.IsFamily)
                return AccessModifier.Protected;
            if (methodBase.IsAssembly)
                return AccessModifier.Internal;
            if (methodBase.IsPublic)
                return AccessModifier.Public;
            return AccessModifier.Private;
        }

        public static bool ShouldMapMember(this IMemberModel member, CompileArgument arg, MemberSide side)
        {
            var predicates = arg.Settings.ShouldMapMember;
            var result = predicates.Select(predicate => predicate(member, side))
                .FirstOrDefault(it => it != null);
            if (result != null)
                return result == true;
            if (arg.Settings.EnableNonPublicMembers == true)
            {
                result = Mapster.ShouldMapMember.AllowNonPublic(member, side);
                if (result != null)
                    return result == true;
            }
            var names = side == MemberSide.Destination ? arg.GetDestinationNames() : arg.GetSourceNames();
            return names.Contains(member.Name);
        }

        public static bool UseDestinationValue(this IMemberModel member, CompileArgument arg)
        {
            var predicates = arg.Settings.UseDestinationValues;
            return predicates.Any(predicate => predicate(member));
        }

        public static string GetMemberName(this IMemberModel member, MemberSide side, List<Func<IMemberModel, MemberSide, string?>> getMemberNames, Func<string, string> nameConverter)
        {
            var memberName = getMemberNames.Select(func => func(member, side))
                .FirstOrDefault(name => name != null)
                ?? member.Name;

            return nameConverter(memberName);
        }

        public static bool IsPrimitiveKind(this Type type)
        {
            return type == typeof(object) || type.UnwrapNullable().IsConvertible();
        }

        public static Expression CreateDefault(this Type type)
        {
            return type.CanBeNull()
                ? Expression.Constant(null, type)
                : Expression.Constant(Activator.CreateInstance(type), type);
        }

        /// <summary>
        /// Determines whether the specific <paramref name="type"/> has default constructor.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        ///   <c>true</c> if specific <paramref name="type"/> has default constructor; otherwise <c>false</c>.
        /// </returns>
        public static bool HasDefaultConstructor(this Type type)
        {
            if (type == typeof(void)
                || type.GetTypeInfo().IsAbstract
                || type.GetTypeInfo().IsInterface)
                return false;
            if (type.GetTypeInfo().IsValueType)
                return true;
            return type.GetConstructor(Type.EmptyTypes) != null;
        }

        public static IEnumerable<InvokerModel> Next(this IEnumerable<InvokerModel> resolvers, IgnoreDictionary ignore, ParameterExpression source, string destName)
        {
            return resolvers.Where(it => !ignore.TryGetValue(it.DestinationMemberName, out var item) || item.Condition != null)
                    .Select(it => it.Next(source, destName))
                    .Where(it => it != null)!;
        }

        public static bool IsObjectReference(this Type type)
        {
            return !type.GetTypeInfo().IsValueType && type != typeof(string);
        }

        public static IEnumerable<Type> GetAllTypes(this Type type)
        {
            do
            {
                yield return type;
                type = type.GetTypeInfo().BaseType;
            } while (type != null && type != typeof(object));
        }
    }
}