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

        public static IEnumerable<IMemberModelEx> GetFieldsAndProperties(this Type type, bool allowNoSetter = true, BindingFlags accessorFlags = BindingFlags.Public)
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

        public static MemberExpression GetMemberInfo(Expression method, bool noThrow = false)
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

            if (memberExpr == null && !noThrow)
                throw new ArgumentException("argument must be member access", nameof(method));

            return memberExpr;
        }

        public static Expression GetDeepFlattening(Expression source, string propertyName, CompileArgument arg)
        {
            var strategy = arg.Settings.NameMatchingStrategy;
            var properties = source.Type.GetFieldsAndProperties();
            foreach (var property in properties)
            {
                var sourceMemberName = property.GetMemberName(arg.Settings.GetMemberNames, strategy.SourceMemberNameConverter);
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

        public static bool ShouldMapMember(this IMemberModel member, IEnumerable<Func<IMemberModel, MemberSide, bool?>> predicates, MemberSide side)
        {
            return predicates.Select(predicate => predicate(member, side))
                .FirstOrDefault(result => result != null) == true;
        }

        public static string GetMemberName(this IMemberModel member, List<Func<IMemberModel, string>> getMemberNames, Func<string, string> nameConverter)
        {
            return getMemberNames.Select(predicate => predicate(member))
                .FirstOrDefault(name => name != null)
                ?? nameConverter(member.Name);
        }
    }
}