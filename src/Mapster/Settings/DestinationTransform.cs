using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Mapster
{
    public class DestinationTransform
    {
        public static readonly DestinationTransform EmptyCollectionIfNull = new DestinationTransform
        {
            Condition = type => type.IsArray
                || type.IsCollectionCompatible()
                || type.GetDictionaryType() != null,
            TransformFunc = type =>
            {
                Expression newExp;
                if (type.IsArray)
                {
                    var rank = type.GetArrayRank();
                    var elemType = type.GetElementType()!;
                    newExp = Expression.NewArrayBounds(elemType, Enumerable.Repeat(Expression.Constant(0), rank));
                }
                else if (type.GetTypeInfo().IsInterface)
                {
                    if (type.GetDictionaryType() != null)
                    {
                        var dict = type.GetDictionaryType()!;
                        var dictArgs = dict.GetGenericArguments();
                        var dictType = typeof(Dictionary<,>).MakeGenericType(dictArgs);
                        newExp = Expression.New(dictType);
                    }
                    else if (type.IsAssignableFromList())
                    {
                        var elemType = type.ExtractCollectionType();
                        var listType = typeof(List<>).MakeGenericType(elemType);
                        newExp = Expression.New(listType);
                    }
                    else //if (type.IsAssignableFromSet())
                    {
                        var elemType = type.ExtractCollectionType();
                        var setType = typeof(HashSet<>).MakeGenericType(elemType);
                        newExp = Expression.New(setType);
                    }
                }
                else
                    newExp = Expression.New(type);

                var p = Expression.Parameter(type);
                return Expression.Lambda(Expression.Coalesce(p, newExp), p);
            }
        };

        public static readonly DestinationTransform CreateNewIfNull = new DestinationTransform
        {
            Condition = type =>
            {
                if (!type.CanBeNull())
                    return false;
                if (type.GetTypeInfo().IsInterface || type.GetTypeInfo().IsAbstract)
                    return false;
                var ci = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SingleOrDefault(c => c.GetParameters().Length == 0);
                return ci != null;
            },
            TransformFunc = type =>
            {
                var p = Expression.Parameter(type);
                return Expression.Lambda(
                    Expression.Coalesce(p, Expression.New(type)), p);
            }
        };

        public Func<Type, bool> Condition { get; set; }
        public Func<Type, LambdaExpression> TransformFunc { get; set; }

        public DestinationTransform WithCondition(Func<Type, bool> condition)
        {
            return new DestinationTransform
            {
                Condition = condition,
                TransformFunc = this.TransformFunc
            };
        }
    }
}
