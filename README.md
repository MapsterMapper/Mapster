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
And you could gain 12x faster with [FEC](https://github.com/MapsterMapper/Mapster/wiki/FastExpressionCompiler) or [Mapster CodeGen](https://github.com/MapsterMapper/Mapster/wiki/CodeGen)!

|                    Method |           Mean |       StdDev |        Error |       Gen 0 | Gen 1 | Gen 2 |  Allocated |
|-------------------------- |---------------:|-------------:|-------------:|------------:|------:|------:|-----------:|
|           'Mapster 4.1.1' | 115,052.1 us |   699.30 us | 1,337.05 us | 31000.0000 |     - |     - | 124.36 MB |
|     'Mapster 4.1.1 (FEC)' |  51,206.8 us |   301.53 us |   506.70 us | 29600.0000 |     - |     - | 118.26 MB |
| 'Mapster 4.1.1 (Codegen)' |  48,275.3 us |   337.06 us |   644.46 us | 31090.9091 |     - |     - | 124.36 MB |
|     'ExpressMapper 1.9.1' | 248,149.0 us | 1,564.35 us | 2,628.78 us | 59000.0000 |     - |     - | 236.51 MB |
|        'AutoMapper 9.0.0' | 596,706.8 us | 5,422.30 us | 9,111.82 us | 87000.0000 |     - |     - | 350.95 MB |


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
