using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Mapster.Models;
using Mapster.Utils;
using ValueAccess = System.Func<Mapster.ResolverSourceInput, Mapster.Models.IMemberModel, Mapster.Models.MemberMapping, Mapster.CompileArgument, bool>;

namespace Mapster
{
    internal static class ValueAccessingStrategy
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

        private static bool CustomResolverFn(ResolverSourceInput srcInput, IMemberModel destinationMember, MemberMapping memberMapping, CompileArgument arg)
        {
            var source = srcInput.Src;
            var config = source.Type == arg.SourceType ? arg.Settings : arg.Context.Config.GetMergedSettings(new TypeTuple(source.Type, arg.DestinationType),arg.MapType);
            var resolvers = srcInput.Settings != null ? srcInput.Settings.ApplyResolversOnly(config) : config.Resolvers;
            if (resolvers.Count == 0)
                return false;
            TypeAdapterSettings? customSettings = null;
            var MatchingGetter = 0;

            foreach (var resolver in resolvers)
            {
                if (!destinationMember.Name.Equals(resolver.DestinationMemberName, StringComparison.InvariantCultureIgnoreCase))
                    continue;

                if(resolver.OvverideSettings != null && customSettings == null)
                    customSettings = resolver.OvverideSettings;

                var invoke = resolver.GetInvokingExpression(source, arg.MapType, customSettings != null);

                if (invoke == null)
                   continue;

                var condition = resolver.GetConditionExpression(source, arg.MapType);

                memberMapping.GetterLines.Add(new(srcInput.Src,invoke, customSettings, true) { Condition = condition});
                MatchingGetter++;
            }

            if(MatchingGetter > 0)
                return true;
            else
                return false;
        }

        private static bool PropertyOrFieldFn(ResolverSourceInput srcInput, IMemberModel destinationMember, MemberMapping memberMapping, CompileArgument arg)
        {
            var source = srcInput.Src;
            var members = source.Type.GetFieldsAndProperties(arg, true, arg.Context.AttributeMetadata);
            var strategy = arg.Settings.NameMatchingStrategy;
            var destinationMemberName = destinationMember.GetMemberName(MemberSide.Destination, arg.Settings.GetMemberNames, strategy.DestinationMemberNameConverter, arg);
            var resolver = members
                .Where(member => member.ShouldMapMember(arg, MemberSide.Source))
                .Where(member => member.GetMemberName(MemberSide.Source, arg.Settings.GetMemberNames, strategy.SourceMemberNameConverter, arg) == destinationMemberName)
                .Select(member => member.GetExpression(source))
                .FirstOrDefault();


            if (resolver == null)
                return false;
            else
                memberMapping.GetterLines.Add(new(srcInput.Src, resolver, srcInput.Settings, srcInput.IsRemapMembers));

            return true;

        }

        private static bool GetMethodFn(ResolverSourceInput srcInput, IMemberModel destinationMember, MemberMapping memberMapping, CompileArgument arg)
        {
            var source = srcInput.Src;
            if (arg.MapType == MapType.Projection)
                return false;
            var strategy = arg.Settings.NameMatchingStrategy;
            var destinationMemberName = "Get" + destinationMember.GetMemberName(MemberSide.Destination, arg.Settings.GetMemberNames, strategy.DestinationMemberNameConverter, arg);
            var getMethod = Array.Find(source.Type.GetMethods(BindingFlags.Public | BindingFlags.Instance), m => strategy.SourceMemberNameConverter(m.Name) == destinationMemberName && m.GetParameters().Length == 0);
            if (getMethod == null)
                return false;
            if (getMethod.Name == "GetType" && destinationMember.Type != typeof(Type))
                return false;

            memberMapping.GetterLines.Add(new(srcInput.Src, Expression.Call(source, getMethod), srcInput.Settings, srcInput.IsRemapMembers));

            return true;
        }

        private static bool FlattenMemberFn(ResolverSourceInput srcInput, IMemberModel destinationMember, MemberMapping memberMapping, CompileArgument arg)
        {
            var source = srcInput.Src;
            var strategy = arg.Settings.NameMatchingStrategy;
            var destinationMemberName = destinationMember.GetMemberName(MemberSide.Destination, arg.Settings.GetMemberNames, strategy.DestinationMemberNameConverter, arg);
            var resolver = GetDeepFlattening(source, destinationMemberName, arg);
            if(resolver == null)
                return false;

            memberMapping.GetterLines.Add(new(srcInput.Src, resolver, srcInput.Settings, srcInput.IsRemapMembers));

            return true;
        }

        private static Expression? GetDeepFlattening(Expression source, string propertyName, CompileArgument arg)
        {
            var strategy = arg.Settings.NameMatchingStrategy;
            var members = source.Type.GetFieldsAndProperties(arg, true);
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
            var members = source.Type.GetFieldsAndProperties(arg, true);

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
            var members = destinationMember.Type.GetFieldsAndProperties(arg, true);
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
                    (propertyType.IsPoco(propertyType.GetFieldsAndProperties(arg)) || propertyType.IsRecordType()))
                {
                    foreach (var prop in GetDeepUnflattening(member, propertyName.Substring(destMemberName.Length).TrimStart('_'), arg))
                    {
                        yield return member.Name + "." + prop;
                    }
                }
            }
        }

        private static bool DictionaryFn(ResolverSourceInput srcInput, IMemberModel destinationMember, MemberMapping memberMapping, CompileArgument arg)
        {
            var source = srcInput.Src;
            var dictType = source.Type.GetDictionaryType();
            if (dictType == null)
                return false;

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

                memberMapping.GetterLines.Add(new(srcInput.Src, resolver, srcInput.Settings, srcInput.IsRemapMembers));
                return true;
            }
            else
            {
                var method = typeof(MapsterHelper).GetMethods()
                    .First(m => m.Name == nameof(MapsterHelper.GetValueOrDefault) && m.GetParameters()[0].ParameterType.Name == dictType.Name)
                    .MakeGenericMethod(args);
                var resolver =  Expression.Call(method, source.To(dictType), key);

                memberMapping.GetterLines.Add(new(srcInput.Src, resolver, null, srcInput.IsRemapMembers));
                return true;
            }
        }

        private static bool CustomResolverForDictionaryFn(ResolverSourceInput srcInput, IMemberModel destinationMember, MemberMapping memberMapping, CompileArgument arg)
        {
            var source = srcInput.Src;
            var config = arg.Settings;
            var resolvers = config.Resolvers;
            if (resolvers.Count == 0)
                return false;
            var dictType = source.Type.GetDictionaryType();
            if (dictType == null)
                return false;
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

            if (getter == null)
                return false;

            memberMapping.GetterLines.Add(new(srcInput.Src, getter, null, true));

            return true;
        }
    }

    public record ResolverResult(Expression Exp , OverrideTypesSettings? Settings = null);
    public record ResolverSourceInput
    {
        private ResolverSourceInput(Expression src)
        {
            Src = src;
        }

        public ResolverSourceInput(string src, OverrideTypesSettings? settings = null, bool isRemapMembers = false)
        {
            _stringSource = src;
            Settings = settings;
            IsRemapMembers = isRemapMembers;
        }

        public ResolverSourceInput(LambdaExpression resolver, OverrideTypesSettings? settings = null, bool isRemapMembers = false)
        {
            _resolver = resolver;
            Settings = settings;
            IsRemapMembers = isRemapMembers;
        }

        private readonly string? _stringSource;
        private readonly LambdaExpression? _resolver;

        public Expression Src { get; private set; }
        public OverrideTypesSettings? Settings { get; init; }
        public bool IsRemapMembers { get; init; }

        public static explicit operator ResolverSourceInput(string src) => new ResolverSourceInput(src);
        public static explicit operator ResolverSourceInput(Expression src) => new ResolverSourceInput(src);
        public static explicit operator ResolverSourceInput(ParameterExpression src) => new ResolverSourceInput(src);
        //public static ResolverSourceInput ConvertFrom(ExtraSourceModel extraSource,Expression source, CompileArgument arg)
        //{
        //    if (extraSource.Src is LambdaExpression lambda)
        //        return new ResolverSourceInput(lambda.ApplyExtraSources(arg.MapType, source), extraSource.Settings, extraSource.IsRemapMembers);
        //    else
        //        return new ResolverSourceInput(ExpressionEx.PropertyOrFieldPath(source, (string)extraSource.Src), extraSource.Settings, extraSource.IsRemapMembers);
        //}

        public ResolverSourceInput Activate (Expression source, CompileArgument arg)
        {
            if (_resolver != null)
                Src = _resolver.ApplyExtraSources(arg.MapType, source);
            if(_stringSource != null)
                Src = ExpressionEx.PropertyOrFieldPath(source, _stringSource);

            return this;
        }
    };
}
