using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Mapster.Models;
using Mapster.Utils;

namespace Mapster
{
    internal static class ReflectionUtils
    {
        private static readonly Type _stringType = typeof (string);

        // Primitive types with their conversion methods from System.Convert class.
        private static Dictionary<Type, string> _primitiveTypes = new Dictionary<Type, string>() {
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
        public static Type GetTypeInfo(this Type type) {
            return type;
        }
#endif

        public static bool IsNullable(this Type type)
        {
            return type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof (Nullable<>);
        }

        public static bool IsPoco(this Type type)
        {
            if (type.GetTypeInfo().IsEnum)
                return false;

            return type.GetFieldsAndProperties(allowNoSetter: false).Any();
        }

        public static IEnumerable<IMemberModel> GetFieldsAndProperties(this Type type, bool allowNoSetter = true, BindingFlags accessorFlags = BindingFlags.Public)
        {
            var bindingFlags = BindingFlags.Instance | accessorFlags;

            var properties = type.GetProperties(bindingFlags)
                .Where(x => allowNoSetter || x.CanWrite)
                .Select(CreateModel);

            var fields = type.GetFields(bindingFlags)
                .Where(x => allowNoSetter || !x.IsInitOnly)
                .Select(CreateModel);

            return properties.Concat(fields);
        }

        public static bool IsCollection(this Type type)
        {
            return typeof (IEnumerable).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()) && type != _stringType;
        }

        public static Type ExtractCollectionType(this Type collectionType)
        {
            var enumerableType = collectionType.GetGenericEnumerableType();
            return enumerableType != null
                ? enumerableType.GetGenericArguments()[0]
                : typeof (object);
        }

        public static bool IsGenericEnumerableType(this Type type)
        {
            return type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof (IEnumerable<>);
        }

        public static Type GetInterface(this Type type, Func<Type, bool> predicate)
        {
            if (predicate(type))
                return type;

            return type.GetInterfaces().FirstOrDefault(predicate);
        }

        public static Type GetGenericEnumerableType(this Type type)
        {
            return type.GetInterface(IsGenericEnumerableType);
        }

        private static Expression CreateConvertMethod(string name, Type srcType, Type destType, Expression source)
        {
            var method = typeof (Convert).GetMethod(name, new[] {srcType});
            if (method != null)
                return Expression.Call(method, source);

            method = typeof (Convert).GetMethod(name, new[] {typeof (object)});
            return Expression.Convert(Expression.Call(method, Expression.Convert(source, typeof (object))), destType);
        }

        public static object GetDefault(this Type type)
        {
            return type.GetTypeInfo().IsValueType && !type.IsNullable()
                ? Activator.CreateInstance(type)
                : null;
        }

        public static Type UnwrapNullable(this Type type)
        {
            return type.IsNullable() ? type.GetGenericArguments()[0] : type;
        }

        public static Expression BuildUnderlyingTypeConvertExpression(Expression source, Type sourceType, Type destinationType, TypeAdapterSettings settings)
        {
            var srcType = sourceType.UnwrapNullable();
            var destType = destinationType.UnwrapNullable();

            if (srcType == destType)
                return source;

            //special handling for string
            if (destType == _stringType)
            {
                if (srcType.GetTypeInfo().IsEnum)
                {
                    var method = typeof (Enum<>).MakeGenericType(srcType).GetMethod("ToString", new[] {srcType});
                    return Expression.Call(method, source);
                }
                else
                {
                    var method = srcType.GetMethod("ToString", Type.EmptyTypes);
                    return Expression.Call(source, method);
                }
            }

            if (srcType == _stringType)
            {
                if (destType.GetTypeInfo().IsEnum)
                {
                    var method = typeof (Enum<>).MakeGenericType(destType).GetMethod("Parse", new[] {typeof (string)});
                    return Expression.Call(method, source);
                }
                else
                {
                    var method = destType.GetMethod("Parse", new[] {typeof (string)});
                    if (method != null)
                        return Expression.Call(method, source);
                }
            }

            if (destType.GetTypeInfo().IsEnum && srcType.GetTypeInfo().IsEnum && settings.MapEnumByName == true)
            {
                var method = typeof(Enum<>).MakeGenericType(srcType).GetMethod("ToString", new[] { srcType });
                var tostring = Expression.Call(method, source);
                var methodParse = typeof(Enum<>).MakeGenericType(destType).GetMethod("Parse", new[] { typeof(string) });

                return Expression.Call(methodParse, tostring);
            }

            if (IsObjectToPrimitiveConversion(srcType, destType))
            {
                return CreateConvertMethod(_primitiveTypes[destType], srcType, destType, source);
            }

            //try using type casting
            try
            {
                return Expression.Convert(source, destType);
            }
            catch
            {
                // ignored
            }

            if (!srcType.IsConvertible())
                throw new InvalidOperationException("Cannot convert immutable type, please consider using 'MapWith' method to create mapping");

            //using Convert
            if (_primitiveTypes.ContainsKey(destType))
            {
                return CreateConvertMethod(_primitiveTypes[destType], srcType, destType, source);
            }

            var changeTypeMethod = typeof (Convert).GetMethod("ChangeType", new[] {typeof (object), typeof (Type)});
            return Expression.Convert(Expression.Call(changeTypeMethod, Expression.Convert(source, typeof (object)), Expression.Constant(destType)), destType);
        }

        private static bool IsObjectToPrimitiveConversion(Type sourceType, Type destinationType)
        {
            return (sourceType == typeof(object)) && _primitiveTypes.ContainsKey(destinationType);
        }

        public static MemberExpression GetMemberInfo(Expression method)
        {
            var lambda = method as LambdaExpression;
            if (lambda == null)
                throw new ArgumentNullException(nameof(method));

            MemberExpression memberExpr = null;

            if (lambda.Body.NodeType == ExpressionType.Convert)
            {
                memberExpr =
                    ((UnaryExpression) lambda.Body).Operand as MemberExpression;
            }
            else if (lambda.Body.NodeType == ExpressionType.MemberAccess)
            {
                memberExpr = lambda.Body as MemberExpression;
            }

            if (memberExpr == null)
                throw new ArgumentException("argument must be member access", nameof(method));

            return memberExpr;
        }

        public static Expression GetDeepFlattening(Expression source, string propertyName, CompileArgument arg)
        {
            var strategy = arg.Settings.NameMatchingStrategy;
            var properties = source.Type.GetFieldsAndProperties();
            foreach (var property in properties)
            {
                var sourceMemberName = property.GetMemberName(arg.Settings.GetMemberName, strategy.SourceMemberNameConverter);
                var propertyType = property.Type;
                if (propertyType.GetTypeInfo().IsClass && propertyType != _stringType
                    && propertyName.StartsWith(sourceMemberName))
                {
                    var exp = property.GetExpression(source);
                    var ifTrue = GetDeepFlattening(exp, propertyName.Substring(sourceMemberName.Length).TrimStart('_'), arg);
                    if (ifTrue == null)
                        continue;
                    if (arg.MapType == MapType.Projection)
                        return ifTrue;
                    return Expression.Condition(
                        Expression.Equal(exp, Expression.Constant(null, exp.Type)),
                        Expression.Constant(ifTrue.Type.GetDefault(), ifTrue.Type),
                        ifTrue);
                }
                else if (string.Equals(propertyName, sourceMemberName))
                {
                    return property.GetExpression(source);
                }
            }
            return null;
        }

        public static bool IsReferenceAssignableFrom(this Type destType, Type srcType)
        {
            if (destType == srcType)
                return true;

            if (!destType.GetTypeInfo().IsValueType && !srcType.GetTypeInfo().IsValueType && destType.GetTypeInfo().IsAssignableFrom(srcType.GetTypeInfo()))
                return true;

            return false;
        }

        public static bool IsRecordType(this Type type)
        {
            //not collection
            if (type.IsCollection())
                return false;

            //not nullable
            if (type.IsNullable())
                return false;

            //not primitives
            if (type.IsConvertible())
                return false;

            //no setter
            var props = type.GetFieldsAndProperties().ToList();
            if (props.Any(p => p.SetterModifier != AccessModifier.None))
                return false;

            //1 non-empty constructor
            var ctors = type.GetConstructors().Where(ctor => ctor.GetParameters().Length > 0).ToList();
            if (ctors.Count != 1)
                return false;

            //all parameters should match getter
            return props.All(prop =>
            {
                var name = prop.Name.ToPascalCase();
                return ctors[0].GetParameters().Any(p => p.ParameterType == prop.Type && p.Name.ToPascalCase() == name);
            });
        }

        public static bool IsConvertible(this Type type)
        {
            return typeof (IConvertible).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo());
        }

        public static IMemberModel CreateModel(this PropertyInfo propertyInfo)
        {
            return new PropertyModel(propertyInfo);
        }

        public static IMemberModel CreateModel(this FieldInfo propertyInfo)
        {
            return new FieldModel(propertyInfo);
        }

        public static IMemberModel CreateModel(this ParameterInfo propertyInfo)
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

        public static IEnumerable<T> ToEnumerable<T>(this IEnumerable<T> source)
        {
            if (source is T[] array)
            {
                for (int i = 0, count = array.Length; i < count; i++)
                {
                    yield return array[i];
                }
                yield break;
            }

            if (source is IList<T> list)
            {
                for (int i = 0, count = list.Count; i < count; i++)
                {
                    yield return list[i];
                }
                yield break;
            }

            foreach (var item in source)
            {
                yield return item;
            }
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

        public static bool ShouldMapMember(this IMemberModel member, IEnumerable<Func<IMemberModel, bool?>> predicates)
        {
            return predicates.Select(predicate => predicate(member))
                .FirstOrDefault(result => result != null) == true;
        }

        public static string GetMemberName(this IMemberModel member, Func<IMemberModel, string> getMemberNameFn, Func<string, string> nameConverter)
        {
            if (getMemberNameFn == null)
                return nameConverter(member.Name);
            else
                return getMemberNameFn(member);
        }

        public static bool HasCustomAttribute(this IMemberModel member, Type type)
        {
            return member.GetCustomAttributes(true).Any(attr => attr.GetType() == type);
        }

        public static T GetCustomAttribute<T>(this IMemberModel member)
        {
            return (T)member.GetCustomAttributes(true).FirstOrDefault(attr => attr is T);
        }
    }
}