##Fpr - The Mapper of Your Domain

A fast, fun and stimulating object to object mapper for .Net 4.5.  

Fpr was originally forked from FastMapper (https://fmapper.codeplex.com/).
Fpr maps properties by convention, including nested complex objects and collections, but also supports
explicit mapping.

This fork fixes some issues and includes some additions to make the mapper more configurable and useful for .Net 4.5:

* Support for IReadOnlyList
* Mapping of members with non-public setters
* Automatic mapping of nullable primitives to non-nullable primitives
* Improved error messages that help you find configuration errors
* Conditional mapping
* Assembly scanning for custom mappers
* Strict modes to err if types or members are not explicity mapped (implicit/forgiving mapping is the default).\
* Type specific destination transforms (typically such as trim or lowercase all strings).  Can be used on any destination type.


###Examples
####Mapping to a new object
Fpr makes the object and maps values to it.

    TDestination destObject = TypeAdapter.Adapt<TSource, TDestination>(sourceObject);

####Mapping to an existing object
You make the object, Fpr maps to the object.

    TDestination destObject = new TDestination();
    destObject = TypeAdapter.Adapt(sourceObject, destObject);

####Mapping Lists Included
This includes lists, arrays, collections, enumerables etc...

    var destObjectList = TypeAdapter.Adapt<List<TSource>, List<TDestination>>(sourceList);

####Mapping Enums Included
Fpr maps enums to numerics automatically, but it also maps strings to and from enums automatically in a fast manner.  
The default Enum.ToString() in .Net is quite slow.  The implementation in Fpr is double the speed.  
Likewise, a fast conversion from strings to enums is also included.  If the string is empty, by default the mapper will err, 
if the string is null, the enum will initialize to 0 which typically represents the default enum value.

In addition, fast Enum mapper extension methods are included for convenience.

    //Convert enum to string
    var myEnum = new SomeEnum.FirstValue;
    myEnum.ToFastString();

    //Convert string to enum
    var myEnumString = "FirstValue";
    myEnumString.ToFastEnum<SomeEnum>();
    

####Customized Mapping
When the default convention mappings aren't enough to do the job, you can specify complex source mappings.

    TypeAdapterConfig<TSource, TDestination>()
    .NewConfig()
    .IgnoreMember(dest => dest.Property)
    .MapFrom(dest => dest.FullName, 
             src => string.Format("{0} {1}", src.FirstName, src.LastName));

####Type-Specific Destination Transforms
This allows transforms for all items of a type, such as trimming all strings.  But really any operation 
can be performed on the destination value before assignment.  This can be set up at either a global
or a mapping level.

    //Global
    TypeAdapterConfig.GlobalSettings.DestinationTransforms.Upsert<string>(x => x.Trim());

    //Per mapping configuration
    TypeAdapterConfig<TSource, TDestination>.NewConfig()
        .DestinationTransforms.Upsert<string>(x => x.Trim());
    

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

####Forcing Explicit Mapping
In order to help with "Fail Fast" situations, the following strict mapping modes have been added.
An ArgumentOutOfRange exception will currently be thrown in the situations below if an appropriate mapping and/or source cannot be located.

Forcing all destination properties to have a corresponding source member or explicit mapping/ignore:

    //Default is "false"
    TypeAdapterConfig.GlobalSettings.RequireDestinationMemberSource = true;

Forcing all classes to be explicitly mapped:

    //Default is "false"
    TypeAdapterConfig.GlobalSettings.RequireExplicitMapping = true;
    //This means you have to have an explicit configuration for each class, even if it's just:
    TypeAdapterConfig<Source, Destination>.NewConfig();


###Assembly Scanning for Custom Mappings
To make it easier to register custom mappings, we've implemented an assembly scanning approach.
To allow this, either inherit from IRegistry or Registry in the Fpr.Registration namespace.

Override the Apply() method and perform your registrations there.  When your app starts up, use the Registrar class to perform registration.

    //Implement a registry class
    public class MyRegistry : Registry
    {
        public override void Apply()
        {
            TypeAdapterConfig<TSource, TDestination>.NewConfig()
                .IgnoreMember(dest => dest.CurrencyCode)
                .IgnoreMember(dest => dest.ExtraElements);
        }
    }

    //In my boostrap/startup code, call the registry
    //Method 1: Call registry directly with extension method
    new MyRegistry().Register();

    //Method 2: Scan the assembly (by assembly or class type)
    Registrar.RegisterFromAssemblyContaining<MyRegistry>();
    //or
    Assembly.GetExecutingAssembly().RegisterFromAssembly();


###Performance Comparisons
Fpr now has speed parity with the original FastMapper.

For complex objects, we're seeing a ~30x speed improvement in comparison to AutoMapper.  

####Benchmark "Complex" Object

The following test converts a Customer object with 2 nested address collections and two nested address sub-objects to a DTO.

Competitors : Handwriting Mapper, Fpr, FastMapper, AutoMapper

    Iterations : 100
    Handwritten Mapper:     1
    Fpr:                    0
    FastMapper:             0
    AutoMapper:             10

    Iterations : 10000
    Handwritten Mapper:     4
    Fpr:                    17
    FastMapper:             17
    AutoMapper:             507

    Iterations : 100000
    Handwritten Mapper:     29
    Fpr:                    177
    FastMapper:             175
    AutoMapper:             5058

