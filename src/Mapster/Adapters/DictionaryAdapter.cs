using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Mapster.Models;
using Mapster.Utils;

namespace Mapster.Adapters
{
    internal class DictionaryAdapter : ClassAdapter
    {
        public static int DefaultScore { get; } = -124;

        protected override int Score => DefaultScore;   //must do before CollectionAdapter

        protected override bool CanMap(PreCompileArgument arg)
        {
            var dictType = arg.DestinationType.GetDictionaryType();
            return dictType?.GetGenericArguments()[0] == typeof(string);
        }

        protected override bool CanInline(Expression source, Expression destination, CompileArgument arg)
        {
            if (!base.CanInline(source, destination, arg))
                return false;

            //allow inline for dict-to-dict, only when IgnoreNonMapped
            return arg.SourceType.GetDictionaryType() == null 
                || arg.Settings.IgnoreNonMapped == true;
        }

        protected override Expression CreateBlockExpression(Expression source, Expression destination, CompileArgument arg)
        {
            var mapped = base.CreateBlockExpression(source, destination, arg);

            //if source is not dict type, use ClassAdapter
            var srcDictType = arg.SourceType.GetDictionaryType();
            if (srcDictType == null || arg.Settings.IgnoreNonMapped == true)
                return mapped;

            var keyType = srcDictType.GetGenericArguments().First();
            var kvpType = source.Type.ExtractCollectionType();
            var kvp = Expression.Variable(kvpType, "kvp");
            var key = Expression.Variable(keyType, "key");
            var keyAssign = Expression.Assign(key, Expression.Property(kvp, "Key"));

            //dest[kvp.Key] = convert(kvp.Value);
            var set = CreateSetFromKvp(kvp, key, destination, arg);
            if (arg.Settings.NameMatchingStrategy.SourceMemberNameConverter != NameMatchingStrategy.Identity)
            {
                set = Expression.Block(
                    Expression.Assign(
                        key,
                        Expression.Call(
                              Expression.Constant(arg.Settings.NameMatchingStrategy.SourceMemberNameConverter),
                              "Invoke",
                              null,
                              key)),
                    set);
            }

            //ignore mapped
            var ignores = arg.Settings.Resolvers
                .Select(r => r.SourceMemberName)
                .Where(name => name != null)
                .ToHashSet();

            //ignoreIf
            var dict = new Dictionary<string, Expression>();
            foreach (var ignoreIf in arg.Settings.IgnoreIfs)
            {
                if (ignoreIf.Value == null)
                    ignores.Add(ignoreIf.Key);
                else 
                {
                    var setWithCondition = Expression.IfThen(
                        ExpressionEx.Not(ignoreIf.Value.Apply(source, destination)),
                        set);
                    dict.Add(ignoreIf.Key, setWithCondition);
                }
            }
            
            //dict to switch
            if (dict.Count > 0 || ignores.Count > 0)
            {
                var cases = dict
                    .Select(k => Expression.SwitchCase(k.Value, Expression.Constant(k.Key)))
                    .ToList();
                if (ignores.Count > 0)
                    cases.Add(Expression.SwitchCase(Expression.Empty(), ignores.Select(Expression.Constant)));

                set = Expression.Switch(typeof(void), key, set, null, cases);
            }

            //if (kvp.Value != null)
            //  dest[kvp.Key] = convert(kvp.Value);
            var kvpValueType = kvpType.GetGenericArguments()[1];
            if (arg.Settings.IgnoreNullValues == true && kvpValueType.CanBeNull())
                set = Expression.IfThen(
                    Expression.NotEqual(
                        Expression.Property(kvp, "Value"),
                        Expression.Constant(null, kvpValueType)),
                    set);

            //foreach (var kvp in source) {
            //  dest[kvp.Key] = convert(kvp.Value);
            //}
            set = Expression.Block(new[] { key }, keyAssign, set);
            var loop = ExpressionEx.ForEach(source, kvp, set);
            return mapped.NodeType == ExpressionType.Default
                ? loop
                : Expression.Block(mapped, loop);
        }

        private Expression CreateSetFromKvp(Expression kvp, Expression key, Expression destination, CompileArgument arg)
        {
            var kvpValue = Expression.Property(kvp, "Value");

            var destDictType = arg.DestinationType.GetDictionaryType();
            var destValueType = destDictType.GetGenericArguments()[1];
            var destGetFn = GetFunction(arg, destDictType);
            var destSetFn = SetFunction(arg, destDictType);

            var value = arg.MapType == MapType.MapToTarget
                ? CreateAdaptToExpression(kvpValue, destGetFn(destination, key), arg)
                : CreateAdaptExpression(kvpValue, destValueType, arg);

            return destSetFn(destination, key, value);
        }

        protected override Expression CreateInlineExpression(Expression source, CompileArgument arg)
        {
            //new TDestination {
            //  { "Prop1", convert(src.Prop1) },
            //  { "Prop2", convert(src.Prop2) },
            //}

            var exp = CreateInstantiationExpression(source, arg);
            var listInit = exp as ListInitExpression;
            var newInstance = listInit?.NewExpression ?? (NewExpression)exp;

            var classModel = GetClassModel(arg);
            var classConverter = CreateClassConverter(source, classModel, arg);
            var members = classConverter.Members;

            var dictType = arg.DestinationType.GetDictionaryType();
            var keyType = dictType.GetGenericArguments()[0];
            var valueType = dictType.GetGenericArguments()[1];
            var add = dictType.GetMethod("Add", new[] { keyType, valueType });

            var lines = new List<ElementInit>();
            if (listInit != null)
                lines.AddRange(listInit.Initializers);
            foreach (var member in members)
            {
                var value = CreateAdaptExpression(member.Getter, member.DestinationMember.Type, arg);

                Expression key = Expression.Constant(member.DestinationMember.Name);
                var itemInit = Expression.ElementInit(add, key, value);
                lines.Add(itemInit);
            }

            return Expression.ListInit(newInstance, lines);
        }

        private ClassModel GetClassModel(CompileArgument arg)
        {
            //get member name from map
            var destNames = arg.GetDestinationNames().AsEnumerable();

            //get member name from properties
            if (arg.SourceType.GetDictionaryType() == null)
            {
                var propNames = arg.SourceType.GetFieldsAndProperties(accessorFlags: BindingFlags.NonPublic | BindingFlags.Public)
                    .Where(model => model.ShouldMapMember(arg, MemberSide.Source))
                    .Select(model => arg.Settings.NameMatchingStrategy.SourceMemberNameConverter(model.Name));
                destNames = destNames.Union(propNames);
            }

            //create model
            var dictType = arg.DestinationType.GetDictionaryType();
            var valueType = dictType.GetGenericArguments()[1];

            var getFn = GetFunction(arg, dictType);
            var setFn = SetFunction(arg, dictType);

            var sourceModels = destNames
                .Select(name => new KeyValuePairModel(name, valueType, getFn, setFn))
                .ToList();

            return new ClassModel
            {
                Members = sourceModels
            };
        }

        private static Func<Expression, Expression, Expression> GetFunction(CompileArgument arg, Type dictType)
        {
            var strategy = arg.Settings.NameMatchingStrategy;
            if (strategy.DestinationMemberNameConverter != NameMatchingStrategy.Identity)
            {
                var args = dictType.GetGenericArguments();
                var getMethod = typeof(MapsterHelper).GetMethods()
                    .First(m => m.Name == nameof(MapsterHelper.FlexibleGet))
                    .MakeGenericMethod(args[1]);
                var destNameConverter = Expression.Constant(strategy.DestinationMemberNameConverter);
                return (dict, key) => Expression.Call(getMethod, dict, key, destNameConverter);
            }
            else
            {
                var args = dictType.GetGenericArguments();
                var getMethod = typeof(MapsterHelper).GetMethods()
                    .First(m => m.Name == nameof(MapsterHelper.GetValueOrDefault))
                    .MakeGenericMethod(args);
                return (dict, key) => Expression.Call(getMethod, dict, key);
            }
        }

        private static Func<Expression, Expression, Expression, Expression> SetFunction(CompileArgument arg, Type dictType)
        {
            var strategy = arg.Settings.NameMatchingStrategy;
            if (arg.MapType == MapType.MapToTarget &&
                strategy.DestinationMemberNameConverter != NameMatchingStrategy.Identity)
            {
                var args = dictType.GetGenericArguments();
                var setMethod = typeof(MapsterHelper).GetMethods()
                    .First(m => m.Name == nameof(MapsterHelper.FlexibleSet))
                    .MakeGenericMethod(args[1]);
                var destNameConverter = Expression.Constant(strategy.DestinationMemberNameConverter);
                return (dict, key, value) => Expression.Call(setMethod, dict, key, destNameConverter, value);
            }
            else
            {
                var indexer = dictType.GetProperties().First(item => item.GetIndexParameters().Length > 0);
                return (dict, key, value) => Expression.Assign(Expression.Property(dict, indexer, key), value);
            }
        }
    }
}
