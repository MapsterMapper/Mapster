![Mapster Icon](https://raw.githubusercontent.com/MapsterMapper/Mapster/master/docs/images/mapster-logo.svg)

# Mapster - The Mapper of Your Domain

Writing mapping methods is a machine job. Do not waste your time, let Mapster do it.

## NuGet Sources

### NuGet Packages

| Package | Stable | Pre-release |
|---------|--------|-------------|
| **Mapster** | [![Mapster](https://img.shields.io/nuget/v/Mapster.svg?label=Mapster&color=informational)](https://www.nuget.org/packages/Mapster/latest) | [![Mapster](https://img.shields.io/nuget/vpre/Mapster.svg?label=Mapster&color=orange)](https://www.nuget.org/packages/Mapster) |
| **Mapster.Core** | [![Mapster.Core](https://img.shields.io/nuget/v/Mapster.Core.svg?label=Mapster.Core&color=informational)](https://www.nuget.org/packages/Mapster.Core/latest) | [![Mapster.Core](https://img.shields.io/nuget/vpre/Mapster.Core.svg?label=Mapster.Core&color=orange)](https://www.nuget.org/packages/Mapster.Core) |
| **Mapster.DependencyInjection** | [![Mapster.DependencyInjection](https://img.shields.io/nuget/v/Mapster.DependencyInjection.svg?label=Mapster.DependencyInjection&color=informational)](https://www.nuget.org/packages/Mapster.DependencyInjection/latest) | [![Mapster.DependencyInjection](https://img.shields.io/nuget/vpre/Mapster.DependencyInjection.svg?label=Mapster.DependencyInjection&color=orange)](https://www.nuget.org/packages/Mapster.DependencyInjection) |
| **Mapster.EFCore** | [![Mapster.EFCore](https://img.shields.io/nuget/v/Mapster.EFCore.svg?label=Mapster.EFCore&color=informational)](https://www.nuget.org/packages/Mapster.EFCore/latest) | [![Mapster.EFCore](https://img.shields.io/nuget/vpre/Mapster.EFCore.svg?label=Mapster.EFCore&color=orange)](https://www.nuget.org/packages/Mapster.EFCore) |
| **Mapster.EF6** | [![Mapster.EF6](https://img.shields.io/nuget/v/Mapster.EF6.svg?label=Mapster.EF6&color=informational)](https://www.nuget.org/packages/Mapster.EF6/latest) | [![Mapster.EF6](https://img.shields.io/nuget/vpre/Mapster.EF6.svg?label=Mapster.EF6&color=orange)](https://www.nuget.org/packages/Mapster.EF6) |
| **Mapster.JsonNet** | [![Mapster.JsonNet](https://img.shields.io/nuget/v/Mapster.JsonNet.svg?label=Mapster.JsonNet&color=informational)](https://www.nuget.org/packages/Mapster.JsonNet/latest) | [![Mapster.JsonNet](https://img.shields.io/nuget/vpre/Mapster.JsonNet.svg?label=Mapster.JsonNet&color=orange)](https://www.nuget.org/packages/Mapster.JsonNet) |
| **Mapster.Immutable** | [![Mapster.Immutable](https://img.shields.io/nuget/v/Mapster.Immutable.svg?label=Mapster.Immutable&color=informational)](https://www.nuget.org/packages/Mapster.Immutable/latest) | [![Mapster.Immutable](https://img.shields.io/nuget/vpre/Mapster.Immutable.svg?label=Mapster.Immutable&color=orange)](https://www.nuget.org/packages/Mapster.Immutable) |
| **Mapster.Diagnostics** | [![Mapster.Diagnostics](https://img.shields.io/nuget/v/Mapster.Diagnostics.svg?label=Mapster.Diagnostics&color=informational)](https://www.nuget.org/packages/Mapster.Diagnostics/latest) |  |
| **ExpressionDebugger** | [![ExpressionDebugger](https://img.shields.io/nuget/v/ExpressionDebugger.svg?label=ExpressionDebugger&color=informational)](https://www.nuget.org/packages/ExpressionDebugger/latest) |  |

### DotNet Tools

| Tool | Stable | Pre-release |
|------|--------|-------------|
| **Mapster.Tool** | [![Mapster.Tool](https://img.shields.io/nuget/v/Mapster.Tool.svg?label=Mapster.Tool&color=informational)](https://www.nuget.org/packages/Mapster.Tool/latest) | [![Mapster.Tool](https://img.shields.io/nuget/vpre/Mapster.Tool.svg?label=Mapster.Tool&color=orange)](https://www.nuget.org/packages/Mapster.Tool) |


## Installation

Install Mapster with the NuGet CLI:

```Powershell
Install-Package Mapster
```

Or use the .NET core CLI to install Mapster:

```bash
dotnet add package Mapster --project <PROJECT_NAME>
```

## Basic usage

### Mapping to a new object

Mapster creates the destination object and maps values to it.

```csharp
var destObject = sourceObject.Adapt<Destination>();
```

### Mapping to an existing object

You create the object, Mapster maps to the object.

```csharp
sourceObject.Adapt(destObject);
```

### Use Mapster with Dependency Injection

You can get your `IMapper` instance via dependency injection, so you do not have to change code, when migrating to mapster from automapper!

**Just add Mapster to service collection:**

```csharp
services.AddMapster();
```

**And use it with DI in your Project:**

```csharp
public class Test
{
    public Test(IMapper mapper)
    {
        var sourceObject = mapper.Adapt<Destination>();
    }
}
```

### Queryable Extensions

Mapster also provides extensions to map queryables.

```csharp
using (MyDbContext context = new MyDbContext())
{
    // Build a Select Expression from DTO
    var destinations = context.Sources.ProjectToType<Destination>().ToList();

    // Versus creating by hand:
    var destinations = context.Sources.Select(c => new Destination {
        Id = c.Id,
        Name = c.Name,
        Surname = c.Surname,
        ....
    })
    .ToList();
}
```

### Generating models & mappers

No need to write your own DTO classes. Mapster provides [Mapster.Tool](https://mapstermapper.github.io/Mapster/articles/tools/mapster-tool/Mapster-Tool-Overview.html) to help you generating models. And if you would like to have explicit mapping, Mapster also generates mapper class for you.

```csharp
[AdaptTo("[name]Dto"), GenerateMapper]
public class Student {
    ...
}
```

Then Mapster will generate for you:

```csharp
public class StudentDto {
    ...
}
public static class StudentMapper {
    public static StudentDto AdaptToDto(this Student poco) { ... }
    public static StudentDto AdaptTo(this Student poco, StudentDto dto) { ... }
    public static Expression<Func<Student, StudentDto>> ProjectToDto => ...
}
```

## What's new

- [Fluent API for code generation](https://mapstermapper.github.io/Mapster/articles/tools/mapster-tool/Fluent-API-Code-generation.html)
- [Automatically generate mapping code on build](https://mapstermapper.github.io/Mapster/articles/tools/mapster-tool/Mapster-Tool-Overview.html)
- [Define setting to nested mapping](https://mapstermapper.github.io/Mapster/articles/configuration/Config-for-nested-mapping.html)
- [`ISet`, `IDictionary`, `IReadOnlyDictionary` support](https://mapstermapper.github.io/Mapster/articles/mapping/data-types/Collections.html)
- [`EmptyCollectionIfNull`, `CreateNewIfNull` DestinationTransform](https://mapstermapper.github.io/Mapster/articles/settings/Setting-values.html#transform-value)
- [Several fixes](https://github.com/MapsterMapper/Mapster/releases/)
- New plugins
  - [Immutable collection support](https://mapstermapper.github.io/Mapster/articles/packages/Immutable.html)

## Why Mapster?

### Performance & Memory efficient

Mapster was designed to be efficient on both speed and memory. The repository includes a benchmark project in [`src/Benchmark`](https://github.com/MapsterMapper/Mapster/tree/master/src/Benchmark) that compares the local Mapster build, its compiler variants, and other modern mapping libraries like AutoMapper, Mapperly, and Facet.

- [Roslyn Compiler](https://mapstermapper.github.io/Mapster/articles/packages/ExpressionDebugging.html)
- [FEC](https://mapstermapper.github.io/Mapster/articles/packages/FastExpressionCompiler.html)
- Code generation
- Facet
- Mapperly

The snapshot below shows the `FlatTypes` scenario (`Person -> PersonDTO`), a best-case DTO with simple property-to-property mapping and no nested objects or collections.

> [!NOTE]
> More complex object shapes can change the relative results. See the [complete benchmark results](https://mapstermapper.github.io/Mapster/articles/benchmarks.html) for `ComplexTypes`, `RecursiveTypes`, and `TotalAllTypes`.

| Method                              | MapOperations | Mean      | StdDev    | Error     | Ns/Map | Ratio | Gen0 | Allocated | Alloc Ratio | Bytes/Map |
|------------------------------------ |-------------- |----------:|----------:|----------:|-------:|------:|-----:|----------:|------------:|----------:|
| `Mapster 10.0.8`                    | 1000000       |  6.849 ms | 0.6851 ms | 1.0358 ms |  6.849 |  1.01 | 4781 |  76.29 MB |        1.00 |        80 |
| `Mapster 10.0.8 (Roslyn)`           | 1000000       |  6.579 ms | 0.2782 ms | 0.4206 ms |  6.579 |  0.97 | 4781 |  76.29 MB |        1.00 |        80 |
| `Mapster 10.0.8 (FEC)`              | 1000000       |  6.549 ms | 0.9130 ms | 1.3803 ms |  6.549 |  0.97 | 4781 |  76.29 MB |        1.00 |        80 |
| `Mapster 10.0.8 (Codegen)`          | 1000000       |  5.868 ms | 0.3266 ms | 0.5488 ms |  5.868 |  0.86 | 4781 |  76.29 MB |        1.00 |        80 |
| `AutoMapper 14.0.0`                 | 1000000       | 29.645 ms | 0.8963 ms | 1.5062 ms | 29.645 |  4.37 | 4750 |  76.29 MB |        1.00 |        80 |
| `Facet 6.5.5`                       | 1000000       |  7.801 ms | 1.0231 ms | 1.5467 ms |  7.801 |  1.15 | 8601 | 137.33 MB |        1.80 |       144 |
| `Facet 6.5.5 (Compiled Projection)` | 1000000       |  5.508 ms | 0.7064 ms | 1.0679 ms |  5.508 |  0.81 | 4781 |  76.29 MB |        1.00 |        80 |
| `Mapperly 4.3.1`                    | 1000000       |  6.521 ms | 0.8369 ms | 1.2652 ms |  6.521 |  0.96 | 4781 |  76.29 MB |        1.00 |        80 |

### Step into debugging

[Step-into debugging](https://mapstermapper.github.io/Mapster/articles/packages/ExpressionDebugging.html) lets you debug your mapping and inspect values just like your code.

![inspect-generated-source-code-while-debugging](https://cloud.githubusercontent.com/assets/5763993/26521773/180427b6-431b-11e7-9188-10c01fa5ba5c.png)

### Code Generation

Code generation allows you to

- Validate mapping at compile time
- Getting raw performance
- Seeing your mapping code & debugging
- Finding usage of your models' properties

There are currently two tools which you can choose based on your preferences.

- [Mapster.Tool](https://mapstermapper.github.io/Mapster/articles/tools/mapster-tool/Mapster-Tool-Overview.html) _**NEW!**_
- [TextTemplate](https://mapstermapper.github.io/Mapster/articles/tools/TextTemplate.html)

## Change logs

https://github.com/MapsterMapper/Mapster/releases

## Usage Guides with Translations

- [English](https://github.com/MapsterMapper/Mapster/wiki)
- [中文文档](https://github.com/rivenfx/Mapster-docs) (sp thx to [@staneee](https://github.com/staneee))

## Acknowledgements

[JetBrains](https://www.jetbrains.com/?from=Mapster) kindly provides Mapster with a free open-source licence for their Resharper and Rider.

- **Resharper** makes Visual Studio a much better IDE
- **Rider** is fast & powerful cross platform .NET IDE
