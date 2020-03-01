![Mapster Icon](https://cloud.githubusercontent.com/assets/5763993/26522718/d16f3e42-4330-11e7-9b78-f8c7402624e7.png)

## Mapster - The Mapper of Your Domain
Writing mapping method is machine job. Do not waste your time, let Mapster do it.

### Get it
```
PM> Install-Package Mapster
```

### Basic usage
#### Mapping to a new object
Mapster creates the destination object and maps values to it.

```csharp
var destObject = sourceObject.Adapt<Destination>();
```

#### Mapping to an existing object
You make the object, Mapster maps to the object.

```csharp
sourceObject.Adapt(destObject);
```

#### Queryable Extensions
Mapster also provides extensions to map queryables.

```csharp
using (MyDbContext context = new MyDbContext())
{
    // Build a Select Expression from DTO
    var destinations = context.Sources.ProjectToType<Destination>().ToList();

    // Versus creating by hand:
    var destinations = context.Sources.Select(c => new Destination {
        Id = p.Id,
        Name = p.Name,
        Surname = p.Surname,
        ....
    })
    .ToList();
}
```

### What's new in Mapster 5.0
- [Generate dynamic proxy for interface](https://github.com/MapsterMapper/Mapster/blob/master/src/Mapster.Tests/WhenMappingToInterface.cs)
- [UseDestinationValue](https://github.com/MapsterMapper/Mapster/wiki/Mapping-readonly-prop)
- [Null propagating for property path](https://github.com/MapsterMapper/Mapster/wiki/Custom-mapping#null-propagation)
- [Support multiple sources](https://github.com/MapsterMapper/Mapster/wiki/Custom-mapping#multiple-sources)
- New plugins
  - [Async support](https://github.com/MapsterMapper/Mapster/wiki/Async)
  - [Inject service to mapping](https://github.com/MapsterMapper/Mapster/wiki/Dependency-Injection)
  - [EF Core support](https://github.com/MapsterMapper/Mapster/wiki/EF-6-&-EF-Core)

### Why Mapster?
#### Performance & Memory efficient
Mapster was designed to be efficient on both speed and memory. You could gain 5x faster while using only 1/3 of memory.
And you could gain to 12x faster with
- [Roslyn Compiler](https://github.com/MapsterMapper/Mapster/wiki/Debugging)
- [FEC](https://github.com/MapsterMapper/Mapster/wiki/FastExpressionCompiler)
- [Mapster CodeGen](https://github.com/MapsterMapper/Mapster/wiki/CodeGen)

|                    Method |           Mean |       StdDev |        Error |       Gen 0 | Gen 1 | Gen 2 |  Allocated |
|-------------------------- |---------------:|-------------:|-------------:|------------:|------:|------:|-----------:|
|           'Mapster 5.0.0' | 141.84 ms | 0.931 ms | 1.408 ms | 31250.0000 |     - |     - | 125.12 MB |
|  'Mapster 5.0.0 (Roslyn)' |  60.48 ms | 1.186 ms | 1.993 ms | 31222.2222 |     - |     - | 125.12 MB |
|     'Mapster 5.0.0 (FEC)' |  58.17 ms | 0.231 ms | 0.442 ms | 29714.2857 |     - |     - | 119.02 MB |
| 'Mapster 5.0.0 (Codegen)' |  51.56 ms | 0.312 ms | 0.524 ms | 31200.0000 |     - |     - | 125.12 MB |
|     'ExpressMapper 1.9.1' | 299.05 ms | 2.081 ms | 3.146 ms | 60000.0000 |     - |     - | 241.85 MB |
|        'AutoMapper 9.0.0' | 708.06 ms | 3.398 ms | 5.137 ms | 91000.0000 |     - |     - | 364.69 MB |



#### Step into debugging

[Step-into debugging](https://github.com/MapsterMapper/Mapster/wiki/Debugging) lets you debug your mapping and inspect values as same as its your code.
![image](https://cloud.githubusercontent.com/assets/5763993/26521773/180427b6-431b-11e7-9188-10c01fa5ba5c.png)

#### Code Generation
[Mapster CodeGen](https://github.com/MapsterMapper/Mapster/wiki/CodeGen) lets you do mapping with
- Validate mapping at compile time
- Getting raw performance
- Seeing your mapping code & debugging
- Finding usage of your models' properties

### Change logs
https://github.com/MapsterMapper/Mapster/releases

### Usages
https://github.com/MapsterMapper/Mapster/wiki

### Acknowledgements

[JetBrains](https://www.jetbrains.com/?from=Mapster) kindly provides Mapster with a free open-source licence for their Resharper and Rider.
- **Resharper** makes Visual Studio a much better IDE
- **Rider** is fast & powerful cross platform .NET IDE

![image](https://upload.wikimedia.org/wikipedia/commons/thumb/1/1a/JetBrains_Logo_2016.svg/121px-JetBrains_Logo_2016.svg.png)
