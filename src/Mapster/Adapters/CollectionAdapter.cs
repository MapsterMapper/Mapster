using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Mapster.Utils;
using System.Reflection;

namespace Mapster.Adapters
{
    internal class CollectionAdapter : ITypeAdapterWithTarget
    {
        public bool CanAdapt(Type sourceType, Type destinationType)
        {
            return sourceType.IsCollection() &&
                destinationType.IsCollection();
        }

        public Func<TSource, TDestination> CreateAdaptFunc<TSource, TDestination>()
        {
            //var depth = Expression.Parameter(typeof (int));
            var p = Expression.Parameter(typeof (TSource));
            var settings = TypeAdapterConfig<TSource, TDestination>.ConfigSettings;
            var body = CreateExpressionBody(p, null, typeof(TSource), typeof(TDestination), settings);
            return Expression.Lambda<Func<TSource, TDestination>>(body, p).Compile();
        }

        public Func<TSource, TDestination, TDestination> CreateAdaptTargetFunc<TSource, TDestination>()
        {
            //var depth = Expression.Parameter(typeof (int));
            var p = Expression.Parameter(typeof (TSource));
            var p2 = Expression.Parameter(typeof (TDestination));
            var settings = TypeAdapterConfig<TSource, TDestination>.ConfigSettings;
            var body = CreateExpressionBody(p, p2, typeof(TSource), typeof(TDestination), settings);
            return Expression.Lambda<Func<TSource, TDestination, TDestination>>(body, p, p2).Compile();
        }

        private static Expression CreateExpressionBody(ParameterExpression p, ParameterExpression p2, Type sourceType, Type destinationType, TypeAdapterConfigSettingsBase settings)
        {
            var list = new List<Expression>();
            var destinationElementType = destinationType.ExtractCollectionType();

            var listType = destinationType.IsArray
                ? destinationType
                : destinationType.IsGenericEnumerableType() || destinationType.GetInterfaces().Any(ReflectionUtils.IsGenericEnumerableType)
                ? typeof(ICollection<>).MakeGenericType(destinationElementType)
                : typeof(IList);

            var pDest = Expression.Variable(listType);
            var pDest2 = Expression.Variable(destinationType);
            //var pDepth = Expression.Variable(typeof (int));
            Expression assign = null;
            Expression set;
            //Expression assignDepth = Expression.Assign(pDepth, Expression.Subtract(depth, Expression.Constant(1)));
            if (p2 != null)
            {
                assign = ExpressionEx.Assign(pDest, p2);
            }
            else if (settings?.ConstructUsing != null)
            {
                assign = ExpressionEx.Assign(pDest, settings.ConstructUsing.Body);
            }
            if (destinationType.IsArray)
            {
                set = CreateArraySet(p, pDest);
                if (assign == null)
                {
                    if (p.Type.IsArray)
                    {
                        assign = ExpressionEx.Assign(pDest,
                            Expression.NewArrayBounds(destinationElementType, Expression.ArrayLength(p)));
                    }
                    else
                    {
                        var countMethod = typeof (Enumerable).GetMethods()
                            .First(m => m.Name == "Count" && m.GetParameters().Length == 1)
                            .MakeGenericMethod(p.Type.ExtractCollectionType());
                        assign = ExpressionEx.Assign(pDest,
                            Expression.NewArrayBounds(destinationElementType, Expression.Call(countMethod, p)));
                    }
                }
            }
            else
            {
                if (!destinationType.IsInterface)
                {
                    set = CreateListSet(p, pDest);
                    if (assign == null)
                        assign = ExpressionEx.Assign(pDest, Expression.New(destinationType));
                }
                else
                {
                    set = CreateListSet(p, pDest);
                    if (assign == null)
                    {
                        var constructorInfo = typeof (List<>).MakeGenericType(destinationElementType);
                        assign = ExpressionEx.Assign(pDest, Expression.New(constructorInfo));
                    }
                }

            }

            //set = Expression.Block(new[] { pDepth }, assignDepth, assign, set);

            if (settings?.PreserveReference == true &&
                !sourceType.IsValueType &&
                !destinationType.IsValueType)
            {
                var propInfo = typeof(MapContext).GetProperty("References", BindingFlags.Static | BindingFlags.Public);
                var refDict = Expression.Property(null, propInfo);
                var refAdd = Expression.Call(refDict, "Add", null, Expression.Convert(p, typeof (object)), Expression.Convert(pDest, typeof (object)));
                set = Expression.Block(assign, refAdd, set);

                var pDest3 = Expression.Variable(typeof(object));
                var tryGetMethod = typeof (Dictionary<object, object>).GetMethod("TryGetValue", new[] {typeof (object), typeof (object).MakeByRefType()});
                var checkHasRef = Expression.Call(refDict, tryGetMethod, p, pDest3);
                set = Expression.IfThenElse(
                    checkHasRef,
                    ExpressionEx.Assign(pDest, pDest3),
                    set);
                set = Expression.Block(new[] {pDest3}, set);
            }
            else
            {
                set = Expression.Block(assign, set);
            }

            //if (TypeAdapterConfig.GlobalSettings.EnableMaxDepth)
            //{
            //    var compareDepth = Expression.Equal(depth, Expression.Constant(0));
            //    set = Expression.IfThenElse(
            //        compareDepth,
            //        ExpressionEx.Assign(pDest, (Expression) p2 ?? Expression.Constant(null, listType)),
            //        set);
            //}

            if (!sourceType.IsValueType || sourceType.IsNullable())
            {
                var compareNull = Expression.Equal(p, Expression.Constant(null, p.Type));
                set = Expression.IfThenElse(
                    compareNull,
                    ExpressionEx.Assign(pDest, (Expression) p2 ?? Expression.Constant(listType.GetDefault(), listType)),
                    set);
            }
            list.Add(set);

            if (destinationType.IsGenericType && destinationType.GetGenericTypeDefinition() == typeof (IQueryable<>))
            {
                var method = typeof (Queryable).GetMethods().First(m => m.Name == "AsQueryable" && m.IsGenericMethod)
                    .MakeGenericMethod(destinationElementType);
                list.Add(ExpressionEx.Assign(pDest2, Expression.Call(method, pDest)));
            }
            else if (destinationType == typeof (IQueryable))
            {
                var method = typeof (Queryable).GetMethods().First(m => m.Name == "AsQueryable" && !m.IsGenericMethod);
                list.Add(ExpressionEx.Assign(pDest2, Expression.Call(method, pDest)));
            }
            else
                list.Add(ExpressionEx.Assign(pDest2, pDest));

            var destinationTransforms = TypeAdapterConfig.GlobalSettings.DestinationTransforms.Transforms;
            if (destinationTransforms.ContainsKey(destinationType))
            {
                var transform = destinationTransforms[destinationType];
                var invoke = Expression.Invoke(transform, pDest2);
                list.Add(ExpressionEx.Assign(pDest2, invoke));
            }
            var localTransform = settings?.DestinationTransforms.Transforms;
            if (localTransform != null && localTransform.ContainsKey(destinationType))
            {
                var transform = localTransform[destinationType];
                var invoke = Expression.Invoke(transform, pDest2);
                list.Add(ExpressionEx.Assign(pDest2, invoke));
            }

            list.Add(pDest2);
            return Expression.Block(destinationType, new[] { pDest, pDest2 }, list);

        }

        private static Expression CreateGetter(Expression p, Type sourceElementType, Type destinationElementType)
        {
            var adapter = TypeAdapter.GetAdapter(sourceElementType, destinationElementType) as ITypeExpression;

            if (adapter != null)
            {
                return adapter.CreateExpression(p, sourceElementType, destinationElementType);
            }
            else
            {
                var typeAdaptType = typeof (TypeAdapter<,>).MakeGenericType(sourceElementType, destinationElementType);
                var method = typeAdaptType.GetMethod("AdaptWithContext",
                    new[] {sourceElementType});
                return Expression.Call(method, p);
            }
        }

        private static Expression CreateArraySet(Expression source, Expression destination)
        {
            var sourceElementType = source.Type.ExtractCollectionType();
            var destinationElementType = destination.Type.ExtractCollectionType();
            var p = Expression.Parameter(sourceElementType);
            var v = Expression.Variable(typeof (int));
            var start = Expression.Assign(v, Expression.Constant(0));
            var getter = CreateGetter(p, sourceElementType, destinationElementType);
            var set = Expression.Assign(
                Expression.ArrayAccess(destination, v),
                getter);
            var inc = Expression.PostIncrementAssign(v);
            var loop = ForLoop(source, p, set, inc);
            return Expression.Block(new[] {v}, start, loop);
        }

        private static Expression CreateListSet(Expression source, Expression destination)
        {
            var sourceElementType = source.Type.ExtractCollectionType();
            var destinationElementType = destination.Type.ExtractCollectionType();
            var p = Expression.Parameter(sourceElementType);
            var getter = CreateGetter(p, sourceElementType, destinationElementType);
            
            var addMethod = destination.Type.GetMethod("Add", new[] {destinationElementType});
            var set = Expression.Call(
                destination,
                addMethod,
                getter);
            var loop = ForLoop(source, p, set);
            return loop;
        }

        private static Expression ForLoop(Expression collection, ParameterExpression loopVar, params Expression[] loopContent)
        {
            var i = Expression.Variable(typeof(int), "i");
            var len = Expression.Variable(typeof(int), "len");
            Expression lenAssign;
            Expression current;
            if (collection.Type.IsArray)
            {
                current = Expression.ArrayIndex(collection, Expression.PostIncrementAssign(i));
                lenAssign = Expression.Assign(len, Expression.ArrayLength(collection));
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

                current = Expression.Property(collection, indexer, Expression.PostIncrementAssign(i));
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
                            new[] { Expression.Assign(loopVar, current) }.Concat(loopContent)
                        ),
                        Expression.Break(breakLabel)
                    ),
                breakLabel)
            );

            return loop;
        }

        private static Expression ForEach(Expression collection, ParameterExpression loopVar, params Expression[] loopContent)
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
