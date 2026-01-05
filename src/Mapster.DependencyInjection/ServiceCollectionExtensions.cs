namespace Mapster.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static void AddMapster(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddTransient<IMapper, Mapper>();
    }

    public static IServiceCollection AddMapster(this IServiceCollection services, Action<MapsterOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.AddOptions<MapsterOptions>().Configure(configureOptions);
        return services.AddMapsterCore();
    }

    public static IServiceCollection AddMapster(this IServiceCollection services, IConfiguration configuration, string sectionName = MapsterOptions.DefaultSectionName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection(sectionName);

        if (section.Exists())
        {
            services.AddOptions<MapsterOptions>().Bind(section);
        }
        else
        {
            services.AddOptions<MapsterOptions>();
        }

        return services.AddMapsterCore();
    }

    /// <summary>
    /// Adds an additional configuration callback that will be applied to the DI-provided <see cref="TypeAdapterConfig"/>.
    /// This enables modular configuration (for example per assembly or feature).
    /// </summary>
    public static IServiceCollection AddMapsterConfig(this IServiceCollection services, Action<TypeAdapterConfig> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        return services.AddMapsterConfig((cfg, _) => configure(cfg));
    }

    /// <summary>
    /// Adds an additional configuration callback that will be applied to the DI-provided <see cref="TypeAdapterConfig"/>.
    /// The <see cref="IServiceProvider"/> allows resolving services used by mapping configuration.
    /// </summary>
    public static IServiceCollection AddMapsterConfig(this IServiceCollection services, Action<TypeAdapterConfig, IServiceProvider> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddSingleton(new MapsterConfigRegistration(configure));
        return services;
    }

    private static IServiceCollection AddMapsterCore(this IServiceCollection services)
    {
        services.AddSingleton(sp =>
        {
            var config = TypeAdapterConfig.GlobalSettings.Clone();
            var options = sp.GetRequiredService<IOptions<MapsterOptions>>().Value;
            options.ApplyTo(config);

            foreach (var reg in sp.GetServices<MapsterConfigRegistration>())
            {
                reg.Configure(config, sp);
            }

            return config;
        });

        services.AddTransient<IMapper, ServiceMapper>();
        return services;
    }
}
