using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Fpr.Utils
{
    public static class ReflectionUtils
    {
        private static readonly Type _stringType = typeof(string);
        private static readonly Type _dateTimeType = typeof(DateTime);
        private static readonly Type _boolType = typeof(bool);
        private static readonly Type _intType = typeof(int);
        private static readonly Type _longType = typeof(long);
        private static readonly Type _shortType = typeof(short);
        private static readonly Type _byteType = typeof(byte);
        private static readonly Type _sByteType = typeof(sbyte);
        private static readonly Type _ulongType = typeof(ulong);
        private static readonly Type _uIntType = typeof(uint);
        private static readonly Type _uShortType = typeof(ushort);
        private static readonly Type _floatType = typeof(float);
        private static readonly Type _doubleType = typeof(double);
        private static readonly Type _decimalType = typeof(decimal);
        private static readonly Type _guidType = typeof(Guid);
        private static readonly Type _objectType = typeof(object);
        private static readonly Type _nullableType = typeof (Nullable<>);

        private static readonly Type _iEnumerableType = typeof(IEnumerable);
        private static readonly Type _arrayListType = typeof(ArrayList);

        private static readonly Type _convertType = typeof (Convert);
        private static readonly Type _fastEnumType = typeof (FastEnum<>);
        private static readonly Type _reflectionUtilsType = typeof(ReflectionUtils);

        public static bool IsNullable(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == _nullableType;
        }

        public static List<MemberInfo> GetPublicFieldsAndProperties(this Type type, bool allowNonPublicSetters = true)
        {
            var results = new List<MemberInfo>();

            results.AddRange(type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Where(x => allowNonPublicSetters || x.GetSetMethod() != null));

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

        public static bool IsCollection(this Type type)
        {
            return _iEnumerableType.IsAssignableFrom(type) && !IsString(type);
            
        }

        public static bool IsPrimitiveRoot(this Type type)
        {
            return
                type.IsPrimitive
                || type == _stringType
                || type == _decimalType
                || type == _guidType
                || type == _dateTimeType
                || type.IsEnum
                || (IsNullable(type) && IsPrimitiveRoot(Nullable.GetUnderlyingType(type)))
                || type == _objectType
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
            if (collectionType.IsArray)
            {
                return collectionType.GetElementType();
            }
            if (collectionType == _arrayListType)
            {
                return _objectType;
            }
            if (collectionType.IsGenericType)
            {
                return collectionType.GetGenericArguments()[0];
            }
            return collectionType;
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
                    return FastInvoker.GetMethodInvoker(_fastEnumType.MakeGenericType(srcType).GetMethod("ToString", new[] { srcType }));
                }
                return FastInvoker.GetMethodInvoker(_convertType.GetMethod("ToString", new[] { _objectType }));
            }

            if (destType == _boolType)
                return FastInvoker.GetMethodInvoker(_convertType.GetMethod("ToBoolean", new[] { _objectType }));

            if (destType == _intType)
                return FastInvoker.GetMethodInvoker(_convertType.GetMethod("ToInt32", new[] { _objectType }));

            if (destType == _longType)
                return FastInvoker.GetMethodInvoker(_convertType.GetMethod("ToInt64", new[] { _objectType }));

            if (destType == _shortType)
                return FastInvoker.GetMethodInvoker(_convertType.GetMethod("ToInt16", new[] { _objectType }));

            if (destType == _decimalType)
                return FastInvoker.GetMethodInvoker(_convertType.GetMethod("ToDecimal", new[] { _objectType }));

            if (destType == _doubleType)
                return FastInvoker.GetMethodInvoker(_convertType.GetMethod("ToDouble", new[] { _objectType }));

            if (destType == _floatType)
                return FastInvoker.GetMethodInvoker(_convertType.GetMethod("ToSingle", new[] { _objectType }));

            if (destType == _dateTimeType)
                return FastInvoker.GetMethodInvoker(_convertType.GetMethod("ToDateTime", new[] { _objectType }));

            if (destType == _guidType)
                return FastInvoker.GetMethodInvoker(_reflectionUtilsType.GetMethod("ConvertToGuid", new[] { _objectType }));

            if (IsEnum(destType) && IsString(sourceType))
            {
                return FastInvoker.GetMethodInvoker(_fastEnumType.MakeGenericType(destType).GetMethod("ToEnum", new[] { srcType }));
            }

            if (destType == _ulongType)
                return FastInvoker.GetMethodInvoker(_convertType.GetMethod("ToUInt64", new[] { _objectType }));

            if (destType == _uIntType)
                return FastInvoker.GetMethodInvoker(_convertType.GetMethod("ToUInt32", new[] { _objectType }));

            if (destType == _uShortType)
                return FastInvoker.GetMethodInvoker(_convertType.GetMethod("ToUInt16", new[] { _objectType }));

            if (destType == _byteType)
                return FastInvoker.GetMethodInvoker(_convertType.GetMethod("ToByte", new[] { _objectType }));

            if (destType == _sByteType)
                return FastInvoker.GetMethodInvoker(_convertType.GetMethod("ToSByte", new[] { _objectType }));

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
                    GetDeepFlattening(property.PropertyType, propertyName.Substring(property.Name.Length), invokers);
                }
                else if (string.Equals(propertyName, property.Name))
                {
                    invokers.Add(PropertyCaller.CreateGetMethod(property));
                }
            }
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

        public static int GetHashKey<TSource, TDestination>(bool hasDestination = false)
        {
            return GetHashKey(typeof(TSource), typeof(TDestination), hasDestination);
        }

        public static int GetHashKey(Type source, Type destination, bool hasDestination = false)
        {
            return (source.GetHashCode()/2) + destination.GetHashCode() + (hasDestination ? 1: 0);
        }

    }
}
