using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Mapster.Models;
using Mapster.Utils;

namespace Mapster.Adapters
{
    public abstract class BaseAdapter
    {
        protected virtual int Score => 0;
        protected virtual ObjectType ObjectType => ObjectType.Primitive;
        protected virtual bool CheckExplicitMapping => this.ObjectType == ObjectType.Class;
        protected virtual bool UseTargetValue => false;

        public virtual int? Priority(PreCompileArgument arg)
        {
            return CanMap(arg) ? this.Score : (int?)null;
        }

        protected abstract bool CanMap(PreCompileArgument arg);

        public LambdaExpression CreateAdaptFunc(CompileArgument arg)
        {
            var p = Expression.Parameter(arg.SourceType);
            var body = CreateExpressionBody(p, null, arg);
            return Expression.Lambda(body,
                    new[] {p}.Concat(arg.Context.ExtraParameters))
                .TrimParameters(1);
        }

        public LambdaExpression CreateAdaptToTargetFunc(CompileArgument arg)
        {
            var p = Expression.Parameter(arg.SourceType);
            var p2 = Expression.Parameter(arg.DestinationType);
            var body = CreateExpressionBody(p, p2, arg);
            return Expression.Lambda(body,
                    new[] {p, p2}.Concat(arg.Context.ExtraParameters))
                .TrimParameters(2);
        }

        public TypeAdapterRule CreateRule()
        {
            var rule = new TypeAdapterRule
            {
                Priority = this.Priority,
                Settings = new TypeAdapterSettings
                {
                    ConverterFactory = this.CreateAdaptFunc,
                    ConverterToTargetFactory = this.CreateAdaptToTargetFunc,
                }
            };
            DecorateRule(rule);
            return rule;
        }

        protected virtual void DecorateRule(TypeAdapterRule rule) { }

        protected virtual bool CanInline(Expression source, Expression? destination, CompileArgument arg)
        {
            if (arg.MapType == MapType.Projection)
                return true;

            //these settings cannot inline for Map/MapToTarget
            if (arg.Settings.PreserveReference == true &&
                arg.SourceType.IsObjectReference() &&
                arg.DestinationType.IsObjectReference())
                return false;
            if (arg.Settings.AfterMappingFactories.Count > 0)
                return false;
            if (arg.Settings.BeforeMappingFactories.Count > 0)
                return false;
            if (arg.Settings.Includes.Count > 0)
                return false;
            if (arg.Settings.AvoidInlineMapping == true)
                return false;

            return true;
        }

        protected virtual Expression CreateExpressionBody(Expression source, Expression? destination, CompileArgument arg)
        {
            if (this.CheckExplicitMapping && arg.Context.Config.RequireExplicitMapping && !arg.ExplicitMapping)
                throw new InvalidOperationException("Implicit mapping is not allowed (check GlobalSettings.RequireExplicitMapping) and no configuration exists");

            var oldMaxDepth = arg.Context.MaxDepth;
            var oldDepth = arg.Context.Depth;
            try
            {
                if (!arg.Context.MaxDepth.HasValue && arg.Settings.MaxDepth.HasValue)
                {
                    arg.Context.MaxDepth = arg.Settings.MaxDepth;
                    arg.Context.Depth = 0;
                }
                if (arg.Context.MaxDepth.HasValue)
                {
                    if (this.ObjectType != ObjectType.Primitive && arg.Context.Depth >= arg.Context.MaxDepth.Value)
                        return arg.DestinationType.CreateDefault();
                    if (this.ObjectType == ObjectType.Class)
                        arg.Context.Depth++;
                }

                if (CanInline(source, destination, arg))
                {
                    var exp = CreateInlineExpressionBody(source, arg);
                    if (exp != null)
                        return exp.To(arg.DestinationType, true);
                }

                if (arg.Context.Running.Count > 1 && 
                    !arg.Context.Config.SelfContainedCodeGeneration &&
                    !arg.UseDestinationValue &&
                    !arg.Context.IsSubFunction())
                {
                    if (destination == null)
                        return arg.Context.Config.CreateMapInvokeExpressionBody(source.Type, arg.DestinationType, source);
                    else 
                        return arg.Context.Config.CreateMapToTargetInvokeExpressionBody(source.Type, arg.DestinationType, source, destination);
                }
                else
                    return CreateBlockExpressionBody(source, destination, arg);
            }
            finally
            {
                arg.Context.Depth = oldDepth;
                arg.Context.MaxDepth = oldMaxDepth;
            }
        }

        protected virtual Expression TransformSource(Expression source)
        {
            return source;
        }

        protected Expression CreateBlockExpressionBody(Expression source, Expression? destination, CompileArgument arg)
        {
            if (arg.MapType == MapType.Projection)
                throw new InvalidOperationException("Mapping is invalid for projection");

            var vars = new List<ParameterExpression>();
            var blocks = new List<Expression>();
            var label = Expression.Label(arg.DestinationType);

            //var drvdSource = source as TDerivedSource
            //if (drvdSource != null)
            //  return adapt<TSource, TDest>(drvdSource);
            foreach (var tuple in arg.Settings.Includes)
            {
                //same type, no redirect to prevent endless loop
                if (tuple.Source == arg.SourceType)
                    continue;

                //type is not compatible, no redirect
                if (!arg.SourceType.GetTypeInfo().IsAssignableFrom(tuple.Source.GetTypeInfo()))
                    continue;

                var drvdSource = Expression.Variable(tuple.Source);
                vars.Add(drvdSource);

                var drvdSourceAssign = Expression.Assign(
                    drvdSource,
                    Expression.TypeAs(source, tuple.Source));
                blocks.Add(drvdSourceAssign);
                var cond = Expression.NotEqual(drvdSource, Expression.Constant(null, tuple.Source));

                ParameterExpression? drvdDest = null;
                if (destination != null)
                {
                    drvdDest = Expression.Variable(tuple.Destination);
                    vars.Add(drvdDest);

                    var drvdDestAssign = Expression.Assign(
                        drvdDest,
                        Expression.TypeAs(destination, tuple.Destination));
                    blocks.Add(drvdDestAssign);
                    cond = Expression.AndAlso(
                        cond,
                        Expression.NotEqual(drvdDest, Expression.Constant(null, tuple.Destination)));
                }

                var adaptExpr = CreateAdaptExpressionCore(drvdSource, tuple.Destination, arg, destination: drvdDest);
                var adapt = Expression.Return(label, adaptExpr);
                var ifExpr = Expression.IfThen(cond, adapt);
                blocks.Add(ifExpr);
            }

            //new TDest();
            Expression transformedSource = source;
            var transform = TransformSource(source);
            if (transform != source)
            {
                var src = Expression.Variable(transform.Type);
                vars.Add(src);
                transformedSource = src;
            }
            var set = CreateInstantiationExpression(transformedSource, destination, arg);
            if (destination != null && (this.UseTargetValue || arg.UseDestinationValue) && arg.GetConstructUsing()?.Parameters.Count != 2 && destination.CanBeNull())
            {
                //dest ?? new TDest();
                set = Expression.Coalesce(destination, set);
            }

            if (set.NodeType == ExpressionType.Throw)
            {
                blocks.Add(set);
            }
            else
            {
                //TDestination result;
                //if (source == null)
                //  return default(TDestination);
                if (source.CanBeNull())
                {
                    var compareNull = Expression.Equal(source, Expression.Constant(null, source.Type));
                    blocks.Add(
                        Expression.IfThen(compareNull,
                            Expression.Return(label, arg.DestinationType.CreateDefault()))
                    );
                }

                //var result = new TDest();
                var result = Expression.Variable(arg.DestinationType, "result");
                var assign = Expression.Assign(result, set);
                var assignActions = new List<Expression>();
                if (transform != source)
                    assignActions.Add(Expression.Assign(transformedSource, transform));
                assignActions.Add(assign);

                //before(source, result);
                var beforeMappings = arg.Settings.BeforeMappingFactories.Select(it => InvokeMapping(it, source, result, arg, true));
                assignActions.AddRange(beforeMappings);

                //result.prop = adapt(source.prop);
                var mapping = CreateBlockExpression(transformedSource, result, arg);
                var settingActions = new List<Expression> {mapping};

                //after(source, result);
                var afterMappings = arg.Settings.AfterMappingFactories.Select(it => InvokeMapping(it, source, result, arg, false));
                settingActions.AddRange(afterMappings);

                //return result;
                settingActions.Add(Expression.Return(label, result));

                //using (var scope = new MapContextScope()) {
                //  var references = scope.Context.Reference;
                //  var key = new ReferenceTuple(source, typeof(TDestination));
                //  if (references.TryGetValue(key, out var cache))
                //      return (TDestination)cache;
                //
                //  var result = new TDestination();
                //  references[source] = (object)result;
                //  result.prop = adapt(source.prop);
                //  return result;
                //}
                
                if (arg.Settings.PreserveReference == true &&
                    arg.SourceType.IsObjectReference() &&
                    arg.DestinationType.IsObjectReference())
                {
                    var scope = Expression.Variable(typeof(MapContextScope), "scope");
                    vars.Add(scope);

                    var newScope = Expression.Assign(scope, Expression.New(typeof(MapContextScope)));
                    blocks.Add(newScope);

                    var dictType = typeof(Dictionary<ReferenceTuple, object>);
                    var references = Expression.Variable(dictType, "references");
                    var refContext = Expression.Property(scope, "Context");
                    var refDict = Expression.Property(refContext, "References");
                    var assignReferences = Expression.Assign(references, refDict);

                    var tupleType = typeof(ReferenceTuple);
                    var key = Expression.Variable(tupleType, "key");
                    var assignKey = Expression.Assign(key,
                        Expression.New(tupleType.GetConstructor(new[] {typeof(object), typeof(Type)})!,
                            source,
                            Expression.Constant(arg.DestinationType)));

                    var cache = Expression.Variable(typeof(object), "cache");
                    var tryGetMethod = dictType.GetMethod("TryGetValue", new[] { typeof(ReferenceTuple), typeof(object).MakeByRefType() });
                    var checkHasRef = Expression.Call(references, tryGetMethod!, key, cache);
                    var setResult = Expression.IfThen(
                        checkHasRef,
                        Expression.Return(label, cache.To(arg.DestinationType)));

                    var indexer = dictType.GetProperties().First(item => item.GetIndexParameters().Length > 0);
                    var refAssign = Expression.Assign(
                        Expression.Property(references, indexer, key),
                        Expression.Convert(result, typeof(object)));
                    assignActions.Add(refAssign);

                    var usingBody = Expression.Block(
                        new[] { cache, references, key, result },
                        new Expression[] {assignReferences, assignKey, setResult}
                            .Concat(assignActions)
                            .Concat(settingActions));

                    var dispose = Expression.Call(scope, "Dispose", null);
                    blocks.Add(Expression.TryFinally(usingBody, dispose));
                }
                else
                {
                    vars.Add(result);
                    blocks.AddRange(assignActions);
                    blocks.AddRange(settingActions);
                }
            }

            blocks.Add(Expression.Label(label, arg.DestinationType.CreateDefault()));
            return Expression.Block(vars, blocks);
        }

        private static Expression InvokeMapping(Func<CompileArgument, LambdaExpression> mappingFactory, Expression source, Expression result, CompileArgument arg, bool setResult)
        {
            var afterMapping = mappingFactory(arg);
            var args = afterMapping.Parameters;
            var invoke = afterMapping.Apply(arg.MapType, source.To(args[0].Type), result.To(args[1].Type));
            if (invoke.Type != typeof(void) && setResult)
                invoke = ExpressionEx.Assign(result, invoke);
            return invoke;
        }

        protected Expression? CreateInlineExpressionBody(Expression source, CompileArgument arg)
        {
            //source == null ? default(TDestination) : adapt(source)

            var exp = CreateInlineExpression(source, arg);
            if (exp == null)
                return null;

            //projection null is handled by EF
            if (arg.MapType != MapType.Projection)
                exp = source.NotNullReturn(exp);

            return exp;
        }

        protected abstract Expression CreateBlockExpression(Expression source, Expression destination, CompileArgument arg);
        protected abstract Expression? CreateInlineExpression(Expression source, CompileArgument arg);

        protected Expression CreateInstantiationExpression(Expression source, CompileArgument arg)
        {
            return CreateInstantiationExpression(source, null, arg);
        }
        protected virtual Expression CreateInstantiationExpression(Expression source, Expression? destination, CompileArgument arg)
        {
            //new TDestination()

            //if there is constructUsing, use constructUsing
            var constructUsing = arg.GetConstructUsing();
            if (constructUsing != null)
            {
                var args = destination == null ? new[] {source} : new[] {source, destination};
                return constructUsing.Apply(arg.MapType, args)
                    .TrimConversion(true)
                    .To(arg.DestinationType);
            }

            //if there is default constructor, use default constructor
            else if (arg.DestinationType.HasDefaultConstructor())
            {
                return Expression.New(arg.DestinationType);
            }

            //if mapToTarget or include derived types, allow mapping & throw exception on runtime
            //instantiation is not needed
            else if (destination != null || arg.Settings.Includes.Count > 0)
            {
                return Expression.Throw(
                   Expression.New(
                       // ReSharper disable once AssignNullToNotNullAttribute
                       typeof(InvalidOperationException).GetConstructor(new[] { typeof(string) }),
                       Expression.Constant("Cannot instantiate type: " + arg.DestinationType.Name)),
                   arg.DestinationType);
            }

            //if mapping to interface, create dynamic type implementing it
            else if (arg.DestinationType.GetTypeInfo().IsInterface)
            {
                return Expression.New(DynamicTypeGenerator.GetTypeForInterface(arg.DestinationType));
            }

            //otherwise throw
            else
            {
                throw new InvalidOperationException($"No default constructor for type '{arg.DestinationType.Name}', please use 'ConstructUsing' or 'MapWith'");
            }
        }

        private static Expression CreateAdaptExpressionCore(Expression source, Type destinationType, CompileArgument arg, MemberMapping? mapping = null, Expression? destination = null)
        {
            var mapType = arg.MapType == MapType.MapToTarget && destination == null ? MapType.Map :
                mapping?.UseDestinationValue == true ? MapType.MapToTarget :
                arg.MapType;
            var extraParams = new HashSet<ParameterExpression>();

            try
            {
                if (mapping != null && mapping.HasSettings())
                {
                    if (arg.Context.ExtraParameters.Add(mapping.Source))
                        extraParams.Add(mapping.Source);
                    if (mapping.Destination != null && arg.Context.ExtraParameters.Add(mapping.Destination))
                        extraParams.Add(mapping.Destination);
                }
                var lambda = arg.Context.Config.CreateInlineMapExpression(source.Type, destinationType, mapType, arg.Context, mapping);
                var paramList = new List<Expression> {source};
                if (destination != null)
                    paramList.Add(destination);
                paramList.AddRange(lambda.Parameters.Skip(paramList.Count));
                return lambda.IsMultiLine() 
                    ? Expression.Invoke(lambda, paramList.ToArray()) 
                    : lambda.Apply(arg.MapType, paramList.ToArray());
            }
            finally
            {
                arg.Context.ExtraParameters.ExceptWith(extraParams);
            }
        }

        protected Expression CreateAdaptExpression(Expression source, Type destinationType, CompileArgument arg, Expression? destination = null)
        {
            return CreateAdaptExpression(source, destinationType, arg, null, destination);
        }
        internal Expression CreateAdaptExpression(Expression source, Type destinationType, CompileArgument arg, MemberMapping? mapping, Expression? destination = null)
        {
            if (source.Type == destinationType &&
                (arg.Settings.ShallowCopyForSameType == true || arg.MapType == MapType.Projection))
                return source;

            //adapt(source);
            var exp = CreateAdaptExpressionCore(source, destinationType, arg, mapping, destination);

            //transform(adapt(source));
            var transform = arg.Settings.DestinationTransforms.Find(it => it.Condition(exp.Type));
            if (transform != null)
                exp = transform.TransformFunc(exp.Type).Apply(arg.MapType, exp);
            return exp.To(destinationType);
        }
    }
}
