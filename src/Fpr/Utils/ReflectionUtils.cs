using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Fpr.Utils
{
    public sealed class ReflectionUtils
    {
        public static bool IsNullable(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        public static MemberInfo[] GetPublicFieldsAndProperties(Type type)
        {
            return type
                .GetMembers(BindingFlags.Instance | BindingFlags.Public)
                .Where(mi => mi.MemberType == MemberTypes.Property || mi.MemberType == MemberTypes.Field)
                .ToArray();
        }

        public static MemberInfo GetPublicFieldOrProperty(Type type, bool isProperty, string name)
        {
            if (isProperty)
                return type.GetProperty(name);
            
            return type.GetField(name);
        }

        public static Type GetMemberType(MemberInfo mi)
        {
            if (mi is PropertyInfo)
            {
                return ((PropertyInfo)mi).PropertyType;
            }
            if (mi is FieldInfo)
            {
                return ((FieldInfo)mi).FieldType;
            }
            if (mi is MethodInfo)
            {
                return ((MethodInfo)mi).ReturnType;
            }
            return null;
        }

        public static bool IsCollection(Type type)
        {
            return
                type.IsArray ||
                (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof (List<>)
                                        || type.GetGenericTypeDefinition() == typeof (IList<>)
                                        || type.GetGenericTypeDefinition() == typeof (IEnumerable<>)
                                        || type.GetGenericTypeDefinition() == typeof (IReadOnlyList<>)
                                        || type.GetGenericTypeDefinition() == typeof (ICollection<>)))
                || type == typeof (ArrayList)
                || typeof (IList).IsAssignableFrom(type)
                || typeof (IEnumerable<>).IsAssignableFrom(type);
        }

        public static bool IsPrimitive(Type type)
        {
            return
                type.IsPrimitive
                || type == typeof(string)
                || type == typeof(decimal)
                || type == typeof(double)
                || type == typeof(Guid)
                || type == typeof(float)
                || type == typeof(long)
                || type == typeof(ulong)
                || type == typeof(short)
                || type == typeof(DateTime)
                || (IsNullable(type) && IsPrimitive(Nullable.GetUnderlyingType(type)))
                || type.IsEnum;
        }

        public static bool IsString(Type type)
        {
            return type == typeof (string);
        }

        public static bool IsEnum(Type type)
        {
            return type.IsEnum || (IsNullable(type) && Nullable.GetUnderlyingType(type).IsEnum);
        }

        public static Type ExtractElementType(Type collection)
        {
            if (collection.IsArray)
            {
                return collection.GetElementType();
            }
            if (collection == typeof(ArrayList))
            {
                return typeof(object);
            }
            if (collection.IsGenericType)
            {
                return collection.GetGenericArguments()[0];
            }
            return collection;
        }
        
        public static FastInvokeHandler CreatePrimitiveConverter(Type sourceType, Type destinationType)
        {
            Type srcType;
            bool isNullableSource = sourceType.IsGenericType && sourceType.GetGenericTypeDefinition() == typeof(Nullable<>);
            if (isNullableSource)
                srcType = sourceType.GetGenericArguments()[0];
            else
                srcType = sourceType;

            Type destType;
            bool isNullableDest = destinationType.IsGenericType && destinationType.GetGenericTypeDefinition() == typeof(Nullable<>);
            if (isNullableDest)
                destType = destinationType.GetGenericArguments()[0];
            else
                destType = destinationType;

            if (srcType == destType)
                return null;

            if (destType == typeof (string))
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
                return FastInvoker.GetMethodInvoker(typeof(FastEnum<>).MakeGenericType(destType).GetMethod("ToEnum", new[] { srcType }));
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


        public static Dictionary<int, int> Clone(Dictionary<int, int> values)
        {
            if (values == null)
                return null;

            var collection = new Dictionary<int, int>();

            foreach (var item in values)
            {
                collection.Add(item.Key, item.Value);
            }

            return collection;
          
        }

        public static int GetHashKey<TSource, TDestination>()
        {
            return (typeof(TSource).GetHashCode() / 2) + typeof(TDestination).GetHashCode();
        }

        public static int GetHashKey(Type source, Type destination)
        {
            return (source.GetHashCode()/2) + destination.GetHashCode();
        }

    }
}
