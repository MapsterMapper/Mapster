using System;
using System.Linq.Expressions;

namespace Mapster
{
    public interface ITypeAdapter
    {
        bool CanAdapt(Type sourceType, Type desinationType);
        Func<MapContext, TSource, TDestination> CreateAdaptFunc<TSource, TDestination>();
    }

    public interface ITypeAdapterWithTarget : ITypeAdapter
    {
        Func<MapContext, TSource, TDestination, TDestination> CreateAdaptTargetFunc<TSource, TDestination>();
    }

    public interface ITypeExpression : ITypeAdapter
    {
        Expression CreateExpression(Expression p, Type sourceType, Type destinationType);
    }
}
