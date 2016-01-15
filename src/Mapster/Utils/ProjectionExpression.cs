using System.Linq;

namespace Mapster.Utils
{
    internal class ProjectionExpression<TSource> : IProjectionExpression
    {
        private readonly IQueryable<TSource> _source;
        public ProjectionExpression(IQueryable<TSource> source)
        {
            _source = source;
        }

        public IQueryable<TDestination> To<TDestination>(TypeAdapterConfig config = null)
        {
            config = config ?? TypeAdapterConfig.GlobalSettings;
            var queryExpression = config.GetProjectionExpression<TSource, TDestination>();
            return _source.Select(queryExpression);
        }
    }
}
