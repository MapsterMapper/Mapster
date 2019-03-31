using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Mapster.Utils;

namespace Mapster.Adapters
{
    public abstract class BaseAdapter
    {
        protected virtual int Score => 0;
        protected virtual bool CheckExplicitMapping => false;
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
            return body == null ? null : Expression.Lambda(body, p);
        }

        public LambdaExpression CreateAdaptToTargetFunc(CompileArgument arg)
        {
            var p = Expression.Parameter(arg.SourceType);
            var p2 = Expression.Parameter(arg.DestinationType);
            var body = CreateExpressionBody(p, p2, arg);
            return body == null ? null : Expression.Lambda(body, p, p2);
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

        protected virtual bool CanInline(Expression source, Expression destination, CompileArgument arg)
        {
            //PreserveReference, AfterMapping, Includes aren't supported by Projection
            if (arg.MapType == MapType.Projection)
                return true;

            if (arg.Settings.PreserveReference == true &&
                !arg.SourceType.GetTypeInfo().IsValueType &&
                !arg.DestinationType.GetTypeInfo().IsValueType)
                return false;
            if (arg.Settings.AfterMappingFactories.Count > 0)
                return false;
            if (arg.Settings.BeforeMappingFactories.Count > 0)
                return false;
            if (arg.Settings.Includes.Count > 0)
                return false;
            return true;
        }

        protected virtual Expression CreateExpressionBody(Expression source, Expression destination, CompileArgument arg)
        {
            if (this.CheckExplicitMapping && arg.Context.Config.RequireExplicitMapping && !arg.ExplicitMapping)
                throw new InvalidOperationException("Implicit mapping is not allowed (check GlobalSettings.RequireExplicitMapping) and no configuration exists");

            if (CanInline(source, destination, arg) && arg.Settings.AvoidInlineMapping != true)
                return CreateInlineExpressionBody(source, arg).To(arg.DestinationType, true);
            else if (!arg.Context.Config.SelfContainedCodeGeneration && arg.Context.Running.Count > 1)
                return null;
            else
                return CreateBlockExpressionBody(source, destination, arg);
        }

        protected Expression CreateBlockExpressionBody(Expression source, Expression destination, CompileArgument arg)
        {
            if (arg.MapType == MapType.Projection)
                throw new InvalidOperationException("Mapping is invalid for projection");

            //var result = new TDest();
            var result = Expression.Variable(arg.DestinationType, "result");
            var set = CreateInstantiationExpression(source, destination, arg);

            if (set.NodeType != ExpressionType.Throw)
            {
                if (arg.MapType == MapType.MapToTarget && this.UseTargetValue && arg.GetConstructUsing()?.Parameters.Count != 2)
                {
                    set = Expression.Coalesce(destination, set);
                }

                var actions = new List<Expression>
                {
                    Expression.Assign(result, set)
                };

                //before(source, result);
                var beforeMappings = arg.Settings.BeforeMappingFactories.Select(it => InvokeMapping(it, source, result, arg));
                actions.AddRange(beforeMappings);

                //result.prop = adapt(source.prop);
                set = CreateBlockExpression(source, result, arg);
                actions.Add(set);

                //after(source, result);
                var afterMappings = arg.Settings.AfterMappingFactories.Select(it => InvokeMapping(it, source, result, arg));
                actions.AddRange(afterMappings);

                //using (var scope = new MapContextScope()) {
                //  var references = scope.Context.Reference;
                //  object cache;
                //  if (references.TryGetValue(source, out cache))
                //      result = (TDestination)cache;
                //  else {
                //      result = new TDestination();
                //      references[source] = (object)result;
                //      result.prop = adapt(source.prop);
                //  }
                //}
                if (arg.Settings.PreserveReference == true &&
                    !arg.SourceType.GetTypeInfo().IsValueType &&
                    !arg.DestinationType.GetTypeInfo().IsValueType)
                {
                    var scope = Expression.Variable(typeof(MapContextScope), "scope");
                    var newScope = Expression.Assign(scope, Expression.New(typeof(MapContextScope)));

                    var dictType = typeof(Dictionary<object, object>);
                    var dict = Expression.Variable(dictType, "references");
                    var refContext = Expression.Property(scope, "Context");
                    var refDict = Expression.Property(refContext, "References");
                    var assignDict = Expression.Assign(dict, refDict);

                    var indexer = dictType.GetProperties().First(item => item.GetIndexParameters().Length > 0);
                    var refAssign = Expression.Assign(
                        Expression.Property(dict, indexer, Expression.Convert(source, typeof(object))),
                        Expression.Convert(result, typeof(object)));
                    actions.Add(refAssign);
                    var setResultAndCache = Expression.Block(actions);

                    var cache = Expression.Variable(typeof(object), "cache");
                    var tryGetMethod = typeof(Dictionary<object, object>).GetMethod("TryGetValue", new[] { typeof(object), typeof(object).MakeByRefType() });
                    var checkHasRef = Expression.Call(dict, tryGetMethod, source, cache);
                    var setResult = Expression.IfThenElse(
                        checkHasRef,
                        ExpressionEx.Assign(result, cache),
                        setResultAndCache);
                    var usingBody = Expression.Block(new[] { cache, dict }, assignDict, setResult);

                    var dispose = Expression.Call(scope, "Dispose", null);
                    set = Expression.Block(new[] { scope }, newScope, Expression.TryFinally(usingBody, dispose));
                }
                else
                {
                    set = Expression.Block(actions);
                }

                //TDestination result;
                //if (source == null)
                //  result = default(TDestination);
                //else {
                //  result = new TDestination();
                //  result.prop = adapt(source.prop);
                //}
                //return result;
                if (source.CanBeNull())
                {
                    var compareNull = Expression.Equal(source, Expression.Constant(null, source.Type));
                    set = Expression.IfThenElse(
                        compareNull,
                        Expression.Assign(result, arg.DestinationType.CreateDefault()),
                        set);
                }
            }

            //var drvdSource = source as TDerivedSource
            //if (drvdSource != null)
            //  result = adapt<TSource, TDest>(drvdSource);
            //else {
            //  result = new TDestination();
            //  result.prop = adapt(source.prop);
            //}
            //return result;
            foreach (var tuple in arg.Settings.Includes)
            {
                //same type, no redirect to prevent endless loop
                if (tuple.Source == arg.SourceType)
                    continue;

                //type is not compatible, no redirect
                if (!arg.SourceType.IsReferenceAssignableFrom(tuple.Source))
                    continue;

                var blocks = new List<Expression>();
                var vars = new List<ParameterExpression>();

                var drvdSource = Expression.Variable(tuple.Source);
                vars.Add(drvdSource);

                var drvdSourceAssign = Expression.Assign(
                    drvdSource,
                    Expression.TypeAs(source, tuple.Source));
                blocks.Add(drvdSourceAssign);
                var cond = Expression.NotEqual(drvdSource, Expression.Constant(null, tuple.Source));

                ParameterExpression drvdDest = null;
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

                var adaptExpr = drvdDest == null
                    ? CreateAdaptExpression(drvdSource, tuple.Destination, arg)
                    : CreateAdaptToExpression(drvdSource, drvdDest, arg);
                var adapt = Expression.Assign(result, adaptExpr);
                var ifExpr = Expression.Condition(cond, adapt, set, typeof(void));
                blocks.Add(ifExpr);
                set = Expression.Block(vars, blocks);
            }

            return Expression.Block(new[] { result }, set, result);
        }

        private Expression InvokeMapping(Func<CompileArgument, LambdaExpression> mappingFactory, Expression source, Expression result, CompileArgument arg)
        {
            var afterMapping = mappingFactory(arg);
            var args = afterMapping.Parameters;
            var invoke = afterMapping.Apply(arg.MapType, source.To(args[0].Type), result.To(args[1].Type));
            if (invoke.Type != typeof(void))
                invoke = ExpressionEx.Assign(result, invoke);
            return invoke;
        }

        protected Expression CreateInlineExpressionBody(Expression source, CompileArgument arg)
        {
            //source == null ? default(TDestination) : adapt(source)

            var exp = CreateInlineExpression(source, arg);

            if (arg.MapType != MapType.Projection && !exp.IsSingleValue() && source.CanBeNull())
            {
                var compareNull = Expression.Equal(source, Expression.Constant(null, source.Type));
                exp = Expression.Condition(
                    compareNull,
                    exp.Type.CreateDefault(),
                    exp);
            }

            return exp;
        }

        protected abstract Expression CreateBlockExpression(Expression source, Expression destination, CompileArgument arg);
        protected abstract Expression CreateInlineExpression(Expression source, CompileArgument arg);

        protected Expression CreateInstantiationExpression(Expression source, CompileArgument arg)
        {
            return CreateInstantiationExpression(source, null, arg);
        }
        protected virtual Expression CreateInstantiationExpression(Expression source, Expression destination, CompileArgument arg)
        {
            //new TDestination()

            //if there is constructUsing, use constructUsing
            var constructUsing = arg.GetConstructUsing();
            if (constructUsing != null)
                return constructUsing.Apply(arg.MapType, source, destination).TrimConversion(true).To(arg.DestinationType);

            //if there is default constructor, use default constructor
            else if (arg.DestinationType.HasDefaultConstructor())
                return Expression.New(arg.DestinationType);

            //if mapToTarget or include derived types, allow mapping & throw exception on runtime
            //instantiation is not needed
            else if (destination != null || arg.Settings.Includes.Count > 0)
                return Expression.Throw(
                    Expression.New(
                        // ReSharper disable once AssignNullToNotNullAttribute
                        typeof(InvalidOperationException).GetConstructor(new[] { typeof(string) }),
                        Expression.Constant("Cannot instantiate type: " + arg.DestinationType.Name)),
                    arg.DestinationType);

            //otherwise throw
            else
                throw new InvalidOperationException($"No default constructor for type '{arg.DestinationType.Name}', please use 'ConstructUsing'");
        }

        protected Expression CreateAdaptExpression(Expression source, Type destinationType, CompileArgument arg)
        {
            if (source.Type == destinationType &&
                (arg.Settings.ShallowCopyForSameType == true || arg.MapType == MapType.Projection))
                return source;

            //adapt(source);
            var lambda = arg.Context.Config.CreateInlineMapExpression(source.Type, destinationType, arg.MapType == MapType.MapToTarget ? MapType.Map : arg.MapType, arg.Context);
            var exp = !lambda.IsMultiLine() 
                ? lambda.Apply(arg.MapType, source)
                : arg.Context.Config.SelfContainedCodeGeneration
                ? (Expression)Expression.Invoke(lambda, source)
                : arg.Context.Config.CreateMapInvokeExpression(source.Type, destinationType);

            //transform(adapt(source));
            if (arg.Settings.DestinationTransforms.Transforms.ContainsKey(exp.Type))
            {
                var transform = arg.Settings.DestinationTransforms.Transforms[exp.Type];
                exp = transform.Apply(arg.MapType, exp);
            }
            return exp.To(destinationType);
        }

        protected Expression CreateAdaptToExpression(Expression source, Expression destination, CompileArgument arg)
        {
            if (destination == null)
                return CreateAdaptExpression(source, arg.DestinationType, arg);

            if (source.Type == destination.Type && arg.Settings.ShallowCopyForSameType == true)
                return source;

            //adapt(source, dest);
            var lambda = arg.Context.Config.CreateInlineMapExpression(source.Type, destination.Type, arg.MapType, arg.Context);
            var exp = !lambda.IsMultiLine()
                ? lambda.Apply(arg.MapType, source, destination)
                : arg.Context.Config.SelfContainedCodeGeneration
                ? (Expression)Expression.Invoke(lambda, source, destination)
                : arg.Context.Config.CreateMapToTargetInvokeExpression(source.Type, destination.Type);

            //transform(adapt(source, dest));
            if (arg.Settings.DestinationTransforms.Transforms.ContainsKey(exp.Type))
            {
                var transform = arg.Settings.DestinationTransforms.Transforms[exp.Type];
                exp = transform.Apply(arg.MapType, exp);
            }
            return exp.To(destination.Type);
        }
    }
}
