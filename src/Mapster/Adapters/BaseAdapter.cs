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
        protected virtual bool CheckExplicitMapping => true;

        public virtual int? Priority(Type sourceType, Type destinationType, MapType mapType)
        {
            return CanMap(sourceType, destinationType, mapType) ? this.Score : (int?)null;
        }

        protected abstract bool CanMap(Type sourceType, Type destinationType, MapType mapType);

        public LambdaExpression CreateAdaptFunc(CompileArgument arg)
        {
            var p = Expression.Parameter(arg.SourceType);
            var body = CreateExpressionBody(p, null, arg);
            return Expression.Lambda(body, p);
        }

        public LambdaExpression CreateAdaptToTargetFunc(CompileArgument arg)
        {
            var p = Expression.Parameter(arg.SourceType);
            var p2 = Expression.Parameter(arg.DestinationType);
            var body = CreateExpressionBody(p, p2, arg);
            return Expression.Lambda(body, p, p2);
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
            if (arg.MapType == MapType.MapToTarget)
                return false;
            var constructUsing = arg.Settings.ConstructUsingFactory?.Invoke(arg);
            if (constructUsing != null &&
                constructUsing.Body.NodeType != ExpressionType.New &&
                constructUsing.Body.NodeType != ExpressionType.MemberInit)
            {
                if (arg.MapType == MapType.Projection)
                    throw new InvalidOperationException("Input ConstructUsing is invalid for projection");
                return false;
            }

            //IgnoreIf, PreserveReference, AfterMapping, Includes aren't supported by Projection
            if (arg.MapType == MapType.Projection)
                return true;

            if (arg.Settings.IgnoreIfs.Any(item => item.Value != null))
                return false;
            if (arg.Settings.PreserveReference == true &&
                !arg.SourceType.GetTypeInfo().IsValueType &&
                !arg.DestinationType.GetTypeInfo().IsValueType)
                return false;
            if (arg.Settings.AfterMappingFactories.Count > 0)
                return false;
            return true;
        }

        protected virtual Expression CreateExpressionBody(Expression source, Expression destination, CompileArgument arg)
        {
            if (this.CheckExplicitMapping
                && arg.Context.Config.RequireExplicitMapping
                && !arg.Context.Config.RuleMap.ContainsKey(new TypeTuple(arg.SourceType, arg.DestinationType)))
            {
                throw new InvalidOperationException("Implicit mapping is not allowed (check GlobalSettings.RequireExplicitMapping) and no configuration exists");
            }

            return CanInline(source, destination, arg)
                ? CreateInlineExpressionBody(source, arg).To(arg.DestinationType, true)
                : CreateBlockExpressionBody(source, destination, arg);
        }

        protected Expression CreateBlockExpressionBody(Expression source, Expression destination, CompileArgument arg)
        {
            if (arg.MapType == MapType.Projection)
                throw new InvalidOperationException("Mapping is invalid for projection");

            //### adapt
            //var result = new TDest();
            //
            //### adapt to target
            //var result = dest ?? new TDest();
            var result = Expression.Variable(arg.DestinationType);
            var newObj = CreateInstantiationExpression(source, arg);
            if (destination != null)
                newObj = Expression.Coalesce(destination, newObj);
            Expression assign = Expression.Assign(result, newObj);

            var set = CreateBlockExpression(source, result, arg);

            //result.prop = adapt(source.prop);
            //action(source, result);
            if (arg.Settings.AfterMappingFactories.Count > 0)
            {
                var actions = new List<Expression> { set };

                foreach (var afterMappingFactory in arg.Settings.AfterMappingFactories)
                {
                    var afterMapping = afterMappingFactory(arg);
                    var args = afterMapping.Parameters;
                    Expression invoke;
                    if (args[0].Type.IsReferenceAssignableFrom(source.Type) && args[1].Type.IsReferenceAssignableFrom(result.Type))
                    {
                        var replacer = new ParameterExpressionReplacer(args, source, result);
                        invoke = replacer.Visit(afterMapping.Body);
                    }
                    else
                    {
                        invoke = Expression.Invoke(afterMapping, source.To(args[0].Type), result.To(args[1].Type));
                    }
                    actions.Add(invoke);
                }

                set = Expression.Block(actions);
            }

            //using (var scope = new MapContextScope()) {
            //  var dict = scope.Context.Reference;
            //  object cache;
            //  if (dict.TryGetValue(source, out cache))
            //      result = (TDestination)cache;
            //  else {
            //      result = new TDestination();
            //      dict.Add(source, (object)result);
            //      result.prop = adapt(source.prop);
            //  }
            //}
            if (arg.Settings.PreserveReference == true &&
                !arg.SourceType.GetTypeInfo().IsValueType &&
                !arg.DestinationType.GetTypeInfo().IsValueType)
            {
                var scope = Expression.Variable(typeof(MapContextScope));
                var newScope = Expression.Assign(scope, Expression.New(typeof(MapContextScope)));

                var dict = Expression.Variable(typeof(Dictionary<object, object>));
                var refContext = Expression.Property(scope, "Context");
                var refDict = Expression.Property(refContext, "References");
                var assignDict = Expression.Assign(dict, refDict);

                var refAdd = Expression.Call(dict, "Add", null, Expression.Convert(source, typeof(object)), Expression.Convert(result, typeof(object)));
                var setResultAndCache = Expression.Block(assign, refAdd, set);

                var cached = Expression.Variable(typeof(object));
                var tryGetMethod = typeof(Dictionary<object, object>).GetMethod("TryGetValue", new[] { typeof(object), typeof(object).MakeByRefType() });
                var checkHasRef = Expression.Call(dict, tryGetMethod, source, cached);
                var setResult = Expression.IfThenElse(
                    checkHasRef,
                    ExpressionEx.Assign(result, cached),
                    setResultAndCache);
                var usingBody = Expression.Block(new[] { cached, dict }, assignDict, setResult);

                var dispose = Expression.Call(scope, "Dispose", null);
                set = Expression.Block(new[] { scope }, newScope, Expression.TryFinally(usingBody, dispose));
            }
            else
            {
                set = Expression.Block(assign, set);
            }

            //TDestination result;
            //if (source == null)
            //  result = default(TDestination);
            //else {
            //  result = new TDestination();
            //  result.prop = adapt(source.prop);
            //}
            //return result;
            if (!arg.SourceType.GetTypeInfo().IsValueType || arg.SourceType.IsNullable())
            {
                var compareNull = Expression.Equal(source, Expression.Constant(null, source.Type));
                set = Expression.IfThenElse(
                    compareNull,
                    Expression.Assign(result, destination ?? Expression.Constant(arg.DestinationType.GetDefault(), arg.DestinationType)),
                    set);
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

                var adapt = Expression.Assign(
                    result,
                    CreateAdaptToExpression(drvdSource, drvdDest, arg));
                var ifExpr = Expression.Condition(cond, adapt, set);
                blocks.Add(ifExpr);
                set = Expression.Block(vars, blocks);
            }

            return Expression.Block(new[] { result }, set, result);
        }
        protected Expression CreateInlineExpressionBody(Expression source, CompileArgument arg)
        {
            //source == null ? default(TDestination) : adapt(source)

            var exp = CreateInlineExpression(source, arg);

            if (arg.MapType != MapType.Projection
                && (!arg.SourceType.GetTypeInfo().IsValueType || arg.SourceType.IsNullable()))
            {
                var compareNull = Expression.Equal(source, Expression.Constant(null, source.Type));
                exp = Expression.Condition(
                    compareNull,
                    Expression.Constant(exp.Type.GetDefault(), exp.Type),
                    exp);
            }

            return exp;
        }

        protected abstract Expression CreateBlockExpression(Expression source, Expression destination, CompileArgument arg);
        protected abstract Expression CreateInlineExpression(Expression source, CompileArgument arg);

        protected virtual Expression CreateInstantiationExpression(Expression source, CompileArgument arg)
        {
            //new TDestination()

            var constructUsing = arg.Settings.ConstructUsingFactory?.Invoke(arg);
            return constructUsing != null
                ? constructUsing.Apply(source).TrimConversion(true).To(arg.DestinationType)
                : Expression.New(arg.DestinationType);
        }

        protected Expression CreateAdaptExpression(Expression source, Type destinationType, CompileArgument arg)
        {
            if (source.Type == destinationType && (arg.Settings.ShallowCopyForSameType == true || arg.MapType == MapType.Projection))
                return source;

            //adapt(source);
            var lambda = arg.Context.Config.CreateInlineMapExpression(source.Type, destinationType, arg.MapType, arg.Context);
            var exp = lambda.Apply(source);

            //transform(adapt(source));
            if (arg.Settings.DestinationTransforms.Transforms.ContainsKey(exp.Type))
            {
                var transform = arg.Settings.DestinationTransforms.Transforms[exp.Type];
                var replacer = new ParameterExpressionReplacer(transform.Parameters, exp);
                var newExp = replacer.Visit(transform.Body);
                exp = replacer.ReplaceCount >= 2 ? Expression.Invoke(transform, exp) : newExp;
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
            var lambda = arg.Context.Config.CreateMapToTargetInvokeExpression(source.Type, destination.Type);
            var exp = lambda.Apply(source, destination);

            //transform(adapt(source, dest));
            if (arg.Settings.DestinationTransforms.Transforms.ContainsKey(exp.Type))
            {
                var transform = arg.Settings.DestinationTransforms.Transforms[exp.Type];
                var replacer = new ParameterExpressionReplacer(transform.Parameters, exp);
                var newExp = replacer.Visit(transform.Body);
                exp = replacer.ReplaceCount >= 2 ? Expression.Invoke(transform, exp) : newExp;
            }
            return exp.To(destination.Type);
        }
    }
}
