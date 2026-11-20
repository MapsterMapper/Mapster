---
uid: Mapster.Settings.ShallowMerge
title: "Settings - Shallow merge"
---

## Deep copy vs. Shallow copy vs. Direct assignment

Depending on the settings specified, the mapping behavior may differ.
```csharp
var src = new Source { AProperty A {get; set;} , BProperty B {get; set;} } 
var dest = src.Adapt<Destination>();
```

### Deep copy

```csharp
// Deep copy - default behavior
    var dest = new Destination { A = new AProperty{ int X = src.A.X }, B = new BProperty { int X = src.B.X} }
```

### Shallow copy

>[!NOTE]
> 1. Changes mapping behavior only for member(property,field) source and destination type in registered custom mapping configuration.
> 2. Disable when destination member(property,field) using [`UseDestinationValue`](xref:Mapster.Settings.Custom.ReadonlyProperty) setting.
> 3. Disable when registered custom mapping configuration `.NewConfig()` for Type member(property,field).

```csharp
//Shallow copy 
TypeAdapterConfig<Source, Destination>
    .NewConfig()
    .ShallowCopyForSameType(true);

    var dest = new Destination { A = src.A, B = src.B }
```

### Direct assignment <sup>[v10.0.8+]<sup>

>[!NOTE]
> 1. Changes mapping behavior for Type.
> 2. Disable when destination member(property,field) using [`UseDestinationValue`](xref:Mapster.Settings.Custom.ReadonlyProperty) setting.
> 3. Disable when registered custom mapping configuration `.NewConfig()` contains the [`.MapWith()` and/or `MapToTargetWith()`](xref:Mapster.Settings.CustomConversionLogic) setting.

```csharp
// DirectAssignment
TypeAdapterConfig<AProperty, AProperty>
    .NewConfig()
    .DirectAssignmentForSameType(true);

    var dest = new Destination { A = src.A, B = new BProperty { int X = src.B.X} }

    //for mapping AProperty to AProperty
        var a = new AProperty();
        var result =  a.Adapt<AProperty>(); 

        // behavior equal
        var result = a;
```



## Copy vs. Merge

By default, Mapster will map all properties, even source properties containing null values. You can copy only properties that have values (merge) by using `IgnoreNullValues` method.

```csharp
TypeAdapterConfig<TSource, TDestination>
    .NewConfig()
    .IgnoreNullValues(true);
```
