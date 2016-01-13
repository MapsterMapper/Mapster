using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Mapster.Utils;

namespace Mapster.Adapters
{
    public abstract class BaseAdapter : ITypeAdapterWithTarget
    {
        public abstract bool CanAdapt(Type sourceType, Type desinationType);

        public Func<TSource, TDestination> CreateAdaptFunc<TSource, TDestination>()
        {
            //var depth = Expression.Parameter(typeof(int));
            var p = Expression.Parameter(typeof(TSource));
            var settings = TypeAdapterConfig<TSource, TDestination>.ConfigSettings;
            var body = CreateExpressionBody(p, null, typeof(TDestination), settings);
            return Expression.Lambda<Func<TSource, TDestination>>(body, p).Compile();
        }

        public Func<TSource, TDestination, TDestination> CreateAdaptTargetFunc<TSource, TDestination>()
        {
            //var depth = Expression.Parameter(typeof(int));
            var p = Expression.Parameter(typeof(TSource));
            var p2 = Expression.Parameter(typeof(TDestination));
            var settings = TypeAdapterConfig<TSource, TDestination>.ConfigSettings;
            var body = CreateExpressionBody(p, p2, typeof(TDestination), settings);
            return Expression.Lambda<Func<TSource, TDestination, TDestination>>(body, p, p2).Compile();
        }

        protected virtual Expression CreateExpressionBody(ParameterExpression source, ParameterExpression destination, Type destinationType, TypeAdapterConfigSettingsBase settings)
        {
            var list = new List<Expression>();

            var result = Expression.Variable(destinationType);

            Expression assign;
            if (destination != null)
            {
                assign = Expression.Assign(result, destination);
            }
            else if (settings?.ConstructUsing != null)
            {
                assign = Expression.Assign(result, settings.ConstructUsing.Apply(source));
            }
            else
            {
                assign = ExpressionEx.Assign(result, CreateInstantiationExpression(source, destinationType, settings));
            }
            var set = CreateSetterExpression(source, result, settings);

            var sourceType = source.Type;
            if ((settings?.PreserveReference ?? TypeAdapterConfig.GlobalSettings.PreserveReference) == true &&
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

            var destinationTransforms = TypeAdapterConfig.GlobalSettings.DestinationTransforms.Transforms;
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

        protected abstract Expression CreateSetterExpression(ParameterExpression source, ParameterExpression destination, TypeAdapterConfigSettingsBase settings);

        protected virtual Expression CreateInstantiationExpression(ParameterExpression source, Type destinationType, TypeAdapterConfigSettingsBase settings)
        {
            return Expression.New(destinationType);
        }

        protected virtual Expression CreateAdaptExpression(Expression sourceElement, Type destinationElementType, TypeAdapterConfigSettingsBase settings)
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
                getter = sourceElementType == destinationElementType && settings?.SameInstanceForSameType == true
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
