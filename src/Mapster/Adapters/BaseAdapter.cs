using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Mapster.Models;
using Mapster.Utils;

namespace Mapster.Adapters
{
    public abstract class BaseAdapter
    {
        public abstract int? Priority(Type sourceType, Type destinationType, MapType mapType);

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
            if (destination != null)
                return false;
            if (arg.Settings.ConstructUsing != null && arg.Settings.ConstructUsing.Body.NodeType != ExpressionType.New)
                return false;
            if (arg.Settings.PreserveReference == true &&
                arg.MapType != MapType.Projection &&
                !arg.SourceType.IsValueType &&
                !arg.DestinationType.IsValueType)
                return false;
            return true;
        }

        protected virtual Expression CreateExpressionBody(Expression source, Expression destination, CompileArgument arg)
        {
            return CanInline(source, destination, arg) 
                ? CreateInlineExpressionBody(source, arg).To(arg.DestinationType) 
                : CreateBlockExpressionBody(source, destination, arg);
        }

        protected Expression CreateBlockExpressionBody(Expression source, Expression destination, CompileArgument arg)
        {
            var result = Expression.Variable(arg.DestinationType);
            Expression assign = Expression.Assign(result, destination ?? CreateInstantiationExpression(source, arg));

            var set = CreateBlockExpression(source, result, arg);

            if (arg.Settings.PreserveReference == true &&
                arg.MapType != MapType.Projection &&
                !arg.SourceType.IsValueType &&
                !arg.DestinationType.IsValueType)
            {
                var dict = Expression.Parameter(typeof (Dictionary<object, object>));
                var propInfo = typeof(MapContext).GetProperty("Context", BindingFlags.Static | BindingFlags.Public);
                var refContext = Expression.Property(null, propInfo);
                var refDict = Expression.Property(refContext, "References");

                var refAdd = Expression.Call(dict, "Add", null, Expression.Convert(source, typeof(object)), Expression.Convert(result, typeof(object)));
                set = Expression.Block(assign, refAdd, set);

                var cached = Expression.Variable(typeof(object));
                var tryGetMethod = typeof(Dictionary<object, object>).GetMethod("TryGetValue", new[] { typeof(object), typeof(object).MakeByRefType() });
                var checkHasRef = Expression.Call(dict, tryGetMethod, source, cached);
                var assignDict = Expression.Assign(dict, refDict);
                set = Expression.IfThenElse(
                    checkHasRef,
                    ExpressionEx.Assign(result, cached),
                    set);
                set = Expression.Block(new[] { cached, dict }, assignDict, set);
            }
            else
            {
                set = Expression.Block(assign, set);
            }

            if (arg.MapType != MapType.Projection && 
                (!arg.SourceType.IsValueType || arg.SourceType.IsNullable()))
            {
                var compareNull = Expression.Equal(source, Expression.Constant(null, source.Type));
                set = Expression.IfThenElse(
                    compareNull,
                    Expression.Assign(result, destination ?? Expression.Constant(arg.DestinationType.GetDefault(), arg.DestinationType)),
                    set);
            }

            return Expression.Block(new[] { result }, set, result);
        }
        protected Expression CreateInlineExpressionBody(Expression source, CompileArgument arg)
        {
            var exp = CreateInlineExpression(source, arg);

            if (arg.MapType != MapType.Projection
                && (!arg.SourceType.IsValueType || arg.SourceType.IsNullable()))
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
            return arg.Settings.ConstructUsing != null 
                ? arg.Settings.ConstructUsing.Apply(source).TrimConversion().To(arg.DestinationType) 
                : Expression.New(arg.DestinationType);
        }

        protected Expression CreateAdaptExpression(Expression source, Type destinationType, CompileArgument arg)
        {
            if (source.Type == destinationType && (arg.Settings.ShallowCopyForSameType == true || arg.MapType == MapType.Projection))
                return source.To(destinationType);

            var lambda = arg.Context.Config.CreateInlineMapExpression(source.Type, destinationType, arg.MapType, arg.Context);
            var exp = lambda.Apply(source);

            if (arg.Settings.DestinationTransforms.Transforms.ContainsKey(exp.Type))
            {
                var transform = arg.Settings.DestinationTransforms.Transforms[exp.Type];
                var replacer = new ParameterExpressionReplacer(transform.Parameters, exp);
                var newExp = replacer.Visit(transform.Body);
                exp = replacer.ReplaceCount >= 2 ? Expression.Invoke(transform, exp) : newExp;
            }
            return exp.To(destinationType);
        }
    }
}
