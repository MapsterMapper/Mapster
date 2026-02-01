---
uid: Mapster.Packages.DependencyInjection.Configuration
title: "Packages - Configuration Support via DI"
---

## Installation

This package allows you to configure Mapster through `appsettings.json` and HostBuilder integration.

```nuget
PM> Install-Package Mapster.DependencyInjection
```

For basic dependency injection setup, see [Dependency Injection Support](xref:Mapster.Packages.DependencyInjection).

## Usage

### HostBuilder Integration

`UseMapster` simplifies setup by binding `MapsterOptions` from configuration and registering `TypeAdapterConfig`:

```csharp
var host = new HostBuilder()
    .UseMapster() // binds from the "Mapster" section by default
    .Build();
```

By default, `UseMapster` registers `TypeAdapterConfig.GlobalSettings`. To use an isolated `TypeAdapterConfig` instance instead:

```csharp
var host = new HostBuilder()
    .UseMapster(useGlobalConfig: false)
    .Build();
```

To bind from a different configuration section:

```csharp
var host = new HostBuilder()
    .UseMapster(sectionName: "CustomMapster")
    .Build();
```

If you want options/config registration without adding the default `IMapper` (`ServiceMapper`) registration:

```csharp
var host = new HostBuilder()
    .UseMapster(registerDefaultMapper: false)
    .Build();
```

### Configure Mapster in appsettings

You can configure Mapster's global `TypeAdapterConfig` switches through your `appsettings.json` file:

```json
{
  "Mapster": {
    "RequireExplicitMapping": true,
    "AllowImplicitSourceInheritance": true
  }
}
```

These options are bound to `IOptions<MapsterOptions>` and applied to `TypeAdapterConfig` on registration.

> [!NOTE]
> Expect the `MapsterOptions` to match the properties available on `TypeAdapterConfig`.

### Advanced Service Collection Extensions

For finer control, you can use the individual extension methods directly:

#### AddMapsterOptions

Registers `MapsterOptions` and binds them from configuration:

```csharp
services.AddMapsterOptions(context, sectionName: "CustomMapster");
```

Or provide a custom configuration delegate:

```csharp
services.AddMapsterOptions(context, configuration: ctx => ctx.Configuration.GetSection("MySection"));
```

#### AddTypeAdapterConfig

Registers `TypeAdapterConfig` as a singleton with several configuration options:

| Parameter         | Default | Description                                  |
| ----------------- | ------- | -------------------------------------------- |
| `useGlobalConfig` | `true`  | Use `TypeAdapterConfig.GlobalSettings`       |
| `useExisting`     | `false` | Use existing registered config as base       |
| `readFromOptions` | `true`  | Apply bound `MapsterOptions` to the config   |
| `configure`       | `null`  | Additional configuration delegate            |

> [!IMPORTANT]
> Configuration options are applied in the order of the parameters. If `useExisting` is `true` and an existing `TypeAdapterConfig` is found, it will replace the config created by `useGlobalConfig`.

```csharp
services.AddTypeAdapterConfig(context, useGlobalConfig: false, configure: config =>
{
    config.RequireExplicitMapping = true;
    config.NewConfig<Source, Destination>()
        .Map(dest => dest.FullName, src => $"{src.FirstName} {src.LastName}");
    return config;
});
```

Complete example with `UseMapster`:

```csharp
var host = new HostBuilder()
    .UseMapster(useGlobalConfig: false, configureMappers: (ctx, services) =>
    {
        services.AddTypeAdapterConfig(ctx, configure: config =>
        {
            config.NewConfig<Source, Destination>()
                .Map(dest => dest.FullName, src => $"{src.FirstName} {src.LastName}");
            return config;
        });
    })
    .Build();
```

For service injection during mapping, see [Dependency Injection Support](xref:Mapster.Packages.DependencyInjection).
