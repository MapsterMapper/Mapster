using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Mapster.Tests
{
    [TestClass]
    public class WhenMappingMultipleSources
    {
        [TestMethod]
        public void TestNestedSources()
        {
            TypeAdapterConfig<Dto, Poco>.NewConfig()
                .Map(dest => dest, src => src.Details);

            var dto = new Dto
            {
                Name = "foo",
                Details = new SubDto {Extras = "bar"}
            };
            var poco = dto.Adapt<Poco>();
            poco.Name.ShouldBe(dto.Name);
            poco.Extras.ShouldBe(dto.Details.Extras);
        }

        [TestMethod]
        public void TestTupleSources()
        {
            TypeAdapterConfig<(Dto, SubDto), Poco>.NewConfig()
                .Map(dest => dest, src => src.Item1)
                .Map(dest => dest, src => src.Item2);

            var dto = new Dto {Name = "foo"};
            var sub = new SubDto {Extras = "bar"};

            var poco = (dto, sub).Adapt<Poco>();
            poco.Name.ShouldBe(dto.Name);
            poco.Extras.ShouldBe(sub.Extras);
        }

        public class SubDto
        {
            public string Extras { get; set; }
        }
        public class Dto
        {
            public string Name { get; set; }
            public SubDto Details { get; set; }
        }
        public class Poco
        {
            public string Name { get; set; }
            public string Extras { get; set; }
        }
    }
}