namespace Mapster.DependencyInjection;

public static class ServiceCollectionExtensions
{
	/// <summary>
	/// Adds Mapster mapping services to the specified <see cref="IServiceCollection"/> and optionally configures <see cref="MapsterOptions"/>.
	/// </summary>
	/// <param name="services">The <see cref="IServiceCollection"/> to which the Mapster services will be added. Cannot be null.</param>
	/// <param name="configure">An optional delegate to configure <see cref="MapsterOptions"/>. If null, default options are used.</param>
	/// <returns>The same <see cref="IServiceCollection"/> instance so that additional calls can be chained.</returns>
	public static IServiceCollection AddMapster(
		this IServiceCollection services,
		Action<MapsterOptions>? configure = null)
	{
		if (configure is not null)
		{
			services.Configure(configure);
		}
		else
		{
			services.AddOptions<MapsterOptions>();
		}

		return services.AddMapster();
	}

	/// <summary>
	/// Adds Mapster <see cref="IOptions{TOptions}"/> to the specified <see cref="IServiceCollection"> using configuration, calls <see cref="AddMapster(IServiceCollection)"/> to register Mapster services.
	/// </summary>
	/// <param name="services">The <see cref="IServiceCollection"/> to insert <paramref name="configuration"/> to.</param>
	/// <param name="configuration">The <see cref="IConfiguration"/> instance to use</param>
	/// <returns>The same <see cref="IServiceCollection"/> as provided, added up with the Mapster services</returns>
	public static IServiceCollection AddMapster(this IServiceCollection services, IConfiguration configuration)
	{
		services.Configure<MapsterOptions>(configuration);
		return services.AddMapster();
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