<img style="float:left" src="http://www.fancyicons.com/free-icons/103/pretty-office-5/png/128/order_128.png" />
<div style="float:left"><h1 >Mapster - The Mapper of Your Domain</h1></div>

<div style="clear:both;float:none"></div>
What was Fpr is now Mapster!  Had to grow up (a little).

[![Build status](https://ci.appveyor.com/api/projects/status/krpp0nhspmklom1d?svg=true)](https://ci.appveyor.com/project/eswann/mapster)

A fast, fun and stimulating object to object mapper for .Net 4.5.  

Mapster was originally forked from FastMapper (https://fmapper.codeplex.com/).
Mapster maps properties by convention, including nested complex objects and collections, but also supports
explicit mapping.

This fork fixes some issues and includes some additions to make the mapper more configurable and useful for .Net 4.5:

* Support for IReadOnlyList
* Mapping of members with non-public setters
* Automatic mapping of nullable primitives to non-nullable primitives
* Improved error messages that help you find configuration errors
* Conditional mapping
* Assembly scanning for custom mappers
* Strict modes to err if types or members are not explicity mapped (implicit/forgiving mapping is the default).
* Type specific destination transforms (typically such as trim or lowercase all strings).  Can be used on any destination type.
* Custom destination creation (not just default constructor)
* Automatic Enum <=> String mapping
* Mapper instance creation for injection situations
* Lots more stuff below...

###Examples
####Mapping to a new object
Mapster makes the object and maps values to it.

    TDestination destObject = TypeAdapter.Adapt<TSource, TDestination>(sourceObject);

####Mapping to an existing object
You make the object, Mapster maps to the object.

    TDestination destObject = new TDestination();
    destObject = TypeAdapter.Adapt(sourceObject, destObject);

####Mapping Lists Included
This includes lists, arrays, collections, enumerables etc...

    var destObjectList = TypeAdapter.Adapt<List<TSource>, List<TDestination>>(sourceList);

####Mapping Enums Included
Mapster maps enums to numerics automatically, but it also maps strings to and from enums automatically in a fast manner.  
The default Enum.ToString() in .Net is quite slow.  The implementation in Mapster is double the speed.  
Likewise, a fast conversion from strings to enums is also included.  If the string is null or empty, 
the enum will initialize to the first enum value.

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
    .Map(dest => dest.FullName, 
             src => string.Format("{0} {1}", src.FirstName, src.LastName));

####Implicit TSource Mapping Inheritance
If a mapping configuration doesn't exist for a source ==> destination type, but a mapper does exist for a base source type 
to the destination, that mapping will be used.  This allows mappings for less derived source types to be used to 
satisfy multiple derived mappings.  FPR will search downward the source class hierarchy until it finds a matching configuration.   
If no match exists, it will create a default configuration (the same behavior if no mapping was present).
It **doesn't** combine derived configs, it will stop at the first match.  
For example, if you have:

    public class SimplePoco
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }

    public class DerivedPoco : SimplePoco
    {...}

    public class DerivedPoco2 : SimplePoco
    {...}

    public class SimpleDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }

The following mapping will be used when mapping the SimplePoco or either of the derived POCOs to the Simple DTO.

    TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
        .Map(dest => dest.Name, src => src.Name + "_Suffix");

If you don't wish a derived type to use the base mapping, just define a new configuration for that type.

    TypeAdapterConfig<DerivedPoco2, SimpleDto>.NewConfig();


####Implicit TSource and TDestination Mapping Inheritance
In some cases you may wish to derive a mapping based on both the source and destination types.  In such a case,
setting the global configuration AllowImplicitDestinationInheritance setting to true will allow inheritance of a mapping
based on both the source and destination types.  The source class hierarchy is traversed in an inside loop and the 
destination class hierarchy is traversed in an outside loop until a match is found. 

    public class SimplePoco
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }

    public class DerivedPoco : SimplePoco
    {...}

    public class SimpleDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }

    public class DerivedDto : DerivedDto
    {...}

The following mapping will be used for any permutation of Simple/Derived Poco to Simple/Derived Dto
unless overridden by a specific configuration. 

    TypeAdapterConfig.GlobalSettings.AllowImplicitDestinationInheritance = true;
    TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
        .Map(dest => dest.Name, src => src.Name + "_Suffix");

If you don't wish a specific permutation to use the base mapping, just define a new configuration for that permutation.

    TypeAdapterConfig<DerivedPoco, SimpleDto>.NewConfig();


#### Explicit Mapping Inheritance
In some cases, you may wish to have one mapping inherit from another mapping.  In this case, anything set on the derived mapping
will override settings on the base mapping.  So unlike implicit mapping inheritance, where the goal is to find an applicable mapping
configuration and configurations are not combined, with Explicit Inheritance, each derived mapping overrides the settings of the 
inherited mapping.  Use the Inherits<TBaseSource, TBaseDestination> configuration method to accomplish this where TBaseSource and 
TBaseDestination must be assignable from the derived configuration's TSource and TDestination respectively. 
For example, if you have:

    public class SimplePoco
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }

    public class DerivedPoco : SimplePoco
    {...}

    public class SimpleDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }

    public class DerivedDto : SimpleDto
    {...}

And you have the following base mapping:

    TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
        .Map(dest => dest.Name, src => src.Name + "_Suffix");

You can base the mapping of the derived classes on this base mapping:
    
    TypeAdapterConfig<DerivedPoco, DerivedDto>.NewConfig()
        .Inherits<SimplePoco, SimpleDto>();


####Custom Destination Object Creation
You can provide a function call to create your destination objects instead of using the default object creation 
(which expects an empty constructor).  To do so, use the "ConstructUsing()" method when configuring.  This method expects
a function that will provide the destination instance. You can call your own constructor, a factory method, 
or anything else that provides an object of the expected type.

    //Example using a non-default constructor
    TypeAdapterConfig<TSource, TDestination>.NewConfig()
                .ConstructUsing(() => new TDestination("constructorValue"));

    //Example using an object initializer
    TypeAdapterConfig<TSource, TDestination>.NewConfig()
                .ConstructUsing(() => new TDestination{Unmapped = "unmapped"});

####Custom Type Resolvers
In some cases, you may want to have complete control over how an object is mapped.  In this case, you can
register a custom type resolver.  It's important to note that when using a custom type resolver, that 
all other mapping associated with the type is ignored.  So all mapping must take place within the resolver.
The custom type resolver must implement the ITypeResolver interface and register it using MapWith().

    //Example using MapWith resolver generic call.
    TypeAdapterConfig<TSource, TDestination>.NewConfig()
                .MapWith<TCustomTypeResolver>();

    //Example using MapWith resolver factory function
    TypeAdapterConfig<TSource, TDestination>.NewConfig()
                .MapWith(() => new TCustomTypeResolver());

    //Example using MapWith resolver instance
    TypeAdapterConfig<TSource, TDestination>.NewConfig()
                .MapWith(customResolverInstance);

####Custom Value Resolvers
In some cases, you may want to encapsulate a value conversion into a separate class.  In this case, you can 
use a custom value resolver by registering it using Resolve().  The value resolver must implement the 
IValueResolver interface.

    //Example using MapWith resolver generic call.
    TypeAdapterConfig<TSource, TDestination>.NewConfig()
                .Resolve<TCustomValueResolver, string>(dest => dest.Name);

    //Example using value resolver factory function
    TypeAdapterConfig<TSource, TDestination>.NewConfig()
                .Resolve(dest => dest.Name, () => new TCustomValueResolver());

    //Example using value resolver instance
    TypeAdapterConfig<TSource, TDestination>.NewConfig()
                .Resolve(dest => dest.Name, customValueResolver);

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
The Map configuration can accept a third parameter that provides a condition based on the source.
If the condition is not met, the mapping is skipped altogether.

    TypeAdapterConfig<TSource, TDestination>()
        .NewConfig()
        .IgnoreMember(dest => dest.Property)
        .Map(dest => dest.FullName, src => src.FullName, srcCond => srcCond.City == "Victoria");

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

####Validating Mappings
Both a specific TypeAdapterConfig<Source, Destination> or all current configurations can be validated.  This will throw
and ArgumentOutOfRangeException that contains all of the existing missing destination mappings.  In addition, if Explicit Mappings (above)
are enabled, it will also include errors for classes that are not registered at all with the mapper.

    //Validate a specific config
    var config = TypeAdapterConfig<Source, Destination>.NewConfig();
    config.Validate();

    //Validate globally
    TypeAdapterConfig<Source, Destination>.NewConfig();
    TypeAdapterConfig<Source2, Destination2>.NewConfig();
    TypeAdapterConfig.Validate();

####Mapper Instance Creation
In some cases, you need an instance of a mapper (or a factory function) to pass into a DI container.  Mapster has
the IAdapter and Adapter to fill this need:

    IAdapter instance = TypeAdapter.GetInstance();


###Assembly Scanning for Custom Mappings
To make it easier to register custom mappings, we've implemented an assembly scanning approach.
To allow this, either inherit from IRegistry or Registry in the Mapster.Registration namespace.

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
For complex objects, we're seeing a ~30x speed improvement in comparison to AutoMapper.  

####Benchmark "Complex" Object

The following test converts a Customer object with 2 nested address collections and two nested address sub-objects to a DTO.

Competitors : Handwriting Mapper, Mapster, FastMapper, AutoMapper

    Iterations : 100
    Handwritten Mapper:     1
    Mapster:                    0
    FastMapper:             0
    AutoMapper:             10

    Iterations : 10000
    Handwritten Mapper:     4
    Mapster:                    17
    FastMapper:             17
    AutoMapper:             507

    Iterations : 100000
    Handwritten Mapper:     29
    Mapster:                    177
    FastMapper:             175
    AutoMapper:             5058

