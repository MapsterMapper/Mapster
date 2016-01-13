using System;
using System.Linq.Expressions;

namespace Mapster
{
    public interface ITypeAdapter
    {
        bool CanAdapt(Type sourceType, Type desinationType);
        Func<TSource, TDestination> CreateAdaptFunc<TSource, TDestination>();
    }

    public interface ITypeAdapterWithTarget : ITypeAdapter
    {
        Func<TSource, TDestination, TDestination> CreateAdaptTargetFunc<TSource, TDestination>();
    }

    public interface IInlineTypeAdapter : ITypeAdapter
    {
        Expression CreateExpression(Expression source, Expression destination, Type destinationType);
    }
}
