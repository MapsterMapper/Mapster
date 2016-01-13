using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Mapster.Utils;
using System.Reflection;

namespace Mapster.Adapters
{
    internal class CollectionAdapter : BaseAdapter
    {
        public override bool CanAdapt(Type sourceType, Type destinationType)
        {
            return sourceType.IsCollection() &&
                destinationType.IsCollection();
        }

        private static Expression CreateCountExpression(ParameterExpression source, bool allowCountAll)
        {
            if (source.Type.IsArray)
                return Expression.ArrayLength(source);
            else
            {
                var countProperty = source.Type.GetProperty("Count");
                if (countProperty != null)
                    return Expression.Property(source, countProperty);
                if (!allowCountAll)
                    return null;
                var countMethod = typeof(Enumerable).GetMethods()
                    .First(m => m.Name == "Count" && m.GetParameters().Length == 1)
                    .MakeGenericMethod(source.Type.ExtractCollectionType());
                return Expression.Call(countMethod, source);
            }
        }

        protected override Expression CreateInstantiationExpression(ParameterExpression source, Type destinationType, TypeAdapterConfigSettingsBase settings)
        {
            var destinationElementType = destinationType.ExtractCollectionType();
            if (destinationType.IsArray)
                return Expression.NewArrayBounds(destinationElementType, CreateCountExpression(source, true));

            var count = CreateCountExpression(source, false);
            var listType = destinationType.IsInterface
                ? typeof (List<>).MakeGenericType(destinationElementType)
                : destinationType;
            if (count == null)
                return Expression.New(listType);
            var ctor = listType.GetConstructor(new[] { typeof(int) });
            if (ctor == null)
                return Expression.New(listType);
            else
                return Expression.New(ctor, count);
        }

        protected override Expression CreateSetterExpression(ParameterExpression source, ParameterExpression destination, TypeAdapterConfigSettingsBase settings)
        {
            if (destination.Type.IsArray)
            {
                return CreateArraySet(source, destination, settings);
            }
            else
            {
                var destinationElementType = destination.Type.ExtractCollectionType();
                var listType = destination.Type.IsGenericEnumerableType() || destination.Type.GetInterfaces().Any(ReflectionUtils.IsGenericEnumerableType)
                    ? typeof(ICollection<>).MakeGenericType(destinationElementType)
                    : typeof(IList);
                var tmp = Expression.Variable(listType);
                var assign = ExpressionEx.Assign(tmp, destination); //convert to list type
                var set = CreateListSet(source, tmp, settings);
                return Expression.Block(new[] {tmp}, assign, set);
            }
        }

        private Expression CreateArraySet(Expression source, Expression destination, TypeAdapterConfigSettingsBase settings)
        {
            var sourceElementType = source.Type.ExtractCollectionType();
            var destinationElementType = destination.Type.ExtractCollectionType();
            var p = Expression.Parameter(sourceElementType);
            var v = Expression.Variable(typeof (int));
            var start = Expression.Assign(v, Expression.Constant(0));
            var getter = CreateAdaptExpression(p, destinationElementType, settings);
            var set = Expression.Assign(
                Expression.ArrayAccess(destination, v),
                getter);
            var inc = Expression.PostIncrementAssign(v);
            var loop = ForLoop(source, p, set, inc);
            return Expression.Block(new[] {v}, start, loop);
        }

        private Expression CreateListSet(Expression source, Expression destination, TypeAdapterConfigSettingsBase settings)
        {
            var sourceElementType = source.Type.ExtractCollectionType();
            var destinationElementType = destination.Type.ExtractCollectionType();
            var p = Expression.Parameter(sourceElementType);
            var getter = CreateAdaptExpression(p, destinationElementType, settings);
            
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
