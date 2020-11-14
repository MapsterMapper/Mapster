using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Mapster.Utils;

namespace Mapster.Adapters
{
    internal class CollectionAdapter : BaseAdapter
    {
        protected override int Score => -125;
        protected override ObjectType ObjectType => ObjectType.Collection;

        protected override bool CanMap(PreCompileArgument arg)
        {
            if (!arg.SourceType.IsCollection() || !arg.DestinationType.IsCollection())
                return false;
            if (arg.DestinationType.IsCollectionCompatible())
                return true;

            return arg.DestinationType.GetDictionaryType() != null;
        }

        protected override bool CanInline(Expression source, Expression? destination, CompileArgument arg)
        {
            if (!base.CanInline(source, destination, arg))
                return false;

            if (arg.MapType == MapType.Projection)
            {
                if (arg.DestinationType.IsAssignableFromCollection())
                    return true;

                throw new InvalidOperationException($"{arg.DestinationType} is not supported for projection, please consider using List<>");
            }

            if (arg.DestinationType == typeof(IEnumerable) || arg.DestinationType.IsGenericEnumerableType())
                return true;

            return false;
        }

        protected override Expression CreateInstantiationExpression(Expression source, Expression? destination, CompileArgument arg)
        {
            var listType = arg.DestinationType;
            if (arg.DestinationType.GetTypeInfo().IsInterface)
            {
                var dict = arg.DestinationType.GetDictionaryType();
                if (dict != null)
                {
                    var dictArgs = dict.GetGenericArguments();
                    listType = typeof(Dictionary<,>).MakeGenericType(dictArgs);
                }
                else if (arg.DestinationType.IsAssignableFromList())
                {
                    var destinationElementType = arg.DestinationType.ExtractCollectionType();
                    listType = typeof(List<>).MakeGenericType(destinationElementType);
                }
                else // if (arg.DestinationType.IsAssignableFromSet())
                {
                    var destinationElementType = arg.DestinationType.ExtractCollectionType();
                    listType = typeof(HashSet<>).MakeGenericType(destinationElementType);
                }
            }
            var count = ExpressionEx.CreateCountExpression(source);
            if (count == null)
                return Expression.New(listType);            //new List<T>()
            var ctor = (from c in listType.GetConstructors()
                        let args = c.GetParameters()
                        where args.Length == 1 && args[0].ParameterType == typeof(int)
                        select c).FirstOrDefault();
            if (ctor == null)
                return Expression.New(listType);            //new List<T>()
            else
                return Expression.New(ctor, count);         //new List<T>(count)
        }

        protected override Expression CreateBlockExpression(Expression source, Expression destination, CompileArgument arg)
        {
            var destinationElementType = destination.Type.ExtractCollectionType();
            var shouldConvert = destination.Type.GetMethod("Add", new[] { destinationElementType }) == null;

            //var list = (ICollection<>)dest;
            var actions = new List<Expression>();
            var list = destination;
            if (shouldConvert)
            {
                var listType = destination.Type.GetGenericEnumerableType() != null
                    ? typeof(ICollection<>).MakeGenericType(destinationElementType)
                    : typeof(IList);
                list = Expression.Variable(listType, "list");
                actions.Add(ExpressionEx.Assign(list, destination)); //convert to list type
            }

            //list.Clear();
            if (arg.UseDestinationValue && arg.MapType == MapType.MapToTarget)
            {
                var clear = list.Type.GetMethod("Clear", Type.EmptyTypes);
                actions.Add(Expression.Call(list, clear!));
            }

            actions.Add(CreateListSet(source, list, arg));

            if (shouldConvert)
                return Expression.Block(new[] { (ParameterExpression)list }, actions);
            return Expression.Block(actions);
        }

        protected override Expression CreateInlineExpression(Expression source, CompileArgument arg)
        {
            if (arg.DestinationType.GetTypeInfo().IsAssignableFrom(source.Type.GetTypeInfo()) && 
                (arg.Settings.ShallowCopyForSameType == true || arg.MapType == MapType.Projection))
                return source;

            var sourceElementType = source.Type.ExtractCollectionType();
            var destinationElementType = arg.DestinationType.ExtractCollectionType();

            var p1 = Expression.Parameter(sourceElementType);
            var adapt = CreateAdaptExpression(p1, destinationElementType, arg)
                .To(destinationElementType, true);
            if (adapt == p1)
            {
                if (arg.MapType == MapType.Projection)
                    return source;

                //create new enumerable to prevent destination casting back to original type and alter directly
                var toEnum = (from m in typeof(MapsterHelper).GetMethods()
                              where m.Name == nameof(MapsterHelper.ToEnumerable)
                              select m).First().MakeGenericMethod(destinationElementType);
                return Expression.Call(toEnum, source);
            }

            //src.Cast<T>() -- for IEnumerable
            Expression exp;
            if (source.Type.GetGenericEnumerableType() != null)
                exp = source;
            else
            {
                var cast = (from m in typeof(Enumerable).GetMethods()
                            where m.Name == nameof(Enumerable.Cast)
                            select m).First().MakeGenericMethod(sourceElementType);
                exp = Expression.Call(cast, source);
            }

            //src.Select(item => convert(item))
            var method = (from m in typeof(Enumerable).GetMethods()
                          where m.Name == nameof(Enumerable.Select)
                          let p = m.GetParameters()[1]
                          where p.ParameterType.GetGenericTypeDefinition() == typeof(Func<,>)
                          select m).First().MakeGenericMethod(sourceElementType, destinationElementType);
            exp = Expression.Call(method, exp, Expression.Lambda(adapt, p1));
            if (exp.Type != arg.DestinationType)
                exp = InlineChangeType(exp, arg);
            return exp;
        }

        private static Expression InlineChangeType(Expression exp, CompileArgument arg)
        {
            var destinationElementType = arg.DestinationType.ExtractCollectionType();

            //src.Select(item => convert(item)).ToList()
            if (arg.DestinationType.IsAssignableFromList())
            {
                var toList = (from m in typeof(Enumerable).GetMethods()
                    where m.Name == nameof(Enumerable.ToList)
                    select m).First().MakeGenericMethod(destinationElementType);
                return Expression.Call(toList, exp);
            }
            else // if (arg.DestinationType.IsAssignableFromSet())
            {
                var set = typeof(HashSet<>).MakeGenericType(destinationElementType);
                var ctor = (from c in set.GetConstructors()
                    let a = c.GetParameters()
                    where a.Length == 1 &&
                          a[0].ParameterType.GetTypeInfo().IsGenericType &&
                          a[0].ParameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                    select c).First();
                return Expression.New(ctor, exp);
            }
        }

        private Expression CreateListSet(Expression source, Expression destination, CompileArgument arg)
        {
            //### IList<T>
            //for (var i = 0, len = src.Count; i < len; i++) {
            //  var item = src[i];
            //  dest.Add(convert(item));
            //}

            //### IEnumerable<T>
            //foreach (var item in src)
            //  dest.Add(convert(item));

            var sourceElementType = source.Type.ExtractCollectionType();
            var destinationElementType = destination.Type.ExtractCollectionType();
            var item = Expression.Variable(sourceElementType, "item");
            var getter = CreateAdaptExpression(item, destinationElementType, arg);

            var addMethod = destination.Type.GetMethod("Add", new[] { destinationElementType });
            var set = Expression.Call(
                destination,
                addMethod!,
                getter);
            return ExpressionEx.ForLoop(source, item, set);
        }
    }
}
