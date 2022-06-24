using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenMappingToConstructorAndPrivateSetters
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

            dto.Id.ShouldBe(poco.Id + "Suffix");
            dto.Name.ShouldBe(poco.Name + "Suffix");
            dto.Age.ShouldBe(-1);
            dto.Prop.ShouldBe(poco.Prop);
            dto.OtherProp.ShouldBeNull();
        }

        [TestMethod]
        public void MapToConstructor_Auto_ReusingSourceProperty()
        {
            TypeAdapterConfig<Poco, Dto>.NewConfig()
                .MapToConstructor(true)
                .Map((d)=> d.OtherProp, (s) => s.Id);

            var poco = new Poco { Id = "A", Name = "Test", Prop = "Prop" };
            var dto = poco.Adapt<Dto>();

            dto.Id.ShouldBe(poco.Id + "Suffix");
            dto.Name.ShouldBe(poco.Name + "Suffix");
            dto.Age.ShouldBe(-1);
            dto.Prop.ShouldBe(poco.Prop);
            dto.OtherProp.ShouldBe(poco.Id);
        }

        public class Poco
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Prop { get; set; }
        }

        public class Dto
        {
            public string Id { get; private set; }
            public string Name { get; private set; }
            public int Age { get; private set; }
            public string Prop { get; private set; }
            public string OtherProp { get; private set; }

            public Dto(string id, string name, int age = -1)
            {
                this.Id = id + "Suffix";
                this.Name = name + "Suffix";
                this.Age = age;
            }
        }
    }
}
