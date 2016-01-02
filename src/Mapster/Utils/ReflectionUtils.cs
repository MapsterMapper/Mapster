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

            results.AddRange(type.GetFields(BindingFlags.Instance | BindingFlags.Public));

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
            if (mti != null)
            {
                return mti.ReturnType;
            }
            return null;
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
            return _iEnumerableType.IsAssignableFrom(type) && !IsString(type);
            
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

        public static bool IsString(this Type type)
        {
            return type == _stringType;
        }

        public static bool IsEnum(this Type type)
        {
            return type.IsEnum || (IsNullable(type) && Nullable.GetUnderlyingType(type).IsEnum);
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

        public static FastInvokeHandler CreatePrimitiveConverter(this Type sourceType, Type destinationType)
        {
            Type srcType;
            bool isNullableSource = sourceType.IsGenericType && sourceType.GetGenericTypeDefinition() == _nullableType;
            if (isNullableSource)
                srcType = sourceType.GetGenericArguments()[0];
            else
                srcType = sourceType;

            Type destType;
            bool isNullableDest = destinationType.IsGenericType && destinationType.GetGenericTypeDefinition() == _nullableType;
            if (isNullableDest)
                destType = destinationType.GetGenericArguments()[0];
            else
                destType = destinationType;

            if (srcType == destType)
                return null;
                
            if (destType == _stringType)
            {
                if (IsEnum(srcType))
                {
                    return FastInvoker.GetMethodInvoker(typeof(FastEnum<>).MakeGenericType(srcType).GetMethod("ToString", new[] { srcType }));
                }
                return FastInvoker.GetMethodInvoker(typeof(Convert).GetMethod("ToString", new[] { typeof(object) }));
            }

            if (destType == typeof(bool))
                return FastInvoker.GetMethodInvoker(typeof(Convert).GetMethod("ToBoolean", new[] { typeof(object) }));

            if (destType == typeof(int))
                return FastInvoker.GetMethodInvoker(typeof(Convert).GetMethod("ToInt32", new[] { typeof(object) }));

            if (destType == typeof(long))
                return FastInvoker.GetMethodInvoker(typeof(Convert).GetMethod("ToInt64", new[] { typeof(object) }));

            if (destType == typeof(short))
                return FastInvoker.GetMethodInvoker(typeof(Convert).GetMethod("ToInt16", new[] { typeof(object) }));

            if (destType == typeof(decimal))
                return FastInvoker.GetMethodInvoker(typeof(Convert).GetMethod("ToDecimal", new[] { typeof(object) }));

            if (destType == typeof(double))
                return FastInvoker.GetMethodInvoker(typeof(Convert).GetMethod("ToDouble", new[] { typeof(object) }));

            if (destType == typeof(float))
                return FastInvoker.GetMethodInvoker(typeof(Convert).GetMethod("ToSingle", new[] { typeof(object) }));

            if (destType == typeof(DateTime))
                return FastInvoker.GetMethodInvoker(typeof(Convert).GetMethod("ToDateTime", new[] { typeof(object) }));

            if (destType == typeof(Guid))
                return FastInvoker.GetMethodInvoker(typeof(ReflectionUtils).GetMethod("ConvertToGuid", new[] { typeof(object) }));

            if (IsEnum(destType) && IsString(sourceType))
            {
                return FastInvoker.GetMethodInvoker(typeof (FastEnum<>).MakeGenericType(destType).GetMethod("ToEnum", new[] { srcType }));
            }

            if (destType == typeof(ulong))
                return FastInvoker.GetMethodInvoker(typeof(Convert).GetMethod("ToUInt64", new[] { typeof(object) }));

            if (destType == typeof(uint))
                return FastInvoker.GetMethodInvoker(typeof(Convert).GetMethod("ToUInt32", new[] { typeof(object) }));

            if (destType == typeof(ushort))
                return FastInvoker.GetMethodInvoker(typeof(Convert).GetMethod("ToUInt16", new[] { typeof(object) }));

            if (destType == typeof(byte))
                return FastInvoker.GetMethodInvoker(typeof(Convert).GetMethod("ToByte", new[] { typeof(object) }));

            if (destType == typeof(sbyte))
                return FastInvoker.GetMethodInvoker(typeof(Convert).GetMethod("ToSByte", new[] { typeof(object) }));

            return null;
        }

        public static Guid ConvertToGuid(object value)
        {
            return Guid.Parse(value.ToString());
        }

        public static MemberExpression GetMemberInfo(Expression method)
        {
            var lambda = method as LambdaExpression;
            if (lambda == null)
                throw new ArgumentNullException("method");

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

        public static void GetDeepFlattening(Type type, string propertyName, List<GenericGetter> invokers)
        {
            var properties = type.GetProperties();
            for (int j = 0; j < properties.Length; j++)
            {
                var property = properties[j];
                if (property.PropertyType.IsClass && property.PropertyType != _stringType
                    && propertyName.StartsWith(property.Name))
                {
                    invokers.Add(PropertyCaller.CreateGetMethod(property));
                    GetDeepFlattening(property.PropertyType, propertyName.Substring(property.Name.Length).TrimStart('_'), invokers);
                }
                else if (string.Equals(propertyName, property.Name))
                {
                    invokers.Add(PropertyCaller.CreateGetMethod(property));
                }
            }
        }


        public static Dictionary<long, int> Clone(Dictionary<long, int> values)
        {
            if (values == null)
                return null;

            return values.ToDictionary(item => item.Key, item => item.Value);
          
        }

        public static long GetHashKey<TSource, TDestination>()
        {
            return ((long)typeof(TSource).GetHashCode() << 32) | typeof(TDestination).GetHashCode();
        }

        public static long GetHashKey(Type source, Type destination)
        {
            return ((long)source.GetHashCode() << 32) | destination.GetHashCode();
        }

    }
}
