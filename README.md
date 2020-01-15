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
And you could gain 13x faster with [Mapster CodeGen](https://github.com/MapsterMapper/Mapster/wiki/CodeGen)!

|                    Method |           Mean |       StdDev |        Error |       Gen 0 | Gen 1 | Gen 2 |  Allocated |
|-------------------------- |---------------:|-------------:|-------------:|------------:|------:|------:|-----------:|
|           &#39;Mapster 4.1.1&#39; | 1,388,744.4 us |  3,987.51 us |  6,028.54 us | 312000.0000 |     - |     - | 1251.22 MB |
| &#39;Mapster 4.1.1 (Codegen)&#39; |   505,727.6 us |  2,525.21 us |  3,817.75 us | 312000.0000 |     - |     - | 1251.22 MB |
|     &#39;ExpressMapper 1.9.1&#39; | 3,122,200.4 us | 11,701.40 us | 19,663.45 us | 604000.0000 |     - |     - | 2418.52 MB |
|        &#39;AutoMapper 9.0.0&#39; | 6,883,546.7 us | 28,159.65 us | 42,573.37 us | 911000.0000 |     - |     - | 3646.85 MB |

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
