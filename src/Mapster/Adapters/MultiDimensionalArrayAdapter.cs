using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Mapster.Utils;

// ReSharper disable once RedundantUsingDirective
using System.Reflection;

namespace Mapster.Adapters
{
    public class MultiDimensionalArrayAdapter : BaseAdapter
    {
        protected override int Score => -122;
        protected override ObjectType ObjectType => ObjectType.Collection;

        protected override bool CanMap(PreCompileArgument arg)
        {
            return arg.SourceType.IsCollection()
                   && arg.DestinationType.IsArray
                   && arg.DestinationType.GetArrayRank() > 1;
        }

        protected override bool CanInline(Expression source, Expression? destination, CompileArgument arg)
        {
            return false;
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
            return Expression.NewArrayBounds(arg.DestinationType.GetElementType()!, GetArrayBounds(source, arg.DestinationType));
        }

        private static IEnumerable<Expression> GetArrayBounds(Expression source, Type destinationType)
        {
            var destRank = destinationType.GetArrayRank();
            if (!source.Type.IsArray)
            {
                for (int i = 0; i < destRank - 1; i++)
                {
                    yield return Expression.Constant(1);
                }
                yield return ExpressionEx.CreateCountExpression(source)!;
            }
            else
            {
                var srcRank = source.Type.GetArrayRank();
                var method = typeof(Array).GetMethod("GetLength", new[] {typeof(int)});
                for (int i = srcRank; i < destRank; i++)
                {
                    yield return Expression.Constant(1);
                }
                for (int i = 0; i < srcRank; i++)
                {
                    yield return Expression.Call(source, method!, Expression.Constant(i));
                }
            }
        }

        protected override Expression CreateBlockExpression(Expression source, Expression destination, CompileArgument arg)
        {
            if (source.Type.IsArray &&
                source.Type.GetArrayRank() == destination.Type.GetArrayRank() &&
                source.Type.GetElementType() == destination.Type.GetElementType() &&
                source.Type.GetElementType()!.IsPrimitiveKind())
            {
                //Array.Copy(src, 0, dest, 0, src.Length)
                var method = typeof(Array).GetMethod("Copy", new[] { typeof(Array), typeof(int), typeof(Array), typeof(int), typeof(int) });
                return Expression.Call(method!, source, Expression.Constant(0), destination, Expression.Constant(0), ExpressionEx.CreateCountExpression(source)!);
            }

            return CreateArraySet(source, destination, arg);
        }

        protected override Expression CreateInlineExpression(Expression source, CompileArgument arg)
        {
            throw new NotImplementedException();
        }

        private Expression CreateArraySet(Expression source, Expression destination, CompileArgument arg)
        {
            //var v0 = 0, v1 = 0;
            //var vlen0 = dest.GetLength(0), vlen1 = dest.GetLength(1);
            //for (var i = 0, len = src.Count; i < len; i++) {
            //  var item = src[i];
            //  dest[v0, v1] = convert(item);
            //  v1++;
            //  if (v1 >= vlen1) {
            //      v1 = 0;
            //      v0++;
            //  }
            //}

            var sourceElementType = source.Type.ExtractCollectionType();
            var destinationElementType = destination.Type.ExtractCollectionType();
            var item = Expression.Variable(sourceElementType, "item");
            var vx = Enumerable.Range(0, destination.Type.GetArrayRank())
                .Select(i => Expression.Variable(typeof(int), "v" + i))
                .ToList();
            var vlenx = Enumerable.Range(0, destination.Type.GetArrayRank())
                .Select(i => Expression.Variable(typeof(int), "vlen" + i))
                .ToList();
            var block = new List<Expression>();
            block.AddRange(vx.Select(v => Expression.Assign(v, Expression.Constant(0))));

            var method = typeof(Array).GetMethod("GetLength", new[] { typeof(int) });
            block.AddRange(
                vlenx.Select((vlen, i) =>
                    Expression.Assign(
                        vlen,
                        Expression.Call(destination, method!, Expression.Constant(i)))));
            var getter = CreateAdaptExpression(item, destinationElementType, arg);
            var set = ExpressionEx.Assign(
                Expression.ArrayAccess(destination, vx),
                getter);

            Expression ifExpr = Expression.Block(
                Expression.Assign(vx[1], Expression.Constant(0)),
                Expression.PostIncrementAssign(vx[0]));
            for (var i = 1; i < vx.Count; i++)
            {
                var list = new List<Expression>();
                if (i + 1 < vx.Count)
                    list.Add(Expression.Assign(vx[i + 1], Expression.Constant(0)));
                list.Add(Expression.PostIncrementAssign(vx[i]));
                list.Add(Expression.IfThen(
                    Expression.GreaterThanOrEqual(vx[i], vlenx[i]),
                    ifExpr));
                ifExpr = Expression.Block(list);
            }

            var loop = ExpressionEx.ForLoop(source, item, set, ifExpr);
            block.Add(loop);
            return Expression.Block(vx.Concat(vlenx), block);
        }
    }
}
