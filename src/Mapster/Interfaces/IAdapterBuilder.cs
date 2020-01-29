using System.Collections.Generic;

namespace Mapster
{
    public interface IAdapterBuilder
    {
        TypeAdapterConfig Config { get; }
        bool HasParameter { get; }
        Dictionary<string, object> Parameters { get; }
        MapContextScope CreateMapContextScope();
        TDestination AdaptToType<TDestination>();
        TDestination AdaptTo<TDestination>(TDestination destination);
    }

    public interface IAdapterBuilder<out T> : IAdapterBuilder
    {
        T Source { get; }
    }
}
