using System.Linq.Expressions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
            var props = path.Split('.');
            return expr.NullPropagate(props);
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
                return result;
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

        public static Expression? CreateCountExpression(Expression source, bool allowCountAll)
        {
            if (source.Type.IsArray)
            {
                if (source.Type.GetArrayRank() == 1)
                    return Expression.ArrayLength(source);      //array.Length
                else
                    return Expression.Property(source, "Length");
            }
            else
            {
                var countProperty = GetCount(source.Type);
                if (countProperty != null)
                    return Expression.Property(source, countProperty);  //list.Count
                if (!allowCountAll)
                    return null;
                var countMethod = typeof(Enumerable).GetMethods()
                    .First(m => m.Name == nameof(Enumerable.Count) && m.GetParameters().Length == 1)
                    .MakeGenericMethod(source.Type.ExtractCollectionType());
                return Expression.Call(countMethod, source);            //list.Count()
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
            var getEnumeratorCall = Expression.Call(collection, enumerableType.GetMethod("GetEnumerator"));
            var enumeratorAssign = Expression.Assign(enumeratorVar, getEnumeratorCall);

            // The MoveNext method's actually on IEnumerator, not IEnumerator<T>
            var moveNextCall = Expression.Call(enumeratorVar, typeof(IEnumerator).GetMethod("MoveNext"));

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

        public static Expression NullPropagate(this Expression exp, Expression value)
        {
            if (value.IsSingleValue() || !exp.CanBeNull())
                return value;

            var compareNull = Expression.Equal(exp, Expression.Constant(null, exp.Type));
            return Expression.Condition(
                compareNull,
                value.Type.CreateDefault(),
                value);
        }

        public static Expression NullPropagate(this Expression exp, string[] props, int i = 0)
        {
            if (props.Length <= i)
                return exp;
            var head = Expression.PropertyOrField(exp, props[i]);
            var tail = NullPropagate(head, props, i + 1);
            if (!exp.CanBeNull())
                return tail;

            var compareNull = Expression.Equal(exp, Expression.Constant(null, exp.Type));
            return Expression.Condition(
                compareNull,
                tail.Type.CreateDefault(),
                tail);
        }
    }
}
