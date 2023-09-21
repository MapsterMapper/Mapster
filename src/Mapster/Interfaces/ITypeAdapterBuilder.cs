using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Mapster
{
    public interface ITypeAdapterBuilder<T> : IAdapterBuilder<T>
    {
        [SuppressMessage("ReSharper", "ExplicitCallerInfoArgument")]
        ITypeAdapterBuilder<T> ForkConfig(Action<TypeAdapterConfig> action,
#if !NET40
            [CallerFilePath]
#endif
            string key1 = "",
#if !NET40
            [CallerLineNumber]
#endif
            int key2 = 0);
        ITypeAdapterBuilder<T> AddParameters(string name, object value);
        Expression<Func<T, TDestination>> CreateMapExpression<TDestination>();
        Expression<Func<T, TDestination, TDestination>> CreateMapToTargetExpression<TDestination>();
        Expression<Func<T, TDestination>> CreateProjectionExpression<TDestination>();
    }
}
