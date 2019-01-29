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
Don't let other libraries slow you down, raw Mapster performance is at least 2.5 times faster!  
And you can boost another 1.3 - 4.0 times faster with [FastExpressionCompiler](https://github.com/MapsterMapper/Mapster/wiki/FastExpressionCompiler)!

<img width="932" alt="screen shot 2018-10-08 at 21 27 55" src="https://user-images.githubusercontent.com/5763993/46615061-8cc78980-cb41-11e8-8bea-b04d9fcabccd.png">

### What's new in 3.3

Step-into debugging now working in .NET Core!!!
![image](https://cloud.githubusercontent.com/assets/5763993/26521773/180427b6-431b-11e7-9188-10c01fa5ba5c.png)


### Change logs
https://github.com/MapsterMapper/Mapster/releases

### Usages
https://github.com/MapsterMapper/Mapster/wiki
