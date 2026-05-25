using Mapster.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Mapster.Utils
{
    internal static class ExpressionEx
    {
        public static Expression Assign(Expression left, Expression right)
        {
            var middle = left.Type.IsReferenceAssignableFrom(right.Type)
                ? right
                : Expression.Convert(right, left.Type);
            return Expression.Assign(left, middle);
        }

        public static Expression PropertyOrFieldPath(Expression expr, string path)
        {
            Expression current = expr;
            string[] props = path.Split('.');

            for (int i = 0; i < props.Length; i++)
            {
                if (IsDictionaryKey(current, props[i], out Expression? next))
                {
                    current = next;
                    continue;
                }

                if (IsPropertyOrField(current, props[i], out next))
                {
                    current = next;
                    continue;
                }

                // For dynamically built types, it is possible to have periods in the property name.
                // Rejoin an incrementing number of parts with periods to try and find a property match. 
                if (IsPropertyOrFieldPathWithPeriods(current, props, i, out next, out int combinationLength))
                {
                    current = next;
                    i += combinationLength - 1;
                    continue;
                }

                throw new ArgumentException($"'{props[i]}' is not a member of type '{current.Type.FullName}'", nameof(path));
            }

            return current;
        }

        private static bool IsPropertyOrFieldPathWithPeriods(Expression expr, string[] path, int startIndex, out Expression? propExpr, out int combinationLength)
        {
            var remaining = path.Length - startIndex;
            if (remaining < 2)
            {
                propExpr = null;
                combinationLength = 0;
                return false;
            }

            for (int count = 2; count <= remaining; count++)
            {
                string prop = string.Join(".", path, startIndex, count);
                if (IsPropertyOrField(expr, prop, out propExpr))
                {
                    combinationLength = count;
                    return true;
                }
            }

            propExpr = null;
            combinationLength = 0;
            return false;
        }

        private static bool IsDictionaryKey(Expression expr, string prop, out Expression? propExpr)
        {
            var type = expr.Type;
            var dictType = type.GetDictionaryType();

            if (dictType?.GetGenericArguments()[0] != typeof(string))
            {
                propExpr = null;
                return false;
            }

                var method = typeof(MapsterHelper).GetMethods()
                    .First(m => m.Name == nameof(MapsterHelper.GetValueOrDefault) && m.GetParameters()[0].ParameterType.Name == dictType.Name)
                    .MakeGenericMethod(dictType.GetGenericArguments());

            propExpr = Expression.Call(method, expr.To(type), Expression.Constant(prop));
            return true;
            }

        private static bool IsPropertyOrField(Expression expr, string prop, out Expression? propExpr)
        {
            Type type = expr.Type;

            if (type.GetTypeInfo().IsInterface)
            {
                var allTypes = type.GetAllInterfaces();
                var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
                var interfaceType = allTypes.FirstOrDefault(it => it.GetProperty(prop, flags) != null || it.GetField(prop, flags) != null);
                if (interfaceType != null)
                {
                    expr = Expression.Convert(expr, interfaceType);
                    type = expr.Type;
            }
            }

            MemberInfo? propertyOrField = type
                .GetMember(
                    prop,
                    MemberTypes.Field | MemberTypes.Property,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                .FirstOrDefault();

            propExpr = propertyOrField?.MemberType switch
            {
                MemberTypes.Property => Expression.Property(expr, (PropertyInfo)propertyOrField),
                MemberTypes.Field => Expression.Field(expr, (FieldInfo)propertyOrField),
                _ => null
            };

            return propExpr != null;
        }

        private static bool IsReferenceAssignableFrom(this Type destType, Type srcType)
        {
            if (destType == srcType)
                return true;

            if (!destType.GetTypeInfo().IsValueType && !srcType.GetTypeInfo().IsValueType && destType.GetTypeInfo().IsAssignableFrom(srcType.GetTypeInfo()))
                return true;

            return false;
        }

        public static Expression Not(Expression exp)
        {
            if (exp is UnaryExpression unary && unary.NodeType == ExpressionType.Not)
                return unary.Operand;
            return Expression.Not(exp);
        }

        public static Expression Apply(this LambdaExpression lambda, MapType mapType, params Expression[] exps)
        {
            return lambda.Apply(mapType != MapType.Projection, exps);
        }

        public static Expression Apply(this LambdaExpression lambda, ParameterExpression p1, ParameterExpression? p2 = null)
        {
            if (p2 == null)
                return lambda.Apply(false, p1);
            else
                return lambda.Apply(false, p1, p2);
        }

        private static Expression Apply(this LambdaExpression lambda, bool allowInvoke, params Expression[] exps)
        {
            var replacer = new ParameterExpressionReplacer(lambda.Parameters, exps);
            var result = replacer.Visit(lambda.Body);
            if (!allowInvoke || !replacer.ReplaceCounts.Where((n, i) => n > 1 && exps[i].IsComplex()).Any())
                return result!;
            return Expression.Invoke(lambda, exps);
        }

        public static LambdaExpression TrimParameters(this LambdaExpression lambda, int skip = 0)
        {
            var replacer = new ParameterExpressionReplacer(lambda.Parameters, lambda.Parameters.ToArray<Expression>());
            replacer.Visit(lambda.Body);
            if (replacer.ReplaceCounts.Skip(skip).All(n => n > 0))
                return lambda;
            return Expression.Lambda(lambda.Body, lambda.Parameters.Where((_, i) => i < skip || replacer.ReplaceCounts[i] > 0));
        }

        public static Expression TrimConversion(this Expression exp, bool force = false)
        {
            while (exp.NodeType == ExpressionType.Convert || exp.NodeType == ExpressionType.ConvertChecked)
            {
                var unary = (UnaryExpression)exp;
                if (force || unary.Type.IsReferenceAssignableFrom(unary.Operand.Type))
                    exp = unary.Operand;
                else
                    break;
            }
            return exp;
        }

        public static Expression To(this Expression exp, Type type, bool force = false)
        {
            exp = exp.TrimConversion();
            var sameType = force ? type == exp.Type : type.IsReferenceAssignableFrom(exp.Type);
            return sameType ? exp : Expression.Convert(exp, type);
        }

        public static Delegate Compile(this LambdaExpression exp, CompileArgument arg)
        {
            try
            {
                return exp.Compile();
            }
            catch (Exception ex)
            {
                throw new CompileException(arg, ex)
                {
                    Expression = exp,
                };
            }
        }

        public static Expression? CreateCountExpression(Expression source)
        {
            if (source.Type.IsArray)
            {
                return source.Type.GetArrayRank() == 1
                    ? (Expression?) Expression.ArrayLength(source)
                    : Expression.Property(source, "Length");
            }
            else
            {
                var countProperty = GetCount(source.Type);
                return countProperty != null 
                    ? Expression.Property(source, countProperty) 
                    : null;
            }
        }

        private static PropertyInfo? GetCount(Type type)
        {
            var c = type.GetProperty("Count");
            if (c != null)
                return c;

            //for IList<>
            var elementType = type.ExtractCollectionType();
            var collection = typeof(ICollection<>).MakeGenericType(elementType);
            return collection.IsAssignableFrom(type)
                ? collection.GetProperty("Count")
                : null;
        }

        public static Expression ForLoop(Expression collection, ParameterExpression loopVar, params Expression[] loopContent)
        {
            var i = Expression.Variable(typeof(int), "i");
            var len = Expression.Variable(typeof(int), "len");
            Expression lenAssign;
            Expression current;
            if (collection.Type.IsArray && collection.Type.GetArrayRank() == 1)
            {
                current = Expression.ArrayIndex(collection, i);
                lenAssign = Expression.Assign(len, Expression.ArrayLength(collection));
            }
            else if (collection.Type.GetDictionaryType() != null)
            {
                return ForEach(collection, loopVar, loopContent);
            }
            else
            {
                var indexer = (from p in collection.Type.GetDefaultMembers().OfType<PropertyInfo>()
                               let q = p.GetIndexParameters()
                               where q.Length == 1 && q[0].ParameterType == typeof(int)
                               select p).SingleOrDefault();
                var count = GetCount(collection.Type);

                //if indexer is not found, fallback to foreach
                if (indexer == null || count == null)
                    return ForEach(collection, loopVar, loopContent);

                current = Expression.Property(collection, indexer, i);
                lenAssign = Expression.Assign(len, Expression.Property(collection, count));
            }

            var iAssign = Expression.Assign(i, Expression.Constant(0));

            var breakLabel = Expression.Label("LoopBreak");

            return Expression.Block(new[] { i, len },
                iAssign,
                lenAssign,
                Expression.Loop(
                    Expression.IfThenElse(
                        Expression.LessThan(i, len),
                        Expression.Block(new[] { loopVar },
                            new[] { Expression.Assign(loopVar, current) }
                                .Concat(loopContent)
                                .Concat(new[] { Expression.PostIncrementAssign(i) })
                        ),
                        Expression.Break(breakLabel)
                    ),
                    breakLabel)
            );
        }

        public static Expression ForEach(Expression collection, ParameterExpression loopVar, params Expression[] loopContent)
        {
            var elementType = loopVar.Type;
            var enumerableType = typeof(IEnumerable<>).MakeGenericType(elementType);
            var enumeratorType = typeof(IEnumerator<>).MakeGenericType(elementType);
            var isGeneric = enumerableType.GetTypeInfo().IsAssignableFrom(collection.Type.GetTypeInfo());
            if (!isGeneric)
            {
                enumerableType = typeof(IEnumerable);
                enumeratorType = typeof(IEnumerator);
            }

            var enumeratorVar = Expression.Variable(enumeratorType, "enumerator");
            var getEnumeratorCall = Expression.Call(collection, enumerableType.GetMethod("GetEnumerator")!);
            var enumeratorAssign = Expression.Assign(enumeratorVar, getEnumeratorCall);

            // The MoveNext method's actually on IEnumerator, not IEnumerator<T>
            var moveNextCall = Expression.Call(enumeratorVar, typeof(IEnumerator).GetMethod("MoveNext")!);

            var breakLabel = Expression.Label("LoopBreak");

            Expression current = Expression.Property(enumeratorVar, "Current");
            if (!isGeneric)
                current = Expression.Convert(current, elementType);
            return Expression.Block(new[] { enumeratorVar },
                enumeratorAssign,
                Expression.Loop(
                    Expression.IfThenElse(
                        moveNextCall,
                        Expression.Block(new[] { loopVar },
                            new[] { Assign(loopVar, current) }.Concat(loopContent)
                        ),
                        Expression.Break(breakLabel)
                    ),
                    breakLabel)
            );
        }

        public static bool CanBeNull(this Expression exp)
        {
            if (!exp.Type.CanBeNull())
                return false;

            var visitor = new NullableExpressionVisitor();
            visitor.Visit(exp);
            return visitor.CanBeNull.GetValueOrDefault();
        }

        public static bool IsComplex(this Expression exp)
        {
            var visitor = new ComplexExpressionVisitor();
            visitor.Visit(exp);
            return visitor.IsComplex;
        }

        public static bool IsSingleValue(this Expression exp)
        {
            switch (exp.NodeType)
            {
                case ExpressionType.Constant:
                case ExpressionType.Default:
                case ExpressionType.Parameter:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsMultiLine(this LambdaExpression lambda)
        {
            var detector = new BlockExpressionDetector();
            detector.Visit(lambda);
            return detector.IsBlockExpression;
        }

        public static Expression NotNullReturn(this Expression exp, Expression value)
        {
            if (value.IsSingleValue() || !exp.CanBeNull())
                return value;

            var compareNull = Expression.Equal(exp, Expression.Constant(null, exp.Type));
            return Expression.Condition(
                compareNull,
                value.Type.CreateDefault(),
                value);
        }

        /// <summary>
        /// Unpack Enum Nullable TSource value
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public static Expression NullableEnumExtractor(this Expression param)
        {
            var _SourceType = param.Type;

            if (_SourceType.IsNullable())
            {
                var _genericType = param.Type.GetGenericArguments()[0];

                if (_genericType.IsEnum)
                {
                    var ExtractionExpression = Expression.Convert(param, typeof(object));
                    return ExtractionExpression;
                }

                return param;
            }

            return param;
        }
        public static Expression ApplyPropertyNullPropagation(this Expression getter)
        {
            var current = getter;
            var result = getter;
            Expression? condition = null;

            while (current.NodeType == ExpressionType.MemberAccess)
            {
                var memEx = (MemberExpression) current;
                var expr = memEx.Expression;
                if (expr == null)
                    break;
                if (expr.NodeType == ExpressionType.Parameter && condition != null)
                {
                    if (!getter.CanBeNull())
                    {
                        var transform = Expression.Convert(getter, typeof(Nullable<>).MakeGenericType(getter.Type));
                        return Expression.Condition(condition, transform, transform.Type.CreateDefault());
                    }
                    else
                        return Expression.Condition(condition, getter, getter.Type.CreateDefault());
                }

                if (expr.CanBeNull())
                {
                    var compareNull = Expression.NotEqual(expr, Expression.Constant(null, expr.Type));
                    if (condition == null)
                        condition = compareNull;
                    else
                        condition = Expression.AndAlso(compareNull, condition);
                }

                current = expr;
            }

            return getter;
        }

        public static Expression ApplyNullPropagationFromCtor(this Expression getter, Expression adapt, CompileArgument arg, MemberMapping? member = null)
        {
            if (getter == null)
                return adapt;

            Expression? condition = null;
            var current = getter;
            var checks = arg.Context.NullChecks
                .Where(x=> !object.ReferenceEquals(x.arg,arg))
                .Select(x=>x.param?.Type);

            while (current != null) 
            {
                Expression? compareNull = null;

                if (current.CanBeNull() && current is not ParameterExpression)
                    compareNull = Expression.NotEqual(current, Expression.Constant(null, current.Type));
                else if (current.CanBeNull() && current is ParameterExpression
                    && !checks.Contains(current.Type))
                    compareNull = Expression.NotEqual(current, Expression.Constant(null, current.Type));

                if (compareNull != null)
                {
                    if (condition == null)
                        condition = compareNull;
                    else
                        condition = Expression.AndAlso(compareNull, condition);
                }

                if (current is MemberExpression member)
                    current = member.Expression;
                else
                    current = null;
            }

            if (condition == null)
                return ApplyDestinationTransform(adapt, arg, member);

            // add supporting DestinationTransforms
            if (HasExplicitMemberMap(member, arg))
                return Expression.Condition(condition, adapt, Expression.Default(adapt.Type));

            var transform = arg.Settings.DestinationTransforms.Find(it => it.Condition(adapt.Type));
            if (transform != null)
                return transform.TransformFunc(adapt.Type).Apply(arg.MapType, Expression.Condition(condition, adapt, Expression.Default(adapt.Type)));

            return Expression.Condition(condition, adapt, Expression.Default(adapt.Type));
        }

        public static Expression ApplyDestinationTransform(Expression exp, CompileArgument arg, MemberMapping? mapping = null)
        {
            if (HasExplicitMemberMap(mapping, arg))
                return exp;

            var transform = arg.Settings.DestinationTransforms.Find(it => it.Condition(exp.Type));
            if (transform == null)
                return exp;

            return transform.TransformFunc(exp.Type).Apply(arg.MapType, exp);
        }

        static bool HasExplicitMemberMap(MemberMapping? mapping, CompileArgument arg)
        {
            if (mapping?.DestinationMember == null)
                return false;

            var memberName = mapping.DestinationMember.Name;
            return arg.Settings.Resolvers.Any(resolver =>
                !resolver.IsChildPath &&
                resolver.DestinationMemberName.Equals(memberName, StringComparison.InvariantCultureIgnoreCase));
        }

        public static string? GetMemberPath(this LambdaExpression lambda, bool firstLevelOnly = false, bool noError = false)
        {
            var props = new List<string>();
            var expr = lambda.Body.TrimConversion(true);
            while (expr?.NodeType == ExpressionType.MemberAccess)
            {
                if (firstLevelOnly && props.Count > 0)
                {
                    if (noError)
                        return null;
                    throw new ArgumentException("Only first level members are allowed (eg. obj => obj.Child)", nameof(lambda));
                }

                var memEx = (MemberExpression)expr;
                props.Add(memEx.Member.Name);
                expr = (Expression?)memEx.Expression;
            }
            if (props.Count == 0 || expr?.NodeType != ExpressionType.Parameter)
            {
                if (noError)
                    return null;
                throw new ArgumentException("Allow only member access (eg. obj => obj.Child.Name)", nameof(lambda));
            }
            props.Reverse();
            return string.Join(".", props);
        }

        public static bool IsIdentity(this LambdaExpression lambda)
        {
            var expr = lambda.Body.TrimConversion(true);
            return lambda.Parameters.Count == 1 && lambda.Parameters[0] == expr;
        }

        internal static Expression GetNameConverterExpression(Func<string, string> converter)
        {
            if (converter == MapsterHelper.Identity)
                return Expression.Field(null, typeof(MapsterHelper), nameof(MapsterHelper.Identity));
            if (converter == MapsterHelper.PascalCase)
                return Expression.Field(null, typeof(MapsterHelper), nameof(MapsterHelper.PascalCase));
            if (converter == MapsterHelper.CamelCase)
                return Expression.Field(null, typeof(MapsterHelper), nameof(MapsterHelper.CamelCase));
            if (converter == MapsterHelper.LowerCase)
                return Expression.Field(null, typeof(MapsterHelper), nameof(MapsterHelper.LowerCase));
            return Expression.Constant(converter);
        }

        public static bool IsNotPrimitiveNullableType(this Type type)
        {
            return Nullable.GetUnderlyingType(type) != null && !type.IsMapsterPrimitive();
        }

        public static Expression GetNotPrimitiveNullableValue(this Expression exp)
        {
            if (exp.Type.IsNotPrimitiveNullableType())
            {
                var getValueOrDefaultMethod = exp.Type.GetMethod("GetValueOrDefault", Type.EmptyTypes);
                var getValue = Expression.Call(exp, getValueOrDefaultMethod);

                return getValue;
            }

            return exp;
        }

    }
}
