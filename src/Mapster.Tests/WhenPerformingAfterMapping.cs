using System;
using NUnit.Framework;
using Should;

namespace Mapster.Tests
{
    [TestFixture]
    public class WhenPerformingAfterMapping
    {
        [Test]
        public void After_Mapping()
        {
            TypeAdapterConfig<SimplePoco, SimpleDto>.NewConfig()
                .AfterMapping((src, dest) => dest.Name += "xxx");

            var poco = new SimplePoco
            {
                Id = Guid.NewGuid(),
                Name = "test",
            };
            var result = TypeAdapter.Adapt<SimpleDto>(poco);

            result.Id.ShouldEqual(poco.Id);
            result.Name.ShouldEqual(poco.Name + "xxx");
        }

        public class SimplePoco
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
        }

        public class SimpleDto
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
        }
    }
}
