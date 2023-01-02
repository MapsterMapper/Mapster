using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;

namespace Mapster
{
    public static class ServiceCollectionExtensions
    {
        public static void AddMapster(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<IMapper, Mapper>();
        }
    }
}