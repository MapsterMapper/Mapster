namespace Mapster.DependencyInjection;

public static class HostBuilderExtensions
{
    public static IHostBuilder UseMapster(
        this IHostBuilder builder, 
        Action<MapsterOptions>? configure = null)
    {
        return builder.ConfigureServices(services => services.AddMapster(configure));
    }

    public static IHostBuilder UseMapster(
        this IHostBuilder builder,
        string sectionName = "",
        Func<HostBuilderContext, IConfiguration>? configSection = null)
    {

        if (configSection is null)
        {
            if (sectionName is not { Length: > 0 })
            {
                sectionName = MapsterOptions.DefaultName;
            }
            configSection = ctx => ctx.Configuration.GetSection(sectionName);
        }

        return builder.ConfigureServices((context, services) =>
        {
            var section = configSection(context);
            services.AddMapster(section);
        });
    }
}
