using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Mapster.Utils;

namespace Mapster.Adapters
{
    public class ArrayAdapter : BaseAdapter
    {
        protected override int Score => -123;
        protected override ObjectType ObjectType => ObjectType.Collection;

        protected override bool CanMap(PreCompileArgument arg)
        {
            return arg.SourceType.IsCollection()
                   && arg.DestinationType.IsArray;
        }

        protected override bool CanInline(Expression source, Expression? destination, CompileArgument arg)
        {
            return arg.MapType == MapType.Projection;
        }

        protected override Expression TransformSource(Expression source)
        {
            if (ExpressionEx.CreateCountExpression(source) != null)
                return source;
            var transformed = source;
            var elemType = source.Type.ExtractCollectionType();
            var type = typeof(IEnumerable<>).MakeGenericType(elemType);
            if (!type.IsAssignableFrom(source.Type))
            {
                var cast = typeof(Enumerable).GetMethod(nameof(Enumerable.Cast))!
                    .MakeGenericMethod(elemType);
                transformed = Expression.Call(cast, transformed);
            }

            var toList = typeof(Enumerable).GetMethod(nameof(Enumerable.ToList))!
                .MakeGenericMethod(elemType);
            return Expression.Call(toList, transformed);
        }

        protected override Expression CreateInstantiationExpression(Expression source, Expression? destination, CompileArgument arg)
        {
            var destinationElementType = arg.DestinationType.ExtractCollectionType();
            return Expression.NewArrayBounds(
                destinationElementType,
                ExpressionEx.CreateCountExpression(source));   //new TDestinationElement[count]
        }

        protected override Expression CreateBlockExpression(Expression source, Expression destination, CompileArgument arg)
        {
            if (source.Type.IsArray &&
                source.Type.GetArrayRank() == 1 &&
                source.Type.GetElementType() == destination.Type.GetElementType() &&
                source.Type.GetElementType()!.IsPrimitiveKind())
            {
                //Array.Copy(src, 0, dest, 0, src.Length)
                var method = typeof(Array).GetMethod("Copy", new[] { typeof(Array), typeof(int), typeof(Array), typeof(int), typeof(int) });
                var len = arg.UseDestinationValue
                    ? Expression.Call(typeof(Math).GetMethod("Min", new[] {typeof(int), typeof(int)})!,
                        ExpressionEx.CreateCountExpression(source)!, 
                        ExpressionEx.CreateCountExpression(destination)!)
                    : ExpressionEx.CreateCountExpression(source);
                return Expression.Call(method!, source, Expression.Constant(0), destination, Expression.Constant(0), len!);
            }

            return CreateArraySet(source, destination, arg);
        }

        protected override Expression CreateInlineExpression(Expression source, CompileArgument arg)
        {
            if (arg.DestinationType.GetTypeInfo().IsAssignableFrom(source.Type.GetTypeInfo()))
                return source;

            var sourceElementType = source.Type.ExtractCollectionType();
            var destinationElementType = arg.DestinationType.ExtractCollectionType();

            var p1 = Expression.Parameter(sourceElementType);
            var adapt = CreateAdaptExpression(p1, destinationElementType, arg);

            //src.Select(item => convert(item))
            var method = (from m in typeof(Enumerable).GetMethods()
                          where m.Name == nameof(Enumerable.Select)
                          let p = m.GetParameters()[1]
                          where p.ParameterType.GetGenericTypeDefinition() == typeof(Func<,>)
                          select m).First().MakeGenericMethod(sourceElementType, destinationElementType);
            var exp = Expression.Call(method, source, Expression.Lambda(adapt, p1));

            //src.Select(item => convert(item)).ToArray()
            var toList = (from m in typeof(Enumerable).GetMethods()
                            where m.Name == nameof(Enumerable.ToArray)
                            select m).First().MakeGenericMethod(destinationElementType);
            exp = Expression.Call(toList, exp);

            return exp;
        }

        private Expression CreateArraySet(Expression source, Expression destination, CompileArgument arg)
        {
            //### IList<T>
            //var v = 0
            //for (var i = 0, len = src.Count; i < len; i++) {
            //  var item = src[i];
            //  dest[v++] = convert(item);
            //}

            //### IEnumerable<T>
            //var v = 0;
            //foreach (var item in src)
            //  dest[v++] = convert(item);

            var sourceElementType = source.Type.ExtractCollectionType();
            var destinationElementType = destination.Type.ExtractCollectionType();
            var item = Expression.Variable(sourceElementType, "item");
            var v = Expression.Variable(typeof(int), "v");
            var start = Expression.Assign(v, Expression.Constant(0));
            var getter = CreateAdaptExpression(item, destinationElementType, arg);
            var set = Expression.Assign(
                Expression.ArrayAccess(destination, Expression.PostIncrementAssign(v)),
                getter);
            var loop = ExpressionEx.ForLoop(source, item, set);
            return Expression.Block(new[] { v }, start, loop);
        }
    }
}
