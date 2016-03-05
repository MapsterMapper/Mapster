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

#if NET4
        public static Type GetTypeInfo(this Type type) {
            return type;
        }
#endif

        public static bool IsNullable(this Type type)
        {
            return type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof (Nullable<>);
        }

        public static List<IMemberModel> GetPublicFieldsAndProperties(this Type type, bool allowNonPublicSetter = true, bool allowNoSetter = true)
        {
            var results = new List<IMemberModel>();

            results.AddRange(
                type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Where(x => (allowNoSetter || x.CanWrite) && (allowNonPublicSetter || x.GetSetMethod() != null))
                    .Select(CreateModel));

            results.AddRange(
                type.GetFields(BindingFlags.Instance | BindingFlags.Public)
                    .Where(x => (allowNoSetter || !x.IsInitOnly))
                    .Select(CreateModel));
            
            return results;
        }

        public static IMemberModel GetMemberModel(Type type, string name)
        {
            var prop = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (prop != null)
                return new PropertyModel(prop);

            var field = type.GetField(name, BindingFlags.Public | BindingFlags.Instance);
            if (field != null)
                return new FieldModel(field);

            return null;
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

        public static Expression BuildUnderlyingTypeConvertExpression(Expression source, Type sourceType, Type destinationType)
        {
            var srcType = sourceType.IsNullable() ? sourceType.GetGenericArguments()[0] : sourceType;
            var destType = destinationType.IsNullable() ? destinationType.GetGenericArguments()[0] : destinationType;

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
                throw new InvalidOperationException(
                    $"Cannot convert immutable type, please consider using 'MapWith' method to create mapping: TSource: {sourceType} TDestination: {destinationType}");

            //using Convert
            if (destType == typeof (bool))
                return CreateConvertMethod("ToBoolean", srcType, destType, source);

            if (destType == typeof (int))
                return CreateConvertMethod("ToInt32", srcType, destType, source);

            if (destType == typeof (long))
                return CreateConvertMethod("ToInt64", srcType, destType, source);

            if (destType == typeof (short))
                return CreateConvertMethod("ToInt16", srcType, destType, source);

            if (destType == typeof (decimal))
                return CreateConvertMethod("ToDecimal", srcType, destType, source);

            if (destType == typeof (double))
                return CreateConvertMethod("ToDouble", srcType, destType, source);

            if (destType == typeof (float))
                return CreateConvertMethod("ToSingle", srcType, destType, source);

            if (destType == typeof (DateTime))
                return CreateConvertMethod("ToDateTime", srcType, destType, source);

            if (destType == typeof (ulong))
                return CreateConvertMethod("ToUInt64", srcType, destType, source);

            if (destType == typeof (uint))
                return CreateConvertMethod("ToUInt32", srcType, destType, source);

            if (destType == typeof (ushort))
                return CreateConvertMethod("ToUInt16", srcType, destType, source);

            if (destType == typeof (byte))
                return CreateConvertMethod("ToByte", srcType, destType, source);

            if (destType == typeof (sbyte))
                return CreateConvertMethod("ToSByte", srcType, destType, source);

            var changeTypeMethod = typeof (Convert).GetMethod("ChangeType", new[] {typeof (object), typeof (Type)});
            return Expression.Convert(Expression.Call(changeTypeMethod, Expression.Convert(source, typeof (object)), Expression.Constant(destType)), destType);
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

        public static Expression GetDeepFlattening(Expression source, string propertyName, bool isProjection)
        {
            var properties = source.Type.GetPublicFieldsAndProperties();
            for (int j = 0; j < properties.Count; j++)
            {
                var property = properties[j];
                var propertyType = property.Type;
                if (propertyType.GetTypeInfo().IsClass && propertyType != _stringType
                    && propertyName.StartsWith(property.Name))
                {
                    var exp = property.GetExpression(source);
                    var ifTrue = GetDeepFlattening(exp, propertyName.Substring(property.Name.Length).TrimStart('_'), isProjection);
                    if (ifTrue == null)
                        return null;
                    if (isProjection)
                        return ifTrue;
                    return Expression.Condition(
                        Expression.Equal(exp, Expression.Constant(null, exp.Type)),
                        Expression.Constant(ifTrue.Type.GetDefault(), ifTrue.Type),
                        ifTrue);
                }
                else if (string.Equals(propertyName, property.Name))
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

        public static bool IsPoco(this Type type)
        {
            if (type.GetTypeInfo().IsEnum)
                return false;

            return type.GetPublicFieldsAndProperties(allowNoSetter: false).Count > 0;
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
            var props = type.GetPublicFieldsAndProperties();
            if (props.Any(p => p.HasSetter))
                return false;

            //1 non-empty constructor
            var ctors = type.GetConstructors().Where(ctor => ctor.GetParameters().Length > 0).ToList();
            if (ctors.Count != 1)
                return false;

            //all parameters should match getter
            var names = props.Select(p => p.Name).ToHashSet();
            return names.SetEquals(ctors[0].GetParameters().Select(p => p.Name.ToProperCase()));
        }

        public static bool IsConvertible(this Type type)
        {
            return typeof (IConvertible).GetTypeInfo().IsAssignableFrom(type);
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
            if (type.IsInterface)
                return type.IsAssignableFromList();

            if (type.IsAbstract)
                return false;

            var elementType = type.ExtractCollectionType();
            if (typeof(ICollection<>).MakeGenericType(elementType).GetTypeInfo().IsAssignableFrom(type))
                return true;

            if (typeof(IList).GetTypeInfo().IsAssignableFrom(type))
                return true;

            return false;
        }
    }
}