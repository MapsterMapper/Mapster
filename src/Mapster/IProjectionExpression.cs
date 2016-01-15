using System.Linq;

namespace Mapster
{
    public interface IProjectionExpression
    {
        IQueryable<TDestination> To<TDestination>(TypeAdapterConfig config = null);
    }
}