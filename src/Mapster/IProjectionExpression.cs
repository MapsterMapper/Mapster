using System.Linq;

namespace Mapster
{
    public interface IProjectionExpression
    {
        IQueryable<TDestination> To<TDestination>();
    }
}