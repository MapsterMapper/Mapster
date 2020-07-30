using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Mapster.Adapters;

namespace Mapster.Immutable
{
    public class ImmutableAdapter : BaseAdapter
    {
        private static readonly Dictionary<Type, Type> _immutableTypes = new Dictionary<Type, Type>
        {
            [typeof(IImmutableDictionary<,>)] = typeof(ImmutableDictionary),
            [typeof(IImmutableList<>)] = typeof(ImmutableList),
            [typeof(IImmutableQueue<>)] = typeof(ImmutableQueue),
            [typeof(IImmutableSet<>)] = typeof(ImmutableHashSet),
            [typeof(IImmutableStack<>)] = typeof(ImmutableStack),
            [typeof(ImmutableArray<>)] = typeof(ImmutableArray),
            [typeof(ImmutableDictionary<,>)] = typeof(ImmutableDictionary),
            [typeof(ImmutableHashSet<>)] = typeof(ImmutableHashSet),
            [typeof(ImmutableList<>)] = typeof(ImmutableList),
            [typeof(ImmutableQueue<>)] = typeof(ImmutableQueue),
            [typeof(ImmutableSortedDictionary<,>)] = typeof(ImmutableSortedDictionary),
            [typeof(ImmutableSortedSet<>)] = typeof(ImmutableSortedSet),
            [typeof(ImmutableStack<>)] = typeof(ImmutableStack),
        };

        protected override bool CanMap(PreCompileArgument arg)
        {
            if (!arg.DestinationType.GetTypeInfo().IsGenericType ||
                !_immutableTypes.ContainsKey(arg.DestinationType.GetGenericTypeDefinition()))
                return false;

            var args = arg.DestinationType.GetGenericArguments();
            if (args.Length == 2 && args[0] == typeof(string))
                return true;
            return IsCollection(arg.SourceType);
        }

        private static bool IsCollection(Type type)
        {
            return typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()) && type != typeof(string);
        }

        protected override Expression CreateInstantiationExpression(Expression source, Expression? destination, CompileArgument arg)
        {
            var args = arg.DestinationType.GetGenericArguments();
            var immediateType = args.Length == 1
                ? typeof(IEnumerable<>).MakeGenericType(args)
                : args[0] == typeof(string)
                ? typeof(Dictionary<,>).MakeGenericType(args)
                : typeof(IEnumerable<>).MakeGenericType(typeof(KeyValuePair<,>).MakeGenericType(args));

            var exp = CreateAdaptExpression(source, immediateType, arg);

            var typeDef = arg.DestinationType.GetGenericTypeDefinition();
            var builder = _immutableTypes[typeDef];
            var createRange = builder.GetMethods()
                .First(it => it.Name == "CreateRange" && it.GetParameters().Length == 1);
            return Expression.Call(createRange.MakeGenericMethod(args), exp);
            
        }
        
        protected override Expression CreateBlockExpression(Expression source, Expression destination, CompileArgument arg)
        {
            return Expression.Empty();
        }

        protected override Expression CreateInlineExpression(Expression source, CompileArgument arg)
        {
            return CreateInstantiationExpression(source, arg);
        }
    }
}
