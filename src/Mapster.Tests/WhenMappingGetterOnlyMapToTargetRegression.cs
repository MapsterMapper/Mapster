using MapsterMapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Mapster.Tests;

/// <summary>
/// https://github.com/MapsterMapper/Mapster/issues/900
/// </summary>
[TestClass]
public class WhenMappingGetterOnlyMapToTargetRegression
{
    [TestMethod]
    public void MapToTarget_WithCustomMap_ShouldReplaceExistingDestinationMemberValue()
    {
        var config = new TypeAdapterConfig();
        config.NewConfig<Dto900, Entity900>()
            .Map(x => x.Data, x => x.Data == null ? null : new Data900(x.Data.Value));

        var mapper = new Mapper(config);

        var dto = new Dto900 { Data = new Data900("new") };
        var entity = mapper.Map(dto, new Entity900 { Data = new Data900("old") });

        entity.Data.ShouldNotBeNull();
        entity.Data!.Value.ShouldBe("new");
    }

    [TestMethod]
    public void MapToTarget_WithCustomMap_ShouldStillWorkWhenDestinationMemberIsNull()
    {
        var config = new TypeAdapterConfig();
        config.NewConfig<Dto900, Entity900>()
            .Map(x => x.Data, x => x.Data == null ? null : new Data900(x.Data.Value));

        var mapper = new Mapper(config);

        var dto = new Dto900 { Data = new Data900("new") };
        var entity = mapper.Map(dto, new Entity900());

        entity.Data.ShouldNotBeNull();
        entity.Data!.Value.ShouldBe("new");
    }

    [TestMethod]
    public void Map_WithCustomMap_ShouldStillWorkForNewDestination()
    {
        var config = new TypeAdapterConfig();
        config.NewConfig<Dto900, Entity900>()
            .Map(x => x.Data, x => x.Data == null ? null : new Data900(x.Data.Value));

        var mapper = new Mapper(config);

        var dto = new Dto900 { Data = new Data900("new") };
        var entity = mapper.Map<Entity900>(dto);

        entity.Data.ShouldNotBeNull();
        entity.Data!.Value.ShouldBe("new");
    }

    private class Dto900
    {
        public Data900? Data { get; set; }
    }

    private class Entity900
    {
        public Data900? Data { get; set; }
    }

    private class Data900(string value)
    {
        public string Value => value;
    }
}
