---
uid: Mapster.Mapping.DataTypes
title: "Mapping - Data Types"
---

## Primitives

Converting between primitive types (ie. int, bool, double, decimal) is supported, including when those types are nullable. For all other types, if you can cast types in c#, you can also cast in Mapster.

```csharp
decimal i = 123.Adapt<decimal>(); //equal to (decimal)123;
```

## Enums

Mapster maps enums to numerics automatically, but it also maps strings to and from enums automatically in a fast manner.  
The default Enum.ToString() in .NET is quite slow. The implementation in Mapster is double the speed. Likewise, a fast conversion from strings to enums is also included.  If the string is null or empty, the enum will initialize to the first enum value.

In Mapster, flagged enums are also supported.

```csharp
var e = "Read, Write, Delete".Adapt<FileShare>();
//FileShare.Read | FileShare.Write | FileShare.Delete
```

For enum to enum with different type, by default, Mapster will map enum by value. You can override to map enum by name by:

```csharp
TypeAdapterConfig.GlobalSettings.Default
    .EnumMappingStrategy(EnumMappingStrategy.ByName);
```

## Strings

When Mapster maps other types to string, Mapster will use `ToString` method. And whenever Mapster maps string to the other types, Mapster will use `Parse` method.

```csharp
var s = 123.Adapt<string>(); //equal to 123.ToString();
var i = "123".Adapt<int>();  //equal to int.Parse("123");
```

## Collections

This includes mapping among lists, arrays, collections, dictionary including various interfaces: `IList<T>`, `ICollection<T>`, `IEnumerable<T>`, `ISet<T>`, `IDictionary<TKey, TValue>` etc...

```csharp
var list = db.Pocos.ToList();
var target = list.Adapt<IEnumerable<Dto>>();
```

## Mappable Objects

Mapster can map two different objects using the following rules:

- Source and destination property names are the same. Ex: `dest.Name = src.Name`
- Source has get method. Ex: `dest.Name = src.GetName()`
- Source property has child object which can flatten to destination. Ex: `dest.ContactName = src.Contact.Name` or `dest.Contact_Name = src.Contact.Name`

Example:

```csharp
class Staff {
    public string Name { get; set; }
    public int GetAge() {
        return (DateTime.Now - this.BirthDate).TotalDays / 365.25;
    }
    public Staff Supervisor { get; set; }
    ...
}

struct StaffDto {
    public string Name { get; set; }
    public int Age { get; set; }
    public string SupervisorName { get; set; }
}

var dto = staff.Adapt<StaffDto>();
//dto.Name = staff.Name, dto.Age = staff.GetAge(), dto.SupervisorName = staff.Supervisor.Name
```

**Mappable Object types are included:**

- POCO classes
- POCO structs
- POCO interfaces
- Dictionary type implement `IDictionary<string, T>`
- Record types (either class, struct, and interface)

Example for object to dictionary:

```csharp
var point = new { X = 2, Y = 3 };
var dict = point.Adapt<Dictionary<string, int>>();
dict["Y"].ShouldBe(3);
```

## Record types

>[!IMPORTANT]
> Mapster treats Record type as an immutable type.
> Only a Nondestructive mutation - creating a new object with modified properties.
>
> ```csharp
> var result = source.adapt(data) 
>//equal var result = data with { X = source.X.Adapt(), ...}
>```

### Features and Limitations:

# [v10.0](#tab/Records-v10)

>[!NOTE]
> By default, all [C# Records](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record) are defined as a record type.
> Limitations by count of constructors and constructor parameters used in Mapster version 7.4.0 do not apply.


#### Using default value in constuctor param

If the source type does not contain members that can be used as constructor parameters, then will be used the default values ​​for the parameter type.

Example: 

```csharp

class SourceData
{
   public string MyString {get; set;}
}

record RecordDestination(int myInt, string myString);

var result = source.Adapt<RecordDestination>()

// equal var result = new RecordDestination (default(int),source.myString)

```

#### MultiConstructor Record types

If there is more than one constructor, by default, mapping will be performed on the constructor with the largest number of parameters.

Example: 

```csharp
record MultiCtorRecord
{
    public MultiCtorRecord(int myInt)
    {
        MyInt = myInt;
    }

    public MultiCtorRecord(int myInt, string myString) // This constructor will be used
        : this(myInt) 
    {
        MyString = myString; 
    }

}
```

# [v7.4.0](#tab/Records-v7-4-0)

>[!NOTE]
>Record type must not have a setter and have only one non-empty constructor, and all parameter names must match with properties.

Otherwise you need to add [`MapToConstructor` configuration](xref:Mapster.Settings.ConstructorMapping#map-to-constructor).

Example for record types:

```csharp
class Person {
    public string Name { get; }
    public int Age { get; }

    public Person(string name, int age) {
        this.Name = name;
        this.Age = age;
    }
}

var src = new { Name = "Mapster", Age = 3 };
var target = src.Adapt<Person>();
```
---

### Support additional mapping features:

| Mapping features | v7.4.0 | v10.0 |
|:-----------------|:------:|:-----:|
|[Custom constructor mapping](xref:Mapster.Settings.ConstructorMapping)| - | ✅ |
|[Ignore](xref:Mapster.Settings.Custom.IgnoringMembers#ignore-extension-method)| - | ✅ |
|[IgnoreNullValues](xref:Mapster.Settings.Custom.IgnoringMembers#ignorenullvalues-extension-method)| - | ✅ |
