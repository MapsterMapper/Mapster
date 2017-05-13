using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Mapster.Models;
using Mapster.Utils;

namespace Mapster
{
    public static class ValueAccessingStrategy
    {
        public static readonly Func<Expression, IMemberModel, CompileArgument, Expression> CustomResolver = CustomResolverFn;
        public static readonly Func<Expression, IMemberModel, CompileArgument, Expression> PropertyOrField = PropertyOrFieldFn;
        public static readonly Func<Expression, IMemberModel, CompileArgument, Expression> GetMethod = GetMethodFn;
        public static readonly Func<Expression, IMemberModel, CompileArgument, Expression> FlattenMember = FlattenMemberFn;
        public static readonly Func<Expression, IMemberModel, CompileArgument, Expression> Dictionary = DictionaryFn;

        internal static List<Func<Expression, IMemberModel, CompileArgument, Expression>> GetDefaultStrategies()
        {
            return new List<Func<Expression, IMemberModel, CompileArgument, Expression>>
            {
                CustomResolver,
                PropertyOrField,
                GetMethod,
                FlattenMember,
            };
        }

        private static Expression CustomResolverFn(Expression source, IMemberModel destinationMember, CompileArgument arg)
        {
            var config = arg.Settings;
            var resolvers = config.Resolvers;
            if (resolvers == null || resolvers.Count <= 0)
                return null;

            Expression getter = null;
            LambdaExpression lastCondition = null;
            for (int j = 0; j < resolvers.Count; j++)
            {
                var resolver = resolvers[j];
                if (destinationMember.Name.Equals(resolver.DestinationMemberName))
                {
                    Expression invoke = resolver.Invoker == null
                        ? Expression.PropertyOrField(source, resolver.SourceMemberName)
                        : resolver.Invoker.Apply(source);
                    getter = lastCondition != null
                        ? Expression.Condition(lastCondition.Apply(source), getter, invoke)
                        : invoke;
                    lastCondition = resolver.Condition;
                    if (resolver.Condition == null)
                        break;
                }
            }
            if (lastCondition != null)
                getter = Expression.Condition(lastCondition.Apply(source), getter, Expression.Constant(getter.Type.GetDefault(), getter.Type));
            return getter;
        }

        private static Expression PropertyOrFieldFn(Expression source, IMemberModel destinationMember, CompileArgument arg)
        {
            var members = source.Type.GetFieldsAndProperties(accessorFlags: BindingFlags.NonPublic | BindingFlags.Public);
            var strategy = arg.Settings.NameMatchingStrategy;
            var destinationMemberName = destinationMember.GetMemberName(arg.Settings.GetMemberName, strategy.DestinationMemberNameConverter);
            return members
                .Where(member => member.ShouldMapMember(arg.Settings.ShouldMapMember))
                .Where(member => member.GetMemberName(arg.Settings.GetMemberName, strategy.SourceMemberNameConverter) == destinationMemberName)
                .Select(member => member.GetExpression(source))
                .FirstOrDefault();
        }

        private static Expression GetMethodFn(Expression source, IMemberModel destinationMember, CompileArgument arg)
        {
            if (arg.MapType == MapType.Projection)
                return null;
            var strategy = arg.Settings.NameMatchingStrategy;
            var destinationMemberName = "Get" + destinationMember.GetMemberName(arg.Settings.GetMemberName, strategy.DestinationMemberNameConverter);
            var getMethod = source.Type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => strategy.SourceMemberNameConverter(m.Name) == destinationMemberName && m.GetParameters().Length == 0);
            if (getMethod == null)
                return null;
            if (getMethod.Name == "GetType" && destinationMember.Type != typeof (Type))
                return null;
            return Expression.Call(source, getMethod);
        }

        private static Expression FlattenMemberFn(Expression source, IMemberModel destinationMember, CompileArgument arg)
        {
            var strategy = arg.Settings.NameMatchingStrategy;
            var destinationMemberName = destinationMember.GetMemberName(arg.Settings.GetMemberName, strategy.DestinationMemberNameConverter);
            return ReflectionUtils.GetDeepFlattening(source, destinationMemberName, arg);
        }

        private static Expression DictionaryFn(Expression source, IMemberModel destinationMember, CompileArgument arg)
        {
            var dictType = source.Type.GetDictionaryType();
            if (dictType == null)
                return null;

            var strategy = arg.Settings.NameMatchingStrategy;
            var destinationMemberName = destinationMember.GetMemberName(arg.Settings.GetMemberName, strategy.DestinationMemberNameConverter);
            var key = Expression.Constant(destinationMemberName);
            var args = dictType.GetGenericArguments();
            if (strategy.SourceMemberNameConverter != NameMatchingStrategy.Identity)
            {
                var method = typeof (Extensions).GetMethods().First(m => m.Name == "FlexibleGet").MakeGenericMethod(args[1]);
                return Expression.Call(method, source.To(dictType), key, Expression.Constant(strategy.SourceMemberNameConverter));
            }
            else
            {
                var method = typeof(Extensions).GetMethods().First(m => m.Name == "GetValueOrDefault").MakeGenericMethod(args);
                return Expression.Call(method, source.To(dictType), key);
            }
        }
    }
}
