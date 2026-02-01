namespace Mapster.DependencyInjection.Tests;

[TestClass]
public class HostBuilderExtensionsTests
{
    [TestMethod]
    public void UseMapster_WithConfigureMappers_AllowsCustomTypeAdapterConfigConfiguration()
    {
        // Arrange
        var host = new HostBuilder()
            .UseMapster(
                configureMappers: (ctx, services) =>
                {
                    services.AddTypeAdapterConfig(ctx, useGlobalConfig: false, configure: config =>
                    {
                        config.NewConfig<ValueSource, ValueDest>()
                            .Map(dest => dest.Value, src => src.Value * 2);
                        return config;
                    });
                })
            .Build();

        // Act
        var mapper = host.Services.GetRequiredService<IMapper>();
        var dest = mapper.Map<ValueDest>(new ValueSource { Value = 5 });

        // Assert
        dest.Value.ShouldBe(10);
    }

    [TestMethod]
    public void UseMapster_Default_RegistersServices()
    {
        // Arrange
        var host = new HostBuilder()
            .UseMapster()
            .Build();

        // Act + Assert
        host.Services.GetRequiredService<IOptions<MapsterOptions>>().ShouldNotBeNull();
        host.Services.GetRequiredService<TypeAdapterConfig>().ShouldBe(TypeAdapterConfig.GlobalSettings);
        host.Services.GetRequiredService<IMapper>().ShouldBeOfType<ServiceMapper>();
    }

    [TestMethod]
    public void UseMapster_WhenCalledTwice_RunsConfigureMappersEachTime_ButRegistersCoreServicesOnce()
    {
        // Arrange
        var configureMappersCalls = 0;
        var typeAdapterConfigRegistrations = 0;
        var mapperRegistrations = 0;
        var mapsterOptionsConfigureRegistrations = 0;

        var host = new HostBuilder()
            .UseMapster(
                configureMappers: (_, _) => configureMappersCalls++)
            .UseMapster(
                configureMappers: (_, _) => configureMappersCalls++)
            .ConfigureServices((_, services) =>
            {
                typeAdapterConfigRegistrations = services.Count(d => d.ServiceType == typeof(TypeAdapterConfig));
                mapperRegistrations = services.Count(d => d.ServiceType == typeof(IMapper));
                mapsterOptionsConfigureRegistrations = services.Count(d => d.ServiceType == typeof(IConfigureOptions<MapsterOptions>));
            })
            .Build();

        // Act + Assert
        configureMappersCalls.ShouldBe(2);

        typeAdapterConfigRegistrations.ShouldBe(1);
        mapperRegistrations.ShouldBe(1);
        mapsterOptionsConfigureRegistrations.ShouldBe(1);

        host.Services.GetRequiredService<TypeAdapterConfig>().ShouldNotBeNull();
    }

    [TestMethod]
    public void UseMapster_WhenUseGlobalConfigFalse_RegistersNewTypeAdapterConfigInstance()
    {
        // Arrange
        var host = new HostBuilder()
            .UseMapster(useGlobalConfig: false)
            .Build();

        // Act
        var config = host.Services.GetRequiredService<TypeAdapterConfig>();

        // Assert
		ReferenceEquals(config, TypeAdapterConfig.GlobalSettings).ShouldBeFalse();
    }

    [TestMethod]
    public void UseMapster_WhenRegisterMapsterSingletonFalse_DoesNotRegisterIMapper()
    {
        // Arrange
        var host = new HostBuilder()
            .UseMapster(registerDefaultMapper: false)
            .Build();

        // Act + Assert
        host.Services.GetService<IMapper>().ShouldBeNull();
        host.Services.GetRequiredService<TypeAdapterConfig>().ShouldNotBeNull();
        host.Services.GetRequiredService<IOptions<MapsterOptions>>().ShouldNotBeNull();
    }

    private sealed class ValueSource
    {
        public int Value { get; set; }
    }

    private sealed class ValueDest
    {
        public int Value { get; set; }
    }
}
