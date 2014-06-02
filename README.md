##Fpr - The Mapper of Your Domain

A fast, fun and stimulating object to object mapper for .Net 4.5.  

Fpr was originally forked from FastMapper (https://fmapper.codeplex.com/).
Fpr maps properties by convention, including nested complex objects and collections, but also supports
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

####Mapping Enums Included
Fpr maps enums to numerics automatically, but it also maps strings to and from enums automatically in a fast manner.  
The default Enum.ToString() in .Net is quite slow.  The implementation in Fpr is double the speed.  
Likewise, a fast conversion from strings to enums is also included.  If the string is emplty, by default the mapper will err, 
if the string is null, the enum will initialize to 0.

In addition, fast Enum mapper extension methods are included for convenience.

    //Convert enum to string
    var myEnum = new SomeEnum.FirstValue;
    myEnum.ToFastString();

    //Convert string to enum
    var myEnumString = "FirstValue";
    myEnumString.ToFastEnum<SomeEnum>();
    

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

###Performance Comparisons
Fpr is slightly slower than FastMapper, mainly due to support for better error messaging.  
While FastMapper is very fast, it can be very difficult to determine the location of errors when mapping across non-identical types.  
We're looking to regain parity in this area with some additional optimization.  

For complex objects, we're seeing a ~28x speed improvement in comparison to AutoMapper.  
Our tests with the original FastMapper were closer to ~30X.

####Benchmark "Complex"" Object

The following test converts a Customer object with 2 nested address collections and two nested address sub-objects to a DTO.

Competitors : Handwriting Mapper, Fpr, FastMapper, AutoMapper
(Value Injecter cannot convert complex type, Value injecter need a custom inject
er)

    Iterations : 100
    Handwritten Mapper:     1
    Fpr:                    0
    FastMapper:             0
    AutoMapper:             10

    Iterations : 10000
    Handwritten Mapper:     4
    Fpr:                    19
    FastMapper:             18
    AutoMapper:             522

    Iterations : 100000
    Handwritten Mapper:     29
    Fpr:                    185
    FastMapper:             175
    AutoMapper:             5167
    Finish
