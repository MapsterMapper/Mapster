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

### What's new in Mapster 4.0
- Two ways mapping
- Unflattening
- Map & ignore by property path
- MaxDepth
- Map to constructor

### Why Mapster?
#### Performance & Memory efficient
Mapster was designed to be efficient on both speed and memory. You could gain 5x faster while using only 1/3 of memory.
And you could gain to 12x faster with
- [Roslyn Compiler](https://github.com/MapsterMapper/Mapster/wiki/Debugging)
- [FEC](https://github.com/MapsterMapper/Mapster/wiki/FastExpressionCompiler)
- [Mapster CodeGen](https://github.com/MapsterMapper/Mapster/wiki/CodeGen)

|                    Method |           Mean |       StdDev |        Error |       Gen 0 | Gen 1 | Gen 2 |  Allocated |
|-------------------------- |---------------:|-------------:|-------------:|------------:|------:|------:|-----------:|
|           'Mapster 4.1.1' | 115.31 ms | 0.849 ms | 1.426 ms | 31000.0000 |     - |     - | 124.36 MB |
|  'Mapster 4.1.1 (Roslyn)' |  53.55 ms | 0.342 ms | 0.654 ms | 31100.0000 |     - |     - | 124.36 MB |
|     'Mapster 4.1.1 (FEC)' |  54.70 ms | 1.023 ms | 1.546 ms | 29600.0000 |     - |     - | 118.26 MB |
| 'Mapster 4.1.1 (Codegen)' |  48.22 ms | 0.868 ms | 1.312 ms | 31090.9091 |     - |     - | 124.36 MB |
|     'ExpressMapper 1.9.1' | 260.04 ms | 6.340 ms | 9.585 ms | 59000.0000 |     - |     - | 236.51 MB |
|        'AutoMapper 9.0.0' | 601.23 ms | 3.869 ms | 6.501 ms | 87000.0000 |     - |     - | 350.95 MB |


#### Step into debugging

[Step-into debugging](https://github.com/MapsterMapper/Mapster/wiki/Debugging) lets you debug your mapping and inspect values as same as its your code.
![image](https://cloud.githubusercontent.com/assets/5763993/26521773/180427b6-431b-11e7-9188-10c01fa5ba5c.png)

#### Code Generation NEW!!
[Mapster CodeGen](https://github.com/MapsterMapper/Mapster/wiki/CodeGen) lets you do mapping with
- Validate mapping at compile time
- Getting raw performance
- Seeing your mapping code & debugging
- Finding usage of your models' properties

### Change logs
https://github.com/MapsterMapper/Mapster/releases

### Usages
https://github.com/MapsterMapper/Mapster/wiki
