using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Mapster.Utils
{
    internal static class ReflectionUtils
    {
        private static readonly Type _stringType = typeof(string);
        private static readonly Type _nullableType = typeof (Nullable<>);

        private static readonly Type _iEnumerableType = typeof(IEnumerable);

        public static bool IsNullable(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == _nullableType;
        }

        public static List<MemberInfo> GetPublicFieldsAndProperties(this Type type, bool allowNonPublicSetter = true, bool allowNoSetter = true)
        {
            var results = new List<MemberInfo>();

            results.AddRange(type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Where(x => (allowNoSetter || x.CanWrite) && (allowNonPublicSetter || x.GetSetMethod() != null)));

            results.AddRange(type.GetFields(BindingFlags.Instance | BindingFlags.Public).Where(x => (allowNoSetter || x.IsInitOnly)));

            return results;
        }

        public static MemberInfo GetPublicFieldOrProperty(Type type, bool isProperty, string name)
        {
            if (isProperty)
                return type.GetProperty(name);
            
            return type.GetField(name);
        }

        public static Type GetMemberType(this MemberInfo mi)
        {
            var pi = mi as PropertyInfo;
            if (pi != null)
            {
                return pi.PropertyType;
            }

            var fi = mi as FieldInfo;
            if (fi != null)
            {
                return fi.FieldType;
            }

            var mti = mi as MethodInfo;
            return mti?.ReturnType;
        }

        public static bool HasPublicSetter(this MemberInfo mi)
        {
            var pi = mi as PropertyInfo;
            if (pi != null)
            {
                return pi.GetSetMethod() != null;
            }

            var fi = mi as FieldInfo;
            if (fi != null)
            {
                return fi.IsPublic;
            }
            return false;
        }

        public static bool IsCollection(this Type type)
        {
            return _iEnumerableType.IsAssignableFrom(type) && type != _stringType;
            
        }

        public static bool IsPrimitiveRoot(this Type type)
        {
            return
                type.IsValueType
                || type == typeof(string)
                || TypeAdapterConfig.GlobalSettings.PrimitiveTypes.Contains(type)
                || type == typeof(object)
                ;
        }

        public static Type ExtractCollectionType(this Type collectionType)
        {
            if (collectionType.IsGenericEnumerableType())
            {
                return collectionType.GetGenericArguments()[0];
            }
            var enumerableType = collectionType.GetInterfaces().FirstOrDefault(IsGenericEnumerableType);
            if (enumerableType != null)
            {
                return enumerableType.GetGenericArguments()[0];
            }
            return typeof (object);
        }

        public static bool IsGenericEnumerableType(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof (IEnumerable<>);
        }

        public static string ToString<T>(T item)
        {
            return item.ToString();
        }

        private static Expression CreateConvertMethod(string name, Type srcType, Type destType, Expression source)
        {
            var method = typeof (Convert).GetMethod(name, new[] {srcType});
            if (method != null)
                return Expression.Call(method, source);

            method = typeof (Convert).GetMethod(name, new[] {typeof (object)});
            return Expression.Convert(Expression.Call(method, Expression.Convert(source, typeof (object))), destType);
        }

        public static Expression BuildUnderlyingTypeConvertExpression<TSource, TDestination>(Expression source)
        {
            var sourceType = typeof (TSource);
            var destinationType = typeof (TDestination);
            var srcType = sourceType.IsNullable() ? sourceType.GetGenericArguments()[0] : sourceType;
            var destType = destinationType.IsNullable() ? destinationType.GetGenericArguments()[0] : destinationType;
            
            if (srcType == destType)
                return source;

            if (destType == _stringType)
            {
                if (srcType.IsEnum)
                {
                    var method = typeof(Enum<>).MakeGenericType(srcType).GetMethod("ToString", new[] { srcType });
                    return Expression.Call(method, source);
                }
                else
                {
                    var method = typeof(ReflectionUtils).GetMethods().First(m => m.Name == "ToString");
                    method = method.MakeGenericMethod(srcType);
                    return Expression.Call(method, source);
                }
            }

            if (srcType == _stringType)
            {
                if (destType.IsEnum)
                {
                    var method = typeof(Enum<>).MakeGenericType(destType).GetMethod("Parse", new[] { typeof(string) });
                    return Expression.Call(method, source);
                }
                else
                {
                    var method = destType.GetMethod("Parse", new[] { typeof(string) });
                    return Expression.Call(method, source);
                }
            }

            if (destType == typeof(bool))
                return CreateConvertMethod("ToBoolean", srcType, destType, source);

            if (destType == typeof(int))
                return CreateConvertMethod("ToInt32", srcType, destType, source);

            if (destType == typeof(long))
                return CreateConvertMethod("ToInt64", srcType, destType, source);

            if (destType == typeof(short))
                return CreateConvertMethod("ToInt16", srcType, destType, source);

            if (destType == typeof(decimal))
                return CreateConvertMethod("ToDecimal", srcType, destType, source);

            if (destType == typeof(double))
                return CreateConvertMethod("ToDouble", srcType, destType, source);

            if (destType == typeof(float))
                return CreateConvertMethod("ToSingle", srcType, destType, source);

            if (destType == typeof(DateTime))
                return CreateConvertMethod("ToDateTime", srcType, destType, source);

            if (destType == typeof(ulong))
                return CreateConvertMethod("ToUInt64", srcType, destType, source);

            if (destType == typeof(uint))
                return CreateConvertMethod("ToUInt32", srcType, destType, source);

            if (destType == typeof(ushort))
                return CreateConvertMethod("ToUInt16", srcType, destType, source);

            if (destType == typeof(byte))
                return CreateConvertMethod("ToByte", srcType, destType, source);

            if (destType == typeof(sbyte))
                return CreateConvertMethod("ToSByte", srcType, destType, source);

            if (srcType.IsAssignableFrom(destType) ||
                destType.IsAssignableFrom(srcType) ||
                (srcType.IsEnum && Enum.GetUnderlyingType(srcType) == destType) ||
                (destType.IsEnum && Enum.GetUnderlyingType(destType) == srcType))
                return Expression.Convert(source, destType);

            var changeTypeMethod = typeof(Convert).GetMethod("ChangeType", new[] { typeof(object), typeof(Type) });
            return Expression.Convert(Expression.Call(changeTypeMethod, Expression.Convert(source, typeof(object)), Expression.Constant(destType)), destType);
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
                    ((UnaryExpression)lambda.Body).Operand as MemberExpression;
            }
            else if (lambda.Body.NodeType == ExpressionType.MemberAccess)
            {
                memberExpr = lambda.Body as MemberExpression;
            }

            if (memberExpr == null)
                throw new ArgumentException("method");

            return memberExpr;
        }

        public static Expression GetDeepFlattening(Expression source, string propertyName)
        {
            var properties = source.Type.GetPublicFieldsAndProperties(allowNoSetter: false);
            for (int j = 0; j < properties.Count; j++)
            {
                var property = properties[j];
                var propertyType = property.GetMemberType();
                if (propertyType.IsClass && propertyType != _stringType
                    && propertyName.StartsWith(property.Name))
                {
                    var exp = property is PropertyInfo
                        ? Expression.Property(source, (PropertyInfo) property)
                        : Expression.Field(source, (FieldInfo) property);
                    var ifTrue = GetDeepFlattening(exp, propertyName.Substring(property.Name.Length).TrimStart('_'));
                    if (ifTrue == null)
                        return null;
                    return Expression.Condition(Expression.Equal(exp, Expression.Constant(null, exp.Type)), Expression.Constant(ifTrue.Type.IsValueType && !ifTrue.Type.IsNullable() ? Activator.CreateInstance(ifTrue.Type) : null, ifTrue.Type), ifTrue);
                }
                else if (string.Equals(propertyName, property.Name))
                {
                    return property is PropertyInfo
                        ? Expression.Property(source, (PropertyInfo)property)
                        : Expression.Field(source, (FieldInfo)property);
                }
            }
            return null;
        }
    }
}
