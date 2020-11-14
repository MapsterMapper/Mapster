using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

// ReSharper disable once CheckNamespace
namespace Mapster
{
    internal static class CustomAttributeUtil
    {
#if NET40
        public static Type GetAttributeType(this CustomAttributeData data)
        {
            return data.Constructor.DeclaringType;
        }

        public static string GetMemberName(this CustomAttributeNamedArgument arg)
        {
            return arg.MemberInfo.Name;
        }

        public static bool IsField(this CustomAttributeNamedArgument arg)
        {
            return arg.MemberInfo is FieldInfo;
        }

#else
        public static Type GetAttributeType(this CustomAttributeData data)
        {
            return data.AttributeType;
        }

        public static string GetMemberName(this CustomAttributeNamedArgument arg)
        {
            return arg.MemberName;
        }

        public static bool IsField(this CustomAttributeNamedArgument arg)
        {
            return arg.IsField;
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
        public static ConstructorInfo GetConstructor(this CustomAttributeData attr)
        {
            return attr.AttributeType.GetConstructor(attr.ConstructorArguments.Select(it => it.ArgumentType).ToArray());
        }
#else
        public static ConstructorInfo GetConstructor(this CustomAttributeData attr)
        {
            return attr.Constructor;
        }
#endif

        private static bool IsInheritable(this CustomAttributeData attr)
        {
            return attr.GetAttributeType().GetCustomAttributesData(true)
                .Where(it => it.GetAttributeType() == typeof(AttributeUsageAttribute))
                .SelectMany(it => it.NamedArguments)
                .Any(it => it.GetMemberName() == nameof(AttributeUsageAttribute.Inherited) &&
                           true.Equals(it.TypedValue.Value));
        }

        public static IEnumerable<CustomAttributeData> GetCustomAttributesData(this Type type, bool inherit)
        {
            IEnumerable<CustomAttributeData> result = type.GetTypeInfo().GetCustomAttributesData();
            if (!inherit)
                return result;

            var baseType = type.GetTypeInfo().BaseType;
            while (baseType != null && baseType != typeof(object))
            {
                result = result.Concat(baseType.GetTypeInfo().GetCustomAttributesData().Where(IsInheritable));
                baseType = baseType.GetTypeInfo().BaseType;
            }

            return result;
        }

        public static T CreateCustomAttribute<T>(this CustomAttributeData attr)
        {
            var attrType = attr.GetAttributeType();
            var obj = attr.GetConstructor().Invoke(attr.ConstructorArguments.Select(it => it.Value).ToArray());

            if (attr.NamedArguments == null) 
                return (T) obj;

            foreach (var arg in attr.NamedArguments)
            {
                if (arg.IsField())
                {
                    var fieldInfo = attrType.GetField(arg.GetMemberName());
                    fieldInfo?.SetValue(obj, GetValue(arg.TypedValue));
                }
                else
                {
                    var propInfo = attrType.GetProperty(arg.GetMemberName());
                    propInfo?.SetValue(obj, GetValue(arg.TypedValue), null);
                }
            }

            return (T) obj;
        }

        private static object GetValue(CustomAttributeTypedArgument typedValue)
        {
            if (typedValue.Value is IList<CustomAttributeTypedArgument> list)
            {
                var array = Array.CreateInstance(typedValue.ArgumentType.GetElementType()!, list.Count);
                for (var i = 0; i < list.Count; i++)
                {
                    array.SetValue(list[i].Value, i);
                }
                return array;
            }
            return typedValue.Value;
        }
    }
}
