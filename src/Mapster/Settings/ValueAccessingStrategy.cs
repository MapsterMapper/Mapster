using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Mapster.Models;
using Mapster.Utils;
using ValueAccess = System.Func<System.Linq.Expressions.Expression, Mapster.Models.IMemberModel, Mapster.CompileArgument, System.Linq.Expressions.Expression?>;

namespace Mapster
{
    public static class ValueAccessingStrategy
    {
        public static readonly ValueAccess CustomResolver = CustomResolverFn;
        public static readonly ValueAccess PropertyOrField = PropertyOrFieldFn;
        public static readonly ValueAccess GetMethod = GetMethodFn;
        public static readonly ValueAccess FlattenMember = FlattenMemberFn;
        public static readonly ValueAccess Dictionary = DictionaryFn;
        public static readonly ValueAccess CustomResolverForDictionary = CustomResolverForDictionaryFn;

        public static readonly HashSet<ValueAccess> CustomResolvers = new HashSet<ValueAccess>
        {
            CustomResolver,
            CustomResolverForDictionary,
        };

        private static Expression? CustomResolverFn(Expression source, IMemberModel destinationMember, CompileArgument arg)
        {
            var config = arg.Settings;
            var resolvers = config.Resolvers;
            if (resolvers.Count == 0)
                return null;

            var invokes = new List<Tuple<Expression, Expression>>();

            Expression? getter = null;
            foreach (var resolver in resolvers)
            {
                if (!destinationMember.Name.Equals(resolver.DestinationMemberName))
                    continue;

                var invoke = resolver.GetInvokingExpression(source, arg.MapType);
                var condition = resolver.GetConditionExpression(source, arg.MapType);
                if (condition == null)
                {
                    getter = invoke;
                    break;
                }

                invokes.Add(Tuple.Create(condition, invoke));
            }

            if (invokes.Count > 0)
            {
                invokes.Reverse();
                if (getter == null)
                {
                    var type = invokes[0].Item2.Type;
                    if (destinationMember.Type.CanBeNull() && !type.CanBeNull())
                        type = typeof(Nullable<>).MakeGenericType(type);
                    getter = type.CreateDefault();
                }
                foreach (var invoke in invokes)
                {
                    getter = Expression.Condition(invoke.Item1, invoke.Item2.To(getter.Type), getter);
                }
            }

            return getter;
        }

        private static Expression? PropertyOrFieldFn(Expression source, IMemberModel destinationMember, CompileArgument arg)
        {
            var members = source.Type.GetFieldsAndProperties(true);
            var strategy = arg.Settings.NameMatchingStrategy;
            var destinationMemberName = destinationMember.GetMemberName(MemberSide.Destination, arg.Settings.GetMemberNames, strategy.DestinationMemberNameConverter);
            return members
                .Where(member => member.ShouldMapMember(arg, MemberSide.Source))
                .Where(member => member.GetMemberName(MemberSide.Source, arg.Settings.GetMemberNames, strategy.SourceMemberNameConverter) == destinationMemberName)
                .Select(member => member.GetExpression(source))
                .FirstOrDefault();
        }

        private static Expression? GetMethodFn(Expression source, IMemberModel destinationMember, CompileArgument arg)
        {
            if (arg.MapType == MapType.Projection)
                return null;
            var strategy = arg.Settings.NameMatchingStrategy;
            var destinationMemberName = "Get" + destinationMember.GetMemberName(MemberSide.Destination, arg.Settings.GetMemberNames, strategy.DestinationMemberNameConverter);
            var getMethod = Array.Find(source.Type.GetMethods(BindingFlags.Public | BindingFlags.Instance), m => strategy.SourceMemberNameConverter(m.Name) == destinationMemberName && m.GetParameters().Length == 0);
            if (getMethod == null)
                return null;
            if (getMethod.Name == "GetType" && destinationMember.Type != typeof(Type))
                return null;
            return Expression.Call(source, getMethod);
        }

        private static Expression? FlattenMemberFn(Expression source, IMemberModel destinationMember, CompileArgument arg)
        {
            var strategy = arg.Settings.NameMatchingStrategy;
            var destinationMemberName = destinationMember.GetMemberName(MemberSide.Destination, arg.Settings.GetMemberNames, strategy.DestinationMemberNameConverter);
            return GetDeepFlattening(source, destinationMemberName, arg);
        }

        private static Expression? GetDeepFlattening(Expression source, string propertyName, CompileArgument arg)
        {
            var strategy = arg.Settings.NameMatchingStrategy;
            var members = source.Type.GetFieldsAndProperties(true);
            foreach (var member in members)
            {
                if (!member.ShouldMapMember(arg, MemberSide.Source))
                    continue;

                var sourceMemberName = member.GetMemberName(MemberSide.Source, arg.Settings.GetMemberNames, strategy.SourceMemberNameConverter);
                if (string.Equals(propertyName, sourceMemberName))
                    return member.GetExpression(source);

                var propertyType = member.Type;
                if (propertyName.StartsWith(sourceMemberName) &&
                    (propertyType.IsPoco() || propertyType.IsRecordType()))
                {
                    var exp = member.GetExpression(source);
                    var ifTrue = GetDeepFlattening(exp, propertyName.Substring(sourceMemberName.Length).TrimStart('_'), arg);
                    if (ifTrue == null)
                        continue;
                    return ifTrue;
                }
            }
            return null;
        }

        internal static IEnumerable<InvokerModel> FindUnflatteningPairs(Expression source, IMemberModel destinationMember, CompileArgument arg)
        {
            var strategy = arg.Settings.NameMatchingStrategy;
            var destinationMemberName = destinationMember.GetMemberName(MemberSide.Destination, arg.Settings.GetMemberNames, strategy.DestinationMemberNameConverter);
            var members = source.Type.GetFieldsAndProperties(true);

            foreach (var member in members)
            {
                if (!member.ShouldMapMember(arg, MemberSide.Source))
                    continue;
                var sourceMemberName = member.GetMemberName(MemberSide.Source, arg.Settings.GetMemberNames, strategy.SourceMemberNameConverter);
                if (!sourceMemberName.StartsWith(destinationMemberName) || sourceMemberName == destinationMemberName)
                    continue;
                foreach (var prop in GetDeepUnflattening(destinationMember, sourceMemberName.Substring(destinationMemberName.Length).TrimStart('_'), arg))
                {
                    yield return new InvokerModel
                    {
                        SourceMemberName = member.Name,
                        DestinationMemberName = destinationMember.Name + "." + prop,
                    };
                }
            }
        }

        private static IEnumerable<string> GetDeepUnflattening(IMemberModel destinationMember, string propertyName, CompileArgument arg)
        {
            var strategy = arg.Settings.NameMatchingStrategy;
            var members = destinationMember.Type.GetFieldsAndProperties(true);
            foreach (var member in members)
            {
                if (!member.ShouldMapMember(arg, MemberSide.Destination))
                    continue;
                var destMemberName = member.GetMemberName(MemberSide.Destination, arg.Settings.GetMemberNames, strategy.DestinationMemberNameConverter);
                var propertyType = member.Type;
                if (string.Equals(propertyName, destMemberName))
                {
                    yield return member.Name;
                }
                else if (propertyName.StartsWith(destMemberName) &&
                    (propertyType.IsPoco() || propertyType.IsRecordType()))
                {
                    foreach (var prop in GetDeepUnflattening(member, propertyName.Substring(destMemberName.Length).TrimStart('_'), arg))
                    {
                        yield return member.Name + "." + prop;
                    }
                }
            }
        }

        private static Expression? DictionaryFn(Expression source, IMemberModel destinationMember, CompileArgument arg)
        {
            var dictType = source.Type.GetDictionaryType();
            if (dictType == null)
                return null;

            var strategy = arg.Settings.NameMatchingStrategy;
            var destinationMemberName = destinationMember.GetMemberName(MemberSide.Destination, arg.Settings.GetMemberNames, strategy.DestinationMemberNameConverter);
            var key = Expression.Constant(destinationMemberName);
            var args = dictType.GetGenericArguments();
            if (strategy.SourceMemberNameConverter != MapsterHelper.Identity)
            {
                var method = typeof(MapsterHelper).GetMethods()
                    .First(m => m.Name == nameof(MapsterHelper.FlexibleGet) && m.GetParameters()[0].ParameterType.Name == dictType.Name)
                    .MakeGenericMethod(args[1]);
                return Expression.Call(method, source.To(dictType), key, ExpressionEx.GetNameConverterExpression(strategy.SourceMemberNameConverter));
            }
            else
            {
                var method = typeof(MapsterHelper).GetMethods()
                    .First(m => m.Name == nameof(MapsterHelper.GetValueOrDefault) && m.GetParameters()[0].ParameterType.Name == dictType.Name)
                    .MakeGenericMethod(args);
                return Expression.Call(method, source.To(dictType), key);
            }
        }

        private static Expression? CustomResolverForDictionaryFn(Expression source, IMemberModel destinationMember, CompileArgument arg)
        {
            var config = arg.Settings;
            var resolvers = config.Resolvers;
            if (resolvers.Count == 0)
                return null;
            var dictType = source.Type.GetDictionaryType();
            if (dictType == null)
                return null;
            var args = dictType.GetGenericArguments();
            var method = typeof(MapsterHelper).GetMethods()
                .First(m => m.Name == nameof(MapsterHelper.GetValueOrDefault) && m.GetParameters()[0].ParameterType.Name == dictType.Name)
                .MakeGenericMethod(args);

            Expression? getter = null;
            Expression? lastCondition = null;
            foreach (var resolver in resolvers)
            {
                if (!destinationMember.Name.Equals(resolver.DestinationMemberName))
                    continue;

                Expression invoke = resolver.Invoker == null
                    ? Expression.Call(method, source.To(dictType), Expression.Constant(resolver.SourceMemberName))
                    : resolver.GetInvokingExpression(source, arg.MapType);
                getter = lastCondition != null
                    ? Expression.Condition(lastCondition, getter!, invoke)
                    : invoke;
                lastCondition = resolver.GetConditionExpression(source, arg.MapType);
                if (lastCondition == null)
                    break;
            }
            if (lastCondition != null)
                getter = Expression.Condition(lastCondition, getter!, getter!.Type.CreateDefault());
            return getter;
        }
    }
}
