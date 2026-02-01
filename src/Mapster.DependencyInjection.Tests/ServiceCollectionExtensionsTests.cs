namespace Mapster.DependencyInjection.Tests;

[TestClass]
public class ServiceCollectionExtensionsTests
{
	[TestMethod]
	public void AddMapster_Parameterless_RegistersServiceMapper_WhenTypeAdapterConfigRegistered()
	{
		var services = new ServiceCollection();
		services.AddSingleton(TypeAdapterConfig.GlobalSettings);
		services.AddMapster();

		using var sp = services.BuildServiceProvider();

		var mapper = sp.GetRequiredService<IMapper>();
		mapper.ShouldNotBeNull();
		mapper.ShouldBeOfType<ServiceMapper>();

		var config = sp.GetRequiredService<TypeAdapterConfig>();
		config.ShouldBe(TypeAdapterConfig.GlobalSettings);
	}

	[TestMethod]
	public void AddMapsterOptions_DefaultSection_BindsOptions()
	{
		using var host = new HostBuilder()
			.ConfigureAppConfiguration(cfg =>
			{
				cfg.AddInMemoryCollection(new[]
				{
					new KeyValuePair<string, string?>("Mapster:RequireExplicitMapping", "true"),
					new KeyValuePair<string, string?>("Mapster:AllowImplicitSourceInheritance", "false"),
				});
			})
			.ConfigureServices((ctx, services) =>
			{
				services.AddMapsterOptions(ctx);
			})
			.Build();

		var options = host.Services.GetRequiredService<IOptions<MapsterOptions>>().Value;
		options.RequireExplicitMapping.ShouldBe(true);
		options.AllowImplicitSourceInheritance.ShouldBe(false);
	}

	[TestMethod]
	public void AddMapsterOptions_WithConfigurationDelegate_BindsOptions()
	{
		using var host = new HostBuilder()
			.ConfigureAppConfiguration(cfg =>
			{
				cfg.AddInMemoryCollection(new[]
				{
					new KeyValuePair<string, string?>("Custom:RequireExplicitMapping", "true"),
				});
			})
			.ConfigureServices((ctx, services) =>
			{
				services.AddMapsterOptions(
					ctx,
					configuration: c => c.Configuration.GetSection("Custom"));
			})
			.Build();

		host.Services.GetRequiredService<IOptions<MapsterOptions>>().Value.RequireExplicitMapping.ShouldBe(true);
	}

	[TestMethod]
	public void AddTypeAdapterConfig_AppliesBoundOptions_AndConfigureDelegate()
	{
		using var host = new HostBuilder()
			.ConfigureAppConfiguration(cfg =>
			{
				cfg.AddInMemoryCollection(new[]
				{
					new KeyValuePair<string, string?>("Mapster:RequireExplicitMapping", "true"),
				});
			})
			.ConfigureServices((ctx, services) =>
			{
				services
					.AddMapsterOptions(ctx)
					.AddTypeAdapterConfig(ctx, useGlobalConfig: false, configure: config =>
					{
						config.RequireDestinationMemberSource = true;
						return config;
					})
					.AddMapster();
			})
			.Build();

		var config = host.Services.GetRequiredService<TypeAdapterConfig>();
		ReferenceEquals(config, TypeAdapterConfig.GlobalSettings).ShouldBeFalse();
		config.RequireExplicitMapping.ShouldBe(true);
		config.RequireDestinationMemberSource.ShouldBe(true);
	}
}
