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

        public static Type GetAttributeType(this CustomAttributeData data)
        {
            return data.Constructor.DeclaringType;
        }
#else
        public static Type GetAttributeType(this CustomAttributeData data)
        {
            return data.AttributeType;
        }
#endif

#if NETSTANDARD1_3
        public static IEnumerable<CustomAttributeData> GetCustomAttributesData(this ParameterInfo parameter)
        {
            return parameter.CustomAttributes;
        }
        public static IEnumerable<CustomAttributeData> GetCustomAttributesData(this MemberInfo member)
        {
            return member.CustomAttributes;
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

            return type.GetFieldsAndProperties(requireSetter: true).Any();
        }

        public static IEnumerable<IMemberModelEx> GetFieldsAndProperties(this Type type, bool requireSetter = false, BindingFlags accessorFlags = BindingFlags.Public)
        {
            var bindingFlags = BindingFlags.Instance | accessorFlags;

            IEnumerable<IMemberModelEx> getPropertiesFunc(Type t) => t.GetProperties(bindingFlags)
                .Where(x => x.GetIndexParameters().Length == 0)
                .Where(x => !requireSetter || x.CanWrite)
                .Select(CreateModel);

            IEnumerable<IMemberModelEx> getFieldsFunc(Type t) => t.GetFields(bindingFlags)
                .Where(x => !requireSetter || !x.IsInitOnly)
                .Select(CreateModel);

            IEnumerable<IMemberModelEx> properties;
            IEnumerable<IMemberModelEx> fields;

            if (type.IsInterface)
            {
                IEnumerable<Type> allInterfaces = GetAllInterfaces(type);
                properties = allInterfaces.SelectMany(currentInterface => getPropertiesFunc(currentInterface));
                fields = allInterfaces.SelectMany(currentInterface => getFieldsFunc(currentInterface));
            }
            else
            {
                properties = getPropertiesFunc(type);
                fields = getFieldsFunc(type);
            }

            return properties.Concat(fields);
        }

        // GetProperties(), GetFields(), GetMethods() do not return properties/methods from parent interfaces,
        // so we need to process every one of them separately.
        public static IEnumerable<Type> GetAllInterfaces(Type interfaceType)
        {
            var allInterfaces = new List<Type>();
            var interfaceQueue = new Queue<Type>();
            allInterfaces.Add(interfaceType);
            interfaceQueue.Enqueue(interfaceType);
            while (interfaceQueue.Count > 0)
            {
                var currentInterface = interfaceQueue.Dequeue();
                foreach (var subInterface in currentInterface.GetInterfaces())
                {
                    if (allInterfaces.Contains(subInterface))
                    {
                        continue;
                    }
                    allInterfaces.Add(subInterface);
                    interfaceQueue.Enqueue(subInterface);
                }
            }
            return allInterfaces;
        }

        public static bool IsCollection(this Type type)
        {
            return typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()) && type != typeof(string);
        }

        public static Type ExtractCollectionType(this Type collectionType)
        {
            if (collectionType.IsArray)
                return collectionType.GetElementType();
            var enumerableType = collectionType.GetGenericEnumerableType();
            return enumerableType != null
                ? enumerableType.GetGenericArguments()[0]
                : typeof(object);
        }

        public static bool IsGenericEnumerableType(this Type type)
        {
            return type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>);
        }

        public static Type GetInterface(this Type type, Predicate<Type> predicate)
        {
            if (predicate(type))
                return type;

            return Array.Find(type.GetInterfaces(), predicate);
        }

        public static Type GetGenericEnumerableType(this Type type)
        {
            return type.GetInterface(IsGenericEnumerableType);
        }

        public static Expression? CreateConvertMethod(Type srcType, Type destType, Expression source)
        {
            var name = _primitiveTypes.GetValueOrDefault(destType);

            if (name == null)
                return null;

            var method = typeof(Convert).GetMethod(name, new[] { srcType });
            if (method != null)
                return Expression.Call(method, source);

            method = typeof(Convert).GetMethod(name, new[] { typeof(object) });
            return Expression.Convert(Expression.Call(method, Expression.Convert(source, typeof(object))), destType);
        }

        public static Type UnwrapNullable(this Type type)
        {
            return type.IsNullable() ? type.GetGenericArguments()[0] : type;
        }

        public static string GetMemberPath(LambdaExpression lambda, bool firstLevelOnly = false, bool noError = false)
        {
            var props = new List<string>();
            var expr = lambda.Body.TrimConversion(true);
            while (expr?.NodeType == ExpressionType.MemberAccess)
            {
                if (firstLevelOnly && props.Count > 0)
                {
                    if (noError)
                        return null!;
                    throw new ArgumentException("Only first level members are allowed (eg. obj => obj.Child)", nameof(lambda));
                }

                var memEx = (MemberExpression)expr;
                props.Add(memEx.Member.Name);
                expr = memEx.Expression;
            }
            if (props.Count == 0 || expr?.NodeType != ExpressionType.Parameter)
            {
                if (noError)
                    return null!;
                throw new ArgumentException("Allow only member access (eg. obj => obj.Child.Name)", nameof(lambda));
            }
            props.Reverse();
            return string.Join(".", props);
        }

        public static bool IsRecordType(this Type type)
        {
            //not nullable
            if (type.IsNullable())
                return false;

            //not primitives
            if (type.IsConvertible())
                return false;

            //no public setter
            var props = type.GetFieldsAndProperties().ToList();
            if (props.Any(p => p.SetterModifier == AccessModifier.Public))
                return false;

            //1 non-empty constructor
            var ctors = type.GetConstructors().Where(ctor => ctor.GetParameters().Length > 0).ToList();
            if (ctors.Count != 1)
                return false;

            //all parameters should match getter
            return props.All(prop =>
            {
                var name = prop.Name.ToPascalCase();
                return ctors[0].GetParameters().Any(p => p.ParameterType == prop.Type && p.Name?.ToPascalCase() == name);
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

        public static bool IsListCompatible(this Type type)
        {
            var typeInfo = type.GetTypeInfo();
            if (typeInfo.IsInterface)
                return type.IsAssignableFromList();

            if (typeInfo.IsAbstract)
                return false;

            var elementType = type.ExtractCollectionType();
            if (typeof(ICollection<>).MakeGenericType(elementType).GetTypeInfo().IsAssignableFrom(typeInfo))
                return true;

            if (typeof(IList).GetTypeInfo().IsAssignableFrom(typeInfo))
                return true;

            return false;
        }

        public static Type GetDictionaryType(this Type destinationType)
        {
            return destinationType.GetInterface(type => type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(IDictionary<,>));
        }

        public static AccessModifier GetAccessModifier(this FieldInfo memberInfo)
        {
            if (memberInfo.IsFamilyOrAssembly)
                return AccessModifier.Protected | AccessModifier.Internal;
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
                return AccessModifier.Protected | AccessModifier.Internal;
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
            var names = side == MemberSide.Destination ? arg.GetDestinationNames() : arg.GetSourceNames();
            return names.Contains(member.Name);
        }

        public static string GetMemberName(this IMemberModel member, List<Func<IMemberModel, string?>> getMemberNames, Func<string, string> nameConverter)
        {
            return getMemberNames.Select(predicate => predicate(member))
                .FirstOrDefault(name => name != null)
                ?? nameConverter(member.Name);
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
    }
}