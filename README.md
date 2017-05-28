![Mapster Icon](https://cloud.githubusercontent.com/assets/5763993/26522718/d16f3e42-4330-11e7-9b78-f8c7402624e7.png)

## Mapster - The Mapper of Your Domain

### Get it
```
PM> Install-Package Mapster
```

### Basic usage
#### Mapping to a new object
Mapster creates the destination object and maps values to it.

    var destObject = sourceObject.Adapt<TDestination>();

#### Mapping to an existing object

You make the object, Mapster maps to the object.

    TDestination destObject = new TDestination();
    destObject = sourceObject.Adapt(destObject);

#### Queryable Extensions

Mapster also provides extensions to map queryables.

    using (MyDbContext context = new MyDbContext())
    {
        // Build a Select Expression from DTO
        var destinations = context.Sources.ProjectToType<Destination>().ToList();

        // Versus creating by hand:
        var destinations = context.Sources.Select(c => new Destination(){
            Id = p.Id,
            Name = p.Name,
            Surname = p.Surname,
            ....
        })
        .ToList();
    }
