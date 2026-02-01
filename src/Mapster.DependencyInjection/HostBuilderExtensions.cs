namespace Mapster.DependencyInjection;

public static class HostBuilderExtensions
{

    /// <summary>
    /// Adds Mapster services to the <see cref="IHostBuilder"/> including options binding and <see cref="TypeAdapterConfig"/> registration.
    /// </summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="sectionName">The configuration section name to bind <see cref="MapsterOptions"/> from. Defaults to <see cref="MapsterOptions.DefaultName"/> .</param>
    /// <param name="useGlobalConfig">If <see langword="true"> <see cref="TypeAdapterConfig.GlobalSettings"/> will be used, otherwise a new instance of <see cref="TypeAdapterConfig"/> will be used.</param>
    /// <param name="registerDefaultMapper">If changed to <see langword="false"/>, Mapster's default <see cref="IMapper"/> registration (<see cref="ServiceCollectionExtensions.AddMapster"/>) will not be added.</param>
    /// <param name="configureMappers">Optional delegate to configure additional services.</param>
    /// <returns>The host builder for chaining.</returns>
    /// <remarks>
    /// To apply custom configuration, use the <paramref name="configureMappers"/> delegate to register services before Mapster services are added.<br/>
    /// </remarks>
    public static IHostBuilder UseMapster(
        this IHostBuilder builder,
        string sectionName = "",
        bool useGlobalConfig = true,
        bool registerDefaultMapper = true,
        Action<HostBuilderContext, IServiceCollection>? configureMappers = default)
    {

        return builder.ConfigureServices((ctx, services) =>
        {
            configureMappers?.Invoke(ctx, services);

            if (!ctx.IsRegistered(nameof(UseMapster)))
            {
                services
                    .AddMapsterOptions(ctx, sectionName)
                    .AddTypeAdapterConfig(ctx, useGlobalConfig);

                // check registration here to avoid breaking changes for those who use the current AddMapster extension method directly which doesn't requires HostBuilderContext
                if (registerDefaultMapper && !ctx.IsRegistered(nameof(ServiceCollectionExtensions.AddMapster)))
                {
                    services.AddMapster();
                }
            }
        });
    }

    #region Registration Check to not duplicate registrations
    internal static bool IsRegistered(this HostBuilderContext builder, string registeredKey, bool newIsRegistered = true)
    {
        return builder.Properties.IsRegistered(registeredKey, newIsRegistered);
    }

    internal static bool IsRegistered(this IDictionary<object, object> properties, string registeredKey, bool newIsRegistered = true)
    {
        if (properties.TryGetValue(registeredKey, out var value) &&
            value is bool registeredValue &&
            registeredValue)
        {
            return true;
        }
        properties[registeredKey] = newIsRegistered;
        return false;
    }
    #endregion
}
