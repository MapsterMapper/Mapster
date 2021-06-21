![Mapster Icon](https://cloud.githubusercontent.com/assets/5763993/26522718/d16f3e42-4330-11e7-9b78-f8c7402624e7.png)

## Mapster - The Mapper of Your Domain
Writing mapping methods is a machine job. Do not waste your time, let Mapster do it.

[![NuGet](https://img.shields.io/nuget/v/Mapster.svg)](https://www.nuget.org/packages/Mapster)

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
You create the object, Mapster maps to the object.

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

#### Generating models & mappers
No need to write your own DTO classes. Mapster provides [Mapster.Tool](https://github.com/MapsterMapper/Mapster/wiki/Mapster.Tool) to help you generating models. And if you would like to have explicit mapping, Mapster also generates mapper class for you.

```csharp
[AdaptTo("[name]Dto"), GenerateMapper]
public class Student {
    ...
}
```

Then Mapster will generate:
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

### What's new
- [Fluent API for code generation](https://github.com/MapsterMapper/Mapster/wiki/Fluent-API-Code-generation)
- [Automatically generate mapping code on build](https://github.com/MapsterMapper/Mapster/wiki/Mapster.Tool)
- [Define setting to nested mapping](https://github.com/MapsterMapper/Mapster/wiki/Config-for-nested-mapping)
- [`ISet`, `IDictionary`, `IReadOnlyDictionary` support](https://github.com/MapsterMapper/Mapster/wiki/Data-types#collections)
- [`EmptyCollectionIfNull`, `CreateNewIfNull` DestinationTransform](https://github.com/MapsterMapper/Mapster/wiki/Setting-values#transform-value)
- [Several fixes](https://github.com/MapsterMapper/Mapster/releases/)
- New plugins
  - [Immutable collection support](https://github.com/MapsterMapper/Mapster/wiki/Immutable)

### Why Mapster?
#### Performance & Memory efficient
Mapster was designed to be efficient on both speed and memory. You could gain a 4x performance improvement whilst using only 1/3 of memory.
And you could gain up to 12x faster performance with
- [Roslyn Compiler](https://github.com/MapsterMapper/Mapster/wiki/Debugging)
- [FEC](https://github.com/MapsterMapper/Mapster/wiki/FastExpressionCompiler)
- Code generation

|                    Method |      Mean |    StdDev |     Error |      Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------------------- |----------:|----------:|----------:|-----------:|------:|------:|----------:|
|           'Mapster 6.0.0' | 108.59 ms |  1.198 ms |  1.811 ms | 31000.0000 |     - |     - | 124.36 MB |
|  'Mapster 6.0.0 (Roslyn)' |  38.45 ms |  0.494 ms |  0.830 ms | 31142.8571 |     - |     - | 124.36 MB |
|     'Mapster 6.0.0 (FEC)' |  37.03 ms |  0.281 ms |  0.472 ms | 29642.8571 |     - |     - | 118.26 MB |
| 'Mapster 6.0.0 (Codegen)' |  34.16 ms |  0.209 ms |  0.316 ms | 31133.3333 |     - |     - | 124.36 MB |
|     'ExpressMapper 1.9.1' | 205.78 ms |  5.357 ms |  8.098 ms | 59000.0000 |     - |     - | 236.51 MB |
|       'AutoMapper 10.0.0' | 420.97 ms | 23.266 ms | 35.174 ms | 87000.0000 |     - |     - | 350.95 MB |



#### Step into debugging

[Step-into debugging](https://github.com/MapsterMapper/Mapster/wiki/Debugging) lets you debug your mapping and inspect values just like your code.
![image](https://cloud.githubusercontent.com/assets/5763993/26521773/180427b6-431b-11e7-9188-10c01fa5ba5c.png)

#### Code Generation
Code generation allows you to
- Validate mapping at compile time
- Getting raw performance
- Seeing your mapping code & debugging
- Finding usage of your models' properties

There are currently two tools which you can choose based on your preferences.
* [Mapster.Tool](https://github.com/MapsterMapper/Mapster/wiki/Mapster.Tool) NEW!
* [TextTemplate](https://github.com/MapsterMapper/Mapster/wiki/TextTemplate)

### Change logs
https://github.com/MapsterMapper/Mapster/releases

### Usages
* [English](https://github.com/MapsterMapper/Mapster/wiki)
* [中文文档](https://github.com/rivenfx/Mapster-docs) (sp thx to [@staneee](https://github.com/staneee))

### Acknowledgements

[JetBrains](https://www.jetbrains.com/?from=Mapster) kindly provides Mapster with a free open-source licence for their Resharper and Rider.
- **Resharper** makes Visual Studio a much better IDE
- **Rider** is fast & powerful cross platform .NET IDE

![image](https://upload.wikimedia.org/wikipedia/commons/thumb/1/1a/JetBrains_Logo_2016.svg/121px-JetBrains_Logo_2016.svg.png)
