![Mapster Icon](http://www.fancyicons.com/free-icons/103/pretty-office-5/png/128/order_128.png)

##Mapster - The Mapper of Your Domain

[![Build status](https://ci.appveyor.com/api/projects/status/krpp0nhspmklom1d?svg=true)](https://ci.appveyor.com/project/eswann/mapster)

### Basic usage
```
var result = TypeAdapter.Adapt<NewType>(original);
```
or with extensions
```
var result = original.Adapt<NewType>();
```
### Get it
```
PM> Install-Package Mapster
```
###Mapster 2.0 Released!
Mapster 2.0 is now blistering fast! We upgraded the whole compilation unit while maintaining all functionality. The benchmarks:

| Engine          | Structs | Simple objects | Parent-Child | Parent-Children | Complex objects | Advance mapping |
|-----------------|--------:|---------------:|-------------:|----------------:|----------------:|----------------:|
| AutoMapper      |   10871 |          27075 |        20895 |           19199 |           19333 |           21496 |
| ExpressMapper   |     690 |           1350 |         1195 |            1678 |            3130 |            3920 |
| OoMapper        |       - |           2043 |         1277 |            1416 |            2777 |               - |
| ValueInjector   |    8534 |          21089 |        17008 |           12355 |           16876 |           19970 |
| TinyMapper      |       - |           1282 |            - |               - |               - |               - |
| Mapster         |       - |           2382 |         1892 |            1626 |            4287 |            6756 |
| **Mapster 2.0** | **515** |       **1251** |      **950** |        **1037** |        **2455** |        **2342** |
| Native          |     458 |            790 |          870 |            1253 |            3037 |            2754 |

(NOTE: Benchmark runner is from [ExpressMapper](https://github.com/Expressmapper/ExpressMapper). Benchmark was run against largest set of data, times are in milliseconds, lower is better. Blank values mean the library did not the test.)

###New Features
- Projection is improved to generate better sql queries
- Mapster is now able to map structs
- Flagged enums are supported
- Setting is now much more flexible
    - You can now both opt-in and opt-out of settings
    - Settings inheritance is able to inherit from interfaces
    - Settings inheritance is now combined (it does not only pick from the closest parent)
    - New rule based settings, you can defined your settings at a more granular level
    - Settings are no longer only static, you can use different setting configurations for particular mappings
- You can ignore properties by attributes
- Now you can set up mapping between different types. Ex: `config.Map(dest => dest.AgeString, src => src.AgeInt)`
- Mapster now supports circular reference mapping!
- Supports more frameworks (.NET 4.0, 4.5, .NET Core RC 5.4)

###Get started

[Mapping](#Mapping)
- [Mapping to a new object](#MappingNew)
- [Mapping to an existing object](#MappingToTarget)
- [Queryable Extensions](#Projection)
- [Mapper instance](#UnitTest)

[Conversion](#Conversion)
- [Conversion of immutable types](#ConversionImmutable)
- [Conversion from/to enum](#ConversionEnum)
- [Mapping POCO](#ConversionPOCO)
- [Mapping Lists](#ConversionList)
- [Mapping generic Dictionaries](#ConversionDictionary)

[Settings](#Settings)
- [Settings per type](#SettingsPerType)
- [Global Settings](#SettingsGlobal)
- [Settings inheritance](#SettingsInheritance)
- [Rule based settings](#SettingsRuleBased)
- [Overload settings](#SettingsOverload)
- [Assembly scanning](#AssemblyScanning)

[Basic Customization](#Basic)
- [Ignore properties & attributes](#Ignore)
- [Property Mapping](#Map)
- [Merge Objects](#Merge)
- [Shallow Copy](#ShallowCopy)
- [Preserve reference (preventing circular reference stackoverflow)](#PreserveReference)

[Advance Customization](#Advance)
- [Custom instance creation](#ConstructUsing)
- [Type-Specific Destination Transforms](#Transform)
- [Custom Type Resolvers](#ConverterFactory)

[Validation](#Validate)
- [Explicit Mapping](#ExplicitMapping)
- [Checking Destination Member](#CheckDestinationMember)
- [Validating Mappings](#Compile)

####Mapping <a name="Mapping"></a>
#####Mapping to a new object <a name="MappingNew"></a>
Mapster creates the destination object and maps values to it.

    var destObject = TypeAdapter.Adapt<TSource, TDestination>(sourceObject);

or just

    var destObject = TypeAdapter.Adapt<TDestination>(sourceObject);

or using extension methods

	var destObject = sourceObject.Adapt<TDestination>();

#####Mapping to an existing object <a name="MappingToTarget"></a>
You make the object, Mapster maps to the object.

    TDestination destObject = new TDestination();
    destObject = TypeAdapter.Adapt(sourceObject, destObject);

or using extension methods

    TDestination destObject = new TDestination();
    destObject = sourceObject.Adapt(destObject);

#####Queryable Extensions <a name="Projection"></a>
Mapster also provides extensions to map queryables.

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

#####Mapper Instance <a name="UnitTest"></a>
In some cases, you need an instance of a mapper (or a factory function) to pass into a DI container. Mapster has
the IAdapter and Adapter to fill this need:

    IAdapter adapter = new Adapter();

And usage is the same as with the static methods.

    var result = adapter.Adapt<TDestination>(source);

####Conversion <a name="Conversion"></a>
Mapster can map nearly all kind of objects. Here are some details.

#####Conversion of immutable types <a name="ConversionImmutable"></a>
Converting between primitive types (ie. int, string, bool, double, decimal) is supported, including when those types are nullable. For all other types, if you can cast types in c#, you can also cast in Mapster.

    var i = TypeAdapter.Adapt<string, int>("123");  //123

#####Conversion from/to enum <a name="ConversionEnum"></a>
Mapster maps enums to numerics automatically, but it also maps strings to and from enums automatically in a fast manner.  
The default Enum.ToString() in .Net is quite slow. The implementation in Mapster is double the speed.  
Likewise, a fast conversion from strings to enums is also included.  If the string is null or empty,
the enum will initialize to the first enum value.

In Mapster 2.0, flagged enums are also supported.

    var e = TypeAdapter.Adapt<string, FileShare>("Read, Write, Delete");  
    //FileShare.Read | FileShare.Write | FileShare.Delete

#####Mapping POCO <a name="ConversionPOCO"></a>
Mapster can map 2 different POCO types using the following rules
- Source and destination property names are the same. Ex: `dest.Name = src.Name`
- Source has get method. Ex: `dest.Name = src.GetName()`
- Source property has child object which can flatten to destination. Ex: `dest.ContactName = src.Contact.Name` or `dest.Contact_Name = src.Contact.Name`

In Mapster 2.0, POCO structs are also supported.

    class Staff {
        public string Name { get; set; }
        public int GetAge() { return (DateTime.Now - this.BirthDate).TotalDays / 365.25; }
        public Staff Supervisor { get; set; }
        ...
    }

    struct StaffDto {
        public string Name { get; set; }
        public int Age { get; set; }
        public string SupervisorName { get; set; }
    }

    var dto = TypeAdapter.Adapt<Staff, StaffDto>(staff);  
    //dto.Name = staff.Name, dto.Age = staff.GetAge(), dto.SupervisorName = staff.Supervisor.Name

#####Mapping Lists <a name="ConversionList"></a>
This includes mapping among lists, arrays, collections, dictionary including various interfaces: IList<T>, ICollection<T>, IEnumerable<T> etc...

    var target = TypeAdapter.Adapt<List<Source>, IEnumerable<Destination>>(list);  


#####Mapping Dictionaries <a name="ConversionDictionary"></a>
To map a generic `Dictionary<string, Foo>` to `Dictionary<string, Bar >` we need to define simple mapping using MapWith
	
```
    TypeAdapterConfig<Dictionary<string, Foo>, Dictionary<string, Bar>>
        .NewConfig() // or .ForType()
        .MapWith(x => x.ToDictionary(k => k.Key, v => v.Value.Adapt<Bar>()))
``` 
    
    var target = TypeAdapter.Adapt<Dictionary<string, Foo>, Dictionary<string, Bar>>(source);  



####Settings <a name="Settings"></a>
#####Settings per type <a name="SettingsPerType"></a>
You can easily create settings for a type mapping by using: `TypeAdapterConfig<TSource, TDestination>.NewConfig()`.
When `NewConfig` is called, any previous configuration for this particular TSource => TDestination mapping is dropped.

    TypeAdapterConfig<TSource, TDestination>
        .NewConfig()
        .Ignore(dest => dest.Age)
        .Map(dest => dest.FullName,
             src => string.Format("{0} {1}", src.FirstName, src.LastName));

As an alternative to `NewConfig`, you can use `ForType` in the same way:

	TypeAdapterConfig<TSource, TDestination>
			.ForType()
			.Ignore(dest => dest.Age)
			.Map(dest => dest.FullName,
				 src => string.Format("{0} {1}", src.FirstName, src.LastName));

`ForType` differs in that it will create a new mapping if one doesn't exist, but if the specified TSource => TDestination
mapping does already exist, it will enhance the existing mapping instead of dropping and replacing it.  

#####Global Settings <a name="SettingsGlobal"></a>
Use global settings to apply policies to all mappings.

    TypeAdapterConfig.GlobalSettings.Default.PreserveReference(true);

Then for individual type mappings, you can easily override the global setting(s).

    TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig().PreserveReference(false);

#####Settings inheritance <a name="SettingsInheritance"></a>
Type mappings will automatically inherit for source types. Ie. if you set up following config.

    TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
        .Map(dest => dest.Name, src => src.Name + "_Suffix");

A derived type of `SimplePoco` will automatically apply the base mapping config.

    var dest = TypeAdapter.Adapt<DerivedPoco, SimpleDto>(src); //dest.Name = src.Name + "_Suffix"

If you don't wish a derived type to use the base mapping, just define `NoInherit` for that type.

    TypeAdapterConfig<DerivedPoco, SimpleDto>.NewConfig().NoInherit(true);

    //or at the global level
    TypeAdapterConfig.GlobalSettings.Default.NoInherit(true);

And by default, Mapster will not inherit destination type mappings. You can turn on by `AllowImplicitDestinationInheritance`.

    TypeAdapterConfig.GlobalSettings.AllowImplicitDestinationInheritance = true;

Finally, Mapster also provides methods to inherit explicitly.

    TypeAdapterConfig<DerivedPoco, DerivedDto>.NewConfig()
        .Inherits<SimplePoco, SimpleDto>();

#####Rule based settings <a name="SettingsRuleBased"></a>
To set the setting at a more granular level. You can use the `When` method in global settings.
In the example below, when any source type and destination type are the same, we will not the copy the `Id` property.

    TypeAdapterConfig.GlobalSettings.When((srcType, destType, mapType) => srcType == destType)
        .Ignore("Id");

In this example, the config would only apply to Query Expressions (projections).

    TypeAdapterConfig.GlobalSettings.When((srcType, destType, mapType) => mapType == MapType.Projection)
        .IgnoreAttribute(typeof(NotMapAttribute));

#####Overload settings <a name="SettingsOverload"></a>
You may wish to have different settings in different scenarios.
If you would not like to apply setting at a static level, Mapster also provides setting instance configurations.

    var config = new TypeAdapterConfig();
    config.Default.Ignore("Id");

For instance configurations, you can use the same `NewConfig` and `ForType` methods that are used at the global level with
the same behavior: `NewConfig` drops any existing configuration and `ForType` creates or enhances a configuration.

    config.NewConfig<TSource, TDestination>()
          .Map(dest => dest.FullName,
               src => string.Format("{0} {1}", src.FirstName, src.LastName));

    config.ForType<TSource, TDestination>()
          .Map(dest => dest.FullName,
               src => string.Format("{0} {1}", src.FirstName, src.LastName));

You can apply a specific config instance by passing it to the `Adapt` method. (NOTE: please reuse your config instance to prevent recompilation)

    var result = TypeAdapter.Adapt<TDestination>(src, config);

Or to an Adapter instance.

    var adapter = new Adapter(config);
    var result = adapter.Adapt<TDestination>(src);

#####Assembly scanning <a name="AssemblyScanning"></a>
It's relatively common to have mapping configurations spread across a number of different assemblies.  
Perhaps your domain assembly has some rules to map to domain objects and your web api has some specific rules to map to your
api contracts. In these cases, it can be helpful to allow assemblies to be scanned for these rules so you have some basic
method of organizing your rules and not forgetting to have the registration code called. In some cases, it may even be necessary to
register the assemblies in a particular order, so that some rules override others. Assembly scanning helps with this.
Assembly scanning is simple, just create any number of IRegister implementations in your assembly, then call `Scan` from your TypeAdapterConfig class:

	public class MyRegister : IRegister
	{
		public void Register(TypeAdapterConfig config){
			config.NewConfig<TSource, TDestination>();

			//OR to create or enhance an existing configuration

			config.ForType<TSource, TDestination>();
		}
	}

To scan and register at the Global level:

	TypeAdapterConfig.GlobalSettings.Scan(assembly1, assembly2, assemblyN)

For a specific config instance:

	var config = new TypeAdapterConfig();
	config.Scan(assembly1, assembly2, assemblyN);

If you use other assembly scanning library such as MEF, you can easily apply registration with `Apply` method.

	var registers = container.GetExports<IRegister>();
	config.Apply(registers);

####Basic Customization <a name="Basic"></a>
When the default convention mappings aren't enough to do the job, you can specify complex source mappings.

#####Ignore Members & Attributes <a name="Ignore"></a>
Mapster will automatically map properties with the same names. You can ignore members by using the `Ignore` method.

    TypeAdapterConfig<TSource, TDestination>
        .NewConfig()
        .Ignore(dest => dest.Id);

You can ignore members annotated with specific attributes by using the `IgnoreAttribute` method.

    TypeAdapterConfig<TSource, TDestination>
        .NewConfig()
        .IgnoreAttribute(typeof(JsonIgnoreAttribute));

#####Property mapping <a name="Map"></a>
You can customize how Mapster maps values to a property.

    TypeAdapterConfig<TSource, TDestination>
        .NewConfig()
        .Map(dest => dest.FullName,
             src => string.Format("{0} {1}", src.FirstName, src.LastName));

The Map configuration can accept a third parameter that provides a condition based on the source.
If the condition is not met, the mapping is skipped altogether.

    TypeAdapterConfig<TSource, TDestination>
        .NewConfig()
        .Map(dest => dest.FullName, src => src.FullName, srcCond => srcCond.City == "Victoria");

In Mapster 2.0, you can even map when source and destination property types are different.

    TypeAdapterConfig<TSource, TDestination>
        .NewConfig()
        .Map(dest => dest.Gender,      //Genders.Male or Genders.Female
             src => src.GenderString); //"Male" or "Female"

Mapster can also match properties with a different case sensitivity for a special configuration or on global level. 

    TypeAdapterConfig<TSource, TDestination>
        .NewConfig()
        .Settings
        .IgnoreCaseSensitiveNames = true;

    TypeAdapterConfig
        .GlobalSettings
        .IgnoreCaseSensitiveNames = true;

#####Merge object <a name="Merge"></a>
By default, Mapster will map all properties, even source properties containing null values.
You can copy only properties that have values by using `IgnoreNullValues` method.

    TypeAdapterConfig<TSource, TDestination>
        .NewConfig()
        .IgnoreNullValues(true);

#####Shallow copy <a name="ShallowCopy"></a>
By default, Mapster will recursively map nested objects. You can do shallow copying by setting `ShallowCopyForSameType` to `true`.

    TypeAdapterConfig<TSource, TDestination>
        .NewConfig()
        .ShallowCopyForSameType(true);

#####Preserve reference (preventing circular reference stackoverflow) <a name="PreserveReference"></a>
When mapping objects with circular references, a stackoverflow exception will result.
This is because Mapster will get stuck in a loop tring to recursively map the circular reference.
If you would like to map circular references or preserve references (such as 2 properties pointing to the same object), you can do it by setting `PreserveReference` to `true`

    TypeAdapterConfig<TSource, TDestination>
        .NewConfig()
        .PreserveReference(true);

NOTE: Projection doesn't support circular reference yet. To overcome, you might use `Adapt` instead of `Project`.

    TypeAdaptConfig.GlobalSettings.Default.PreserveReference(true);
    var students = context.Student.Include(p => p.Schools).Adapt<List<StudentDTO>>();

####Advance Customization <a name="Advance"></a>
#####Custom Destination Object Creation <a name="ConstructUsing"></a>
You can provide a function call to create your destination objects instead of using the default object creation
(which expects an empty constructor). To do so, use the `ConstructUsing` method when configuring.  This method expects
a function that will provide the destination instance. You can call your own constructor, a factory method,
or anything else that provides an object of the expected type.

    //Example using a non-default constructor
    TypeAdapterConfig<TSource, TDestination>.NewConfig()
                .ConstructUsing(src => new TDestination(src.Id, src.Name));

    //Example using an object initializer
    TypeAdapterConfig<TSource, TDestination>.NewConfig()
                .ConstructUsing(src => new TDestination{Unmapped = "unmapped"});

#####Type-Specific Destination Transforms <a name="Transform"></a>
This allows transforms for all items of a type, such as trimming all strings. But really any operation
can be performed on the destination value before assignment.

    //Global
    TypeAdapterConfig.GlobalSettings.Default.AddDestinationTransforms((string x) => x.Trim());

    //Per mapping configuration
    TypeAdapterConfig<TSource, TDestination>.NewConfig()
        .AddDestinationTransforms((string x) => x.Trim());

#####Custom Type Resolvers <a name="ConverterFactory"></a>
In some cases, you may want to have complete control over how an object is mapped. You can register specific transformations using the `MapWith`
method.

    //Example of transforming string to char[].
    TypeAdapterConfig<string, char[]>.NewConfig()
                .MapWith(str => str.ToCharArray());

####Validation <a name="Validate"></a>
To validate your mapping in unit tests and in order to help with "Fail Fast" situations, the following strict mapping modes have been added.

#####Explicit Mapping <a name="ExplicitMapping"></a>
Forcing all classes to be explicitly mapped:

    //Default is "false"
    TypeAdapterConfig.GlobalSettings.RequireExplicitMapping = true;
    //This means you have to have an explicit configuration for each class, even if it's just:
    TypeAdapterConfig<Source, Destination>.NewConfig();

#####Checking Destination Member <a name="CheckDestinationMember"></a>
Forcing all destination properties to have a corresponding source member or explicit mapping/ignore:

    //Default is "false"
    TypeAdapterConfig.GlobalSettings.RequireDestinationMemberSource = true;

#####Validating Mappings <a name="Compile"></a>
Both a specific TypeAdapterConfig<Source, Destination> or all current configurations can be validated. In addition, if Explicit Mappings (above) are enabled, it will also include errors for classes that are not registered at all with the mapper.

    //Validate a specific config
    var config = TypeAdapterConfig<Source, Destination>.NewConfig();
    config.Compile();

    //Validate globally
    TypeAdapterConfig<Source, Destination>.NewConfig();
    TypeAdapterConfig<Source2, Destination2>.NewConfig();
    TypeAdapterConfig.GlobalSettings.Compile();
