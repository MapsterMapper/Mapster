using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Mapster.Utils;

namespace Mapster.Adapters
{
    public static class CollectionAdapter<TSource, TDestination>
    {
        public static Expression<Func<int, TSource, TDestination>> CreateAdaptFunc()
        {
            var depth = Expression.Parameter(typeof (int));
            var p = Expression.Parameter(typeof (TSource));
            var body = CreateExpressionBody(depth, p, null);
            return Expression.Lambda<Func<int, TSource, TDestination>>(body, depth, p);
        }

        public static Expression<Func<int, TSource, TDestination, TDestination>> CreateAdaptTargetFunc()
        {
            var depth = Expression.Parameter(typeof (int));
            var p = Expression.Parameter(typeof (TSource));
            var p2 = Expression.Parameter(typeof (TDestination));
            var body = CreateExpressionBody(depth, p, p2);
            return Expression.Lambda<Func<int, TSource, TDestination, TDestination>>(body,depth, p, p2);
        }

        public static Expression CreateExpressionBody(ParameterExpression depth, ParameterExpression p, ParameterExpression p2)
        {
            var destinationType = typeof (TDestination);
            var list = new List<Expression>();
            var setting = TypeAdapterConfig<TSource, TDestination>.ConfigSettings;
            var destinationElementType = destinationType.ExtractCollectionType();

            var listType = destinationType.IsArray
                ? destinationType
                : destinationType.IsGenericEnumerableType() || destinationType.GetInterfaces().Any(ReflectionUtils.IsGenericEnumerableType)
                ? typeof(ICollection<>).MakeGenericType(destinationElementType)
                : typeof(IList);

            var pDest = Expression.Variable(listType);
            Expression assign = null;
            Expression set;
            if (p2 != null)
            {
                assign = ExpressionEx.Assign(pDest, p2);
            }
            else if (setting?.ConstructUsing != null)
            {
                assign = ExpressionEx.Assign(pDest, Expression.Invoke(setting.ConstructUsing));
            }
            if (destinationType.IsArray)
            {
                set = CreateArraySet(depth, p, pDest);
                if (assign == null)
                {
                    var countMethod = typeof (Enumerable).GetMethods()
                        .First(m => m.Name == "Count" && m.GetParameters().Length == 1)
                        .MakeGenericMethod(p.Type.ExtractCollectionType());
                    assign = ExpressionEx.Assign(pDest,
                        Expression.NewArrayBounds(destinationElementType, Expression.Call(countMethod, p)));
                }
            }
            else
            {
                if (!destinationType.IsInterface)
                {
                    set = CreateListSet(depth, p, pDest);
                    if (assign == null)
                        assign = ExpressionEx.Assign(pDest, Expression.New(destinationType));
                }
                else
                {
                    set = CreateListSet(depth, p, pDest);
                    if (assign == null)
                    {
                        var constructorInfo = typeof (List<>).MakeGenericType(destinationElementType);
                        assign = ExpressionEx.Assign(pDest, Expression.New(constructorInfo));
                    }
                }

            }

            set = Expression.Block(assign, set);
            if (setting?.MaxDepth != null && setting.MaxDepth.Value > 0)
            {
                var compareDepth = Expression.GreaterThan(depth, Expression.Constant(setting.MaxDepth.Value));
                set = Expression.IfThenElse(
                    compareDepth,
                    ExpressionEx.Assign(pDest, (Expression)p2 ?? Expression.Constant(null, listType)),
                    set);
            }

            var compareNull = Expression.Equal(p, Expression.Constant(null, p.Type));
            set = Expression.IfThenElse(
                compareNull,
                ExpressionEx.Assign(pDest, (Expression)p2 ?? Expression.Constant(null, listType)),
                set);
            list.Add(set);

            var destinationTransforms = setting != null
                ? setting.DestinationTransforms.Transforms
                : TypeAdapterConfig.GlobalSettings.DestinationTransforms.Transforms;
            if (destinationTransforms.ContainsKey(destinationType))
            {
                var transform = destinationTransforms[destinationType];
                var invoke = Expression.Invoke(transform, pDest);
                list.Add(ExpressionEx.Assign(pDest, invoke));
            }
            if (listType == destinationType)
                list.Add(pDest);
            else
                list.Add(Expression.Convert(pDest, destinationType));
            return Expression.Block(destinationType, new[] { pDest }, list);

        }

        private static Expression CreateArraySet(Expression depth, Expression source, Expression destination)
        {
            var sourceElementType = source.Type.ExtractCollectionType();
            var destinationElementType = destination.Type.ExtractCollectionType();
            var p = Expression.Parameter(sourceElementType);
            var v = Expression.Variable(typeof (int));
            var start = Expression.Assign(v, Expression.Constant(0));
            var typeAdaptType = typeof (TypeAdapter<,>).MakeGenericType(sourceElementType, destinationElementType);
            var method = typeAdaptType.GetMethod("AdaptWithDepth",
                new[] {typeof (int), sourceElementType});
            var set = Expression.Assign(
                Expression.ArrayAccess(destination, v),
                Expression.Call(method, depth, p));
            var inc = Expression.PostIncrementAssign(v);
            var loop = ForEach(source, p, set, inc);
            return Expression.Block(new[] {v}, start, loop);
        }

        private static Expression CreateListSet(Expression depth, Expression source, Expression destination)
        {
            var sourceElementType = source.Type.ExtractCollectionType();
            var destinationElementType = destination.Type.ExtractCollectionType();
            var p = Expression.Parameter(sourceElementType);
            var typeAdaptType = typeof(TypeAdapter<,>).MakeGenericType(sourceElementType, destinationElementType);
            var adaptMethod = typeAdaptType.GetMethod("AdaptWithDepth",
                new[] { typeof(int), sourceElementType });
            
            var addMethod = destination.Type.GetMethod("Add", new[] {destinationElementType});
            var set = Expression.Call(
                destination,
                addMethod,
                Expression.Call(adaptMethod, depth, p));
            var loop = ForEach(source, p, set);
            return loop;
        }

        public static Expression ForEach(Expression collection, ParameterExpression loopVar, params Expression[] loopContent)
        {
            var elementType = loopVar.Type;
            var enumerableType = typeof (IEnumerable<>).MakeGenericType(elementType);
            var enumeratorType = typeof (IEnumerator<>).MakeGenericType(elementType);
            var isGeneric = enumerableType.IsAssignableFrom(collection.Type);
            if (!isGeneric)
            {
                enumerableType = typeof (IEnumerable);
                enumeratorType = typeof (IEnumerator);
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
                            new[] { Expression.Assign(loopVar, current) }.Concat(loopContent)
                        ),
                        Expression.Break(breakLabel)
                    ),
                breakLabel)
            );

            return loop;
        }
    }
}
