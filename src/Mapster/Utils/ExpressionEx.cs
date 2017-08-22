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

        public static Expression Apply(this LambdaExpression lambda, params Expression[] exps)
        {
            var replacer = new ParameterExpressionReplacer(lambda.Parameters, exps);
            return replacer.Visit(lambda.Body);
        }

        public static Expression TrimConversion(this Expression exp, bool force = false)
        {
            while (exp.NodeType == ExpressionType.Convert || exp.NodeType == ExpressionType.ConvertChecked)
            {
                var unary = (UnaryExpression) exp;
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
            bool sameType = force ? type == exp.Type : type.IsReferenceAssignableFrom(exp.Type);
            if (sameType)
                return exp;
            else
                return Expression.Convert(exp, type);
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

        public static Expression CreateCountExpression(Expression source, bool allowCountAll)
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
                var countProperty = source.Type.GetProperty("Count");
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
                var count = collection.Type.GetProperty("Count");

                //if indexer is not found, fallback to foreach
                if (indexer == null || count == null)
                    return ForEach(collection, loopVar, loopContent);

                current = Expression.Property(collection, indexer, i);
                lenAssign = Expression.Assign(len, Expression.Property(collection, count));
            }

            var iAssign = Expression.Assign(i, Expression.Constant(0));

            var breakLabel = Expression.Label("LoopBreak");

            var loop = Expression.Block(new[] { i, len },
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

            return loop;
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
            var loop = Expression.Block(new[] { enumeratorVar },
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

            return loop;
        }
    }
}
