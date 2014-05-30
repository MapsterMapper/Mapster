##Fapper - The Mapper of Your Domain

A fast, fun and stimulating object to object mapper for .Net 4.5.  

Fapper was originally forked from FastMapper (https://fmapper.codeplex.com/).
Fapper maps properties by convention, including nested complex objects and collections, but also supports
explicit mapping.

This fork fixes some issues and includes some additions to make the mapper more configurable and useful for .Net 4.5:

* Support for IReadOnlyList
* Mapping of members with non-public setters
* Improved error messages that help you find configuration errors
* Conditional mapping


###Examples
####Mapping to a new object

    TDestination destObject = TypeAdapter.Adapt<TSource, TDestination>(sourceObject);

####Mapping to an existing object

    TDestination destObject = new TDestination();
    destObject = TypeAdapter.Adapt(sourceObject, destObject);

####Mapping Lists Included

    var destObjectList = TypeAdapter.Adapt<List<TSource>, List<TDestination>>(sourceList);

####Customized Mapping

    TypeAdapterConfig<TSource, TDestination>()
    .NewConfig()
    .IgnoreMember(dest => dest.Property)
    .MapFrom(dest => dest.FullName, 
             src => string.Format("{0} {1}", src.FirstName, src.LastName));

####Conditional Mapping
The MapFrom configuration can accept a third parameter that provides a condition based on the source.
If the condition is not met, the mapping is skipped altogether.

    TypeAdapterConfig<TSource, TDestination>()
        .NewConfig()
        .IgnoreMember(dest => dest.Property)
        .MapFrom(dest => dest.FullName, src => src.FullName, srcCond => srcCond.City == "Victoria");

####Max Depth
When mapping nested or tree-type structures, it's often necessary to specify a max nesting depth to prevent overflows.

    TypeAdapterConfig<TSource, TDestination>
                .NewConfig()
                .MaxDepth(3);

####Queryable Extensions

    using(MyDbContext context = new MyDbContext())
    {
        // Build a Select Expression from DTO
        var destinations = context.Sources.Project().To<Destination>().ToList();

        // Versus creating by hand:
        var destinations = context.Sources.Select(c => new Destination(){
            Id = p.Id,
            Name = p.Name,
            Surname = p.Surname,
            ....
        })
        .ToList();
    }

####Performance Comparisons
Fapper is slightly slower than FastMapper, mainly due to support for better error messaging.  
While FastMapper is very fast, it can be very difficult to determine the location of errors when mapping across non-identical types.  
We're looking to regain parity in this area with some additional optimization.  

For complex objects, we're seeing a 20-25x speed improvement in comparison to AutoMapper.  
Our tests with the original FastMapper were closer to 30X.