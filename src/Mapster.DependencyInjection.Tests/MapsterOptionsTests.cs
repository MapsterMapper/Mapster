namespace Mapster.DependencyInjection.Tests;

[TestClass]
public class MapsterOptionsTests
{
    [TestMethod]
    public void UseMapster_BindsDefaultSection()
    {
        var host = new HostBuilder()
            .ConfigureAppConfiguration(cfg =>
            {
                cfg.AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("Mapster:RequireExplicitMapping", "true"),
                });
            })
            .UseMapster(useGlobalConfig: false)
            .Build();

        var options = host.Services.GetRequiredService<IOptions<MapsterOptions>>().Value;
        options.RequireExplicitMapping.ShouldBe(true);

        var config = host.Services.GetRequiredService<TypeAdapterConfig>();
        ReferenceEquals(config, TypeAdapterConfig.GlobalSettings).ShouldBeFalse();
        config.RequireExplicitMapping.ShouldBe(true);
    }

    [TestMethod]
    public void UseMapster_WithSectionName_BindsCustomSection()
    {
        var host = new HostBuilder()
            .ConfigureAppConfiguration(cfg =>
            {
                cfg.AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("CustomMapster:RequireDestinationMemberSource", "true"),
                });
            })
            .UseMapster(sectionName: "CustomMapster", useGlobalConfig: false)
            .Build();

        var options = host.Services.GetRequiredService<IOptions<MapsterOptions>>().Value;
        options.RequireDestinationMemberSource.ShouldBe(true);
    }

    [TestMethod]
    public void UseMapster_WhenNoSection_UsesDefaultOptionsValues()
    {
        using var host = new HostBuilder()
            // intentionally no appsettings / Mapster section
            .UseMapster(sectionName: "", useGlobalConfig: false)
            .Build();

        var options = host.Services.GetRequiredService<IOptions<MapsterOptions>>().Value;
        options.RequireDestinationMemberSource.ShouldBeFalse();
        options.RequireExplicitMapping.ShouldBeFalse();
        options.RequireExplicitMappingPrimitive.ShouldBeFalse();
        options.AllowImplicitDestinationInheritance.ShouldBeFalse();
        options.AllowImplicitSourceInheritance.ShouldBeTrue();
        options.SelfContainedCodeGeneration.ShouldBeFalse();

        host.Services.GetRequiredService<TypeAdapterConfig>().ShouldNotBeNull();
        host.Services.GetRequiredService<IMapper>().ShouldBeOfType<ServiceMapper>();
    }
}

