using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenMappingToConstructor
    {
        [TestInitialize]
        public void Setup()
        {
            TypeAdapterConfig.GlobalSettings.Clear();
        }

        [TestMethod]
        public void MapToConstructor_Auto()
        {
            TypeAdapterConfig<Poco, Dto>.NewConfig()
                .MapToConstructor(true);

            var poco = new Poco { Id = "A", Name = "Test", Prop = "Prop" };
            var dto = poco.Adapt<Dto>();

            dto.Id.ShouldBe(poco.Id);
            dto.Name.ShouldBe(poco.Name);
            dto.Age.ShouldBe(-1);
            dto.Prop.ShouldBe(poco.Prop);
        }

        [TestMethod]
        public void MapToConstructor_UsingConstructorInfo()
        {
            var ctor = typeof(Dto).GetConstructor(new[] { typeof(string) });
            TypeAdapterConfig<Poco, Dto>.NewConfig()
                .MapToConstructor(ctor);

            var poco = new Poco { Id = "A", Name = "Test", Prop = "Prop" };
            var dto = poco.Adapt<Dto>();

            dto.Id.ShouldBe(poco.Id);
            dto.Name.ShouldBeNull();
            dto.Age.ShouldBe(0);
            dto.Prop.ShouldBe(poco.Prop);
        }

        public class Poco
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Prop { get; set; }
        }

        public class Dto
        {
            public string Id { get; }
            public string Name { get; }
            public int Age { get; }
            public string Prop { get; set; }

            public Dto(string id)
            {
                this.Id = id;
            }
            public Dto(string id, string name, int age = -1)
            {
                this.Id = id;
                this.Name = name;
                this.Age = age;
            }

            public Dto(string id, string name, string unmapped, int age = -2)
            {
                this.Id = id;
                this.Name = name;
                this.Age = age;
            }
        }
    }
}
