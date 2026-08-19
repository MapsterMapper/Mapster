using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Mapster.Models;
using Mapster.Utils;
using ValueAccess = System.Func<Mapster.ResolverSourceInput, Mapster.Models.IMemberModel, Mapster.CompileArgument, Mapster.ResolverResult?>;

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

        private static ResolverResult? CustomResolverFn(ResolverSourceInput srcInput, IMemberModel destinationMember, CompileArgument arg)
        {
            var source = srcInput.Src;
            var config = source.Type == arg.SourceType ? arg.Settings : arg.Context.Config.GetMergedSettings(new TypeTuple(source.Type, arg.DestinationType),arg.MapType);
            var resolvers = srcInput.Settings != null ? srcInput.Settings.ApplyResolversOnly(config) : config.Resolvers;
            if (resolvers.Count == 0)
                return null;
            TypeAdapterSettings? customSettings = null;

            var invokes = new List<Tuple<Expression, Expression>>();

            Expression? getter = null;
            foreach (var resolver in resolvers)
            {
                if (!destinationMember.Name.Equals(resolver.DestinationMemberName, StringComparison.InvariantCultureIgnoreCase))
                    continue;

                if(resolver.OvverideSettings != null && customSettings == null)
                    customSettings = resolver.OvverideSettings;

                var invoke = resolver.GetInvokingExpression(source, arg.MapType, customSettings != null);
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
                    getter = type.CreateDefault(arg);
                }
                foreach (var invoke in invokes)
                {
                    getter = Expression.Condition(invoke.Item1, invoke.Item2.To(getter.Type), getter);
                }
            }

            if (getter == null)
                return null;
            return new ResolverResult(getter,(OverrideTypesSettings?)customSettings);
        }

        private static ResolverResult? PropertyOrFieldFn(ResolverSourceInput srcInput, IMemberModel destinationMember, CompileArgument arg)
        {
            var source = srcInput.Src;
            var members = source.Type.GetFieldsAndProperties(true);
            var strategy = arg.Settings.NameMatchingStrategy;
            var destinationMemberName = destinationMember.GetMemberName(MemberSide.Destination, arg.Settings.GetMemberNames, strategy.DestinationMemberNameConverter, arg);
            var resolver = members
                .Where(member => member.ShouldMapMember(arg, MemberSide.Source))
                .Where(member => member.GetMemberName(MemberSide.Source, arg.Settings.GetMemberNames, strategy.SourceMemberNameConverter, arg) == destinationMemberName)
                .Select(member => member.GetExpression(source))
                .FirstOrDefault();

            if (resolver == null)
                return null;
            else
                return new ResolverResult(resolver, srcInput.Settings != null ? srcInput.Settings.CloneOnlySkipSettings() : null);

        }

        private static ResolverResult? GetMethodFn(ResolverSourceInput srcInput, IMemberModel destinationMember, CompileArgument arg)
        {
            var source = srcInput.Src;
            if (arg.MapType == MapType.Projection)
                return null;
            var strategy = arg.Settings.NameMatchingStrategy;
            var destinationMemberName = "Get" + destinationMember.GetMemberName(MemberSide.Destination, arg.Settings.GetMemberNames, strategy.DestinationMemberNameConverter, arg);
            var getMethod = Array.Find(source.Type.GetMethods(BindingFlags.Public | BindingFlags.Instance), m => strategy.SourceMemberNameConverter(m.Name) == destinationMemberName && m.GetParameters().Length == 0);
            if (getMethod == null)
                return null;
            if (getMethod.Name == "GetType" && destinationMember.Type != typeof(Type))
                return null;
            return new ResolverResult( Expression.Call(source, getMethod),null);
        }

        private static ResolverResult? FlattenMemberFn(ResolverSourceInput srcInput, IMemberModel destinationMember, CompileArgument arg)
        {
            var source = srcInput.Src;
            var strategy = arg.Settings.NameMatchingStrategy;
            var destinationMemberName = destinationMember.GetMemberName(MemberSide.Destination, arg.Settings.GetMemberNames, strategy.DestinationMemberNameConverter, arg);
            var resolver = GetDeepFlattening(source, destinationMemberName, arg);
            if(resolver == null)
                return null;
            return new ResolverResult(resolver, null);
        }

        private static Expression? GetDeepFlattening(Expression source, string propertyName, CompileArgument arg)
        {
            var strategy = arg.Settings.NameMatchingStrategy;
            var members = source.Type.GetFieldsAndProperties(true);
            foreach (var member in members)
            {
                if (!member.ShouldMapMember(arg, MemberSide.Source))
                    continue;

                var sourceMemberName = member.GetMemberName(MemberSide.Source, arg.Settings.GetMemberNames, strategy.SourceMemberNameConverter, arg);
                if (string.Equals(propertyName, sourceMemberName))
                    return member.GetExpression(source);

                var propertyType = member.Type;
                if (propertyName.StartsWith(sourceMemberName) && !propertyType.IsMapsterPrimitive())
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
            var destinationMemberName = destinationMember.GetMemberName(MemberSide.Destination, arg.Settings.GetMemberNames, strategy.DestinationMemberNameConverter, arg);
            var members = source.Type.GetFieldsAndProperties(true);

            foreach (var member in members)
            {
                if (!member.ShouldMapMember(arg, MemberSide.Source))
                    continue;
                var sourceMemberName = member.GetMemberName(MemberSide.Source, arg.Settings.GetMemberNames, strategy.SourceMemberNameConverter, arg);
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
                var destMemberName = member.GetMemberName(MemberSide.Destination, arg.Settings.GetMemberNames, strategy.DestinationMemberNameConverter, arg);
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

        private static ResolverResult? DictionaryFn(ResolverSourceInput srcInput, IMemberModel destinationMember, CompileArgument arg)
        {
            var source = srcInput.Src;
            var dictType = source.Type.GetDictionaryType();
            if (dictType == null)
                return null;

            var strategy = arg.Settings.NameMatchingStrategy;
            var destinationMemberName = destinationMember.GetMemberName(MemberSide.Destination, arg.Settings.GetMemberNames, strategy.DestinationMemberNameConverter, arg);
            var key = Expression.Constant(destinationMemberName);
            var args = dictType.GetGenericArguments();
            if (strategy.SourceMemberNameConverter != MapsterHelper.Identity)
            {
                var method = typeof(MapsterHelper).GetMethods()
                    .First(m => m.Name == nameof(MapsterHelper.FlexibleGet) && m.GetParameters()[0].ParameterType.Name == dictType.Name)
                    .MakeGenericMethod(args[1]);
                var resolver = Expression.Call(method, source.To(dictType), key, ExpressionEx.GetNameConverterExpression(strategy.SourceMemberNameConverter));
                return new ResolverResult(resolver);
            }
            else
            {
                var method = typeof(MapsterHelper).GetMethods()
                    .First(m => m.Name == nameof(MapsterHelper.GetValueOrDefault) && m.GetParameters()[0].ParameterType.Name == dictType.Name)
                    .MakeGenericMethod(args);
                var resolver =  Expression.Call(method, source.To(dictType), key);
                return new ResolverResult(resolver);
            }
        }

        private static ResolverResult? CustomResolverForDictionaryFn(ResolverSourceInput srcInput, IMemberModel destinationMember, CompileArgument arg)
        {
            var source = srcInput.Src;
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
                getter = Expression.Condition(lastCondition, getter!, getter!.Type.CreateDefault(arg));
            return new ResolverResult(getter);
        }
    }

    public record ResolverResult(Expression Exp , OverrideTypesSettings? Settings = null);
    public record ResolverSourceInput(Expression Src, OverrideTypesSettings? Settings = null)
    {
        public static explicit operator ResolverSourceInput(Expression src) => new ResolverSourceInput(src);
        public static explicit operator ResolverSourceInput(ParameterExpression src) => new ResolverSourceInput(src);
        public static ResolverSourceInput ConvertFrom(ExtraSourceModel extraSource,Expression source, CompileArgument arg)
        {
            if (extraSource.Src is LambdaExpression lambda)
                return new ResolverSourceInput(lambda.ApplyExtraSources(arg.MapType, source), extraSource.Settings);
            else
                return new ResolverSourceInput(ExpressionEx.PropertyOrFieldPath(source, (string)extraSource.Src), extraSource.Settings);
        }
    };
}
