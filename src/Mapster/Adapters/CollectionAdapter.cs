using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Mapster.Models;
using Mapster.Utils;

namespace Mapster.Adapters
{
    internal static class CollectionAdapter<TSource, TDestination>
    {
        public static Expression<Func<ReferenceChecker, TSource, TDestination>> CreateAdaptFunc()
        {
            var checker = Expression.Parameter(typeof (ReferenceChecker));
            var p = Expression.Parameter(typeof (TSource));
            var body = CreateExpressionBody(checker, p, null);
            return Expression.Lambda<Func<ReferenceChecker, TSource, TDestination>>(body, checker, p);
        }

        public static Expression<Func<ReferenceChecker, TSource, TDestination, TDestination>> CreateAdaptTargetFunc()
        {
            var checker = Expression.Parameter(typeof (ReferenceChecker));
            var p = Expression.Parameter(typeof (TSource));
            var p2 = Expression.Parameter(typeof (TDestination));
            var body = CreateExpressionBody(checker, p, p2);
            return Expression.Lambda<Func<ReferenceChecker, TSource, TDestination, TDestination>>(body,checker, p, p2);
        }

        public static Expression CreateExpressionBody(ParameterExpression checker, ParameterExpression p, ParameterExpression p2)
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
            var pCheck = Expression.Variable(typeof (ReferenceChecker));
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
                set = CreateArraySet(pCheck, p, pDest);
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
                    set = CreateListSet(pCheck, p, pDest);
                    if (assign == null)
                        assign = ExpressionEx.Assign(pDest, Expression.New(destinationType));
                }
                else
                {
                    set = CreateListSet(pCheck, p, pDest);
                    if (assign == null)
                    {
                        var constructorInfo = typeof (List<>).MakeGenericType(destinationElementType);
                        assign = ExpressionEx.Assign(pDest, Expression.New(constructorInfo));
                    }
                }

            }
            list.Add(assign);

            var hasCircularCheck = setting?.CircularReferenceCheck ?? TypeAdapterConfig.GlobalSettings.CircularReferenceCheck;
            list.Add(hasCircularCheck 
                ? Expression.Assign(pCheck, Expression.Call(checker, "Add", Type.EmptyTypes, Expression.Convert(p, typeof(object)), Expression.Convert(pDest, typeof(object)))) 
                : Expression.Assign(pCheck, checker));

            if (hasCircularCheck)
            {
                var checkCircular = Expression.Property(pCheck, "IsCircular");
                set = Expression.IfThenElse(
                    checkCircular,
                    ExpressionEx.Assign(pDest, Expression.Property(pCheck, "Result")),
                    set);
            }

            var compareNull = Expression.Equal(p, Expression.Constant(null, p.Type));
            set = Expression.IfThenElse(
                compareNull,
                ExpressionEx.Assign(pDest, (Expression)p2 ?? Expression.Constant(null, listType)),
                set);
            list.Add(set);

            var destinationTransforms = TypeAdapterConfig.GlobalSettings.DestinationTransforms.Transforms;
            if (destinationTransforms.ContainsKey(destinationType))
            {
                var transform = destinationTransforms[destinationType];
                var invoke = Expression.Invoke(transform, pDest);
                list.Add(Expression.Assign(pDest, invoke));
            }
            var localTransform = setting?.DestinationTransforms.Transforms;
            if (localTransform != null && localTransform.ContainsKey(destinationType))
            {
                var transform = localTransform[destinationType];
                var invoke = Expression.Invoke(transform, pDest);
                list.Add(Expression.Assign(pDest, invoke));
            }

            if (hasCircularCheck)
                list.Add(ExpressionEx.Assign(Expression.Property(pCheck, "Result"), pDest));
            if (listType == destinationType)
                list.Add(pDest);
            else
                list.Add(Expression.Convert(pDest, destinationType));
            return Expression.Block(destinationType, new[] {pDest, pCheck}, list);
        }

        private static Expression CreateArraySet(Expression checker, Expression source, Expression destination)
        {
            var sourceElementType = source.Type.ExtractCollectionType();
            var destinationElementType = destination.Type.ExtractCollectionType();
            var p = Expression.Parameter(sourceElementType);
            var v = Expression.Variable(typeof (int));
            var start = Expression.Assign(v, Expression.Constant(0));
            var typeAdaptType = typeof (TypeAdapter<,>).MakeGenericType(sourceElementType, destinationElementType);
            var method = typeAdaptType.GetMethod("AdaptWithCheck",
                new[] {typeof (ReferenceChecker), sourceElementType});
            var set = Expression.Assign(
                Expression.ArrayAccess(destination, v),
                Expression.Call(method, checker, p));
            var inc = Expression.PostIncrementAssign(v);
            var loop = ForEach(source, p, set, inc);
            return Expression.Block(new[] {v}, start, loop);
        }

        private static Expression CreateListSet(Expression checker, Expression source, Expression destination)
        {
            var sourceElementType = source.Type.ExtractCollectionType();
            var destinationElementType = destination.Type.ExtractCollectionType();
            var p = Expression.Parameter(sourceElementType);
            var typeAdaptType = typeof(TypeAdapter<,>).MakeGenericType(sourceElementType, destinationElementType);
            var adaptMethod = typeAdaptType.GetMethod("AdaptWithCheck",
                new[] { typeof(ReferenceChecker), sourceElementType });
            
            var addMethod = destination.Type.GetMethod("Add", new[] {destinationElementType});
            var set = Expression.Call(
                destination,
                addMethod,
                Expression.Call(adaptMethod, checker, p));
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
