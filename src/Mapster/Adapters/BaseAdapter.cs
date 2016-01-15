using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Mapster.Utils;

namespace Mapster.Adapters
{
    public abstract class BaseAdapter
    {
        public abstract int? Priority(Type sourceType, Type desinationType, MapType mapType);

        public virtual LambdaExpression CreateAdaptFunc(CompileArgument arg)
        {
            //var depth = Expression.Parameter(typeof(int));
            var p = Expression.Parameter(arg.SourceType);
            var body = CreateExpressionBody(p, null, arg);
            return Expression.Lambda(body, p);
        }

        public virtual LambdaExpression CreateAdaptToTargetFunc(CompileArgument arg)
        {
            //var depth = Expression.Parameter(typeof(int));
            var p = Expression.Parameter(arg.SourceType);
            var p2 = Expression.Parameter(arg.DestinationType);
            var body = CreateExpressionBody(p, p2, arg);
            return Expression.Lambda(body, p, p2);
        }

        public TypeAdapterRule CreateRule()
        {
            return new TypeAdapterRule
            {
                Priority = this.Priority,
                Settings = new TypeAdapterSettings
                {
                    ConverterFactory = this.CreateAdaptFunc,
                    ConverterToTargetFactory = this.CreateAdaptToTargetFunc,
                }
            };
        }

        protected virtual Expression CreateExpressionBody(Expression source, Expression destination, CompileArgument arg)
        {
            var list = new List<Expression>();

            if (destination == null)
            {
                if (arg.Settings.ConstructUsing != null)
                {
                    destination = arg.Settings.ConstructUsing.Apply(source).TrimConversion().To(destination.Type);
                }
                else
                {
                    destination = CreateInstantiationExpression(source, arg);
                }
            }
            var set = CreateSetterExpression(source, result, settings);

            var sourceType = source.Type;
            if ((settings?.PreserveReference ?? BaseTypeAdapterConfig.GlobalSettings.PreserveReference) == true &&
                !sourceType.IsValueType &&
                !destinationType.IsValueType)
            {
                var propInfo = typeof(MapContext).GetProperty("References", BindingFlags.Static | BindingFlags.Public);
                var refDict = Expression.Property(null, propInfo);
                var refAdd = Expression.Call(refDict, "Add", null, Expression.Convert(source, typeof(object)), Expression.Convert(result, typeof(object)));
                set = Expression.Block(assign, refAdd, set);

                var cached = Expression.Variable(typeof(object));
                var tryGetMethod = typeof(Dictionary<object, object>).GetMethod("TryGetValue", new[] { typeof(object), typeof(object).MakeByRefType() });
                var checkHasRef = Expression.Call(refDict, tryGetMethod, source, cached);
                set = Expression.IfThenElse(
                    checkHasRef,
                    ExpressionEx.Assign(result, cached),
                    set);
                set = Expression.Block(new[] { cached }, set);
            }
            else
            {
                set = Expression.Block(assign, set);
            }

            //if (TypeAdapterConfig.GlobalSettings.EnableMaxDepth)
            //{
            //    var compareDepth = Expression.Equal(depth, Expression.Constant(0));
            //    set = Expression.IfThenElse(
            //        compareDepth,
            //        Expression.Assign(pDest, (Expression) p2 ?? Expression.Constant(null, destinationType)),
            //        set);
            //}

            if (!sourceType.IsValueType || sourceType.IsNullable())
            {
                var compareNull = Expression.Equal(source, Expression.Constant(null, source.Type));
                set = Expression.IfThenElse(
                    compareNull,
                    Expression.Assign(result, (Expression)destination ?? Expression.Constant(destinationType.GetDefault(), destinationType)),
                    set);
            }
            list.Add(set);

            var destinationTransforms = BaseTypeAdapterConfig.GlobalSettings.DestinationTransforms.Transforms;
            if (destinationTransforms.ContainsKey(destinationType))
            {
                var transform = destinationTransforms[destinationType];
                var invoke = Expression.Invoke(transform, result);
                list.Add(Expression.Assign(result, invoke));
            }
            var localTransform = settings?.DestinationTransforms.Transforms;
            if (localTransform != null && localTransform.ContainsKey(destinationType))
            {
                var transform = localTransform[destinationType];
                var invoke = Expression.Invoke(transform, result);
                list.Add(Expression.Assign(result, invoke));
            }

            list.Add(result);

            return Expression.Block(new[] { result }, list);
        }

        protected abstract Expression CreateSetterExpression(ParameterExpression source, ParameterExpression destination, TypeAdapterSettings settings);

        protected virtual Expression CreateInstantiationExpression(Expression source, CompileArgument arg)
        {
            return Expression.New(arg.DestinationType);
        }

        protected virtual Expression CreateAdaptExpression(Expression sourceElement, Type destinationElementType, TypeAdapterSettings settings)
        {
            var sourceElementType = sourceElement.Type;
            var adapter = TypeAdapter.GetAdapter(sourceElementType, destinationElementType) as IInlineTypeAdapter;

            Expression getter;
            if (adapter != null)
            {
                getter = adapter.CreateExpression(sourceElement, null, destinationElementType);
            }
            else
            {
                var typeAdaptType = typeof(TypeAdapter<,>).MakeGenericType(sourceElementType, destinationElementType);
                var method = typeAdaptType.GetMethod("AdaptWithContext",
                    new[] { sourceElementType });
                getter = sourceElementType == destinationElementType && settings?.ShallowCopyForSameType == true
                    ? sourceElement
                    : Expression.Call(method, sourceElement);
            }

            var localTransform = settings?.DestinationTransforms.Transforms;
            if (localTransform != null && localTransform.ContainsKey(getter.Type))
                getter = Expression.Invoke(localTransform[getter.Type], getter);

            return getter;
        }
    }
}
