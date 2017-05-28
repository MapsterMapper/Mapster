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

### Performance
Don't let other libraries slow you down, Mapster is at least twice faster!

![image](https://cloud.githubusercontent.com/assets/5763993/26527206/bbde5490-43b8-11e7-8363-34644e5e709e.png)

### What's new in 3.0

Debugging generated mapping function!
![image](https://cloud.githubusercontent.com/assets/5763993/26521773/180427b6-431b-11e7-9188-10c01fa5ba5c.png)

Mapping your DTOs directly to EF6!
```
var poco = dto.BuildAdapter()
              .CreateEntityFromContext(db)
              .AdaptToType<DomainPoco>();
```

### Change logs
https://github.com/chaowlert/Mapster/releases

### Usages
https://github.com/chaowlert/Mapster/wiki
