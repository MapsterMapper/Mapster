using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Shouldly;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenIgnoreMapping
    {
        [TestInitialize]
        public void Setup()
        {
            TypeAdapterConfig.GlobalSettings.Clear();
        }

        [TestMethod]
        public void TestIgnore()
        {
            TypeAdapterConfig<Poco, Dto>.NewConfig()
                .TwoWays()
                .Ignore(it => it.Name);

            var poco = new Poco { Id = Guid.NewGuid(), Name = "test" };
            var dto = poco.Adapt<Dto>();

            dto.Id.ShouldBe(poco.Id);
            dto.Name.ShouldBeNull();

            dto.Name = "bar";
            var poco2 = dto.Adapt<Poco>();

            poco2.Id.ShouldBe(dto.Id);
            poco2.Name.ShouldBeNull();
        }

        [TestMethod]
        public void TestIgnoreMember()
        {
            TypeAdapterConfig<Poco, Dto>.NewConfig()
                .TwoWays()
                .IgnoreMember((member, side) =>
                    member.GetCustomAttribute<JsonIgnoreAttribute>() != null && side == MemberSide.Destination);

            var poco = new Poco { Id = Guid.NewGuid(), Name = "test" };
            var dto = poco.Adapt<Dto>();

            dto.Id.ShouldBe(poco.Id);
            dto.Name.ShouldBeNull();

            dto.Name = "bar";
            var poco2 = dto.Adapt<Poco>();

            poco2.Id.ShouldBe(dto.Id);
            poco2.Name.ShouldBeNull();
        }

        public class Poco
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
        }
        public class Dto
        {
            public Guid Id { get; set; }

            [JsonIgnore]
            public string Name { get; set; }
        }
    }
}
