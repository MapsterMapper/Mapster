namespace Mapster.DependencyInjection;

public static class ServiceCollectionExtensions
{
	/// <summary>
	/// Adds <see cref="MapsterOptions"/> to the specified <see cref="IServiceCollection"/>.
	/// </summary>
	/// <param name="services">The <see cref="IServiceCollection"/> to which the Mapster options will be added.</param>
	/// <param name="context">The <see cref="HostBuilderContext"/> providing context for the host builder.</param>
	/// <param name="sectionName">The configuration section name to bind <see cref="MapsterOptions"/> from. Defaults to <see cref="MapsterOptions.DefaultName"/> .</param>
	/// <param name="configuration">A delegate to provide a custom <see cref="IConfiguration"/> instance for binding.</param>
	/// <returns>The same <see cref="IServiceCollection"/> instance so that additional calls can be chained.</returns>
	public static IServiceCollection AddMapsterOptions(
		this IServiceCollection services,
		HostBuilderContext context,
		string sectionName = "",
	 	Func<HostBuilderContext, IConfiguration>? configuration = default)
	{
		if (context.IsRegistered(nameof(AddMapsterOptions)))
		{
			return services;
		}
		if (configuration is null)
		{
			if (sectionName is not { Length: > 0 })
			{
				sectionName = MapsterOptions.DefaultName;
			}
			configuration = ctx => ctx.Configuration.GetSection(sectionName);
		}
		var configSection = configuration.Invoke(context);
		return services.Configure<MapsterOptions>(configSection);
	}
	/// <summary>
	/// Adds Mapster <see cref="TypeAdapterConfig"/> as Singleton to the specified <see cref="IServiceCollection"/>.
	/// </summary>
	/// <param name="services">The <see cref="IServiceCollection"/> to which the Mapster services will be added. Cannot be null.</param>
	/// <param name="useGlobalConfig">If <see langword="true"> <see cref="TypeAdapterConfig.GlobalSettings"/> will be used, otherwise a new instance of <see cref="TypeAdapterConfig"/> will be used.</param>
	/// <param name="useExisting">If true, uses an existing registered <see cref="TypeAdapterConfig"/> instance as base if available.</param>
	/// <param name="readFromOptions">If true, applies the configured <see cref="MapsterOptions"/> to the <see cref="TypeAdapterConfig"/>.</param>
	/// <param name="configure">Delegate to configure <see cref="MapsterOptions"/>. If empty, the configuration defined by the previous Settings will be used.</param>
	/// <returns>The same <see cref="IServiceCollection"/> instance so that additional calls can be chained.</returns>
	/// <remarks>
	/// The configuration options will be applyed in the order of parameters in the method signature.<br/>
	/// Important: If <paramref name="useExisting"/> is <see langword="true"/> and an existing <see cref="TypeAdapterConfig"/> is found, this will replace the config created by <paramref name="useGlobalConfig"/>!<br/>
	/// </remarks>
	public static IServiceCollection AddTypeAdapterConfig(
		this IServiceCollection services,
		HostBuilderContext context,
		bool useGlobalConfig = true,
		bool useExisting = false,
		bool readFromOptions = true,
		Func<TypeAdapterConfig, TypeAdapterConfig>? configure = default)
	{
		if (context.IsRegistered(nameof(AddTypeAdapterConfig)))
		{
			return services;
		}
		return services.AddSingleton(serviceProvider =>
		{
			var config = useGlobalConfig ? TypeAdapterConfig.GlobalSettings : new TypeAdapterConfig();

			if (useExisting && serviceProvider.GetService<TypeAdapterConfig>() is TypeAdapterConfig existingConfig)
			{
				config = existingConfig; // TODO: Find a way to apply to existing config instead of replacing it
			}

			if (readFromOptions && serviceProvider.GetService<IOptions<MapsterOptions>>()?.Value is MapsterOptions options)
			{
				config = options.ApplyTo(config);
			}

			return configure?.Invoke(config) ?? config;
		});

	}

	/// <summary>
	/// Adds Mapster mapping services to the specified <see cref="IServiceCollection"/>.
	/// </summary>
	/// <param name="services">The <see cref="IServiceCollection"/> to which the Mapster services will be added.</param>
	/// <returns>The same <see cref="IServiceCollection"/> instance so that additional calls can be chained.</returns>
	public static IServiceCollection AddMapster(this IServiceCollection services)
	{
		services.TryAddTransient<IMapper, ServiceMapper>();
		return services;
	}
}