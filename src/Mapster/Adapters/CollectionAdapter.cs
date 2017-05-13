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
        protected override int Score => -125;
        protected override bool CheckExplicitMapping => false;

        protected override bool CanMap(Type sourceType, Type destinationType, MapType mapType)
        {
            return sourceType.IsCollection()
                   && destinationType.IsCollection()
                   && destinationType.IsListCompatible();
        }

        private static Expression CreateCountExpression(Expression source, bool allowCountAll)
        {
            if (source.Type.IsArray)
                return Expression.ArrayLength(source);
            else
            {
                var countProperty = source.Type.GetProperty("Count");
                if (countProperty != null)
                    return Expression.Property(source, countProperty);  //list.Count
                if (!allowCountAll)
                    return null;
                var countMethod = typeof(Enumerable).GetMethods()
                    .First(m => m.Name == "Count" && m.GetParameters().Length == 1)
                    .MakeGenericMethod(source.Type.ExtractCollectionType());
                return Expression.Call(countMethod, source);            //list.Count()
            }
        }

        protected override bool CanInline(Expression source, Expression destination, CompileArgument arg)
        {
            if (!base.CanInline(source, destination, arg))
                return false;

            if (arg.MapType == MapType.Projection)
            {
                if (arg.DestinationType.IsAssignableFromList())
                    return true;

                throw new InvalidOperationException($"{arg.DestinationType} is not supported for projection, please consider using List<>");
            }

            if (arg.DestinationType == typeof (IEnumerable) || arg.DestinationType.IsGenericEnumerableType())
                return true;

            return false;
        }

        protected override Expression CreateInstantiationExpression(Expression source, Expression destination, CompileArgument arg)
        {
            var destinationElementType = arg.DestinationType.ExtractCollectionType();
            if (arg.DestinationType.IsArray)
                return Expression.NewArrayBounds(
                    destinationElementType, 
                    CreateCountExpression(source, true));   //new TDestinationElement[count]

            var count = CreateCountExpression(source, false);
            var listType = arg.DestinationType.GetTypeInfo().IsInterface
                ? typeof (List<>).MakeGenericType(destinationElementType)
                : arg.DestinationType;
            if (count == null)
                return Expression.New(listType);            //new List<T>()
            var ctor = (from c in listType.GetConstructors()
                        let args = c.GetParameters()
                        where args.Length == 1 && args[0].ParameterType == typeof (int)
                        select c).FirstOrDefault();
            if (ctor == null)
                return Expression.New(listType);            //new List<T>()
            else
                return Expression.New(ctor, count);         //new List<T>(count)
        }

        protected override Expression CreateBlockExpression(Expression source, Expression destination, CompileArgument arg)
        {
            if (destination.Type.IsArray)
            {
                if (source.Type.IsArray && 
                    source.Type.GetElementType() == destination.Type.GetElementType() &&
                    source.Type.GetElementType().UnwrapNullable().IsConvertible())
                {
                    //Array.Copy(src, 0, dest, 0, src.Length)
                    var method = typeof (Array).GetMethod("Copy", new[] {typeof (Array), typeof (int), typeof (Array), typeof (int), typeof (int)});
                    return Expression.Call(method, source, Expression.Constant(0), destination, Expression.Constant(0), Expression.ArrayLength(source));
                }
                else
                    return CreateArraySet(source, destination, arg);
            }
            else
            {
                var destinationElementType = destination.Type.ExtractCollectionType();
                var listType = destination.Type.GetGenericEnumerableType() != null
                    ? typeof(ICollection<>).MakeGenericType(destinationElementType)
                    : typeof(IList);
                var tmp = Expression.Variable(listType);
                var assign = ExpressionEx.Assign(tmp, destination); //convert to list type
                var set = CreateListSet(source, tmp, arg);
                return Expression.Block(new[] {tmp}, assign, set);
            }
        }

        protected override Expression CreateInlineExpression(Expression source, CompileArgument arg)
        {
            if (arg.DestinationType.GetTypeInfo().IsAssignableFrom(source.Type.GetTypeInfo()) && (arg.Settings.ShallowCopyForSameType == true || arg.MapType == MapType.Projection))
                return source;

            var sourceElementType = source.Type.ExtractCollectionType();
            var destinationElementType = arg.DestinationType.ExtractCollectionType();

            var p1 = Expression.Parameter(sourceElementType);
            var adapt = CreateAdaptExpression(p1, destinationElementType, arg);
            if (adapt == p1)
            {
                if (arg.MapType == MapType.Projection)
                    return source;

                var toEnum = (from m in typeof(Expression).GetMethods()
                              where m.Name == "ToEnumerable"
                              select m).First().MakeGenericMethod(destinationElementType);
                return Expression.Call(toEnum, source);
            }

            //src.Select(item => convert(item))
            var method = (from m in typeof(Enumerable).GetMethods()
                          where m.Name == "Select"
                          let p = m.GetParameters()[1]
                          where p.ParameterType.GetGenericTypeDefinition() == typeof(Func<,>)
                          select m).First().MakeGenericMethod(sourceElementType, destinationElementType);
            var exp = Expression.Call(method, source, Expression.Lambda(adapt, p1));
            if (exp.Type != arg.DestinationType)
            {
                //src.Select(item => convert(item)).ToList()
                var toList = (from m in typeof (Enumerable).GetMethods()
                              where m.Name == "ToList"
                              select m).First().MakeGenericMethod(destinationElementType);
                exp = Expression.Call(toList, exp);
            }
            return exp;
        }

        private Expression CreateArraySet(Expression source, Expression destination, CompileArgument arg)
        {
            //### IList<T>
            //var v = 0
            //for (var i = 0, len = src.Count; i < len; i++) {
            //  var p = src[i];
            //  dest[v++] = convert(p);
            //}

            //### IEnumerable<T>
            //var v = 0;
            //foreach (var p in src)
            //  dest[v++] = convert(p);

            var sourceElementType = source.Type.ExtractCollectionType();
            var destinationElementType = destination.Type.ExtractCollectionType();
            var p = Expression.Parameter(sourceElementType);
            var v = Expression.Variable(typeof (int));
            var start = Expression.Assign(v, Expression.Constant(0));
            var getter = CreateAdaptExpression(p, destinationElementType, arg);
            var set = Expression.Assign(
                Expression.ArrayAccess(destination, Expression.PostIncrementAssign(v)),
                getter);
            var loop = ForLoop(source, p, set);
            return Expression.Block(new[] {v}, start, loop);
        }

        private Expression CreateListSet(Expression source, Expression destination, CompileArgument arg)
        {
            //### IList<T>
            //for (var i = 0, len = src.Count; i < len; i++) {
            //  var p = src[i];
            //  dest.Add(convert(p));
            //}

            //### IEnumerable<T>
            //foreach (var p in src)
            //  dest.Add(convert(p));

            var sourceElementType = source.Type.ExtractCollectionType();
            var destinationElementType = destination.Type.ExtractCollectionType();
            var p = Expression.Parameter(sourceElementType);
            var getter = CreateAdaptExpression(p, destinationElementType, arg);
            
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

        private static Expression ForEach(Expression collection, ParameterExpression loopVar, params Expression[] loopContent)
        {
            var elementType = loopVar.Type;
            var enumerableType = typeof (IEnumerable<>).MakeGenericType(elementType);
            var enumeratorType = typeof (IEnumerator<>).MakeGenericType(elementType);
            var isGeneric = enumerableType.GetTypeInfo().IsAssignableFrom(collection.Type.GetTypeInfo());
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
