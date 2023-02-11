using Microsoft.Extensions.DependencyInjection;

namespace Mapster.Tool.Tests;

public class TestBase
{
    private readonly IServiceScopeFactory _scopeFactory;

    public TestBase()
    {
        var services = ConfigureServiceCollection();
        using var scope = services.BuildServiceProvider().CreateScope();
        _scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
    }

    private ServiceCollection ConfigureServiceCollection()
    {
        ServiceCollection services = new();
        services.Scan(selector => selector
            .FromCallingAssembly()
            .AddClasses()
            .AsMatchingInterface()
            .WithSingletonLifetime());
        return services;
    }

    protected TInterface GetMappingInterface<TInterface>()
    {
        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetService(typeof(TInterface));
        if (service == null)
        {
            throw new Exception($"Service of type {typeof(TInterface).Name} not found!");
        }

        return (TInterface)service;
    }
}