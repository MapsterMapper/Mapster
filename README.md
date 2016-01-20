![Mapster Icon](http://www.fancyicons.com/free-icons/103/pretty-office-5/png/128/order_128.png)

##Mapster - The Mapper of Your Domain

### Basic usage
```
var result = TypeAdapter.Adapt<NewType>(original);
```
### Get it
```
PM> Install-Package Mapster
```
###Mapster 2.0 Release!
Mapster 2.0 is now become blistering fast! We upgraded the whole compilation unit while still maintain its functionalities. Here is benchmark.

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

(NOTE: Benchmark runner is from [ExpressMapper](https://github.com/Expressmapper/ExpressMapper). Benchmark was run against largest set of data, times are in milliseconds, lower is better. Blank values mean the library did not supported.)

And here are list of new features!
- Projection is improved to generate nicer sql query
- Mapster is now able to map struct
- Flagged enum is supported
- Setting is now much more flexible
    - You can now both opt-in and opt-out setting
    - Setting inheritance is able to inherit from interface
    - Setting inheritance is now combined (it does not only pick from the closest parent)
    - New rule based setting, you can defined your setting more granular level
    - Setting is no more static, you can overload your setting to use different setting for your mapping
- You can ignore properties by attributes
- Now you can setup your map from different type ie `config.Map(dest => dest.AgeString, src => src.AgeInt)`
- Mapster is now support circular reference mapping!
- Supported more frameworks (.NET 4.0, 4.5, .NET Core RC 5.4)

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

[Setting](#Setting)
- [Setting per type](#SettingPerType)
- [Global Settings](#SettingGlobal)
- [Setting inheritance](#SettingInheritance)
- [Rule based setting](#SettingRuleBased)
- [Overload setting](#SettingOverload)

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
Mapster makes the object and maps values to it.

    var destObject = TypeAdapter.Adapt<TSource, TDestination>(sourceObject);

or just

    var destObject = TypeAdapter.Adapt<TDestination>(sourceObject);

or using extension methods

	var destObject = sourceObject.Adapt<TDestination>

#####Mapping to an existing object <a name="MappingToTarget"></a>
You make the object, Mapster maps to the object.

    TDestination destObject = new TDestination();
    destObject = TypeAdapter.Adapt(sourceObject, destObject);

#####Queryable Extensions <a name="Projection"></a>
Mapster also provides extension to map queryable.

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

And usage is the same with static method.

    var result = adapter.Adapt<TDestination>(source);

####Conversion <a name="Conversion"></a>
Mapster basically can map nearly all kind of objects. Here are some details.

#####Conversion of immutable types <a name="ConversionImmutable"></a>
Converting between primitive types (ie. int, string, bool, double, decimal) are supported, including when those types are nullable. For all other types, if you can cast types in c#, you can also cast in Mapster.

    var i = TypeAdapter.Adapt<string, int>("123");  //123

#####Conversion from/to enum <a name="ConversionEnum"></a>
Mapster maps enums to numerics automatically, but it also maps strings to and from enums automatically in a fast manner.  
The default Enum.ToString() in .Net is quite slow. The implementation in Mapster is double the speed.  
Likewise, a fast conversion from strings to enums is also included.  If the string is null or empty,
the enum will initialize to the first enum value.

In Mapster 2.0, flagged enum is also supported.

    var e = TypeAdapter.Adapt<string, FileShare>("Read, Write, Delete");  
    //FileShare.Read | FileShare.Write | FileShare.Delete

#####Mapping POCO <a name="ConversionPOCO"></a>
Mapster can map 2 different POCO types by maching following
- Source and destination property names are the same ie. `dest.Name = src.Name`
- Source has get method ie. `dest.Name = src.GetName()`
- Source properties has child object which can flatten to destination ie. `dest.ContactName = src.Contact.Name` or `dest.Contact_Name = src.Contact.Name`

In Mapster 2.0, POCO struct is also supported.

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
This includes mapping among lists, arrays, collections, dictionary including various interface ie. IList<T>, ICollection<T>, IEnumerable<T> etc...

    var target = TypeAdapter.Adapt<List<Source>, IEnumerable<Destination>>(list);  

####Setting <a name="Setting"></a>
#####Setting per type <a name="SettingPerType"></a>
You can easily create setting for type mapping by `TypeAdapterConfig<TSource, TDestination>().NewConfig()`

    TypeAdapterConfig<TSource, TDestination>()
        .NewConfig()
        .Ignore(dest => dest.Age)
        .Map(dest => dest.FullName,
             src => string.Format("{0} {1}", src.FirstName, src.LastName));

#####Global Settings <a name="SettingGlobal"></a>
If you would like to apply to all type mappings, you can set to global settings

    TypeAdaptConfig.GlobalSettings.Default.PreserveReference(true);

Then for some type mappings, you can opt-out the option.

    TypeAdaptConfig<SimplePoco, SimpleDto>.NewConfig().PreserveReference(false);

#####Setting inheritance <a name="SettingInheritance"></a>
Type mapping will automatically inherit for source type. Ie. if you set up following config.

    TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
        .Map(dest => dest.Name, src => src.Name + "_Suffix");

Derived type of `SimplePoco` will automatically apply above property mapping config.

    var dest = TypeAdapter.Adapt<DerivedPoco, SimpleDto>(src); //dest.Name = src.Name + "_Suffix"

If you don't wish a derived type to use the base mapping, just define `NoInherit` for that type.

    TypeAdapterConfig<DerivedPoco, SimpleDto>.NewConfig().NoInherit(true);

    //or in global level
    TypeAdapterConfig.GlobalSettings.Default.NoInherit(true);

And by default, Mapster will not inherit destination type. You can turn on by `AllowImplicitDestinationInheritance`.

    TypeAdapterConfig.GlobalSettings.AllowImplicitDestinationInheritance = true;

Finally, Mapster also provide method to inherit explicitly.

    TypeAdapterConfig<DerivedPoco, DerivedDto>.NewConfig()
        .Inherits<SimplePoco, SimpleDto>();

#####Rule based setting <a name="SettingRuleBased"></a>
To set the setting in more granular level. You can use `When` method in global setting. For example, when source type and destination type is the same, we will not copy `Id` property.

    TypeAdapterConfig.GlobalSettings.When((srcType, destType, mapType) => srcType == destType)
        .Ignore("Id");

Another example, you may would like to apply config only for Query Expression.

    TypeAdapterConfig.GlobalSettings.When((srcType, destType, mapType) => mapType == MapType.Projection)
        .IgnoreAttribute(typeof(NotMapAttribute));

#####Overload setting <a name="SettingOverload"></a>
You may wish to have different settings in different scenarios. If you would not like to apply setting in static level, Mapster also provides setting instance.

    var config = new TypeAdapterConfig();
    config.Default.Ignore("Id");

For type mapping, you can use `ForType` method.

    config.ForType<TSource, TDestination>()
          .Map(dest => dest.FullName,
               src => string.Format("{0} {1}", src.FirstName, src.LastName));

You can apply setting instance by passing to `Adapt` method. (NOTE: please reuse your config instance to prevent recompilation)

    var result = TypeAdapter.Adapt<TDestination>(src, config);

Or to Adapter instance.

    var adapter = new Adapter(config);
    var result = adapter.Adapt<TDestination>(src);

####Basic Customization <a name="Basic"></a>
When the default convention mappings aren't enough to do the job, you can specify complex source mappings.

#####Ignore Members & Attributes <a name="Ignore"></a>
Mapster will automatically map properties with the same names. You can ignore members by using `Ignore` method.

    TypeAdapterConfig<TSource, TDestination>()
        .NewConfig()
        .Ignore(dest => dest.Id);

You can ignore members annotated with specific attribute by using `IgnoreAttribute` method.

    TypeAdapterConfig<TSource, TDestination>()
        .NewConfig()
        .IgnoreAttribute(typeof(JsonIgnoreAttribute));

#####Property mapping <a name="Map"></a>
You can customize how Mapster maps value to property.

    TypeAdapterConfig<TSource, TDestination>()
        .NewConfig()
        .Map(dest => dest.FullName,
             src => string.Format("{0} {1}", src.FirstName, src.LastName));

The Map configuration can accept a third parameter that provides a condition based on the source.
If the condition is not met, the mapping is skipped altogether.

    TypeAdapterConfig<TSource, TDestination>()
        .NewConfig()
        .Map(dest => dest.FullName, src => src.FullName, srcCond => srcCond.City == "Victoria");

In Mapster 2.0, you can map even type of source and destination properties are different.

    TypeAdapterConfig<TSource, TDestination>()
        .NewConfig()
        .Map(dest => dest.Gender,      //Genders.Male or Genders.Female
             src => src.GenderString); //"Male" or "Female"

#####Merge object <a name="Merge"></a>
By default, Mapster will map all properties, even source properties contains null value. You can copy only properties that have value by using `IgnoreNullValues` method.

    TypeAdapterConfig<TSource, TDestination>()
        .NewConfig()
        .IgnoreNullValues(true);

#####Shallow copy <a name="ShallowCopy"></a>
By default, Mapster will recursively map nested objects. You can do shallow copying by setting `ShallowCopyForSameType` to `true`.

    TypeAdapterConfig<TSource, TDestination>()
        .NewConfig()
        .ShallowCopyForSameType(true);

#####Preserve reference (preventing circular reference stackoverflow) <a name="PreserveReference"></a>
When you map circular reference objects, there will be stackoverflow exception. This is because Mapster will try to recursively map all objects in circular. If you would like to map circular reference objects, or preserve references (such as 2 properties point to the same object), you can do it by setting `PreserveReference` to `true`

    TypeAdapterConfig<TSource, TDestination>()
        .NewConfig()
        .PreserveReference(true);

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
In some cases, you may want to have complete control over how an object is mapped. You can register transformation using `MapWith`
method.

    //Example of transforming string to char[].
    TypeAdapterConfig<string, char[]>.NewConfig()
                .MapWith(str => str.ToCharArray());

####Validation <a name="Validate"></a>
To validate your mapping in Unit Test and in order to help with "Fail Fast" situations, the following strict mapping modes have been added.

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
