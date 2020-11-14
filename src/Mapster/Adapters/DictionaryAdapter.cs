using System;
using System.Collections.Generic;
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
        protected override ObjectType ObjectType => ObjectType.Collection;

        protected override bool CanMap(PreCompileArgument arg)
        {
            var dictType = arg.DestinationType.GetDictionaryType();
            return dictType?.GetGenericArguments()[0] == typeof(string);
        }

        protected override bool CanInline(Expression source, Expression? destination, CompileArgument arg)
        {
            if (!base.CanInline(source, destination, arg))
                return false;

            //allow inline for dict-to-dict, only when IgnoreNonMapped
            return arg.SourceType.GetDictionaryType() == null
                || arg.Settings.IgnoreNonMapped == true;
        }

        protected override Expression CreateInstantiationExpression(Expression source, Expression? destination, CompileArgument arg)
        {
            if (arg.DestinationType.GetTypeInfo().IsInterface)
            {
                var dict = arg.DestinationType.GetDictionaryType()!;
                var dictArgs = dict.GetGenericArguments();
                var dictType = typeof(Dictionary<,>).MakeGenericType(dictArgs);
                return Expression.New(dictType);
            }
            return base.CreateInstantiationExpression(source, destination, arg);
        }

        protected override Expression CreateBlockExpression(Expression source, Expression destination, CompileArgument arg)
        {
            var dictArgs = destination.Type.GetDictionaryType()!.GetGenericArguments();
            var shouldConvert = destination.Type.GetMethod("Add", dictArgs) == null;
            
            //var dict = (IDictionary<,>)dest;
            var actions = new List<Expression>();
            var dict = destination;
            if (shouldConvert)
            {
                var dictType = typeof(IDictionary<,>).MakeGenericType(dictArgs);
                dict = Expression.Variable(dictType, "dict");
                actions.Add(ExpressionEx.Assign(dict, destination)); //convert to dict type
            }

            var mapped = base.CreateBlockExpression(source, dict, arg);
            if (mapped.NodeType != ExpressionType.Default)
                actions.Add(mapped);

            //if source is not dict type, use ClassAdapter only
            var srcDictType = arg.SourceType.GetDictionaryType();
            if (srcDictType == null || arg.Settings.IgnoreNonMapped == true)
            {
                return shouldConvert && mapped.NodeType != ExpressionType.Default
                    ? Expression.Block(new[] {(ParameterExpression)dict}, actions)
                    : mapped;
            }

            //if (object.ReferenceEquals(source, destination))
            //  return destination;
            LabelTarget? label = null;
            if (source.Type.IsObjectReference() &&
                destination.Type.IsObjectReference() &&
                (source.Type.IsAssignableFrom(destination.Type) || destination.Type.IsAssignableFrom(source.Type)))
            {
                label = Expression.Label(arg.DestinationType);
                var refEquals = Expression.Call(typeof(object), nameof(ReferenceEquals), null, source, destination);
                actions.Add(Expression.IfThen(refEquals, Expression.Return(label, destination)));
            }

            var keyType = srcDictType.GetGenericArguments().First();
            var kvpType = source.Type.ExtractCollectionType();
            var kvp = Expression.Variable(kvpType, "kvp");
            var key = Expression.Variable(keyType, "key");
            var keyAssign = Expression.Assign(key, Expression.Property(kvp, "Key"));

            //dest[kvp.Key] = convert(kvp.Value);
            var set = CreateSetFromKvp(kvp, key, dict, arg);
            if (arg.Settings.NameMatchingStrategy.SourceMemberNameConverter != MapsterHelper.Identity)
            {
                set = Expression.Block(
                    Expression.Assign(
                        key,
                        Expression.Call(
                              ExpressionEx.GetNameConverterExpression(arg.Settings.NameMatchingStrategy.SourceMemberNameConverter),
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

            //ignore
            var ignoreIfs = new Dictionary<string, Expression>();
            foreach (var ignore in arg.Settings.Ignore)
            {
                if (ignore.Value.Condition == null)
                    ignores.Add(ignore.Key);
                else
                {
                    var body = ignore.Value.IsChildPath
                        ? ignore.Value.Condition.Body
                        : ignore.Value.Condition.Apply(arg.MapType, source, dict);
                    var setWithCondition = Expression.IfThen(
                        ExpressionEx.Not(body),
                        set);
                    ignoreIfs.Add(ignore.Key, setWithCondition);
                }
            }

            //dict to switch
            if (ignoreIfs.Count > 0 || ignores.Count > 0)
            {
                var cases = ignoreIfs
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
            {
                set = Expression.IfThen(
                    Expression.NotEqual(
                        Expression.Property(kvp, "Value"),
                        Expression.Constant(null, kvpValueType)),
                    set);
            }

            //foreach (var kvp in source) {
            //  dest[kvp.Key] = convert(kvp.Value);
            //}
            set = Expression.Block(new[] { key }, keyAssign, set);
            var loop = ExpressionEx.ForEach(source, kvp, set);
            actions.Add(loop);

            if (label != null)
                actions.Add(Expression.Label(label, arg.DestinationType.CreateDefault()));

            return shouldConvert
                ? Expression.Block(new[] {(ParameterExpression)dict}, actions)
                : Expression.Block(actions);
        }

        private Expression CreateSetFromKvp(Expression kvp, Expression key, Expression destination, CompileArgument arg)
        {
            var kvpValue = Expression.Property(kvp, "Value");

            var destDictType = destination.Type.GetDictionaryType()!;
            var destValueType = destDictType.GetGenericArguments()[1];
            var destGetFn = GetFunction(arg, destDictType);
            var destSetFn = SetFunction(arg, destDictType);

            var destValue = arg.MapType == MapType.MapToTarget ? destGetFn(destination, key) : null;
            var value = CreateAdaptExpression(kvpValue, destValueType, arg, destValue);

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

            var classModel = GetSetterModel(arg);
            var classConverter = CreateClassConverter(source, classModel, arg);
            var members = classConverter.Members;

            var dictType = exp.Type.GetDictionaryType()!;
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
                var itemInit = Expression.ElementInit(add!, key, value);
                lines.Add(itemInit);
            }

            return Expression.ListInit(newInstance, lines);
        }

        protected override ClassModel GetSetterModel(CompileArgument arg)
        {
            //get member name from map
            var destNames = arg.GetDestinationNames().AsEnumerable();

            //get member name from properties
            if (arg.SourceType.GetDictionaryType() == null)
            {
                var srcNames = arg.GetSourceNames();
                var propNames = arg.SourceType.GetFieldsAndProperties(true)
                    .Where(model => model.ShouldMapMember(arg, MemberSide.Source))
                    .Select(model => model.Name)
                    .Where(name => !srcNames.Contains(name))
                    .Select(name => arg.Settings.NameMatchingStrategy.SourceMemberNameConverter(name));
                destNames = destNames.Union(propNames);
            }

            //create model
            var dictArgs = arg.DestinationType.GetDictionaryType()!.GetGenericArguments();
            var dictType = typeof(IDictionary<,>).MakeGenericType(dictArgs);

            var getFn = GetFunction(arg, dictType);
            var setFn = SetFunction(arg, dictType);

            var sourceModels = destNames
                .Select(name => new KeyValuePairModel(name, dictArgs[1], getFn, setFn))
                .ToList();

            return new ClassModel
            {
                Members = sourceModels
            };
        }

        private static Func<Expression, Expression, Expression> GetFunction(CompileArgument arg, Type dictType)
        {
            var strategy = arg.Settings.NameMatchingStrategy;
            var args = dictType.GetGenericArguments();
            if (strategy.DestinationMemberNameConverter != MapsterHelper.Identity)
            {
                var getMethod = typeof(MapsterHelper).GetMethods()
                    .First(m => m.Name == nameof(MapsterHelper.FlexibleGet) && m.GetParameters()[0].ParameterType.Name == dictType.Name)
                    .MakeGenericMethod(args[1]);
                var destNameConverter = ExpressionEx.GetNameConverterExpression(strategy.DestinationMemberNameConverter);
                return (dict, key) => Expression.Call(getMethod, dict, key, destNameConverter);
            }
            else
            {
                var getMethod = typeof(MapsterHelper).GetMethods()
                    .First(m => m.Name == nameof(MapsterHelper.GetValueOrDefault) && m.GetParameters()[0].ParameterType.Name == dictType.Name)
                    .MakeGenericMethod(args);
                return (dict, key) => Expression.Call(getMethod, dict, key);
            }
        }

        private static Func<Expression, Expression, Expression, Expression> SetFunction(CompileArgument arg, Type dictType)
        {
            var strategy = arg.Settings.NameMatchingStrategy;
            if (arg.MapType == MapType.MapToTarget &&
                strategy.DestinationMemberNameConverter != MapsterHelper.Identity)
            {
                var args = dictType.GetGenericArguments();
                var setMethod = typeof(MapsterHelper).GetMethods()
                    .First(m => m.Name == nameof(MapsterHelper.FlexibleSet))
                    .MakeGenericMethod(args[1]);
                var destNameConverter = ExpressionEx.GetNameConverterExpression(strategy.DestinationMemberNameConverter);
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
